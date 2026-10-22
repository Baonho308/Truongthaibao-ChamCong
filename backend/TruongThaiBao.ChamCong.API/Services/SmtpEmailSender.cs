using System.Net;
using System.Net.Mail;

namespace TruongThaiBao.ChamCong.API.Services;

public class SmtpEmailSender(IConfiguration configuration) : IEmailSender
{
    public async Task SendAsync(string to, string subject, string body)
    {
        var section = configuration.GetSection("Email");
        var host = section["Host"];
        var user = section["User"];
        var password = section["Password"];
        var from = section["From"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(user) ||
            string.IsNullOrWhiteSpace(password) || password.StartsWith("YOUR_"))
            throw new InvalidOperationException("Chưa cấu hình SMTP trong appsettings.json.");

        using var message = new MailMessage(from ?? user, to, subject, body);
        using var client = new SmtpClient(host, int.Parse(section["Port"] ?? "587"))
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(user, password)
        };
        await client.SendMailAsync(message);
    }
}
