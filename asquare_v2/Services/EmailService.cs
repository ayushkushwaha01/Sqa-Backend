using System.Diagnostics;

namespace sqa_core.Services
{
    public interface IEmailService
    {
        Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetLink);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetLink)
        {
            // Mock email service - logs the reset link to console
            await Task.Delay(1000);

            Console.WriteLine("\n====================================================");
            Console.WriteLine("📧 MOCK EMAIL INTERCEPTED!");
            Console.WriteLine($"To: {toEmail}");
            Console.WriteLine($"CLICK THIS LINK TO RESET: {resetLink}");
            Console.WriteLine("====================================================\n");

            Debug.WriteLine($"\n📧 RESET LINK: {resetLink}\n");

            return true;
        }
    }
}