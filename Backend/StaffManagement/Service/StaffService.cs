using StaffManagement.Common;
using StaffManagement.Interface;
using StaffManagement.Model.Entities;
using StaffManagement.Model.Request;
using System.Net;
using System.Text;

namespace StaffManagement.Service;

public class StaffService : IStaffService
{
    private const string StaffIdPrefix = "STF";
    private const int StaffIdNumberLength = 5;
    private const int MaxReportRows = 1000;

    private readonly IStaffRepository _repository;

    public StaffService(IStaffRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<Staff>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<Staff?> GetByIdAsync(string staffId)
    {
        return await _repository.GetByIdAsync(staffId);
    }

    public async Task<ServiceResult<List<Staff>>> SearchAsync(StaffSearchRequest request)
    {
        var validationResult = ValidateSearchRequest(request);

        if (!validationResult.Success)
        {
            return ServiceResult<List<Staff>>.Fail(validationResult.Message!);
        }

        var staffs = await _repository.SearchAsync(
            request.StaffId,
            request.Gender,
            request.BirthdayFrom,
            request.BirthdayTo);

        return ServiceResult<List<Staff>>.Ok(staffs);
    }

    public async Task<ServiceResult<byte[]>> ExportExcelAsync(StaffSearchRequest request)
    {
        var searchResult = await SearchAsync(request);

        if (!searchResult.Success)
        {
            return ServiceResult<byte[]>.Fail(searchResult.Message!);
        }

        var html = BuildExcelHtml(searchResult.Data!.Take(MaxReportRows).ToList());
        var bytes = Encoding.UTF8.GetBytes(html);

        return ServiceResult<byte[]>.Ok(bytes);
    }

    public async Task<ServiceResult<byte[]>> ExportPdfAsync(StaffSearchRequest request)
    {
        var searchResult = await SearchAsync(request);

        if (!searchResult.Success)
        {
            return ServiceResult<byte[]>.Fail(searchResult.Message!);
        }

        var bytes = BuildPdf(searchResult.Data!.Take(MaxReportRows).ToList());

        return ServiceResult<byte[]>.Ok(bytes);
    }

    public async Task<ServiceResult<Staff>> CreateAsync(CreateStaffRequest request)
    {
        var validationResult = ValidateStaffRequest(request.FullName, request.Gender);

        if (!validationResult.Success)
        {
            return ServiceResult<Staff>.Fail(validationResult.Message!);
        }

        var staff = await CreateStaffFromRequestAsync(request);

        await _repository.AddAsync(staff);

        return ServiceResult<Staff>.Ok(staff);
    }

    public async Task<ServiceResult> UpdateAsync(string staffId, UpdateStaffRequest request)
    {
        var validationResult = ValidateStaffRequest(request.FullName, request.Gender);

        if (!validationResult.Success)
        {
            return validationResult;
        }

        var staff = await _repository.GetByIdAsync(staffId);

        if (staff == null)
        {
            return ServiceResult.Fail("Staff not found.");
        }

        staff.FullName = request.FullName.Trim();
        staff.Birthday = request.Birthday;
        staff.Gender = request.Gender;

        await _repository.UpdateAsync(staff);

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DeleteAsync(string staffId)
    {
        var staff = await _repository.GetByIdAsync(staffId);

        if (staff == null)
        {
            return ServiceResult.Fail("Staff not found.");
        }

        await _repository.DeleteAsync(staff);

        return ServiceResult.Ok();
    }

    private async Task<Staff> CreateStaffFromRequestAsync(CreateStaffRequest request)
    {
        return new Staff
        {
            StaffId = await GenerateStaffIdAsync(),
            FullName = request.FullName.Trim(),
            Birthday = request.Birthday,
            Gender = request.Gender
        };
    }

    private async Task<string> GenerateStaffIdAsync()
    {
        var staffs = await _repository.GetAllAsync();
        var lastStaffNumber = staffs
            .Select(staff => GetStaffNumber(staff.StaffId))
            .DefaultIfEmpty(0)
            .Max();

        return FormatStaffId(lastStaffNumber + 1);
    }

    private static int GetStaffNumber(string staffId)
    {
        if (!staffId.StartsWith(StaffIdPrefix))
        {
            return 0;
        }

        var numberText = staffId[StaffIdPrefix.Length..];

        return int.TryParse(numberText, out var number) ? number : 0;
    }

    private static string FormatStaffId(int number)
    {
        return $"{StaffIdPrefix}{number.ToString().PadLeft(StaffIdNumberLength, '0')}";
    }

    private static string BuildExcelHtml(List<Staff> staffs)
    {
        var html = new StringBuilder();

        html.AppendLine("<html>");
        html.AppendLine("<body>");
        html.AppendLine("<table border=\"1\">");
        html.AppendLine("<tr>");
        html.AppendLine("<th>Staff ID</th>");
        html.AppendLine("<th>Full Name</th>");
        html.AppendLine("<th>Birthday</th>");
        html.AppendLine("<th>Gender</th>");
        html.AppendLine("</tr>");

        foreach (var staff in staffs)
        {
            html.AppendLine("<tr>");
            html.AppendLine($"<td>{WebUtility.HtmlEncode(staff.StaffId)}</td>");
            html.AppendLine($"<td>{WebUtility.HtmlEncode(SanitizeExcelText(staff.FullName))}</td>");
            html.AppendLine($"<td>{staff.Birthday:yyyy-MM-dd}</td>");
            html.AppendLine($"<td>{GetGenderName(staff.Gender)}</td>");
            html.AppendLine("</tr>");
        }

        html.AppendLine("</table>");
        html.AppendLine("</body>");
        html.AppendLine("</html>");

        return html.ToString();
    }

    private static byte[] BuildPdf(List<Staff> staffs)
    {
        var lines = new List<string>
        {
            "Staff Report",
            "Staff ID | Full Name | Birthday | Gender"
        };

        lines.AddRange(staffs.Select(staff =>
            $"{staff.StaffId} | {staff.FullName} | {staff.Birthday:yyyy-MM-dd} | {GetGenderName(staff.Gender)}"));

        var content = BuildPdfContent(lines);

        return BuildPdfDocument(content);
    }

    private static string BuildPdfContent(List<string> lines)
    {
        var content = new StringBuilder();

        content.AppendLine("BT");
        content.AppendLine("/F1 12 Tf");
        content.AppendLine("50 780 Td");

        foreach (var line in lines)
        {
            content.AppendLine($"({EscapePdfText(line)}) Tj");
            content.AppendLine("0 -18 Td");
        }

        content.AppendLine("ET");

        return content.ToString();
    }

    private static byte[] BuildPdfDocument(string content)
    {
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}endstream"
        };

        var pdf = new StringBuilder();
        var offsets = new List<int> { 0 };

        pdf.AppendLine("%PDF-1.4");

        for (var index = 0; index < objects.Count; index++)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(pdf.ToString()));
            pdf.AppendLine($"{index + 1} 0 obj");
            pdf.AppendLine(objects[index]);
            pdf.AppendLine("endobj");
        }

        var crossReferenceOffset = Encoding.ASCII.GetByteCount(pdf.ToString());

        pdf.AppendLine("xref");
        pdf.AppendLine($"0 {objects.Count + 1}");
        pdf.AppendLine("0000000000 65535 f ");

        foreach (var offset in offsets.Skip(1))
        {
            pdf.AppendLine($"{offset:0000000000} 00000 n ");
        }

        pdf.AppendLine("trailer");
        pdf.AppendLine($"<< /Size {objects.Count + 1} /Root 1 0 R >>");
        pdf.AppendLine("startxref");
        pdf.AppendLine(crossReferenceOffset.ToString());
        pdf.AppendLine("%%EOF");

        return Encoding.ASCII.GetBytes(pdf.ToString());
    }

    private static string EscapePdfText(string text)
    {
        return text
            .Replace("\\", "\\\\")
            .Replace("(", "\\(")
            .Replace(")", "\\)");
    }

    private static string GetGenderName(int gender)
    {
        return gender == 1 ? "Male" : "Female";
    }

    private static ServiceResult ValidateStaffRequest(string fullName, int gender)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return ServiceResult.Fail("Full name is required.");
        }

        if (fullName.Trim().Length > 100)
        {
            return ServiceResult.Fail("Full name must be 100 characters or fewer.");
        }

        if (gender is not 1 and not 2)
        {
            return ServiceResult.Fail("Gender must be 1 for Male or 2 for Female.");
        }

        return ServiceResult.Ok();
    }

    private static ServiceResult ValidateSearchRequest(StaffSearchRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.StaffId) && request.StaffId.Length > 8)
        {
            return ServiceResult.Fail("Staff ID search value must be 8 characters or fewer.");
        }

        if (request.Gender.HasValue && request.Gender.Value is not 1 and not 2)
        {
            return ServiceResult.Fail("Gender must be 1 for Male or 2 for Female.");
        }

        if (request.BirthdayFrom.HasValue &&
            request.BirthdayTo.HasValue &&
            request.BirthdayFrom.Value > request.BirthdayTo.Value)
        {
            return ServiceResult.Fail("Birthday from date cannot be after birthday to date.");
        }

        return ServiceResult.Ok();
    }

    private static string SanitizeExcelText(string text)
    {
        var trimmedText = text.Trim();

        if (trimmedText.StartsWith('=') ||
            trimmedText.StartsWith('+') ||
            trimmedText.StartsWith('-') ||
            trimmedText.StartsWith('@'))
        {
            return $"'{trimmedText}";
        }

        return trimmedText;
    }
}
