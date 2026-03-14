using BLL.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;

namespace BLL.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink, CancellationToken ct = default)
    {
        var subject = "Reset Your Password - Room Rental Management";
        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #007bff; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .button {{ display: inline-block; background-color: #007bff; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; margin: 20px 0; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Reset Your Password</h1>
        </div>
        <div class=""content"">
            <p>Hello,</p>
            <p>You have requested to reset your password for your Host account in Room Rental Management System.</p>
            <p>Please click the button below to reset your password:</p>
            
            <a href=""{resetLink}"" class=""button"">Reset Password</a>
            
            <p>If the button doesn't work, you can copy and paste the following link into your browser:</p>
            <p style=""word-break: break-all;"">{resetLink}</p>
            
            <p><strong>Important:</strong></p>
            <ul>
                <li>This link will expire in 1 hour</li>
                <li>If you didn't request this reset, please ignore this email</li>
                <li>For security, never share this link with anyone</li>
            </ul>
        </div>
        <div class=""footer"">
            <p>© 2024 Room Rental Management System. All rights reserved.</p>
            <p>This is an automated email, please do not reply.</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(toEmail, subject, body, ct);
    }

    public async Task SendWelcomeEmailAsync(string toEmail, string userName, CancellationToken ct = default)
    {
        var subject = "Welcome to Room Rental Management";
        var body = $@"
<html>
<body style=""font-family: Arial, sans-serif;"">
    <h2>Welcome {userName}!</h2>
    <p>Your host account has been created successfully.</p>
    <p>You can now login and start managing your properties.</p>
    <p>Best regards,<br/>Room Rental Management Team</p>
</body>
</html>";

        await SendEmailAsync(toEmail, subject, body, ct);
    }

    private async Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        try
        {
            //if (_configuration["ASPNETCORE_ENVIRONMENT"] == "Development")
            //{
            //    _logger.LogInformation("=== EMAIL SENT (DEV MODE) ===");
            //    _logger.LogInformation("To: {ToEmail}", toEmail);
            //    _logger.LogInformation("Subject: {Subject}", subject);
            //    _logger.LogInformation("Body: {Body}", htmlBody);
            //    _logger.LogInformation("===============================");
            //    return;
            //}

            var smtpSettings = _configuration.GetSection("EmailSettings");
            var smtpHost = smtpSettings["SmtpHost"];
            var smtpPort = int.Parse(smtpSettings["SmtpPort"] ?? "587");
            var smtpUser = smtpSettings["SmtpUser"];
            var smtpPassword = smtpSettings["SmtpPassword"];
            var fromEmail = smtpSettings["FromEmail"];

            // Null check
            if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUser) || 
                string.IsNullOrEmpty(smtpPassword) || string.IsNullOrEmpty(fromEmail))
            {
                _logger.LogError("Email configuration is incomplete");
                return; // Không throw exception trong dev mode
            }

            using var client = new SmtpClient(smtpHost, smtpPort);
            client.EnableSsl = true;
            client.Credentials = new NetworkCredential(smtpUser, smtpPassword);

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail, "Room Rental Management"),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);

            await client.SendMailAsync(mailMessage, ct);
            _logger.LogInformation("Email sent successfully to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
        }
    }
}