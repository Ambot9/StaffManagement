using Microsoft.AspNetCore.Mvc;
using StaffManagement.Interface;
using StaffManagement.Model.Entities;
using StaffManagement.Model.Request;

namespace StaffManagement.Controller;

[ApiController]
[Route("api/[controller]")]
public class StaffsController : ControllerBase
{
    private readonly IStaffService _staffService;

    public StaffsController(IStaffService staffService)
    {
        _staffService = staffService;
    }

    [HttpGet]
    public async Task<ActionResult<List<Staff>>> GetAll()
    {
        var staffs = await _staffService.GetAllAsync();

        return Ok(staffs);
    }

    [HttpGet("search")]
    public async Task<ActionResult<List<Staff>>> Search([FromQuery] StaffSearchRequest request)
    {
        var result = await _staffService.SearchAsync(request);

        if (!result.Success)
        {
            return BadRequest(result.Message);
        }

        return Ok(result.Data);
    }

    [HttpGet("export/excel")]
    public async Task<IActionResult> ExportExcel([FromQuery] StaffSearchRequest request)
    {
        var result = await _staffService.ExportExcelAsync(request);

        if (!result.Success)
        {
            return BadRequest(result.Message);
        }

        return File(result.Data!, "application/vnd.ms-excel", "staff-report.xls");
    }

    [HttpGet("export/pdf")]
    public async Task<IActionResult> ExportPdf([FromQuery] StaffSearchRequest request)
    {
        var result = await _staffService.ExportPdfAsync(request);

        if (!result.Success)
        {
            return BadRequest(result.Message);
        }

        return File(result.Data!, "application/pdf", "staff-report.pdf");
    }

    [HttpGet("{staffId}")]
    public async Task<ActionResult<Staff>> GetById(string staffId)
    {
        var staff = await _staffService.GetByIdAsync(staffId);

        if (staff == null)
        {
            return NotFound("Staff not found.");
        }

        return Ok(staff);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateStaffRequest request)
    {
        var result = await _staffService.CreateAsync(request);

        if (!result.Success)
        {
            return BadRequest(result.Message);
        }

        return CreatedAtAction(
            nameof(GetById),
            new { staffId = result.Data!.StaffId },
            result.Data
        );
    }

    [HttpPut("{staffId}")]
    public async Task<IActionResult> Update(string staffId, UpdateStaffRequest request)
    {
        var result = await _staffService.UpdateAsync(staffId, request);

        if (!result.Success)
        {
            return BadRequest(result.Message);
        }

        return NoContent();
    }

    [HttpDelete("{staffId}")]
    public async Task<IActionResult> Delete(string staffId)
    {
        var result = await _staffService.DeleteAsync(staffId);

        if (!result.Success)
        {
            return NotFound(result.Message);
        }

        return NoContent();
    }
}
