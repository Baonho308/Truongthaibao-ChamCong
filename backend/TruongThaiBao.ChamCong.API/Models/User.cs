using System.ComponentModel.DataAnnotations;

namespace TruongThaiBao.ChamCong.API.Models;

public class User
{
    public int Id { get; set; }
    [MaxLength(50)] public string Username { get; set; } = "";
    [MaxLength(256)] public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    [MaxLength(20)] public string Role { get; set; } = "Employee"; // Admin, Employee, Customer
    public bool IsEmailVerified { get; set; }
    public string? OtpHash { get; set; }
    public DateTime? OtpExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
