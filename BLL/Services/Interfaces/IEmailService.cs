namespace BLL.Services.Interfaces;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string toEmail, string resetLink, CancellationToken ct = default);
    Task SendWelcomeEmailAsync(string toEmail, string userName, CancellationToken ct = default);
}
