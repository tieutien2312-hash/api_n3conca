using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Nhom3ConCa.Models;

namespace Nhom3ConCa.Data;

public static class DbInitializer
{
    private const string SampleQrCode = "QR_SAMPLE_TOKEN_123456";

    public static async Task InitializeAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        bool seedSampleData)
    {
        if (seedSampleData)
        {
            await context.Database.EnsureCreatedAsync();
        }

        foreach (var role in new[] { "Admin", "Teacher", "Student" })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(role));
                EnsureSucceeded(result, $"creating role '{role}'");
            }
        }

        if (!seedSampleData)
        {
            return;
        }

        await EnsureUserAsync(
            userManager,
            "admin@student.edu.vn",
            "Admin@123",
            "Quản Trị Viên",
            "Admin");

        var teacherUser = await EnsureUserAsync(
            userManager,
            "teacher@student.edu.vn",
            "Teacher@123",
            "Giảng Viên Mẫu",
            "Teacher");
        var teacher = await context.Teachers.SingleOrDefaultAsync(item => item.UserId == teacherUser.Id);
        if (teacher is null)
        {
            teacher = new Teacher
            {
                UserId = teacherUser.Id,
                Department = "Công nghệ Thông tin"
            };
            context.Teachers.Add(teacher);
            await context.SaveChangesAsync();
        }
        else if (string.IsNullOrWhiteSpace(teacher.Department))
        {
            teacher.Department = "Công nghệ Thông tin";
            await context.SaveChangesAsync();
        }

        var studentUser = await EnsureUserAsync(
            userManager,
            "student@student.edu.vn",
            "Student@123",
            "Sinh Viên Mẫu",
            "Student");
        var student = await context.Students.SingleOrDefaultAsync(item => item.UserId == studentUser.Id);
        if (student is null)
        {
            student = new Student
            {
                UserId = studentUser.Id,
                StudentCode = "SV2024001",
                Department = "Công nghệ Thông tin"
            };
            context.Students.Add(student);
            await context.SaveChangesAsync();
        }
        else
        {
            var changed = false;
            if (student.StudentCode != "SV2024001")
            {
                student.StudentCode = "SV2024001";
                changed = true;
            }
            if (string.IsNullOrWhiteSpace(student.Department))
            {
                student.Department = "Công nghệ Thông tin";
                changed = true;
            }
            if (changed)
            {
                await context.SaveChangesAsync();
            }
        }

        var course = await context.Courses.SingleOrDefaultAsync(item => item.Code == "MMT101");
        if (course is null)
        {
            course = new Course
            {
                Code = "MMT101",
                Name = "Lập trình Di động Flutter",
                Description = "Học phần lập trình ứng dụng di động đa nền tảng với Flutter và ASP.NET Core.",
                Credits = 3,
                TeacherId = teacher.Id
            };
            context.Courses.Add(course);
            await context.SaveChangesAsync();
        }
        else if (course.TeacherId is null)
        {
            course.TeacherId = teacher.Id;
            await context.SaveChangesAsync();
        }

        if (!await context.StudentCourses.AnyAsync(item =>
                item.StudentId == student.Id && item.CourseId == course.Id))
        {
            context.StudentCourses.Add(new StudentCourse
            {
                StudentId = student.Id,
                CourseId = course.Id
            });
            await context.SaveChangesAsync();
        }

        var classSession = await context.ClassSessions.SingleOrDefaultAsync(item =>
            item.CourseId == course.Id && item.TeacherId == teacher.Id && item.Room == "Phòng Lab 3");
        if (classSession is null)
        {
            var startsAt = DateTimeOffset.Now.Date.AddDays(1).AddHours(8);
            classSession = new ClassSession
            {
                CourseId = course.Id,
                TeacherId = teacher.Id,
                Room = "Phòng Lab 3",
                StartsAt = startsAt,
                EndsAt = startsAt.AddHours(3.5)
            };
            context.ClassSessions.Add(classSession);
            await context.SaveChangesAsync();
        }

        var attendanceSession = await context.AttendanceSessions
            .SingleOrDefaultAsync(item => item.ClassSessionId == classSession.Id);
        if (attendanceSession is null)
        {
            context.AttendanceSessions.Add(new AttendanceSession
            {
                ClassSessionId = classSession.Id,
                QrCode = SampleQrCode,
                QrExpiresAt = DateTimeOffset.UtcNow.AddMinutes(60),
                IsOpen = true
            });
            await context.SaveChangesAsync();
        }
        else if (attendanceSession.IsOpen &&
                 (attendanceSession.QrCode != SampleQrCode ||
                  attendanceSession.QrExpiresAt <= DateTimeOffset.UtcNow))
        {
            attendanceSession.QrCode = SampleQrCode;
            attendanceSession.QrExpiresAt = DateTimeOffset.UtcNow.AddMinutes(60);
            await context.SaveChangesAsync();
        }
    }

    private static async Task<ApplicationUser> EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string password,
        string fullName,
        string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                EmailConfirmed = true
            };
            var createResult = await userManager.CreateAsync(user, password);
            EnsureSucceeded(createResult, $"creating sample account '{email}'");
        }
        else
        {
            var changed = false;
            if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(user.FullName))
            {
                user.FullName = fullName;
                changed = true;
            }

            if (changed)
            {
                var updateResult = await userManager.UpdateAsync(user);
                EnsureSucceeded(updateResult, $"updating sample account '{email}'");
            }
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            var roleResult = await userManager.AddToRoleAsync(user, role);
            EnsureSucceeded(roleResult, $"assigning role '{role}' to '{email}'");
        }

        return user;
    }

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Identity failed while {operation}: {string.Join("; ", result.Errors.Select(error => error.Description))}");
        }
    }
}
