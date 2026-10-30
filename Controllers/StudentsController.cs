using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom3ConCa.Data;
using Nhom3ConCa.DTOs;

namespace Nhom3ConCa.Controllers;

[ApiController]
[Route("api/students")]
[Authorize]
public class StudentsController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> GetStudents()
    {
        var students = await db.Students
            .AsNoTracking()
            .OrderBy(item => item.User.FullName)
            .Select(item => new
            {
                id = item.Id,
                studentCode = item.StudentCode,
                department = item.Department,
                userId = item.UserId,
                fullName = item.User.FullName,
                email = item.User.Email
            })
            .ToListAsync();
        return Ok(students);
    }

    

    [HttpGet("me")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetMe()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var student = await db.Students.AsNoTracking()
            .Where(item => item.UserId == userId)
            .Select(item => new
            {
                id = item.Id,
                studentCode = item.StudentCode,
                department = item.Department,
                userId = item.UserId,
                fullName = item.User.FullName,
                email = item.User.Email
            })
            .SingleOrDefaultAsync();
        return student is null ? NotFound(new { message = "Không tìm thấy hồ sơ sinh viên." }) : Ok(student);
    }

    [HttpPut("me")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateStudentProfileDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var student = await db.Students
            .Include(item => item.User)
            .SingleOrDefaultAsync(item => item.UserId == userId);
        if (student is null)
        {
            return NotFound(new { message = "Không tìm thấy hồ sơ sinh viên." });
        }

        student.User.FullName = dto.FullName.Trim();
        student.Department = dto.Department?.Trim();

        await db.SaveChangesAsync();
        return Ok(new
        {
            message = "Cập nhật hồ sơ thành công.",
            fullName = student.User.FullName,
            department = student.Department
        });
    }

    [HttpGet("me/courses")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetMyCourses()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var courses = await db.StudentCourses.AsNoTracking()
            .Where(item => item.Student.UserId == userId)
            .OrderBy(item => item.Course.Name)
            .Select(item => new
            {
                id = item.Course.Id,
                code = item.Course.Code,
                name = item.Course.Name,
                description = item.Course.Description,
                credits = item.Course.Credits,
                teacher = item.Course.Teacher == null ? null : item.Course.Teacher.User.FullName,
                enrolledAt = item.EnrolledAt
            })
            .ToListAsync();
        return Ok(courses);
    }

    [HttpGet("me/schedule")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetMySchedule()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var schedule = await db.ClassSessions.AsNoTracking()
            .Where(item => item.Course.StudentCourses.Any(enrollment => enrollment.Student.UserId == userId))
            .OrderBy(item => item.StartsAt)
            .Select(item => new
            {
                id = item.Id,
                courseId = item.CourseId,
                courseCode = item.Course.Code,
                courseName = item.Course.Name,
                teacher = item.Teacher.User.FullName,
                startsAt = item.StartsAt,
                endsAt = item.EndsAt,
                room = item.Room
            })
            .ToListAsync();
        return Ok(schedule);
    }

    [HttpGet("me/attendance")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetMyAttendance()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var attendance = await db.AttendanceRecords.AsNoTracking()
            .Where(item => item.Student.UserId == userId)
            .OrderByDescending(item => item.MarkedAt)
            .Select(item => new
            {
                id = item.Id,
                status = item.Status,
                markedAt = item.MarkedAt,
                attendanceSessionId = item.AttendanceSessionId,
                classSessionId = item.AttendanceSession.ClassSessionId,
                courseCode = item.AttendanceSession.ClassSession.Course.Code,
                courseName = item.AttendanceSession.ClassSession.Course.Name,
                room = item.AttendanceSession.ClassSession.Room
            })
            .ToListAsync();
        return Ok(attendance);
    }
}
