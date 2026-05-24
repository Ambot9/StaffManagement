using System.ComponentModel.DataAnnotations;

namespace StaffManagement.Model.Request;

public class UpdateStaffRequest
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Full name must be 100 characters or fewer.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Birthday is required.")]
    public DateOnly Birthday { get; set; }

    [Range(1, 2, ErrorMessage = "Gender must be Male or Female.")]
    public int Gender { get; set; }
}
