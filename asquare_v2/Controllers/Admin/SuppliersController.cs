using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sqa_core.Data;
using sqa_core.Models;
using BCrypt.Net;

namespace sqa_core.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SuppliersController : ControllerBase
    {
        private readonly AppDbContext _context;
        public SuppliersController(AppDbContext context) { _context = context; }

        [HttpGet("get-all")]
        public async Task<IActionResult> GetAll()
        {
            var data = await (from s in _context.SupplierMasters
                              join st in _context.StateMasters on s.StateId equals st.StateId into s_st
                              from st in s_st.DefaultIfEmpty()
                              join c in _context.CityMasters on s.CityId equals c.CityId into s_c
                              from c in s_c.DefaultIfEmpty()
                              where s.IsDeleted != true
                              orderby s.CreatedDate descending
                              select new
                              {
                                  s.SupplierId,
                                  s.UserName,
                                  s.SupplierName,
                                  s.ContactPerson,
                                  s.Email,
                                  s.Phone,
                                  s.StateId,
                                  StateName = st != null ? st.StateName : "",
                                  s.CityId,
                                  CityName = c != null ? c.CityName : "",
                                  s.Address,
                                  s.IsActive
                              }).ToListAsync();

            return Ok(new { Data = data, Success = true });
        }

        [HttpPost("upsert")]
        public async Task<IActionResult> Upsert([FromBody] SupplierMaster model)
        {
            var exists = await _context.SupplierMasters.AnyAsync(x =>
                (x.Email == model.Email || x.UserName == model.UserName) &&
                x.SupplierId != model.SupplierId &&
                x.IsDeleted != true);

            if (exists) return BadRequest(new { Message = "Email or Username already exists!", Success = false });

            if (model.SupplierId == 0)
            {
                model.CreatedDate = DateTime.UtcNow;
                model.CreatedBy = 1; // Replace with actual logged-in user ID
                model.Password = BCrypt.Net.BCrypt.HashPassword("Default@123"); // Default password for new suppliers

                _context.SupplierMasters.Add(model);
                await _context.SaveChangesAsync();
                return Ok(new { Message = "Supplier Created Successfully", Success = true });
            }
            else
            {
                var dbItem = await _context.SupplierMasters.FindAsync(model.SupplierId);
                if (dbItem == null) return NotFound(new { Message = "Not Found", Success = false });

                dbItem.UserName = model.UserName;
                dbItem.SupplierName = model.SupplierName;
                dbItem.ContactPerson = model.ContactPerson;
                dbItem.Email = model.Email;
                dbItem.Phone = model.Phone;
                dbItem.StateId = model.StateId;
                dbItem.CityId = model.CityId;
                dbItem.Address = model.Address;
                dbItem.ModifiedDate = DateTime.UtcNow;
                dbItem.ModifiedBy = 1;

                await _context.SaveChangesAsync();
                return Ok(new { Message = "Supplier Updated Successfully", Success = true });
            }
        }

        [HttpPost("toggle-status/{id}")]
        public async Task<IActionResult> ToggleStatus(long id)
        {
            var dbItem = await _context.SupplierMasters.FindAsync(id);
            if (dbItem == null) return NotFound(new { Message = "Not Found", Success = false });
            dbItem.IsActive = !dbItem.IsActive;
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Status Changed", Success = true });
        }

        [HttpPost("delete/{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var dbItem = await _context.SupplierMasters.FindAsync(id);
            if (dbItem == null) return NotFound(new { Message = "Not Found", Success = false });
            dbItem.IsDeleted = true;
            dbItem.DeletedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Supplier Deleted", Success = true });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetSupplierPasswordDto model)
        {
            var dbItem = await _context.SupplierMasters.FindAsync(model.SupplierId);
            if (dbItem == null) return NotFound(new { Message = "Not Found", Success = false });

            dbItem.Password = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Password Reset Successfully", Success = true });
        }
    }

    public class ResetSupplierPasswordDto
    {
        public long SupplierId { get; set; }
        public string NewPassword { get; set; }
    }
}