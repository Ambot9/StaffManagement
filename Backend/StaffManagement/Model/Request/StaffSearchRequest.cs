using System.ComponentModel.DataAnnotations;

namespace StaffManagement.Model.Request;

public class StaffSearchRequest
{
    [StringLength(8, MinimumLength = 1, ErrorMessage = "Staff ID must be 8 characters or fewer.")]
    public string? StaffId { get; set; }

    [Range(1, 2, ErrorMessage = "Gender must be Male or Female.")]
    public int? Gender { get; set; }

    public DateOnly? BirthdayFrom { get; set; }
    public DateOnly? BirthdayTo { get; set; }
}
