using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sqa_core.Data;
using sqa_core.Models;

namespace sqa_core.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PartMasterController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PartMasterController(AppDbContext context)
        {
            _context = context;
        }

        //=========================================================
        // GET ALL
        //=========================================================

        [HttpGet("get-all")]
        public async Task<IActionResult> GetAll([FromQuery] PartMasterFilter filter)
        {
            var query =
                from pm in _context.PartMasters

                join pf in _context.PartFamilies
                    on pm.PartFamilyId equals pf.PartFamilyId into pfGroup
                from pf in pfGroup.DefaultIfEmpty()

                join c in _context.CommodityMasters
                    on pm.CommodityId equals c.CommodityId into commodityGroup
                from c in commodityGroup.DefaultIfEmpty()

                join p in _context.Parameters.Where(x => x.IsDeleted != true)
                    on pm.PartMasterId equals p.PartMasterId into parameterGroup

                where pm.IsDeleted != true

                select new
                {
                    pm.PartMasterId,
                    pm.PartMasterName,
                    pm.PartMasterCode,
                    pm.PartFamilyId,
                    PartFamilyName = pf != null ? pf.PartFamilyName : "",

                    pm.CommodityId,
                    CommodityName = c != null ? c.Name : "",

                    pm.SupplierIds,

                    SupplierCount = string.IsNullOrWhiteSpace(pm.SupplierIds)
                        ? 0
                        : pm.SupplierIds.Split(',', StringSplitOptions.RemoveEmptyEntries).Length,

                    ParametersCount = parameterGroup.Count(),

                    pm.IsActive,
                    pm.IsDeleted,
                    pm.CreatedBy,
                    pm.CreatedDate,
                    pm.ModifiedBy,
                    pm.ModifiedDate,
                    pm.DeletedBy,
                    pm.DeletedDate
                };

            // Keyword Filter
            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                query = query.Where(x =>
                    x.PartMasterName.Contains(filter.Keyword) ||
                    x.PartMasterCode.Contains(filter.Keyword) ||
                    x.PartFamilyName.Contains(filter.Keyword) ||
                    x.CommodityName.Contains(filter.Keyword));
            }

            // Status Filter
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
        public async Task<IActionResult> Upsert([FromBody] PartMaster model)
        {
            var exists = await _context.PartMasters.AnyAsync(x =>
                x.PartMasterName == model.PartMasterName &&
                x.PartMasterId != model.PartMasterId &&
                x.IsDeleted != true);

            if (exists)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "Part Master already exists!"
                });
            }

            if (model.PartMasterId == 0)
            {
                model.CreatedDate = DateTime.UtcNow;
                model.IsActive = true;
                model.IsDeleted = false;

                _context.PartMasters.Add(model);

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Success = true,
                    Message = "Part Master Added Successfully"
                });
            }
            else
            {
                var dbItem = await _context.PartMasters.FindAsync(model.PartMasterId);

                if (dbItem == null)
                {
                    return NotFound(new
                    {
                        Success = false,
                        Message = "Record Not Found"
                    });
                }

                dbItem.PartMasterName = model.PartMasterName;
                dbItem.PartMasterCode = model.PartMasterCode;
                dbItem.PartFamilyId = model.PartFamilyId;
                dbItem.CommodityId = model.CommodityId;
                dbItem.SupplierIds = model.SupplierIds;
                dbItem.ModifiedBy = model.ModifiedBy;
                dbItem.ModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Success = true,
                    Message = "Part Master Updated Successfully"
                });
            }
        }

        //=========================================================
        // TOGGLE STATUS
        //=========================================================

        [HttpPost("toggle-status")]
        public async Task<IActionResult> ToggleStatus([FromBody] PartMaster model)
        {
            var dbItem = await _context.PartMasters.FindAsync(model.PartMasterId);

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
        public async Task<IActionResult> Delete([FromBody] PartMaster model)
        {
            var dbItem = await _context.PartMasters.FindAsync(model.PartMasterId);

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
                Message = "Part Master Deleted Successfully"
            });
        }
    }
}