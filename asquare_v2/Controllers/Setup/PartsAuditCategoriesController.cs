using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sqa_core.Data;
using sqa_core.Models;

namespace sqa_core.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PartsAuditCategoriesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PartsAuditCategoriesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/PartsAuditCategories/get-all
        //[HttpGet("get-all")]
        //public async Task<IActionResult> GetAll()
        //{
        //    var data = await _context.PartsAuditCategories
        //        .Where(x => x.IsDeleted != true)
        //        .OrderByDescending(x => x.CreatedDate)
        //        .ToListAsync();

        //    return Ok(new
        //    {
        //        Success = true,
        //        //Data = data
        //        Data = new
        //        {
        //            ToatalRecords = data.Count,   // or TotalRecords = data.Count
        //            Data = data
        //        }
        //    });
        //}


        [HttpGet("get-all")]
        public async Task<IActionResult> GetAll([FromQuery] PartsAuditCategoryFilter filter)
        {
            IQueryable<PartsAudit> query = _context.PartsAuditCategories
                .Where(x => x.IsDeleted != true);

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                query = query.Where(x =>
                    x.CategoryName.Contains(filter.Keyword) ||
                    x.CategoryCode.Contains(filter.Keyword));
            }

            if (filter.Status.HasValue)
            {
                query = query.Where(x => x.IsActive == filter.Status.Value);
            }

            var data = await query
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();

            return Ok(new
            {
                Success = true,
                Data = new
                {
                    ToatalRecords = data.Count,
                    Data = data
                }
            });
        }
        // POST: api/PartsAuditCategories/upsert
        [HttpPost("upsert")]
        public async Task<IActionResult> Upsert([FromBody] PartsAudit model)
        {
            var exists = await _context.PartsAuditCategories.AnyAsync(x =>
                x.CategoryName == model.CategoryName &&
                x.PartId != model.PartId &&
                x.IsDeleted != true);

            if (exists)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "Category already exists!"
                });
            }

            if (model.PartId == 0)
            {
                model.CreatedDate = DateTime.UtcNow;
                model.IsActive = true;
                model.IsDeleted = false;

                _context.PartsAuditCategories.Add(model);

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Success = true,
                    Message = "Category Added Successfully"
                });
            }
            else
            {
                var dbItem = await _context.PartsAuditCategories.FindAsync(model.PartId);

                if (dbItem == null)
                {
                    return NotFound(new
                    {
                        Success = false,
                        Message = "Record Not Found"
                    });
                }

                dbItem.CategoryName = model.CategoryName;
                dbItem.CategoryCode = model.CategoryCode;
                dbItem.ModifiedBy = model.ModifiedBy;
                dbItem.ModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Success = true,
                    Message = "Category Updated Successfully"
                });
            }
        }

        // POST: api/PartsAuditCategories/toggle-status
        [HttpPost("toggle-status")]
        public async Task<IActionResult> ToggleStatus([FromBody] PartsAudit model)
        {
            var dbItem = await _context.PartsAuditCategories.FindAsync(model.PartId);

            if (dbItem == null)
            {
                return NotFound(new
                {
                    Success = false,
                    Message = "Record Not Found"
                });
            }

            dbItem.IsActive = !dbItem.IsActive;
            dbItem.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Success = true,
                Message = "Status Changed Successfully"
            });
        }

        // POST: api/PartsAuditCategories/delete
        [HttpPost("delete")]
        public async Task<IActionResult> Delete([FromBody] PartsAudit model)
        {
            var dbItem = await _context.PartsAuditCategories.FindAsync(model.PartId);

            if (dbItem == null)
            {
                return NotFound(new
                {
                    Success = false,
                    Message = "Record Not Found"
                });
            }

            dbItem.IsDeleted = true;
            dbItem.DeletedBy = model.DeletedBy;
            dbItem.DeletedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Success = true,
                Message = "Category Deleted Successfully"
            });
        }
    }
}