using Resend;
using System.Diagnostics;

namespace sqa_core.Services
{
    public interface IEmailService
    {
        Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetLink);
    }

    public class EmailService : IEmailService
    {
        private readonly IResend _resend;
        private readonly IConfiguration _config;

        public EmailService(IResend resend, IConfiguration config)
        {
            _resend = resend;
            _config = config;
        }

        public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetLink)
        {
            // 🚨 MOCK EMAIL SERVICE (Bypassing Resend) 🚨
            // Simulate network delay to make the UI feel realistic
            await Task.Delay(1000);

            // Print the reset link to your Visual Studio Output/Console window
            Console.WriteLine("\n====================================================");
            Console.WriteLine("📧 MOCK EMAIL INTERCEPTED!");
            Console.WriteLine($"To: {toEmail}");
            Console.WriteLine($"CLICK THIS LINK TO RESET: {resetLink}");
            Console.WriteLine("====================================================\n");

            Debug.WriteLine($"\n📧 RESET LINK: {resetLink}\n");

            // Return true to tell the Controller that the "email" was sent successfully
            return true;

            /* 
            // 🛑 ORIGINAL RESEND CODE (Commented out until you have an API key) 🛑
            var fromEmail = _config["Resend:FromEmail"];
            var message = new EmailMessage
            {
                From = $"SQA System <{fromEmail}>",
                Subject = "Password Reset Request",
                HtmlBody = $"<a href='{resetLink}'>Reset Password</a>"
            };
            message.To.Add(toEmail);
            var response = await _resend.EmailSendAsync(message);
            return response != null && response.Content != Guid.Empty; 
            */
        }
    }
}