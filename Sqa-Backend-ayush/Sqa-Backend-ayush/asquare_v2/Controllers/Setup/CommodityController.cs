using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sqa_core.Data;
using sqa_core.Models;

namespace sqa_core.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommodityController : ControllerBase
    {
        private readonly AppDbContext _context;
        public CommodityController(AppDbContext context) { _context = context; }

        // --- COMMODITY MASTER ---
        [HttpGet("get-commodities")]
        public async Task<IActionResult> GetCommodities()
        {
            var data = await _context.CommodityMasters
                .Where(c => c.IsDeleted != true)
                .Select(c => new
                {
                    c.CommodityId,
                    c.Name,
                    c.Code,
                    c.IsActive,
                    TargetCount = _context.CommodityTargets.Count(t => t.CommodityId == c.CommodityId && t.IsDeleted != true)
                }).ToListAsync();
            return Ok(new { Data = data, Success = true });
        }

        [HttpPost("upsert-commodity")]
        public async Task<IActionResult> UpsertCommodity([FromBody] CommodityMaster model)
        {
            if (model.CommodityId == 0)
            {
                model.CreatedDate = DateTime.UtcNow;
                model.CreatedBy = 1;
                _context.CommodityMasters.Add(model);
            }
            else
            {
                var dbItem = await _context.CommodityMasters.FindAsync(model.CommodityId);
                if (dbItem == null) return NotFound();

                dbItem.Name = model.Name;
                dbItem.Code = model.Code;
                dbItem.ModifiedDate = DateTime.UtcNow;
                dbItem.ModifiedBy = 1;
            }
            await _context.SaveChangesAsync();
            return Ok(new { Success = true, Message = "Commodity Saved" });
        }

        [HttpPost("toggle-commodity-status/{id}")]
        public async Task<IActionResult> ToggleCommodityStatus(long id)
        {
            var dbItem = await _context.CommodityMasters.FindAsync(id);
            if (dbItem == null) return NotFound();
            dbItem.IsActive = !dbItem.IsActive;
            await _context.SaveChangesAsync();
            return Ok(new { Success = true, Message = "Status Changed" });
        }

        [HttpPost("delete-commodity/{id}")]
        public async Task<IActionResult> DeleteCommodity(long id)
        {
            var dbItem = await _context.CommodityMasters.FindAsync(id);
            if (dbItem == null) return NotFound();
            dbItem.IsDeleted = true;
            dbItem.DeletedDate = DateTime.UtcNow;

            var targets = await _context.CommodityTargets.Where(t => t.CommodityId == id).ToListAsync();
            targets.ForEach(t => { t.IsDeleted = true; t.DeletedDate = DateTime.UtcNow; });

            await _context.SaveChangesAsync();
            return Ok(new { Success = true, Message = "Commodity Deleted" });
        }

        // --- TARGETS ---
        [HttpGet("get-targets/{commodityId}")]
        public async Task<IActionResult> GetTargets(long commodityId)
        {
            var data = await _context.CommodityTargets
                .Where(t => t.CommodityId == commodityId && t.IsDeleted != true)
                .ToListAsync();
            return Ok(new { Data = data, Success = true });
        }

        [HttpPost("upsert-target")]
        public async Task<IActionResult> UpsertTarget([FromBody] CommodityTarget model)
        {
            if (model.TargetId == 0)
            {
                model.CreatedDate = DateTime.UtcNow;
                model.CreatedBy = 1;
                _context.CommodityTargets.Add(model);
            }
            else
            {
                var dbItem = await _context.CommodityTargets.FindAsync(model.TargetId);
                if (dbItem == null) return NotFound();

                dbItem.TargetYear = model.TargetYear;
                dbItem.Q1 = model.Q1;
                dbItem.Q2 = model.Q2;
                dbItem.Q3 = model.Q3;
                dbItem.Q4 = model.Q4;
                dbItem.ModifiedDate = DateTime.UtcNow;
                dbItem.ModifiedBy = 1;
            }
            await _context.SaveChangesAsync();
            return Ok(new { Success = true, Message = "Target Saved" });
        }

        [HttpPost("delete-target/{id}")]
        public async Task<IActionResult> DeleteTarget(long id)
        {
            var dbItem = await _context.CommodityTargets.FindAsync(id);
            if (dbItem == null) return NotFound();
            dbItem.IsDeleted = true;
            dbItem.DeletedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { Success = true, Message = "Target Deleted" });
        }
    }
}