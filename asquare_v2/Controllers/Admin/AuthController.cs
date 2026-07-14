using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sqa_core.Data;
using BCrypt.Net;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace sqa_core.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public AuthController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        //    [HttpPost("login")]
        //    public async Task<IActionResult> Login([FromBody] LoginRequest request)
        //    {
        //        var user = await _context.Users
        //            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsDeleted != true);

        //        if (user == null) return Unauthorized(new { Message = "Invalid email", Success = false });


        //        bool valid = BCrypt.Net.BCrypt.Verify(request.Password, user.Password);

        //        if (!valid) return Unauthorized(new { Message = "Invalid password", Success = false });


        //        var tokenHandler = new JwtSecurityTokenHandler();


        //        var jwtKey = _config["Jwt:Key"];
        //        if (string.IsNullOrEmpty(jwtKey))
        //        {
        //            return StatusCode(500, new { Message = "JWT Key is missing from configuration", Success = false });
        //        }

        //        var key = Encoding.ASCII.GetBytes(jwtKey);


        //        var tokenDescriptor = new SecurityTokenDescriptor
        //        {
        //            Subject = new ClaimsIdentity(new[]
        //            {
        //                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
        //                new Claim(ClaimTypes.Email, user.Email),
        //                new Claim("RoleId", user.RoleId.ToString())
        //            }),
        //            Expires = DateTime.UtcNow.AddHours(8), // Token valid for 8 hours
        //            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        //        };

        //        var token = tokenHandler.CreateToken(tokenDescriptor);
        //        var actualJwtToken = tokenHandler.WriteToken(token);

        //        return Ok(new
        //        {
        //            Success = true,
        //            Message = "Login Successful",
        //            Token = actualJwtToken,
        //            UserData = new { user.UserId, user.UserName, user.RoleId }
        //        });
        //    }
        //}



        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            long id = 0;
            string userName = "";
            string roleId = "";
            string userType = "";

            // 1. Try to find the user in the Internal Users table
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email && u.IsDeleted != true);

            if (user != null)
            {
                // Internal User found, verify password
                bool valid = BCrypt.Net.BCrypt.Verify(request.Password, user.Password);
                if (!valid) return Unauthorized(new { Message = "Invalid password", Success = false });

                id = user.UserId;
                userName = user.UserName;
                roleId = user.RoleId.ToString();
                userType = "Internal";
            }
            else
            {
                // 2. Try to find the user in the Suppliers table
                var supplier = await _context.SupplierMasters.FirstOrDefaultAsync(s => s.Email == request.Email && s.IsDeleted != true);

                if (supplier == null)
                {
                    // Not found in either table
                    return Unauthorized(new { Message = "Invalid email or user not found", Success = false });
                }

                // Supplier found, verify password
                bool valid = BCrypt.Net.BCrypt.Verify(request.Password, supplier.Password);
                if (!valid) return Unauthorized(new { Message = "Invalid password", Success = false });

                id = supplier.SupplierId;
                userName = supplier.UserName;
                roleId = "Supplier"; // Static identifier so the frontend knows it's a supplier
                userType = "Supplier";
            }

            // 3. Generate JWT Token
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtKey = _config["Jwt:Key"];

            if (string.IsNullOrEmpty(jwtKey))
            {
                return StatusCode(500, new { Message = "JWT Key is missing from configuration", Success = false });
            }

            var key = Encoding.ASCII.GetBytes(jwtKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
            new Claim(ClaimTypes.NameIdentifier, id.ToString()),
            new Claim(ClaimTypes.Email, request.Email),
            new Claim("RoleId", roleId),
            new Claim("UserType", userType) // Added to the token claims
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
                // Returns the dynamic userType so Angular can route accordingly
                UserData = new { userId = id, userName = userName, roleId = roleId, userType = userType }
            });
        }

        public class LoginRequest
        {
            public string Email { get; set; }
            public string Password { get; set; }
        }
    }

}




