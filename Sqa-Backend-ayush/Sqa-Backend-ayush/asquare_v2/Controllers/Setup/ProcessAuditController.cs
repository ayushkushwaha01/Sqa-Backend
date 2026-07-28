using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sqa_core.Data;
using sqa_core.Models;

namespace sqa_core.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProcessAuditController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProcessAuditController(AppDbContext context)
        {
            _context = context;
        }

        // --- PROCESS CATEGORIES ---
        [HttpGet("get-categories")]
        public async Task<IActionResult> GetCategories()
        {
            // Joins Checklists to get the count. Defaults to 0 if none exist.
            var data = await _context.ProcessCategories
                .Where(c => c.IsDeleted != true)
                .Select(c => new
                {
                    c.ProcessCategoryId,
                    c.Name,
                    c.Code,
                    c.IsActive,
                    ChecklistCount = _context.Checklists.Count(chk => chk.ProcessCategoryId == c.ProcessCategoryId && chk.IsDeleted != true)
                })
                .ToListAsync();

            return Ok(new { Data = data, Success = true });
        }

        [HttpPost("upsert-category")]
        public async Task<IActionResult> UpsertCategory([FromBody] ProcessCategory model)
        {
            if (model.ProcessCategoryId == 0)
            {
                model.CreatedDate = DateTime.UtcNow;
                model.CreatedBy = 1; // Replace with actual logged-in UserId
                _context.ProcessCategories.Add(model);
            }
            else
            {
                var dbItem = await _context.ProcessCategories.FindAsync(model.ProcessCategoryId);
                if (dbItem == null) return NotFound();

                dbItem.Name = model.Name;
                dbItem.Code = model.Code;
                dbItem.ModifiedDate = DateTime.UtcNow;
                dbItem.ModifiedBy = 1;
            }
            await _context.SaveChangesAsync();
            return Ok(new { Success = true, Message = "Category Saved" });
        }

        // --- CHECKLISTS (QUESTIONS) ---
        [HttpGet("get-checklists/{categoryId}")]
        public async Task<IActionResult> GetChecklists(long categoryId)
        {
            var data = await _context.Checklists
                .Where(c => c.ProcessCategoryId == categoryId && c.IsDeleted != true)
                .ToListAsync();
            return Ok(new { Data = data, Success = true });
        }

        [HttpPost("upsert-checklist")]
        public async Task<IActionResult> UpsertChecklist([FromBody] Checklist model)
        {
            if (model.ChecklistId == 0)
            {
                model.CreatedDate = DateTime.UtcNow;
                model.CreatedBy = 1;
                _context.Checklists.Add(model);
            }
            else
            {
                var dbItem = await _context.Checklists.FindAsync(model.ChecklistId);
                if (dbItem == null) return NotFound();

                dbItem.Question = model.Question;
                dbItem.Guideline = model.Guideline;
                dbItem.IsMandatory = model.IsMandatory;
                dbItem.IsPriority = model.IsPriority;
                dbItem.ModifiedDate = DateTime.UtcNow;
                dbItem.ModifiedBy = 1;
            }
            await _context.SaveChangesAsync();
            return Ok(new { Success = true, Message = "Question Saved" });
        }




        // --- STATUS & DELETE (CATEGORIES) ---
        [HttpPost("toggle-category-status/{id}")]
        public async Task<IActionResult> ToggleCategoryStatus(long id)
        {
            var dbItem = await _context.ProcessCategories.FindAsync(id);
            if (dbItem == null) return NotFound(new { Message = "Not Found", Success = false });

            dbItem.IsActive = !dbItem.IsActive;
            dbItem.ModifiedDate = DateTime.UtcNow;
            dbItem.ModifiedBy = 1; // Replace with JWT user ID

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Status Changed Successfully", Success = true });
        }

        [HttpPost("delete-category/{id}")]
        public async Task<IActionResult> DeleteCategory(long id)
        {
            var dbItem = await _context.ProcessCategories.FindAsync(id);
            if (dbItem == null) return NotFound(new { Message = "Not Found", Success = false });

            // 1. Soft Delete the Category
            dbItem.IsDeleted = true;
            dbItem.DeletedDate = DateTime.UtcNow;
            dbItem.DeletedBy = 1;

            // 2. Cascade Soft Delete: Also delete all Checklists under this Category
            var checklists = await _context.Checklists.Where(c => c.ProcessCategoryId == id).ToListAsync();
            foreach (var check in checklists)
            {
                check.IsDeleted = true;
                check.DeletedDate = DateTime.UtcNow;
                check.DeletedBy = 1;
            }

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Category Deleted Successfully", Success = true });
        }

        // --- DELETE (CHECKLISTS) ---
        [HttpPost("delete-checklist/{id}")]
        public async Task<IActionResult> DeleteChecklist(long id)
        {
            var dbItem = await _context.Checklists.FindAsync(id);
            if (dbItem == null) return NotFound(new { Message = "Not Found", Success = false });

            // Soft Delete the Question
            dbItem.IsDeleted = true;
            dbItem.DeletedDate = DateTime.UtcNow;
            dbItem.DeletedBy = 1;

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Question Deleted Successfully", Success = true });
        }
    }
}