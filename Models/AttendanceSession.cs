namespace Nhom3ConCa.Models;

public class AttendanceSession
{
    public int Id { get; set; }
    public int ClassSessionId { get; set; }
    public string? QrCode { get; set; }
    public DateTimeOffset? QrExpiresAt { get; set; }
    public bool IsOpen { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ClassSession ClassSession { get; set; } = null!;
    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
    public ICollection<NoiseMeasurement> NoiseMeasurements { get; set; } = new List<NoiseMeasurement>();
}
