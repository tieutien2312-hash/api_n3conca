namespace Nhom3ConCa.Models;

public class ClassSession
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public int TeacherId { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public string Room { get; set; } = string.Empty;
    public Course Course { get; set; } = null!;
    public Teacher Teacher { get; set; } = null!;
    public ICollection<AttendanceSession> AttendanceSessions { get; set; } = new List<AttendanceSession>();
}
