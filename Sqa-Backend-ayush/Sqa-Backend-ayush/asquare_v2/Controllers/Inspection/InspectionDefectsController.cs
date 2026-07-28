using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sqa_core.Data;
using sqa_core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;

namespace sqa_core.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InspectionDefectsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public InspectionDefectsController(AppDbContext context)
        {
            _context = context;
        }



        //hhhhh


        [HttpGet("GetDefectsByInspection/{inspectionId}")]
        public async Task<IActionResult> GetDefectsByInspection(long inspectionId)
        {
            try
            {
                var inspectionDefect = await _context.InspectionDefects
                    .FirstOrDefaultAsync(d => d.InspectionId == inspectionId);

                if (inspectionDefect == null || string.IsNullOrEmpty(inspectionDefect.DefectsId))
                    return Ok(new { Success = true, Message = "No defects mapped.", Data = new List<object>() });

                // FIX 1: Deserialize as List<long> to match BIGINT in the database
                var defectIds = JsonSerializer.Deserialize<List<long>>(inspectionDefect.DefectsId);

                var defectStatuses = string.IsNullOrEmpty(inspectionDefect.Status)
                    ? new Dictionary<string, int>()
                    : JsonSerializer.Deserialize<Dictionary<string, int>>(inspectionDefect.Status);

                // FIX 2: Remove the (int) cast. EF Core will now successfully translate this to SQL
                var defectDetails = await _context.DefectMasters
                    .Where(d => defectIds.Contains(d.DefectId) && d.IsDeleted == false)
                    .Select(d => new { d.DefectId, d.DefectName })
                    .ToListAsync();

                var result = defectDetails.Select(d => new
                {
                    DefectId = d.DefectId,
                    DefectName = d.DefectName,
                    Status = defectStatuses.ContainsKey(d.DefectId.ToString()) ? defectStatuses[d.DefectId.ToString()] : 5
                });

                return Ok(new { Success = true, Data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        public class UpdateDefectsDto
        {
            public long InspectionId { get; set; }
            public Dictionary<string, int> Statuses { get; set; }
        }

        [HttpPost("UpdateDefectsStatus")]
        public async Task<IActionResult> UpdateDefectsStatus([FromBody] UpdateDefectsDto dto)
        {
            try
            {
                var inspectionDefect = await _context.InspectionDefects
                    .FirstOrDefaultAsync(d => d.InspectionId == dto.InspectionId);

                if (inspectionDefect == null)
                    return NotFound(new { Success = false, Message = "Mapping not found for this inspection." });

                // Update the JSON status dictionary
                inspectionDefect.Status = JsonSerializer.Serialize(dto.Statuses);

                _context.InspectionDefects.Update(inspectionDefect);
                await _context.SaveChangesAsync();

                return Ok(new { Success = true, Message = "Defects updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }
    }
}