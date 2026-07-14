//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using sqa_core.Data;
//using BCrypt.Net;
//using System.IdentityModel.Tokens.Jwt;
//using System.Security.Claims;
//using System.Text;
//using Microsoft.IdentityModel.Tokens;

//namespace sqa_core.Controllers
//{
//    [Route("api/[controller]")]
//    [ApiController]
//    public class AuthController : ControllerBase
//    {
//        private readonly AppDbContext _context;
//        private readonly IConfiguration _config;

//        public AuthController(AppDbContext context, IConfiguration config)
//        {
//            _context = context;
//            _config = config;
//        }

//        [HttpPost("login")]
//        public async Task<IActionResult> Login([FromBody] LoginRequest request)
//        {
//            var user = await _context.Users
//                .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsDeleted != true);

//            if (user == null) return Unauthorized(new { Message = "Invalid email", Success = false });


//            bool valid = BCrypt.Net.BCrypt.Verify(request.Password, user.Password);

//            if (!valid) return Unauthorized(new { Message = "Invalid password", Success = false });


//            var tokenHandler = new JwtSecurityTokenHandler();


//            var jwtKey = _config["Jwt:Key"];
//            if (string.IsNullOrEmpty(jwtKey))
//            {
//                return StatusCode(500, new { Message = "JWT Key is missing from configuration", Success = false });
//            }

//            var key = Encoding.ASCII.GetBytes(jwtKey);


//            var tokenDescriptor = new SecurityTokenDescriptor
//            {
//                Subject = new ClaimsIdentity(new[]
//                {
//                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
//                    new Claim(ClaimTypes.Email, user.Email),
//                    new Claim("RoleId", user.RoleId.ToString())
//                }),
//                Expires = DateTime.UtcNow.AddHours(8), // Token valid for 8 hours
//                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
//            };

//            var token = tokenHandler.CreateToken(tokenDescriptor);
//            var actualJwtToken = tokenHandler.WriteToken(token);

//            return Ok(new
//            {
//                Success = true,
//                Message = "Login Successful",
//                Token = actualJwtToken, 
//                UserData = new { user.UserId, user.UserName, user.RoleId }
//            });
//        }
//    }

//    public class LoginRequest
//    {
//        public string Email { get; set; }
//        public string Password { get; set; }
//    }
//}




using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sqa_core.Data;
using BCrypt.Net;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using sqa_core.Services;

namespace sqa_core.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;

        public AuthController(AppDbContext context, IConfiguration config, IEmailService emailService)
        {
            _context = context;
            _config = config;
            _emailService = emailService;
        }

        // --- 1. LOGIN ---
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email && u.IsDeleted != true);
            if (user == null) return Unauthorized(new { Message = "Invalid email", Success = false });

            bool valid = BCrypt.Net.BCrypt.Verify(request.Password, user.Password);
            if (!valid) return Unauthorized(new { Message = "Invalid password", Success = false });

            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtKey = _config["Jwt:Key"];
            if (string.IsNullOrEmpty(jwtKey)) return StatusCode(500, new { Message = "JWT Key is missing", Success = false });

            var key = Encoding.ASCII.GetBytes(jwtKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim("RoleId", user.RoleId.ToString())
                }),
                Expires = DateTime.UtcNow.AddHours(8),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return Ok(new
            {
                Success = true,
                Message = "Login Successful",
                Token = tokenHandler.WriteToken(token),
                UserData = new { user.UserId, user.UserName, user.RoleId }
            });
        }

        // --- 2. FORGOT PASSWORD ---
        // --- 2. FORGOT PASSWORD ---
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email && u.IsDeleted != true);
            if (user == null) return Ok(new { Success = true, Message = "If that email exists, a reset link has been sent." });

            // 1. Generate Token and Save to DB
            string token = Guid.NewGuid().ToString("N");
            user.ResetToken = token;
            user.ResetTokenExpires = DateTime.UtcNow.AddHours(1);
            await _context.SaveChangesAsync();

            // 2. Build the Angular Reset Link
            var resetLink = $"http://localhost:4200/#/login/reset-password?token={token}&email={user.Email}";

            // 3. ⚠️ DEVELOPER BYPASS: We are skipping Resend entirely so it doesn't crash or fail silently.
            // await _emailService.SendPasswordResetEmailAsync(user.Email, resetLink);

            // 4. Return the link directly in the JSON response!
            return Ok(new
            {
                Success = true,
                Message = "Dev Mode: Email bypassed. Use the link below.",
                DevResetLink = resetLink // 🔥 Copy this from the Network tab!
            });
        }

        // --- 3. RESET PASSWORD ---
        [HttpPost("reset-password-with-token")]
        public async Task<IActionResult> ResetPasswordWithToken([FromBody] ResetPasswordTokenRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.Email == request.Email &&
                u.ResetToken == request.Token &&
                u.IsDeleted != true);

            if (user == null || user.ResetTokenExpires < DateTime.UtcNow)
                return BadRequest(new { Success = false, Message = "Invalid or expired password reset token." });

            user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.ResetToken = null;
            user.ResetTokenExpires = null;

            await _context.SaveChangesAsync();
            return Ok(new { Success = true, Message = "Password has been successfully reset." });
        }
    }

    public class LoginRequest { public string Email { get; set; } public string Password { get; set; } }
    public class ForgotPasswordRequest { public string Email { get; set; } }
    public class ResetPasswordTokenRequest { public string Email { get; set; } public string Token { get; set; } public string NewPassword { get; set; } }
}   