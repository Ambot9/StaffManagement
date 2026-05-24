using System.ComponentModel.DataAnnotations;

namespace StaffManagement.Model.Entities;

public class Staff
{
    [Key]
    [StringLength(8, MinimumLength = 8)]
    public string StaffId { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    public DateOnly Birthday { get; set; }

    [Range(1, 2)]
    public int Gender { get; set; }
}
