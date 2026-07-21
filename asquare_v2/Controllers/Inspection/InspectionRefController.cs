using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sqa_core.Data;
using sqa_core.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace sqa_core.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InspectionRefController : ControllerBase
    {
        private readonly AppDbContext _context;

        public InspectionRefController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("GetParametersByInspectionId/{inspectionId}")]
        public async Task<IActionResult> GetParametersByInspectionId(int inspectionId)
        {
            try
            {
                var query = await (from r in _context.Inspectionrefs
                                   where r.InspectionId == inspectionId && r.IsDeleted == false

                                   // Join Part Master
                                   join pm in _context.PartMasters
                                   on r.PartNameId equals pm.PartMasterId into pmJoin
                                   from pm in pmJoin.DefaultIfEmpty()

                                       // Join Part Family
                                   join pf in _context.PartFamilies
                                   on r.PartFamilyId equals pf.PartFamilyId into pfJoin
                                   from pf in pfJoin.DefaultIfEmpty()

                                       // Join Audit Category (adjust 'PartsAuditCategories' to your actual DbSet name)
                                   join cat in _context.PartsAuditCategories
                                   on r.PartId equals cat.PartId into catJoin
                                   from cat in catJoin.DefaultIfEmpty()

                                   select new
                                   {
                                       Id = r.InspectionRefId,
                                       Parameter = r.ParameterName,
                                       PartName = pm != null ? pm.PartMasterName : null,
                                       PartFamily = pf != null ? pf.PartFamilyName : null,
                                       CategoryName = cat != null ? cat.CategoryName : "Uncategorized",
                                       PartId = r.PartId,             // NEW
                                       PartFamilyId = r.PartFamilyId, // NEW
                                       PartNameId = r.PartNameId,     // NEW
                                       Spec = r.Spec,
                                       Unit = r.Unit,
                                       Min = r.Min,
                                       Max = r.Max,
                                       Defects = r.Defects,
                                       Okay = r.Okay,
                                       Capa = r.CAPA,
                                       Method = r.Method,
                                       S1 = r.S1,
                                       S2 = r.S2,
                                       S3 = r.S3,
                                       S4 = r.S4,
                                       S5 = r.S5,
                                       Remarks = r.Remarks
                                   }).ToListAsync();

                if (!query.Any())
                {
                    return Ok(new { success = true, data = new string[] { }, message = "No records found for this inspection." });
                }

                return Ok(new { success = true, data = query, message = "Records fetched successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while fetching records.", error = ex.Message });
            }
        }




        [HttpPost("AddOrUpdateParameter")]
        public async Task<IActionResult> AddOrUpdateParameter([FromBody] InspectionRef model)
        {
            try
            {
                if (model.InspectionRefId == 0)
                {
                    model.CreatedDate = DateTime.Now;
                    model.IsActive = true;
                    model.IsDeleted = false;

                    _context.Inspectionrefs.Add(model);
                }
                else
                {
                    var existing = await _context.Inspectionrefs.FindAsync(model.InspectionRefId);
                    if (existing == null)
                        return NotFound(new { success = false, message = "Record not found" });

                    // Update existing fields
                    existing.ParameterName = model.ParameterName;
                    existing.Spec = model.Spec;
                    existing.Min = model.Min;
                    existing.Max = model.Max;
                    existing.Method = model.Method;
                    existing.S1 = model.S1;
                    existing.S2 = model.S2;
                    existing.S3 = model.S3;
                    existing.S4 = model.S4;
                    existing.S5 = model.S5;
                    existing.Remarks = model.Remarks;
                    existing.ModifiedDate = DateTime.Now;

                    _context.Inspectionrefs.Update(existing);
                }

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Parameter saved successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error saving parameter", error = ex.Message });
            }
        }
    }
}