namespace TruongThaiBao.ChamCong.API.Models;

public record RegisterRequest(string Username, string Email, string Password);
public record LoginRequest(string Username, string Password);
public record VerifyOtpRequest(string Username, string Otp);
public record AuthResponse(string Token, string Username, string Role);
public record CheckOutRequest(string? Note);
public record UpdateAttendanceRequest(DateTime CheckInUtc, DateTime? CheckOutUtc, string? Note);
