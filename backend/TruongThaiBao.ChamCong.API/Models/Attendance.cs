using System.ComponentModel.DataAnnotations;

namespace TruongThaiBao.ChamCong.API.Models;

public class Attendance
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public DateTime CheckInUtc { get; set; }
    public DateTime? CheckOutUtc { get; set; }
    [MaxLength(500)] public string? Note { get; set; }
}
