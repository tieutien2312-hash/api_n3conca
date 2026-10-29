using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Nhom3ConCa.Data;
using Nhom3ConCa.DTOs;
using Nhom3ConCa.Models;

namespace Nhom3ConCa.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    UserManager<ApplicationUser> userManager,
    IConfiguration configuration,
    ApplicationDbContext db) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto model)
    {
        var username = string.IsNullOrWhiteSpace(model.Username) ? model.Email : model.Username.Trim();
        var user = new ApplicationUser
        {
            UserName = username,
            Email = model.Email.Trim(),
            FullName = model.FullName.Trim(),
            EmailConfirmed = true
        };
        var createResult = await userManager.CreateAsync(user, model.Password);
        if (!createResult.Succeeded)
        {
            return BadRequest(createResult.Errors);
        }

        var roleResult = await userManager.AddToRoleAsync(user, "Student");
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            return Problem(
                title: "Không thể gán vai trò cho tài khoản.",
                detail: string.Join("; ", roleResult.Errors.Select(error => error.Description)),
                statusCode: StatusCodes.Status500InternalServerError);
        }

        db.Students.Add(new Student
        {
            UserId = user.Id,
            StudentCode = $"SV-{Guid.NewGuid():N}"[..11]
        });
        await db.SaveChangesAsync();

        return Ok(new
        {
            message = "Đăng ký thành công.",
            userId = user.Id,
            email = user.Email,
            fullName = user.FullName,
            roles = new[] { "Student" }
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto model)
    {
        var user = await userManager.FindByEmailAsync(model.EmailOrUsername)
            ?? await userManager.FindByNameAsync(model.EmailOrUsername);
        if (user is null || !await userManager.CheckPasswordAsync(user, model.Password))
        {
            return Unauthorized(new { message = "Thông tin đăng nhập không chính xác." });
        }

        return await IssueTokensAsync(user);
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken(RefreshTokenDto model)
    {
        var tokenHash = HashRefreshToken(model.RefreshToken);
        var user = await userManager.Users.SingleOrDefaultAsync(user => user.RefreshToken == tokenHash);
        if (user is null || user.RefreshTokenExpiresAt <= DateTimeOffset.UtcNow)
        {
            return Unauthorized(new { message = "Refresh token không hợp lệ hoặc đã hết hạn." });
        }

        return await IssueTokensAsync(user);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound(new { message = "Không tìm thấy người dùng." });
        }

        return Ok(new
        {
            id = user.Id,
            email = user.Email,
            fullName = user.FullName,
            roles = await userManager.GetRolesAsync(user)
        });
    }

    private async Task<IActionResult> IssueTokensAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        var accessToken = CreateAccessToken(user, roles);
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        user.RefreshToken = HashRefreshToken(refreshToken);
        user.RefreshTokenExpiresAt = DateTimeOffset.UtcNow.AddDays(GetJwtInteger("RefreshTokenExpirationDays", 30));
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return Problem(
                title: "Không thể lưu refresh token.",
                detail: string.Join("; ", updateResult.Errors.Select(error => error.Description)),
                statusCode: StatusCodes.Status500InternalServerError);
        }

        return Ok(new
        {
            accessToken,
            refreshToken,
            userId = user.Id,
            email = user.Email,
            fullName = user.FullName,
            roles
        });
    }

    private string CreateAccessToken(ApplicationUser user, IEnumerable<string> roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? user.Id),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var settings = configuration.GetSection("JwtSettings");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings["Secret"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(GetJwtInteger("AccessTokenExpirationMinutes", 15));
        var token = new JwtSecurityToken(
            settings["Issuer"],
            settings["Audience"],
            claims,
            expires: expires,
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private int GetJwtInteger(string key, int fallback) =>
        int.TryParse(configuration[$"JwtSettings:{key}"], out var value) && value > 0 ? value : fallback;

    private static string HashRefreshToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
