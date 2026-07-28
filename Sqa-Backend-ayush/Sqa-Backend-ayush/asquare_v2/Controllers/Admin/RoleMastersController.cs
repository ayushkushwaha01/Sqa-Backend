using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sqa_core.Data;
using sqa_core.Models;

namespace sqa_core.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoleMastersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RoleMastersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("get-all")]
        public async Task<IActionResult> GetAll()
        {
            var data = await _context.RoleMasters
                .Where(x => x.IsDeleted != true)
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();

            return Ok(new { Data = data, Success = true });
        }



        [HttpPost("upsert")]
        public async Task<IActionResult> Upsert([FromBody] RoleMaster model)
        {
            var exists = await _context.RoleMasters.AnyAsync(x =>
                x.RoleName == model.RoleName &&
                x.RoleId != model.RoleId &&
                x.IsDeleted != true);

            if (exists) return BadRequest(new { Message = "Role Name already exists!", Success = false });

            if (model.RoleId == 0)
            {
                model.CreatedDate = DateTime.UtcNow;
                model.IsActive = true;
                model.IsDeleted = false;
                _context.RoleMasters.Add(model);

                await _context.SaveChangesAsync();
                return Ok(new { Message = "Role Created Successfully", Success = true });
            }
            else
            {
                var dbItem = await _context.RoleMasters.FindAsync(model.RoleId);
                if (dbItem == null) return NotFound(new { Message = "Not Found", Success = false });

                dbItem.RoleName = model.RoleName;
                dbItem.ModifiedDate = DateTime.UtcNow;
                dbItem.ModifiedBy = model.ModifiedBy;

                await _context.SaveChangesAsync();
                return Ok(new { Message = "Role Updated Successfully", Success = true });
            }
        }


        [HttpPost("toggle-status")]
        public async Task<IActionResult> ToggleStatus([FromBody] RoleMaster model)
        {
            var dbItem = await _context.RoleMasters.FindAsync(model.RoleId);
            if (dbItem == null) return NotFound(new { Message = "Not Found", Success = false });

            dbItem.IsActive = !dbItem.IsActive;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Status Changed", Success = true });
        }



        [HttpPost("delete")]
        public async Task<IActionResult> Delete([FromBody] RoleMaster model)
        {
            var dbItem = await _context.RoleMasters.FindAsync(model.RoleId);
            if (dbItem == null) return NotFound(new { Message = "Not Found", Success = false });

            dbItem.IsDeleted = true;
            dbItem.DeletedDate = DateTime.UtcNow;
            dbItem.DeletedBy = model.DeletedBy;

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Role Deleted", Success = true });
        }
    }
}