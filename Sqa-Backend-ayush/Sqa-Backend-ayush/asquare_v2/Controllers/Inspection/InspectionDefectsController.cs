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

        //public class UpdateDefectsDto
        //{
        //    public long InspectionId { get; set; }
        //    public Dictionary<string, int> Statuses { get; set; }
        //}

        //[HttpPost("UpdateDefectsStatus")]
        //public async Task<IActionResult> UpdateDefectsStatus([FromBody] UpdateDefectsDto dto)
        //{
        //    try
        //    {
        //        var inspectionDefect = await _context.InspectionDefects
        //            .FirstOrDefaultAsync(d => d.InspectionId == dto.InspectionId);

        //        if (inspectionDefect == null)
        //            return NotFound(new { Success = false, Message = "Mapping not found for this inspection." });

        //        // Update the JSON status dictionary
        //        inspectionDefect.Status = JsonSerializer.Serialize(dto.Statuses);

        //        _context.InspectionDefects.Update(inspectionDefect);
        //        await _context.SaveChangesAsync();

        //        return Ok(new { Success = true, Message = "Defects updated successfully" });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { Success = false, Message = ex.Message });
        //    }
        //}














        public class UpdateDefectsDto
        {
            public long InspectionId { get; set; }
            public Dictionary<string, int> Statuses { get; set; }

            // Optional: Add this if you want to track WHO modified the record
            // public string ModifiedBy { get; set; } 
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

                 
                inspectionDefect.ModifiedDate = DateTime.Now;
                // inspectionDefect.ModifiedBy = dto.ModifiedBy; // Uncomment if you add this to your DTO

                _context.InspectionDefects.Update(inspectionDefect);
                await _context.SaveChangesAsync();

                return Ok(new { Success = true, Message = "Defects updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }















        [HttpGet("GetDefectStats/{year}/{month}")]
        public async Task<IActionResult> GetDefectStats(int year, int month)
        {
            try
            {
                // 1. Fetch valid records for the given month and year
                var records = await _context.InspectionDefects
                    .Where(d => d.CreatedDate.Year == year &&
                                d.CreatedDate.Month == month &&
                                d.IsDeleted == false &&
                                d.IsActive == true)
                    .ToListAsync();

                if (!records.Any())
                    return Ok(new { Success = true, Message = "No defects found for this period.", Data = new List<object>() });

                // 2. Dictionary to aggregate count and total status per DefectId
                // Key = DefectId, Value = (Count, TotalStatus)
                var defectAggregates = new Dictionary<long, (int Count, int TotalStatus)>();

                foreach (var record in records)
                {
                    if (!string.IsNullOrEmpty(record.Status))
                    {
                        try
                        {
                            // Parse JSON like {"2": 5, "3": 1} as seen in image_0f7af2.png
                            var statuses = JsonSerializer.Deserialize<Dictionary<string, int>>(record.Status);
                            if (statuses != null)
                            {
                                foreach (var kvp in statuses)
                                {
                                    if (long.TryParse(kvp.Key, out long defectId))
                                    {
                                        if (!defectAggregates.ContainsKey(defectId))
                                        {
                                            defectAggregates[defectId] = (0, 0);
                                        }

                                        var current = defectAggregates[defectId];
                                        // Increment count by 1, add status to total
                                        defectAggregates[defectId] = (current.Count + 1, current.TotalStatus + kvp.Value);
                                    }
                                }
                            }
                        }
                        catch
                        {
                            // Skip records with malformed JSON safely
                            continue;
                        }
                    }
                }

                if (!defectAggregates.Any())
                    return Ok(new { Success = true, Data = new List<object>() });

                // 3. Fetch Defect Names from DefectMasters
                var defectIds = defectAggregates.Keys.ToList();
                var defectMasters = await _context.DefectMasters
                    .Where(d => defectIds.Contains(d.DefectId) && d.IsDeleted == false)
                    .ToDictionaryAsync(d => d.DefectId, d => d.DefectName);

                // 4. Calculate Average and prepare final response
                var result = defectAggregates.Select(da => new
                {
                    DefectId = da.Key,
                    DefectName = defectMasters.ContainsKey(da.Key) ? defectMasters[da.Key] : $"Unknown ({da.Key})",
                    Count = da.Value.Count,
                    // Calculate exact average, e.g., (1+2+3)/3 = 2
                    AverageStatus = Math.Round((double)da.Value.TotalStatus / da.Value.Count, 1)
                })
                .OrderByDescending(x => x.Count) // Optional: order by highest count first
                .ToList();

                return Ok(new { Success = true, Data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }
    }
}