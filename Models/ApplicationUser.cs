using Microsoft.AspNetCore.Identity;

namespace Nhom3ConCa.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public DateTimeOffset? RefreshTokenExpiresAt { get; set; }
    public Student? Student { get; set; }
    public Teacher? Teacher { get; set; }
}
