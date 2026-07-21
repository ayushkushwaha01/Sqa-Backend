using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sqa_core.Data;
using sqa_core.Models;

namespace sqa_core.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PartFamilyController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PartFamilyController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/PartFamily/get-all
        [HttpGet("get-all")]
        public async Task<IActionResult> GetAll([FromQuery] PartsFamilyFilter filter)
        {
            var query =
                from pf in _context.PartFamilies
                join p in _context.Parameters.Where(x => x.IsDeleted != true)
                    on pf.PartFamilyId equals p.PartFamilyId into parameterGroup
                where pf.IsDeleted != true
                select new
                {
                    pf.PartFamilyId,
                    pf.PartFamilyName,
                    pf.PartFamilyCode,
                    pf.IsActive,
                    pf.IsDeleted,
                    pf.CreatedBy,
                    pf.CreatedDate,
                    pf.ModifiedBy,
                    pf.ModifiedDate,
                    pf.DeletedBy,
                    pf.DeletedDate,

                    ParametersCount = parameterGroup.Count()
                };

            // Keyword Filter
            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                query = query.Where(x =>
                    x.PartFamilyName.Contains(filter.Keyword) ||
                    x.PartFamilyCode.Contains(filter.Keyword));
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

        // POST: api/PartFamily/upsert
        [HttpPost("upsert")]
        public async Task<IActionResult> Upsert([FromBody] PartFamilyModel model)
        {
            var exists = await _context.PartFamilies.AnyAsync(x =>
                x.PartFamilyName == model.PartFamilyName &&
                x.PartFamilyId != model.PartFamilyId &&
                x.IsDeleted != true);

            if (exists)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "Part Family already exists!"
                });
            }

            if (model.PartFamilyId == 0)
            {
                model.CreatedDate = DateTime.UtcNow;
                model.IsActive = true;
                model.IsDeleted = false;

                _context.PartFamilies.Add(model);

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Success = true,
                    Message = "Part Family Added Successfully"
                });
            }
            else
            {
                var dbItem = await _context.PartFamilies.FindAsync(model.PartFamilyId);

                if (dbItem == null)
                {
                    return NotFound(new
                    {
                        Success = false,
                        Message = "Record Not Found"
                    });
                }

                dbItem.PartFamilyName = model.PartFamilyName;
                dbItem.PartFamilyCode = model.PartFamilyCode;
                dbItem.ModifiedBy = model.ModifiedBy;
                dbItem.ModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Success = true,
                    Message = "Part Family Updated Successfully"
                });
            }
        }

        // POST: api/PartFamily/toggle-status
        [HttpPost("toggle-status")]
        public async Task<IActionResult> ToggleStatus([FromBody] PartFamilyModel model)
        {
            var dbItem = await _context.PartFamilies.FindAsync(model.PartFamilyId);

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

        // POST: api/PartFamily/delete
        [HttpPost("delete")]


        public async Task<IActionResult> Delete([FromBody] PartFamilyModel model)
        {
            var dbItem = await _context.PartFamilies.FindAsync(model.PartFamilyId);

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
                Message = "Part Family Deleted Successfully"
            });
        }







        // upsert-parameter

        [HttpPost("upsert-parameter")]
        public async Task<IActionResult> Upsert([FromBody] ParameterModel model)
        {
            var exists = await _context.Parameters.AnyAsync(x =>
                x.ParmeterName == model.ParmeterName &&
                x.ParameterId != model.ParameterId &&
                x.IsDeleted != true);

            if (exists)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "Parameter already exists!"
                });
            }

            if (model.ParameterId == 0)
            {
                model.CreatedDate = DateTime.UtcNow;
                model.IsActive = true;
                model.IsDeleted = false;

                _context.Parameters.Add(model);

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Success = true,
                    Message = "Parameter Added Successfully"
                });
            }
            else
            {
                var dbItem = await _context.Parameters.FindAsync(model.ParameterId);

                if (dbItem == null)
                {
                    return NotFound(new
                    {
                        Success = false,
                        Message = "Record Not Found"
                    });
                }

                dbItem.ParmeterName = model.ParmeterName;
                dbItem.Spec = model.Spec;
                dbItem.Min = model.Min;
                dbItem.Max = model.Max;
                dbItem.Method = model.Method;

                dbItem.S1 = model.S1;
                dbItem.S2 = model.S2;
                dbItem.S3 = model.S3;
                dbItem.S4 = model.S4;
                dbItem.S5 = model.S5;

                dbItem.Remarks = model.Remarks;

                dbItem.PartId = model.PartId;
                dbItem.PartFamilyId = model.PartFamilyId;
                dbItem.PartMasterId = model.PartMasterId;

                dbItem.Okay = model.Okay;

                dbItem.ModifiedBy = model.ModifiedBy;
                dbItem.ModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Success = true,
                    Message = "Parameter Updated Successfully"
                });
            }
        }




        // Get- all parameters


        [HttpGet("get-parameters")]
        public async Task<IActionResult> GetParameters([FromQuery] ParameterFilter filter)
        {
            IQueryable<ParameterModel> query = _context.Parameters
                .Where(x => x.IsDeleted != true);

            // Filter by Part Family
            if (filter.PartFamilyId.HasValue)
            {
                query = query.Where(x => x.PartFamilyId == filter.PartFamilyId.Value);
            }
            if (filter.PartMasterId.HasValue)
            {
                query = query.Where(x => x.PartMasterId == filter.PartMasterId.Value);
            }


            // Keyword Filter
            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                query = query.Where(x =>
                    x.ParmeterName.Contains(filter.Keyword) ||
                    x.Spec.Contains(filter.Keyword) ||
                    x.Method.Contains(filter.Keyword));
            }

            // Status Filter
            if (filter.Status.HasValue)
            {
                query = query.Where(x => x.IsActive == filter.Status.Value);
            }

            //var data = await query
            //    .OrderByDescending(x => x.CreatedDate)
            //    .ToListAsync
            //    
            var data = await query
    .OrderByDescending(x => x.CreatedDate)
    .Select(x => new
    {
        x.ParameterId,
        x.PartId,
        x.PartFamilyId,
        x.PartMasterId,
        x.ParmeterName,
        x.Spec,
        x.Min,
        x.Max,
        x.Method,
        x.IsActive,

        IsCopied = _context.PartsAuditParameters.Any(a =>
            a.ParameterId == x.ParameterId &&
            a.IsDeleted != true)
    })
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





        // POST: api/PartFamily/delete-parameter
        [HttpPost("delete-parameter")]
        public async Task<IActionResult> DeleteParameter([FromBody] ParameterModel model)
        {
            var dbItem = await _context.Parameters.FindAsync(model.ParameterId);

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
                Message = "Parameter Deleted Successfully"
            });
        }

    }
}