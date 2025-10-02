using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using Models;

namespace Services
{
    public class EmailService
    {
        private readonly SmtpSettings _smtpSettings;

        public EmailService(IOptions<SmtpSettings> smtpSettings)
        {
            _smtpSettings = smtpSettings.Value;
        }

        public async Task<bool> SendOtpEmail(string recipientEmail, string otpCode)
        {
            try
            {
                var mail = new MailMessage
                {
                    // Lỗi: Đã sửa từ _smtpSettings.SenderEmail sang _smtpSettings.FromEmail
                    From = new MailAddress(_smtpSettings.FromEmail, "Hệ thống Xác thực OTP"),
                    Subject = "Mã Xác thực OTP của bạn",
                    Body = $"Mã OTP của bạn là: <b>{otpCode}</b>. Mã này sẽ hết hạn trong 5 phút.",
                    IsBodyHtml = true,
                };

                mail.To.Add(recipientEmail);

                // Lỗi: Đã sửa từ _smtpSettings.Server sang _smtpSettings.Host
                using (var smtp = new SmtpClient(_smtpSettings.Host, _smtpSettings.Port))
                {
                    // Lỗi: Đã sửa từ SenderEmail, SenderPassword sang Username, Password
                    smtp.Credentials = new NetworkCredential(_smtpSettings.Username, _smtpSettings.Password);
                    smtp.EnableSsl = true; // Bắt buộc cho Port 587

                    await smtp.SendMailAsync(mail);
                }

                return true;
            }
            catch (Exception ex)
            {
                // Nên cập nhật logic catch để in ra lỗi chi tiết hơn
                Console.Error.WriteLine($"Lỗi gửi email: {ex.Message}");
                return false;
            }
        }
    }
}