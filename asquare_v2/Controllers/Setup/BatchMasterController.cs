using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sqa_core.Data;
using sqa_core.Models;

namespace sqa_core.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BatchMasterController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BatchMasterController(AppDbContext context)
        {
            _context = context;
        }

        //=========================================================
        // GET ALL
        //=========================================================

        [HttpGet("get-all")]
        public async Task<IActionResult> GetAll([FromQuery] BatchMasterFilter filter)
        {
            var query =
                from b in _context.BatchMasters
                join pf in _context.PartFamilies
                    on b.PartFamilyId equals pf.PartFamilyId
                join pm in _context.PartMasters
                    on b.PartMasterId equals pm.PartMasterId
                where b.IsDeleted != true
                select new
                {
                    b.BatchId,
                    b.BatchNumber,
                    b.PartFamilyId,
                    PartFamilyName = pf.PartFamilyName,
                    b.PartMasterId,
                    PartMasterName = pm.PartMasterName,
                    b.BatchDate,
                    b.Remakrs,
                    b.IsActive,
                    b.IsDeleted,
                    b.CreatedDate
                };

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                query = query.Where(x =>
                    x.BatchNumber.Contains(filter.Keyword) ||
                    x.PartFamilyName.Contains(filter.Keyword) ||
                    x.PartMasterName.Contains(filter.Keyword));
            }

            if (filter.PartFamilyId.HasValue)
            {
                query = query.Where(x => x.PartFamilyId == filter.PartFamilyId);
            }

            if (filter.PartMasterId.HasValue)
            {
                query = query.Where(x => x.PartMasterId == filter.PartMasterId);
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
        //=========================================================
        // UPSERT
        //=========================================================

        [HttpPost("upsert")]
        public async Task<IActionResult> Upsert([FromBody] BatchMaster model)
        {
            var exists = await _context.BatchMasters.AnyAsync(x =>
                x.BatchNumber == model.BatchNumber &&
                x.BatchId != model.BatchId &&
                x.IsDeleted != true);

            if (exists)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "Batch Number already exists!"
                });
            }

            if (model.BatchId == 0)
            {
                model.CreatedDate = DateTime.UtcNow;
                model.IsActive = true;
                model.IsDeleted = false;

                _context.BatchMasters.Add(model);

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Success = true,
                    Message = "Batch Added Successfully"
                });
            }
            else
            {
                var dbItem = await _context.BatchMasters.FindAsync(model.BatchId);

                if (dbItem == null)
                {
                    return NotFound(new
                    {
                        Success = false,
                        Message = "Record Not Found"
                    });
                }

                dbItem.BatchNumber = model.BatchNumber;
                dbItem.PartFamilyId = model.PartFamilyId;
                dbItem.PartMasterId = model.PartMasterId;
                dbItem.BatchDate = model.BatchDate;
                dbItem.Remakrs = model.Remakrs;
                dbItem.ModifiedBy = model.ModifiedBy;
                dbItem.ModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Success = true,
                    Message = "Batch Updated Successfully"
                });
            }
        }

        //=========================================================
        // TOGGLE STATUS
        //=========================================================

        [HttpPost("toggle-status")]
        public async Task<IActionResult> ToggleStatus([FromBody] BatchMaster model)
        {
            var dbItem = await _context.BatchMasters.FindAsync(model.BatchId);

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

        //=========================================================
        // DELETE
        //=========================================================

        [HttpPost("delete")]
        public async Task<IActionResult> Delete([FromBody] BatchMaster model)
        {
            var dbItem = await _context.BatchMasters.FindAsync(model.BatchId);

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
                Message = "Batch Deleted Successfully"
            });
        }
    }
}