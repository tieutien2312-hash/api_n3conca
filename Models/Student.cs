namespace Nhom3ConCa.Models;

public class Student
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string StudentCode { get; set; } = string.Empty;
    public string? Department { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public ICollection<StudentCourse> StudentCourses { get; set; } = new List<StudentCourse>();
    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
}
