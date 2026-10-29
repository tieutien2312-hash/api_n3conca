namespace Nhom3ConCa.Models;

public class Teacher
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? Department { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public ICollection<Course> Courses { get; set; } = new List<Course>();
    public ICollection<ClassSession> ClassSessions { get; set; } = new List<ClassSession>();
}
