using System.ComponentModel.DataAnnotations;

namespace Nhom3ConCa.DTOs;

public sealed class UpdateStudentProfileDto
{
    [Required, StringLength(150)]
    public string FullName { get; init; } = string.Empty;

    [StringLength(150)]
    public string? Department { get; init; }
}
