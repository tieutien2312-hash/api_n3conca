namespace Nhom3ConCa.Models;

public class NoiseMeasurement
{
    public int Id { get; set; }
    public int AttendanceSessionId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public decimal Decibels { get; set; }
    public DateTimeOffset MeasuredAt { get; set; } = DateTimeOffset.UtcNow;
    public AttendanceSession AttendanceSession { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}
