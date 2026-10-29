namespace Nhom3ConCa.Models;

public class AttendanceRecord
{
    public int Id { get; set; }
    public int AttendanceSessionId { get; set; }
    public int StudentId { get; set; }
    public DateTimeOffset MarkedAt { get; set; } = DateTimeOffset.UtcNow;
    public string Status { get; set; } = "Present";
    public AttendanceSession AttendanceSession { get; set; } = null!;
    public Student Student { get; set; } = null!;
}
