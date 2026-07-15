using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sqa_core.Data;
using sqa_core.Models;
using System.Security.Claims;

namespace sqa_core.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DefectsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DefectsController(AppDbContext context)
        {
            _context = context;
        }

        // Helper to get logged-in User ID from JWT Token
        private long GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return string.IsNullOrEmpty(userIdClaim) ? 0 : long.Parse(userIdClaim);
        }

        [HttpGet("get-all")]
        public async Task<IActionResult> GetAll()
        {
            var data = await _context.DefectMasters
                .Where(x => x.IsDeleted != true)
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();

            return Ok(new { Data = data, Success = true });
        }

        [HttpPost("upsert")]
        public async Task<IActionResult> Upsert([FromBody] DefectMaster model)
        {
            var exists = await _context.DefectMasters.AnyAsync(x =>
                x.DefectName.ToLower() == model.DefectName.ToLower() &&
                x.DefectId != model.DefectId &&
                x.IsDeleted != true);

            if (exists) return BadRequest(new { Message = "Defect name already exists!", Success = false });

            long currentUserId = GetCurrentUserId();

            if (model.DefectId == 0)
            {
                model.CreatedBy = currentUserId;
                model.CreatedDate = DateTime.UtcNow;

                _context.DefectMasters.Add(model);
                await _context.SaveChangesAsync();
                return Ok(new { Message = "Defect Added Successfully", Success = true });
            }
            else
            {
                var dbItem = await _context.DefectMasters.FindAsync(model.DefectId);
                if (dbItem == null) return NotFound(new { Message = "Not Found", Success = false });

                dbItem.DefectName = model.DefectName;
                dbItem.ModifiedBy = currentUserId;
                dbItem.ModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return Ok(new { Message = "Defect Updated Successfully", Success = true });
            }
        }

        [HttpPost("delete")]
        public async Task<IActionResult> Delete([FromBody] DefectMaster model)
        {
            var dbItem = await _context.DefectMasters.FindAsync(model.DefectId);
            if (dbItem == null) return NotFound(new { Message = "Not Found", Success = false });

            dbItem.IsDeleted = true;
            dbItem.DeletedDate = DateTime.UtcNow;
            dbItem.DeletedBy = GetCurrentUserId();

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Defect Deleted", Success = true });
        }
    }
}