namespace Nhom3ConCa.Models;

public class Course
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Credits { get; set; }
    public int? TeacherId { get; set; }
    public Teacher? Teacher { get; set; }
    public ICollection<StudentCourse> StudentCourses { get; set; } = new List<StudentCourse>();
    public ICollection<ClassSession> ClassSessions { get; set; } = new List<ClassSession>();
}
