using System.Net.Mail;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace CyberTech.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly string _smtpServer;
        private readonly int _port;
        private readonly string _senderEmail;
        private readonly string _senderPassword;
        private readonly string _senderName;
        private readonly bool _enableSsl;
        private readonly bool _useDefaultCredentials;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            try
            {
                _smtpServer = _configuration["EmailSettings:SmtpServer"];
                _port = int.Parse(_configuration["EmailSettings:Port"]);
                _senderEmail = _configuration["EmailSettings:SenderEmail"];
                _senderPassword = _configuration["EmailSettings:SenderPassword"];

                _senderName = _configuration["EmailSettings:SenderName"] ?? "CyberTech";
                _enableSsl = bool.Parse(_configuration["EmailSettings:EnableSsl"] ?? "true");
                _useDefaultCredentials = bool.Parse(_configuration["EmailSettings:UseDefaultCredentials"] ?? "false");

                _logger.LogDebug("Email configuration loaded: Server={Server}, Port={Port}, Sender={Sender}, SSL={SSL}, UseDefaultCredentials={UseDefault}",
                    _smtpServer, _port, _senderEmail, _enableSsl, _useDefaultCredentials);

                ValidateEmailSettings();
                _logger.LogInformation("EmailService initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing EmailService: {Message}", ex.Message);
                throw new InvalidOperationException("Email configuration is invalid", ex);
            }
        }

        private void ValidateEmailSettings()
        {
            if (string.IsNullOrEmpty(_smtpServer))
                throw new InvalidOperationException("SMTP server is not configured");
            if (_port <= 0)
                throw new InvalidOperationException("Invalid SMTP port");
            if (string.IsNullOrEmpty(_senderEmail))
                throw new InvalidOperationException("Sender email is not configured");
            if (string.IsNullOrEmpty(_senderPassword))
                throw new InvalidOperationException("Sender password is not configured");
        }

        public async Task SendPasswordResetEmailAsync(string email, string resetUrl)
        {
            _logger.LogDebug("Preparing to send password reset email to {Email}", email);

            try
            {
                using var message = new MailMessage();
                message.From = new MailAddress(_senderEmail, _senderName);
                message.To.Add(new MailAddress(email));
                message.Subject = "Yêu cầu đặt lại mật khẩu - CyberTech";
                message.IsBodyHtml = true;
                message.Body = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 5px;'>
                        <div style='text-align: center; margin-bottom: 20px;'>
                            <h1 style='color: #007bff; margin: 0;'>Cửa hàng CyberTech</h1>
                        </div>
                        <div style='background-color: #f8f9fa; padding: 20px; border-radius: 5px; margin-bottom: 20px;'>
                            <h2 style='color: #333; margin-top: 0;'>Yêu cầu đặt lại mật khẩu</h2>
                            <p style='color: #666; line-height: 1.6;'>
                                Xin chào,<br><br>
                                Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn tại CyberTech.
                                Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email này.
                            </p>
                            <div style='text-align: center; margin: 30px 0;'>
                                <a href='{resetUrl}' 
                                   style='background-color: #007bff; color: white; padding: 12px 24px; 
                                          text-decoration: none; border-radius: 4px; display: inline-block;
                                          font-weight: bold;'>
                                    Đặt lại mật khẩu
                                </a>
                            </div>
                            <p style='color: #666; font-size: 14px; margin-bottom: 0;'>
                                <strong>Lưu ý quan trọng:</strong><br>
                                - Link đặt lại mật khẩu sẽ hết hạn sau 15 phút<br>
                                - Mật khẩu mới phải có ít nhất 6 ký tự<br>
                                - Vui lòng không chia sẻ link này với người khác
                            </p>
                        </div>
                        <div style='text-align: center; color: #666; font-size: 12px; border-top: 1px solid #e0e0e0; padding-top: 20px;'>
                            <p style='margin: 0;'>Email này được gửi tự động, vui lòng không trả lời.</p>
                            <p style='margin: 5px 0 0 0;'>© {DateTime.Now.Year} CyberTech. All rights reserved.</p>
                        </div>
                    </div>";

                using var client = new SmtpClient(_smtpServer, _port)
                {
                    EnableSsl = true,  // Bắt buộc bật SSL/TLS
                    UseDefaultCredentials = false,  // Không dùng thông tin mặc định
                    Credentials = new NetworkCredential(_senderEmail, _senderPassword),
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Timeout = 10000  // 10 giây
                };
                try
                {
                    _logger.LogDebug("Connecting to SMTP server {Server}:{Port}", _smtpServer, _port);
                    await client.SendMailAsync(message);
                    _logger.LogInformation("Password reset email sent successfully to {Email}", email);
                }
                catch (SmtpException ex)
                {
                    _logger.LogError(ex, "SMTP Error: {Message}, StatusCode: {StatusCode}", ex.Message, ex.StatusCode);
                    throw new InvalidOperationException($"Lỗi gửi email: {ex.Message}", ex);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during SMTP operation: {Message}", ex.Message);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to {Email}: {Message}", email, ex.Message);
                throw new InvalidOperationException("Không thể gửi email đặt lại mật khẩu. Vui lòng thử lại sau.", ex);
            }
        }
    }
}