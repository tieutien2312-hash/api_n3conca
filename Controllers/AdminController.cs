using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom3ConCa.Data;
using Nhom3ConCa.Models;

namespace Nhom3ConCa.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpGet("dashboard-stats")]
    public async Task<IActionResult> GetDashboardStats()
    {
        return Ok(new
        {
            students = await db.Students.CountAsync(),
            teachers = await db.Teachers.CountAsync(),
            courses = await db.Courses.CountAsync(),
            classSessions = await db.ClassSessions.CountAsync()
        });
    }

    [HttpPost("teachers")]
    public async Task<IActionResult> CreateTeacher(CreateTeacherRequest request)
    {
        var user = await userManager.FindByIdAsync(request.UserId);
        if (user is null)
        {
            return NotFound(new { message = "Không tìm thấy người dùng." });
        }
        if (await db.Teachers.AnyAsync(item => item.UserId == user.Id))
        {
            return Conflict(new { message = "Người dùng đã có hồ sơ giảng viên." });
        }

        var roleResult = await userManager.AddToRoleAsync(user, "Teacher");
        if (!roleResult.Succeeded)
        {
            return Problem(
                title: "Không thể gán vai trò giảng viên.",
                detail: string.Join("; ", roleResult.Errors.Select(error => error.Description)),
                statusCode: StatusCodes.Status500InternalServerError);
        }

        var teacher = new Teacher { UserId = user.Id, Department = request.Department?.Trim() };
        db.Teachers.Add(teacher);
        await db.SaveChangesAsync();
        return Ok(new { id = teacher.Id, userId = teacher.UserId, fullName = user.FullName, department = teacher.Department });
    }
}

public sealed class CreateTeacherRequest
{
    [Required]
    public string UserId { get; init; } = string.Empty;

    [StringLength(150)]
    public string? Department { get; init; }
}
