using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sqa_core.Data;
using sqa_core.Models;


namespace sqa_core.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PartsAuditController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PartsAuditController(AppDbContext context)
        {
            _context = context;
        }

        //====================================================
        // GET PART AUDITS
        //====================================================

        [HttpGet("get-part-audits")]
        public async Task<IActionResult> GetPartAudits([FromQuery] PartsAuditFilter filter)
        {
            var query =
                from pa in _context.PartsAudits.Where(x => x.IsDeleted != true)

                join c in _context.CommodityMasters
                    on pa.CommodityId equals c.CommodityId into commodityGroup
                from commodity in commodityGroup.DefaultIfEmpty()

                join pf in _context.PartFamilies
                    on pa.PartFamilyId equals pf.PartFamilyId into partFamilyGroup
                from partFamily in partFamilyGroup.DefaultIfEmpty()

                join pm in _context.PartMasters
                    on pa.PartMasterId equals pm.PartMasterId into partMasterGroup
                from partMaster in partMasterGroup.DefaultIfEmpty()

                join s in _context.SupplierMasters
                    on pa.SupplierId equals s.SupplierId into supplierGroup
                from supplier in supplierGroup.DefaultIfEmpty()

                join st in _context.StateMasters
                    on pa.StateId equals st.StateId into stateGroup
                from state in stateGroup.DefaultIfEmpty()

                join ct in _context.CityMasters
                    on pa.CityId equals ct.CityId into cityGroup
                from city in cityGroup.DefaultIfEmpty()

                join u in _context.Users
                    on pa.AuditorId equals u.UserId into auditorGroup
                from auditor in auditorGroup.DefaultIfEmpty()

                join l in _context.Lookups
    on pa.StatusId equals l.LookupId into statusGroup
                from status in statusGroup.DefaultIfEmpty()

                join capa in _context.PartsAuditCapas.Where(x => x.IsDeleted != true)
    on pa.PartAuditId equals capa.PartAuditId into capaGroup


                select new
                {
                    pa.PartAuditId,

                    pa.CommodityId,
                    CommodityName = commodity != null ? commodity.Name : "",

                    pa.PartFamilyId,
                    PartFamilyName = partFamily != null ? partFamily.PartFamilyName : "",

                    pa.PartMasterId,
                    PartMasterName = partMaster != null ? partMaster.PartMasterName : "",

                    pa.SupplierId,
                    SupplierName = supplier != null ? supplier.SupplierName : "",

                    pa.StateId,
                    StateName = state != null ? state.StateName : "",

                    pa.CityId,
                    CityName = city != null ? city.CityName : "",

                    pa.AuditorId,
                    AuditorName = auditor != null ? auditor.UserName : "",

                    pa.AuditDate,
                    pa.Done,
                    //pa.StatusId,
                    pa.Remakrs,
                    pa.StatusId,
                    StatusName = status != null ? status.LookupName : "",
                    // CAPA Counts
                    TotalCapaCount = capaGroup.Count(),

                    ResolvedCapaCount = capaGroup.Count(x => x.IsResolved == true),

                    PendingCapaCount = capaGroup.Count(x => x.IsResolved != true),

                    pa.IsActive,
                    pa.IsDeleted,
                    pa.CreatedBy,
                    pa.CreatedDate,
                    pa.ModifiedBy,
                    pa.ModifiedDate,
                    pa.DeletedBy,
                    pa.DeletedDate
                };

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var keyword = filter.Keyword.Trim().ToLower();

                query = query.Where(x =>
                    x.CommodityName.ToLower().Contains(keyword) ||
                    x.PartFamilyName.ToLower().Contains(keyword) ||
                    x.PartMasterName.ToLower().Contains(keyword) ||
                    x.SupplierName.ToLower().Contains(keyword) ||
                    x.StateName.ToLower().Contains(keyword) ||
                    x.CityName.ToLower().Contains(keyword) ||
                    x.AuditorName.ToLower().Contains(keyword) ||
                    x.StatusName.ToLower().Contains(keyword) ||
                    (x.Remakrs != null && x.Remakrs.ToLower().Contains(keyword))
                );
            }

            // Commodity
            if (filter.CommodityId.HasValue)
            {
                query = query.Where(x => x.CommodityId == filter.CommodityId.Value);
            }

            // Part Family
            if (filter.PartFamilyId.HasValue)
            {
                query = query.Where(x => x.PartFamilyId == filter.PartFamilyId.Value);
            }

            // Part
            if (filter.PartMasterId.HasValue)
            {
                query = query.Where(x => x.PartMasterId == filter.PartMasterId.Value);
            }

            // Supplier
            if (filter.SupplierId.HasValue)
            {
                query = query.Where(x => x.SupplierId == filter.SupplierId.Value);
            }

            // Auditor
            if (filter.AuditorId.HasValue)
            {
                query = query.Where(x => x.AuditorId == filter.AuditorId.Value);
            }

            // State
            if (filter.StateId.HasValue)
            {
                query = query.Where(x => x.StateId == filter.StateId.Value);
            }

            // City
            if (filter.CityId.HasValue)
            {
                query = query.Where(x => x.CityId == filter.CityId.Value);
            }

            // Status Lookup
            if (filter.StatusId.HasValue)
            {
                query = query.Where(x => x.StatusId == filter.StatusId.Value);
            }

            // Active / Inactive
            if (filter.Status.HasValue)
            {
                query = query.Where(x => x.IsActive == filter.Status.Value);
            }

            // From Date
            if (filter.FromDate.HasValue)
            {
                query = query.Where(x =>
                    x.AuditDate.HasValue &&
                    x.AuditDate.Value.Date >= filter.FromDate.Value.Date);
            }

            if (filter.ToDate.HasValue)
            {
                query = query.Where(x =>
                    x.AuditDate.HasValue &&
                    x.AuditDate.Value.Date <= filter.ToDate.Value.Date);
            }

            if (filter.Done == true)
            {
                query = query.Where(x => x.Done == true);
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

        //====================================================
        // UPSERT PART AUDIT
        //====================================================

        //[HttpPost("upsert-part-audit")]
        //public async Task<IActionResult> UpsertPartAudit([FromBody] PartsAudits model)
        //{
        //    if (model.PartAuditId == 0)
        //    {
        //        model.CreatedDate = DateTime.UtcNow;
        //        model.IsActive = true;
        //        model.IsDeleted = false;

        //        _context.PartsAudits.Add(model);

        //        await _context.SaveChangesAsync();

        //        List<ParameterModel> parameters = new();

        //        // First try to get Part Master parameters
        //        if (model.PartMasterId.HasValue)
        //        {
        //            parameters = await _context.Parameters
        //                .Where(x =>
        //                    x.IsDeleted != true &&
        //                    x.IsActive == true &&
        //                    x.PartMasterId == model.PartMasterId)
        //                .ToListAsync();
        //        }

        //        // If no Part Master parameters exist, then get Part Family parameters
        //        if (parameters.Count == 0 && model.PartFamilyId.HasValue)
        //        {
        //            parameters = await _context.Parameters
        //                .Where(x =>
        //                    x.IsDeleted != true &&
        //                    x.IsActive == true &&
        //                    x.PartFamilyId == model.PartFamilyId)
        //                .ToListAsync();
        //        }

        //        foreach (var p in parameters)
        //        {
        //            _context.PartsAuditParameters.Add(new PartsAuditParameter
        //            {
        //                PartAuditId = model.PartAuditId,
        //                ParameterId = p.ParameterId,

        //                ParmeterName = p.ParmeterName,
        //                Spec = p.Spec,
        //                Min = p.Min,
        //                Max = p.Max,
        //                Method = p.Method,
        //                PartId = p.PartId,
        //                PartFamilyId = p.PartFamilyId,
        //                PartMasterId = p.PartMasterId,

        //                // Audit values start empty
        //                S1 = null,
        //                S2 = null,
        //                S3 = null,
        //                S4 = null,
        //                S5 = null,

        //                Remarks = null,
        //                Okay = false,

        //                IsActive = true,
        //                IsDeleted = false,
        //                CreatedDate = DateTime.UtcNow
        //            });
        //        }

        //        await _context.SaveChangesAsync();

        //        return Ok(new
        //        {
        //            Success = true,
        //            Message = "Part Audit Added Successfully"
        //        });
        //    }
        //    else
        //    {
        //        var dbItem = await _context.PartsAudits.FindAsync(model.PartAuditId);

        //        if (dbItem == null)
        //        {
        //            return NotFound(new
        //            {
        //                Success = false,
        //                Message = "Record Not Found"
        //            });
        //        }

        //        dbItem.CommodityId = model.CommodityId;
        //        dbItem.PartFamilyId = model.PartFamilyId;
        //        dbItem.PartMasterId = model.PartMasterId;
        //        dbItem.SupplierId = model.SupplierId;
        //        dbItem.StateId = model.StateId;
        //        dbItem.CityId = model.CityId;
        //        dbItem.AuditorId = model.AuditorId;
        //        dbItem.AuditDate = model.AuditDate;
        //        dbItem.Done = model.Done;
        //        dbItem.StatusId = model.StatusId;
        //        dbItem.Remakrs = model.Remakrs;

        //        dbItem.ModifiedBy = model.ModifiedBy;
        //        dbItem.ModifiedDate = DateTime.UtcNow;

        //        await _context.SaveChangesAsync();

        //        return Ok(new
        //        {
        //            Success = true,
        //            Message = "Part Audit Updated Successfully"
        //        });
        //    }
        //}





        [HttpPost("upsert-part-audit")]
        public async Task<IActionResult> UpsertPartAudit([FromBody] PartsAudits model)
        {
            if (model == null)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "Invalid Request"
                });
            }

            // ========================= ADD =========================

            if (model.PartAuditId == 0)
            {
                model.CreatedDate = DateTime.UtcNow;
                model.IsActive = true;
                model.IsDeleted = false;

                _context.PartsAudits.Add(model);

                await _context.SaveChangesAsync();

                await CopyAuditParameters(model.PartAuditId, model.PartMasterId, model.PartFamilyId);

                return Ok(new
                {
                    Success = true,
                    Message = "Part Audit Added Successfully"
                });
            }

            // ========================= UPDATE =========================

            var dbItem = await _context.PartsAudits
                .FirstOrDefaultAsync(x => x.PartAuditId == model.PartAuditId);

            if (dbItem == null)
            {
                return NotFound(new
                {
                    Success = false,
                    Message = "Part Audit Not Found"
                });
            }

            bool parameterChanged =
                dbItem.PartMasterId != model.PartMasterId ||
                dbItem.PartFamilyId != model.PartFamilyId;

            dbItem.CommodityId = model.CommodityId;
            dbItem.PartFamilyId = model.PartFamilyId;
            dbItem.PartMasterId = model.PartMasterId;
            dbItem.SupplierId = model.SupplierId;
            dbItem.StateId = model.StateId;
            dbItem.CityId = model.CityId;
            dbItem.AuditorId = model.AuditorId;
            dbItem.AuditDate = model.AuditDate;
            dbItem.Done = model.Done;
            dbItem.StatusId = model.StatusId;
            dbItem.Remakrs = model.Remakrs;
            dbItem.ModifiedBy = model.ModifiedBy;
            dbItem.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // If Part changed, recreate audit parameters
            if (parameterChanged)
            {
                // Soft delete old parameters
                var oldParameters = await _context.PartsAuditParameters
                    .Where(x =>
                        x.PartAuditId == model.PartAuditId &&
                        x.IsDeleted != true)
                    .ToListAsync();

                foreach (var item in oldParameters)
                {
                    item.IsDeleted = true;
                    item.IsActive = false;
                    item.ModifiedDate = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                // Copy new parameters
                await CopyAuditParameters(model.PartAuditId, model.PartMasterId, model.PartFamilyId);
            }

            return Ok(new
            {
                Success = true,
                Message = "Part Audit Updated Successfully"
            });
        }

        private async Task CopyAuditParameters(long partAuditId, long? partMasterId, long? partFamilyId)
        {
            List<ParameterModel> parameters = new();

            // First get Part Master parameters
            if (partMasterId.HasValue)
            {
                parameters = await _context.Parameters
                    .Where(x =>
                        x.IsDeleted != true &&
                        x.IsActive == true &&
                        x.PartMasterId == partMasterId)
                    .ToListAsync();
            }

            // If none, get Part Family parameters
            if (!parameters.Any() && partFamilyId.HasValue)
            {
                parameters = await _context.Parameters
                    .Where(x =>
                        x.IsDeleted != true &&
                        x.IsActive == true &&
                        x.PartFamilyId == partFamilyId)
                    .ToListAsync();
            }

            foreach (var p in parameters)
            {
                _context.PartsAuditParameters.Add(new PartsAuditParameter
                {
                    PartAuditId = partAuditId,

                    ParameterId = p.ParameterId,

                    PartId = p.PartId,
                    PartFamilyId = p.PartFamilyId,
                    PartMasterId = p.PartMasterId,

                    ParmeterName = p.ParmeterName,
                    Spec = p.Spec,
                    Min = p.Min,
                    Max = p.Max,
                    Method = p.Method,
                    UnitId=p.UnitId,

                    S1 = null,
                    S2 = null,
                    S3 = null,
                    S4 = null,
                    S5 = null,

                    Remarks = null,
                    Okay = false,

                    IsActive = true,
                    IsDeleted = false,

                    CreatedDate = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
        }
        //====================================================
        // DELETE
        //====================================================

        [HttpPost("delete")]
        public async Task<IActionResult> Delete([FromBody] PartsAudits model)
        {
            var dbItem = await _context.PartsAudits.FindAsync(model.PartAuditId);

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
                Message = "Part Audit Deleted Successfully"
            });
        }






        [HttpPost("update-status")]
        public async Task<IActionResult> UpdateStatus([FromBody] PartsAudits model)
        {
            var dbItem = await _context.PartsAudits
                .FirstOrDefaultAsync(x => x.PartAuditId == model.PartAuditId);

            if (dbItem == null)
            {
                return NotFound(new
                {
                    Success = false,
                    Message = "Record Not Found"
                });
            }

            dbItem.StatusId = model.StatusId;
            dbItem.ModifiedBy = model.ModifiedBy;
            dbItem.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Success = true,
                Message = "Status Updated Successfully"
            });
        }





        [HttpPost("done-status-change")]
        public async Task<IActionResult> DoneStatuchange([FromBody] PartsAudits model)
        {
            var dbItem = await _context.PartsAudits.FindAsync(model.PartAuditId);
            if (dbItem == null)
            {
                return NotFound(new
                {
                    Success = false,
                    Message = "Record Not Found"
                });
            }

            dbItem.Done = !(dbItem.Done ?? false);

            await _context.SaveChangesAsync();
            return Ok(new
            {
                Success = true,
                Message = "Status Updated Successfully",
                Done = dbItem.Done
            });
        }

        [HttpGet("get-audit-parameters")]
        public async Task<IActionResult> GetAuditParameters([FromQuery] ParameterFilter filter)
        {
            var data =
                from c in _context.PartsAuditCategories
                    .Where(x => x.IsActive == true && x.IsDeleted != true)

                join p in _context.PartsAuditParameters.Where(x =>
                        x.IsActive == true &&
                        x.IsDeleted != true &&
                        x.PartAuditId == filter.PartAuditId)
                on c.PartId equals p.PartId into parameterGroup

                select new
                {
                    c.PartId,
                    c.CategoryName,
                    c.CategoryCode,

                    ParametersCount = parameterGroup.Count(),

                    Parameters = parameterGroup.Select(x => new
                    {
                        x.AuditParameterId,
                        x.PartAuditId,
                        x.ParameterId,

                        x.ParmeterName,
                        x.Spec,
                        x.Min,
                        x.Max,
                        x.Method,
                        x.UnitId,
                        UnitName = _context.Lookups
        .Where(l => l.LookupId == x.UnitId)
        .Select(l => l.LookupName)
        .FirstOrDefault(),

                        x.S1,
                        x.S2,
                        x.S3,
                        x.S4,
                        x.S5,

                        x.Remarks,
                        x.Okay
                    }).ToList()
                };

            var result = await data
                .OrderBy(x => x.CategoryName)
                .ToListAsync();

            return Ok(new
            {
                Success = true,
                Data = result
            });
        }

        [HttpPost("upsert-audit-parameter")]
        public async Task<IActionResult> UpsertAuditParameter([FromBody] AuditParameterModel model)
        {
            if (model == null)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "Invalid request."
                });
            }

            PartsAuditParameter entity;

            if (model.AuditParameterId > 0)
            {
                entity = await _context.PartsAuditParameters
                    .FirstOrDefaultAsync(x => x.AuditParameterId == model.AuditParameterId);

                if (entity == null)
                {
                    return NotFound(new
                    {
                        Success = false,
                        Message = "Parameter not found."
                    });
                }

                entity.ParmeterName = model.ParmeterName;
                entity.Spec = model.Spec;
                entity.Min = model.Min;
                entity.Max = model.Max;
                entity.Method = model.Method;

                entity.S1 = model.S1;
                entity.S2 = model.S2;
                entity.S3 = model.S3;
                entity.S4 = model.S4;
                entity.S5 = model.S5;

                entity.Remarks = model.Remarks;
                entity.Okay = model.Okay;

                entity.PartAuditId = model.PartAuditId;
                entity.PartId = model.PartId;
                entity.PartFamilyId = model.PartFamilyId;
                entity.PartMasterId = model.PartMasterId;
                entity.UnitId = model.UnitId;

                entity.ModifiedDate = DateTime.UtcNow;
            }
            else
            {
                entity = new PartsAuditParameter
                {
                    PartAuditId = model.PartAuditId,
                    ParameterId = model.ParameterId,

                    PartId = model.PartId,
                    PartFamilyId = model.PartFamilyId,
                    PartMasterId = model.PartMasterId,

                    ParmeterName = model.ParmeterName,
                    Spec = model.Spec,
                    Min = model.Min,
                    Max = model.Max,
                    Method = model.Method,
                    UnitId = model.UnitId,

                    S1 = model.S1,
                    S2 = model.S2,
                    S3 = model.S3,
                    S4 = model.S4,
                    S5 = model.S5,

                    Remarks = model.Remarks,
                    Okay = model.Okay,

                    IsActive = true,
                    IsDeleted = false,
                    CreatedDate = DateTime.UtcNow
                };

                _context.PartsAuditParameters.Add(entity);
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Success = true,
                Message = model.AuditParameterId > 0
                    ? "Parameter updated successfully."
                    : "Parameter added successfully."
            });
        }




        [HttpPost("save-user-gridcolumns")]
        public async Task<IActionResult> SaveUserGridColumns([FromBody] UserGridColumnsModel model)
        {
            if (model == null)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "Invalid Request."
                });
            }

            var existing = await _context.UserGridSettings
                .FirstOrDefaultAsync(x =>
                    x.UserId == model.UserId &&
                    x.GridType == model.GridType &&
                    x.IsDeleted != true);

            if (existing == null)
            {
                var entity = new UserGridSetting
                {
                    UserId = model.UserId,
                    GridType = model.GridType,
                    SelectedColumnsJSON = model.SelectedColumnsJSON,

                    IsActive = true,
                    IsDeleted = false,

                    CreatedDate = DateTime.UtcNow
                };

                _context.UserGridSettings.Add(entity);
            }
            else
            {
                existing.SelectedColumnsJSON = model.SelectedColumnsJSON;
                existing.ModifiedDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Success = true,
                Message = "Columns saved successfully."
            });
        }


        [HttpGet("get-user-gridcolumns")]
        public async Task<IActionResult> GetUserGridColumns(
    [FromQuery] long userId,
    [FromQuery] string gridType)
        {
            var data = await _context.UserGridSettings
                .Where(x =>
                    x.UserId == userId &&
                    x.GridType == gridType &&
                    x.IsDeleted != true)
                .Select(x => new
                {
                    x.SettingId,
                    x.UserId,
                    x.GridType,
                    x.SelectedColumnsJSON
                })
                .FirstOrDefaultAsync();

            return Ok(new
            {
                Success = true,
                Data = data
            });
        }





       
    }
}