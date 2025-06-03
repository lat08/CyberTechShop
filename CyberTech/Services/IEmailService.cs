namespace CyberTech.Services
{
    public interface IEmailService
    {
        Task SendPasswordResetEmailAsync(string email, string resetUrl);
        Task SendEmailAsync(string email, string subject, string htmlContent);
    }
}