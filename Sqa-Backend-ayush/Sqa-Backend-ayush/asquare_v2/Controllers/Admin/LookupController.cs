using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sqa_core.Data;
using sqa_core.Models;

namespace sqa_core.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LookupController : ControllerBase
    {
        private readonly AppDbContext _context;
        public LookupController(AppDbContext context) { _context = context; }

        [HttpGet("get-code-masters")]
        public async Task<IActionResult> GetCodeMasters()
        {
            var data = await _context.CodeMasters
                .Where(c => c.IsDeleted != true && c.IsActive == true)
                .Select(c => new { c.CodeId, c.CodeName })
                .ToListAsync();
            return Ok(new { Data = data, Success = true });
        }

        [HttpGet("get-lookups")]
        public async Task<IActionResult> GetLookups()
        {
            var data = await (from l in _context.Lookups
                              join c in _context.CodeMasters on l.CodeId equals c.CodeId
                              where l.IsDeleted != true
                              select new
                              {
                                  l.LookupId,
                                  l.CodeId,
                                  CodeMasterName = c.CodeName, // Brought in from join
                                  l.LookupName,
                                  l.IsActive
                              }).ToListAsync();

            return Ok(new { Data = data, Success = true });
        }

        [HttpPost("upsert-lookup")]
        public async Task<IActionResult> UpsertLookup([FromBody] Lookup model)
        {
            if (model.LookupId == 0)
            {
                model.CreatedDate = DateTime.UtcNow;
                model.CreatedBy = 1;
                _context.Lookups.Add(model);
            }
            else
            {
                var dbItem = await _context.Lookups.FindAsync(model.LookupId);
                if (dbItem == null) return NotFound();

                dbItem.CodeId = model.CodeId;
                dbItem.LookupName = model.LookupName;
                dbItem.ModifiedDate = DateTime.UtcNow;
                dbItem.ModifiedBy = 1;
            }
            await _context.SaveChangesAsync();
            return Ok(new { Success = true, Message = "Lookup Saved" });
        }

        [HttpPost("toggle-status/{id}")]
        public async Task<IActionResult> ToggleStatus(long id)
        {
            var dbItem = await _context.Lookups.FindAsync(id);
            if (dbItem == null) return NotFound();
            dbItem.IsActive = !dbItem.IsActive;
            await _context.SaveChangesAsync();
            return Ok(new { Success = true });
        }

        [HttpPost("delete/{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var dbItem = await _context.Lookups.FindAsync(id);
            if (dbItem == null) return NotFound();
            dbItem.IsDeleted = true;
            dbItem.DeletedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { Success = true });
        }
    }
}