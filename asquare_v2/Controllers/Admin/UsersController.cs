using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sqa_core.Data;
using sqa_core.Models;
using BCrypt.Net;

namespace sqa_core.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("get-all")]
        public async Task<IActionResult> GetAll()
        {
            // Join with RoleMasters to fetch the text name of the Role for the grid
            var data = await (from u in _context.Users
                              join r in _context.RoleMasters on u.RoleId equals r.RoleId into ur
                              from r in ur.DefaultIfEmpty()
                              where u.IsDeleted != true
                              orderby u.CreatedDate descending
                              select new
                              {
                                  u.UserId,
                                  u.UserName,
                                  u.Email,
                                  u.PhoneNumber,
                                  u.RoleId,
                                  RoleName = r != null ? r.RoleName : "", // Joined field
                                  u.Department,
                                  u.Manager,
                                  u.IsHod,
                                  u.IsInspector,
                                  u.IsAuditor,
                                  u.IsManagerialRole,
                                  u.TwoFactor,
                                  u.IsActive
                              }).ToListAsync();

            return Ok(new { Data = data, Success = true });
        }

        //[HttpPost("upsert")]
        //public async Task<IActionResult> Upsert([FromBody] UserMaster model)
        //{

        //    var exists = await _context.Users.AnyAsync(x =>
        //        x.Email == model.Email &&
        //        x.UserId != model.UserId &&
        //        x.IsDeleted != true);

        //    if (exists) return BadRequest(new { Message = "Email already exists!", Success = false });

        //    if (model.UserId == 0)
        //    {
        //         model.CreatedDate = DateTime.UtcNow;
        //        model.IsActive = true;
        //        model.IsDeleted = false;

        //         model.Password = "Default@123";

        //        _context.Users.Add(model);
        //        await _context.SaveChangesAsync();

        //        return Ok(new { Message = "User Created Successfully", Success = true });
        //    }
        //    else
        //    {
        //        var dbItem = await _context.Users.FindAsync(model.UserId);
        //        if (dbItem == null) return NotFound(new { Message = "Not Found", Success = false });

        //        dbItem.UserName = model.UserName;
        //        dbItem.PhoneNumber = model.PhoneNumber;
        //        dbItem.RoleId = model.RoleId;
        //        dbItem.Department = model.Department;
        //        dbItem.Manager = model.Manager;

        //         dbItem.IsHod = model.IsHod;
        //        dbItem.IsInspector = model.IsInspector;
        //        dbItem.IsAuditor = model.IsAuditor;
        //        dbItem.IsManagerialRole = model.IsManagerialRole;

        //        dbItem.ModifiedDate = DateTime.UtcNow;
        //        dbItem.ModifiedBy = model.ModifiedBy; 

        //        await _context.SaveChangesAsync();
        //        return Ok(new { Message = "User Updated Successfully", Success = true });
        //    }
        //}


        [HttpPost("upsert")]
        public async Task<IActionResult> Upsert([FromBody] UserMaster model)
        {
            // 1. Prevent duplicate emails
            var exists = await _context.Users.AnyAsync(x =>
                x.Email == model.Email &&
                x.UserId != model.UserId &&
                x.IsDeleted != true);

            if (exists) return BadRequest(new { Message = "Email already exists!", Success = false });

            if (model.UserId == 0) // NEW USER
            {
                model.CreatedDate = DateTime.UtcNow;
                model.IsActive = true;
                model.IsDeleted = false;

                // 🔥 FIX: Hash the password before saving
                // Use a default password or the one provided in the model
                string passwordToHash = !string.IsNullOrEmpty(model.Password) ? model.Password : "Default@123";
                model.Password = BCrypt.Net.BCrypt.HashPassword(passwordToHash);

                _context.Users.Add(model);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "User Created Successfully", Success = true });
            }
            else // UPDATE EXISTING USER
            {
                var dbItem = await _context.Users.FindAsync(model.UserId);
                if (dbItem == null) return NotFound(new { Message = "Not Found", Success = false });

                dbItem.UserName = model.UserName;
                dbItem.PhoneNumber = model.PhoneNumber;
                dbItem.RoleId = model.RoleId;
                dbItem.Department = model.Department;
                dbItem.Manager = model.Manager;
                dbItem.IsHod = model.IsHod;
                dbItem.IsInspector = model.IsInspector;
                dbItem.IsAuditor = model.IsAuditor;
                dbItem.IsManagerialRole = model.IsManagerialRole;

                dbItem.ModifiedDate = DateTime.UtcNow;

                // Note: We DO NOT touch the password here during a general update.
                // Password changes should ONLY happen via the ResetPassword endpoint.

                await _context.SaveChangesAsync();
                return Ok(new { Message = "User Updated Successfully", Success = true });
            }
        }

        [HttpPost("toggle-status")]
        public async Task<IActionResult> ToggleStatus([FromBody] UserMaster model)
        {
            var dbItem = await _context.Users.FindAsync(model.UserId);
            if (dbItem == null) return NotFound(new { Message = "Not Found", Success = false });

            dbItem.IsActive = !dbItem.IsActive;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Status Changed", Success = true });
        }

        [HttpPost("delete")]
        public async Task<IActionResult> Delete([FromBody] UserMaster model)
        {
            var dbItem = await _context.Users.FindAsync(model.UserId);
            if (dbItem == null) return NotFound(new { Message = "Not Found", Success = false });

            // Soft Delete
            dbItem.IsDeleted = true;
            dbItem.DeletedDate = DateTime.UtcNow;
            dbItem.DeletedBy = model.DeletedBy;

            await _context.SaveChangesAsync();
            return Ok(new { Message = "User Deleted", Success = true });
        }


        [HttpGet("get-managers")]
        public async Task<IActionResult> GetManagers()
        {
            // Fetch only users who are marked as a Managerial Role
            var managers = await _context.Users
                .Where(u => u.IsManagerialRole == true && u.IsDeleted != true)
                .Select(u => new { u.UserId, u.UserName })
                .ToListAsync();

            return Ok(new { Data = managers, Success = true });
        }
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto model)
        {
            var user = await _context.Users.FindAsync(model.UserId);
            if (user == null) return NotFound(new { Message = "User not found", Success = false });

            // 🔥 FIX: Hash the NEW password
            user.Password = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Password reset successfully", Success = true });
        }
        // Add this DTO class at the very bottom of the file (outside the controller)
        public class ResetPasswordDto
        {
            public long UserId { get; set; }
            public string NewPassword { get; set; }
        }

        //Bash MEthod for forget passwords
    }
}