using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TruongThaiBao.ChamCong.API.Data;
using TruongThaiBao.ChamCong.API.Models;
using TruongThaiBao.ChamCong.API.Services;

namespace TruongThaiBao.ChamCong.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    AppDbContext db,
    IPasswordHasher<User> passwordHasher,
    IEmailSender emailSender,
    IConfiguration config) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Email) ||
            request.Password.Length < 8)
            return BadRequest("Nhập đủ thông tin; mật khẩu tối thiểu 8 ký tự.");
        if (await db.Users.AnyAsync(x => x.Username == request.Username || x.Email == request.Email))
            return Conflict("Tên đăng nhập hoặc email đã tồn tại.");

        var user = new User
        {
            Username = request.Username.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            Role = "Employee",
            IsEmailVerified = false
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var verifyCode = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        user.OtpHash = HashOtp(verifyCode);
        user.OtpExpiresAtUtc = DateTime.UtcNow.AddMinutes(10);
        await db.SaveChangesAsync();
        try
        {
            await emailSender.SendAsync(user.Email, "Xác minh tài khoản chấm công",
                $"Mã xác minh email của bạn là: {verifyCode}. Mã hết hạn sau 10 phút.");
        }
        catch
        {
            return StatusCode(503, "Đã tạo tài khoản nhưng chưa gửi được email. Kiểm tra cấu hình SMTP.");
        }
        return Ok("Đăng ký thành công. Hãy nhập mã OTP gửi đến email để xác minh.");
    }

    [HttpPost("resend-verification")]
public async Task<IActionResult> ResendVerification(ResendOtpRequest request)
{
    var username = request.Username?.Trim();

    if (string.IsNullOrWhiteSpace(username))
        return BadRequest("Vui lòng nhập tên đăng nhập.");

    var user = await db.Users
        .SingleOrDefaultAsync(x => x.Username == username);

    if (user is null)
        return NotFound("Không tìm thấy tài khoản.");

    if (user.IsEmailVerified)
        return BadRequest("Tài khoản đã xác minh email.");

    var verifyCode = RandomNumberGenerator
        .GetInt32(100000, 1000000).ToString();

    user.OtpHash = HashOtp(verifyCode);
    user.OtpExpiresAtUtc = DateTime.UtcNow.AddMinutes(10);
    await db.SaveChangesAsync();

    try
    {
        await emailSender.SendAsync(
            user.Email,
            "Xác minh tài khoản chấm công",
            $"Mã xác minh email của bạn là: {verifyCode}. " +
            "Mã hết hạn sau 10 phút.");

        return Ok("Đã gửi lại OTP xác minh email.");
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.ToString());

        return StatusCode(503,
            "Không gửi được OTP. Kiểm tra cấu hình SMTP.");
    }
}

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail(VerifyOtpRequest request)
    {
        var user = await db.Users.SingleOrDefaultAsync(x => x.Username == request.Username);
        if (user is null || user.OtpExpiresAtUtc < DateTime.UtcNow ||
            user.OtpHash is null || user.OtpHash != HashOtp(request.Otp))
            return BadRequest("OTP không đúng hoặc đã hết hạn.");
        user.IsEmailVerified = true;
        user.OtpHash = null;
        user.OtpExpiresAtUtc = null;
        await db.SaveChangesAsync();
        return Ok("Email đã được xác minh.");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await db.Users.SingleOrDefaultAsync(x => x.Username == request.Username);
        if (user is null) return Unauthorized("Tên đăng nhập hoặc mật khẩu không đúng.");
        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
            return Unauthorized("Tên đăng nhập hoặc mật khẩu không đúng.");
        if (!user.IsEmailVerified) return BadRequest("Bạn cần xác minh email trước khi đăng nhập.");

        var otp = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        user.OtpHash = HashOtp(otp);
        user.OtpExpiresAtUtc = DateTime.UtcNow.AddMinutes(5);
        await db.SaveChangesAsync();
        try
        {
            await emailSender.SendAsync(user.Email, "OTP đăng nhập chấm công",
                $"Mã OTP đăng nhập: {otp}. Mã hết hạn sau 5 phút.");
        }
        catch
        {
            return StatusCode(503, "Không gửi được OTP. Kiểm tra cấu hình SMTP.");
        }
        return Ok("OTP đã được gửi đến email của bạn.");
    }

    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp(VerifyOtpRequest request)
    {
        var user = await db.Users.SingleOrDefaultAsync(x => x.Username == request.Username);
        if (user is null || user.OtpExpiresAtUtc < DateTime.UtcNow ||
            user.OtpHash is null || user.OtpHash != HashOtp(request.Otp))
            return Unauthorized("OTP không đúng hoặc đã hết hạn.");

        user.OtpHash = null;
        user.OtpExpiresAtUtc = null;
        await db.SaveChangesAsync();

        var jwt = config.GetSection("Jwt");
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role)
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
        var token = new JwtSecurityToken(jwt["Issuer"], jwt["Audience"], claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return Ok(new AuthResponse(new JwtSecurityTokenHandler().WriteToken(token), user.Username, user.Role));
    }

    private static string HashOtp(string otp) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(otp)));
}
