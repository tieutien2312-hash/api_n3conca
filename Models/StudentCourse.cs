namespace Nhom3ConCa.Models;

public class StudentCourse
{
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public DateTimeOffset EnrolledAt { get; set; } = DateTimeOffset.UtcNow;
    public Student Student { get; set; } = null!;
    public Course Course { get; set; } = null!;
}
