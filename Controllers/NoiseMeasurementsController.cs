using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom3ConCa.Data;
using Nhom3ConCa.Models;

namespace Nhom3ConCa.Controllers;

[ApiController]
[Route("api/noise-measurements")]
[Authorize]
public class NoiseMeasurementsController(ApplicationDbContext db) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateNoiseMeasurementRequest request)
    {
        var session = await db.AttendanceSessions
            .Include(item => item.ClassSession)
            .SingleOrDefaultAsync(item => item.Id == request.AttendanceSessionId);
        if (session is null)
        {
            return NotFound(new { message = "Không tìm thấy phiên học." });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAllowed = User.IsInRole("Admin") ||
                        await db.Teachers.AnyAsync(item => item.Id == session.ClassSession.TeacherId && item.UserId == userId) ||
                        await db.StudentCourses.AnyAsync(item =>
                            item.Student.UserId == userId &&
                            item.CourseId == session.ClassSession.CourseId);
        if (!isAllowed)
        {
            return Forbid();
        }

        var measurement = new NoiseMeasurement
        {
            AttendanceSessionId = request.AttendanceSessionId,
            UserId = userId!,
            Decibels = request.Decibels,
            MeasuredAt = DateTimeOffset.UtcNow
        };
        db.NoiseMeasurements.Add(measurement);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetSessionMeasurements), new { id = measurement.AttendanceSessionId }, new
        {
            id = measurement.Id,
            attendanceSessionId = measurement.AttendanceSessionId,
            decibels = measurement.Decibels,
            measuredAt = measurement.MeasuredAt
        });
    }

    [HttpGet("session/{id:int}")]
    public async Task<IActionResult> GetSessionMeasurements(int id)
    {
        var session = await db.AttendanceSessions
            .Include(item => item.ClassSession)
            .SingleOrDefaultAsync(item => item.Id == id);
        if (session is null)
        {
            return NotFound(new { message = "Không tìm thấy phiên học." });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAllowed = User.IsInRole("Admin") ||
                        await db.Teachers.AnyAsync(item => item.Id == session.ClassSession.TeacherId && item.UserId == userId) ||
                        await db.StudentCourses.AnyAsync(item =>
                            item.Student.UserId == userId &&
                            item.CourseId == session.ClassSession.CourseId);
        if (!isAllowed)
        {
            return Forbid();
        }

        var measurements = await db.NoiseMeasurements.AsNoTracking()
            .Where(item => item.AttendanceSessionId == id)
            .OrderBy(item => item.MeasuredAt)
            .Select(item => new
            {
                id = item.Id,
                userId = item.UserId,
                fullName = item.User.FullName,
                decibels = item.Decibels,
                measuredAt = item.MeasuredAt
            })
            .ToListAsync();
        return Ok(new
        {
            attendanceSessionId = id,
            averageDecibels = measurements.Count == 0 ? (decimal?)null : measurements.Average(item => item.decibels),
            measurements
        });
    }
}

public sealed class CreateNoiseMeasurementRequest
{
    [Range(1, int.MaxValue)]
    public int AttendanceSessionId { get; init; }

    [Range(typeof(decimal), "0", "200")]
    public decimal Decibels { get; init; }
}
