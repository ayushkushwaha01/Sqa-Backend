using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sqa_core.Data;
using sqa_core.Models;
using System.Security.Claims;

namespace sqa_core.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SeverityController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SeverityController(AppDbContext context)
        {
            _context = context;
        }

        private long GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return string.IsNullOrEmpty(userIdClaim) ? 0 : long.Parse(userIdClaim);
        }

        [HttpGet("get-all")]
        public async Task<IActionResult> GetAll()
        {
            var data = await _context.SeverityMasters
                .Where(x => x.IsDeleted != true)
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();

            return Ok(new { Data = data, Success = true });
        }

        [HttpPost("upsert")]
        public async Task<IActionResult> Upsert([FromBody] SeverityMaster model)
        {
            var exists = await _context.SeverityMasters.AnyAsync(x =>
                x.SeverityName.ToLower() == model.SeverityName.ToLower() &&
                x.SeverityId != model.SeverityId &&
                x.IsDeleted != true);

            if (exists) return BadRequest(new { Message = "Severity name already exists!", Success = false });

            long currentUserId = GetCurrentUserId();

            if (model.SeverityId == 0)
            {
                model.CreatedBy = currentUserId;
                model.CreatedDate = DateTime.UtcNow;
                
                _context.SeverityMasters.Add(model);
                await _context.SaveChangesAsync();
                return Ok(new { Message = "Severity Added Successfully", Success = true });
            }
            else
            {
                var dbItem = await _context.SeverityMasters.FindAsync(model.SeverityId);
                if (dbItem == null) return NotFound(new { Message = "Not Found", Success = false });

                dbItem.SeverityName = model.SeverityName;
                dbItem.Rating = model.Rating;
                dbItem.IsActive = model.IsActive;
                dbItem.ModifiedBy = currentUserId;
                dbItem.ModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return Ok(new { Message = "Severity Updated Successfully", Success = true });
            }
        }

        //[HttpPost("toggle-status")]
        //public async Task<IActionResult> ToggleStatus([FromBody] SeverityMaster model)
        //{
        //    var dbItem = await _context.SeverityMasters.FindAsync(model.SeverityId);
        //    if (dbItem == null) return NotFound(new { Message = "Not Found", Success = false });

        //    dbItem.IsActive = !dbItem.IsActive;
        //    await _context.SaveChangesAsync();

        //    return Ok(new { Message = "Status Changed", Success = true });
        //}

        //[HttpPost("delete")]
        //public async Task<IActionResult> Delete([FromBody] SeverityMaster model)
        //{
        //    var dbItem = await _context.SeverityMasters.FindAsync(model.SeverityId);
        //    if (dbItem == null) return NotFound(new { Message = "Not Found", Success = false });

        //    dbItem.IsDeleted = true;
        //    dbItem.DeletedDate = DateTime.UtcNow;
        //    dbItem.DeletedBy = GetCurrentUserId();

        //    await _context.SaveChangesAsync();
        //    return Ok(new { Message = "Severity Deleted", Success = true });
        //}


        [HttpPost("toggle-status")]
        public async Task<IActionResult> ToggleStatus([FromBody] SeverityActionRequest request) 
        {
            var dbItem = await _context.SeverityMasters.FindAsync(request.SeverityId);
            if (dbItem == null) return NotFound(new { Message = "Not Found", Success = false });

            dbItem.IsActive = !dbItem.IsActive;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Status Changed", Success = true });
        }

        [HttpPost("delete")]
        public async Task<IActionResult> Delete([FromBody] SeverityActionRequest request) 
        {
            var dbItem = await _context.SeverityMasters.FindAsync(request.SeverityId);
            if (dbItem == null) return NotFound(new { Message = "Not Found", Success = false });

            dbItem.IsDeleted = true;
            dbItem.DeletedDate = DateTime.UtcNow;
            dbItem.DeletedBy = GetCurrentUserId();

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Severity Deleted", Success = true });
        }
    }

    public class SeverityActionRequest
    {
        public long SeverityId { get; set; }
    }
}