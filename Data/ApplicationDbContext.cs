using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Nhom3ConCa.Models;

namespace Nhom3ConCa.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<StudentCourse> StudentCourses => Set<StudentCourse>();
    public DbSet<ClassSession> ClassSessions => Set<ClassSession>();
    public DbSet<AttendanceSession> AttendanceSessions => Set<AttendanceSession>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NoiseMeasurement> NoiseMeasurements => Set<NoiseMeasurement>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<StudentCourse>().HasKey(item => new { item.StudentId, item.CourseId });
        builder.Entity<Student>().HasIndex(item => item.UserId).IsUnique();
        builder.Entity<Student>().HasIndex(item => item.StudentCode).IsUnique();
        builder.Entity<Teacher>().HasIndex(item => item.UserId).IsUnique();
        builder.Entity<Course>().HasIndex(item => item.Code).IsUnique();
        builder.Entity<AttendanceRecord>()
            .HasIndex(item => new { item.AttendanceSessionId, item.StudentId })
            .IsUnique();
        builder.Entity<NoiseMeasurement>().Property(item => item.Decibels).HasPrecision(6, 2);

        builder.Entity<Student>()
            .HasOne(item => item.User)
            .WithOne(item => item.Student)
            .HasForeignKey<Student>(item => item.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Teacher>()
            .HasOne(item => item.User)
            .WithOne(item => item.Teacher)
            .HasForeignKey<Teacher>(item => item.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Notification>()
            .HasOne(item => item.User)
            .WithMany()
            .HasForeignKey(item => item.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<NoiseMeasurement>()
            .HasOne(item => item.User)
            .WithMany()
            .HasForeignKey(item => item.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<AttendanceRecord>()
            .HasOne(item => item.Student)
            .WithMany(item => item.AttendanceRecords)
            .HasForeignKey(item => item.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<AttendanceRecord>()
            .HasOne(item => item.AttendanceSession)
            .WithMany(item => item.AttendanceRecords)
            .HasForeignKey(item => item.AttendanceSessionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Course>()
            .HasOne(item => item.Teacher)
            .WithMany(item => item.Courses)
            .HasForeignKey(item => item.TeacherId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.Entity<ClassSession>()
            .HasOne(item => item.Course)
            .WithMany(item => item.ClassSessions)
            .HasForeignKey(item => item.CourseId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ClassSession>()
            .HasOne(item => item.Teacher)
            .WithMany(item => item.ClassSessions)
            .HasForeignKey(item => item.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
