using System.Net;
using System.Net.Mail;
using Nirmaan.Api.Interface;

namespace Nirmaan.Api.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendResetCodeAsync(string toEmail, string uniqueCode)
    {
        var host = _config["Email:Host"] ?? "smtp.gmail.com";
        var port = int.Parse(_config["Email:Port"] ?? "587");
        var username = _config["Email:Username"] ?? throw new Exception("Email Username missing");
        var password = _config["Email:Password"] ?? throw new Exception("Email Password missing");
        var from = _config["Email:From"] ?? username;

        using var client = new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(username, password),
            EnableSsl = true
        };

        var mail = new MailMessage
        {
            From = new MailAddress(from),
            Subject = "Nirmaan - Password Reset Code",
            IsBodyHtml = true,
            Body = $@"
            <div style='font-family: Arial; padding: 20px;'>
                <h2 style='color: #2c3e50;'>Nirmaan - Citizen Complaint Portal</h2>
                <p>You requested password reset for <b>{toEmail}</b></p>
                <p>Your unique reset code is:</p>
                <h1 style='background: #f1f1f1; padding: 15px; letter-spacing: 5px; text-align: center;'>{uniqueCode}</h1>
                <p>This code expires in <b>15 minutes</b>.</p>
                <p>If you didn't request this, ignore this email.</p>
                <hr/>
                <p style='color: gray; font-size: 12px;'>Nirmaan Team - Municipal Corporation</p>
            </div>"
        };
        mail.To.Add(toEmail);

        _logger.LogInformation("Sending reset email to {Email}", toEmail);
        await client.SendMailAsync(mail);
    }
}