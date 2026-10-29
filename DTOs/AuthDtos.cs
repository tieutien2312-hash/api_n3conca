using System.ComponentModel.DataAnnotations;

namespace Nhom3ConCa.DTOs;

public sealed class RegisterDto
{
    [Required, StringLength(150)]
    public string FullName { get; init; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; init; } = string.Empty;

    [StringLength(100)]
    public string? Username { get; init; }

    [Required, MinLength(8), StringLength(100)]
    public string Password { get; init; } = string.Empty;
}

public sealed class LoginDto
{
    [Required]
    public string EmailOrUsername { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}

public sealed class RefreshTokenDto
{
    [Required]
    public string RefreshToken { get; init; } = string.Empty;
}
