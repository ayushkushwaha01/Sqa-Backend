using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sqa_core.Data;
using sqa_core.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;

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

        //[HttpGet("GetParametersByInspectionId/{inspectionId}")]
        //public async Task<IActionResult> GetParametersByInspectionId(int inspectionId) //ff
        //{
        //    try
        //    {
        //        var query = await (from r in _context.Inspectionrefs
        //                           where r.InspectionId == inspectionId && r.IsDeleted == false

        //                           // Join Part Master
        //                           join pm in _context.PartMasters
        //                           on r.PartNameId equals pm.PartMasterId into pmJoin
        //                           from pm in pmJoin.DefaultIfEmpty()

        //                               // Join Part Family
        //                           join pf in _context.PartFamilies
        //                           on r.PartFamilyId equals pf.PartFamilyId into pfJoin
        //                           from pf in pfJoin.DefaultIfEmpty()

        //                               // Join Audit Category (adjust 'PartsAuditCategories' to your actual DbSet name)
        //                           join cat in _context.PartsAuditCategories
        //                           on r.PartId equals cat.PartId into catJoin
        //                           from cat in catJoin.DefaultIfEmpty()


        //                           join u in _context.Lookups
        //on r.Unit equals u.LookupId.ToString() into unitJoin
        //                           from u in unitJoin.DefaultIfEmpty()

        //                           select new
        //                           {
        //                               Id = r.InspectionRefId,
        //                               Parameter = r.ParameterName,
        //                               PartName = pm != null ? pm.PartMasterName : null,
        //                               PartFamily = pf != null ? pf.PartFamilyName : null,
        //                               CategoryName = cat != null ? cat.CategoryName : "Uncategorized",

        //                               PartId = r.PartId,
        //                               PartFamilyId = r.PartFamilyId,
        //                               PartNameId = r.PartNameId,

        //                               Spec = r.Spec,
        //                               Unit = u != null ? u.LookupName : null,   // <-- Return LookupName
        //                               Min = r.Min,
        //                               Max = r.Max,
        //                               Defects = r.Defects,
        //                               Okay = r.Okay,
        //                               Capa = r.CAPA,
        //                               Method = r.Method,
        //                               S1 = r.S1,
        //                               S2 = r.S2,
        //                               S3 = r.S3,
        //                               S4 = r.S4,
        //                               S5 = r.S5,
        //                               Remarks = r.Remarks
        //                           }).ToListAsync();

        //        if (!query.Any())
        //        {
        //            return Ok(new { success = true, data = new string[] { }, message = "No records found for this inspection." });
        //        }

        //        return Ok(new { success = true, data = query, message = "Records fetched successfully." });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { success = false, message = "An error occurred while fetching records.", error = ex.Message });
        //    }
        //}















        [HttpGet("GetParametersByInspectionId/{inspectionId}")]
        public async Task<IActionResult> GetParametersByInspectionId(int inspectionId)
        {
            try
            {
                // 1. Fetch data from DB
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

                                       // Join Audit Category 
                                   join cat in _context.PartsAuditCategories
                                   on r.PartId equals cat.PartId into catJoin
                                   from cat in catJoin.DefaultIfEmpty()

                                   join u in _context.Lookups
                                   on r.Unit equals u.LookupId.ToString() into unitJoin
                                   from u in unitJoin.DefaultIfEmpty()

                                   select new
                                   {
                                       Id = r.InspectionRefId,
                                       Parameter = r.ParameterName,
                                       PartName = pm != null ? pm.PartMasterName : null,
                                       PartFamily = pf != null ? pf.PartFamilyName : null,
                                       CategoryName = cat != null ? cat.CategoryName : "Uncategorized",

                                       PartId = r.PartId,
                                       PartFamilyId = r.PartFamilyId,
                                       PartNameId = r.PartNameId,

                                       Spec = r.Spec,
                                       Unit = u != null ? u.LookupName : null,
                                       Min = r.Min,
                                       Max = r.Max,
                                       DefectsRaw = r.Defects,             // Still fetch JSON string
                                       DefectRate = r.DefectRate,          // <-- FETCH DIRECTLY FROM DB
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

                // 2. Process Defects count in memory, but pass DefectRate through directly
                var processedData = query.Select(item =>
                {
                    int defectCount = 0;

                    // Only calculate the defect *count* from the JSON array
                    if (!string.IsNullOrEmpty(item.DefectsRaw) &&
                        !string.IsNullOrEmpty(item.Min) &&
                        !string.IsNullOrEmpty(item.Max))
                    {
                        if (double.TryParse(item.Min, out double minVal) && double.TryParse(item.Max, out double maxVal))
                        {
                            try
                            {
                                var values = JsonSerializer.Deserialize<List<string>>(item.DefectsRaw);

                                if (values != null && values.Count > 0)
                                {
                                    foreach (var valStr in values)
                                    {
                                        if (double.TryParse(valStr, out double val))
                                        {
                                            if (val < minVal || val > maxVal)
                                            {
                                                defectCount++;
                                            }
                                        }
                                    }
                                }
                            }
                            catch
                            {
                                // Silently handle JSON format issues
                            }
                        }
                    }

                    return new
                    {
                        Id = item.Id,
                        Parameter = item.Parameter,
                        PartName = item.PartName,
                        PartFamily = item.PartFamily,
                        CategoryName = item.CategoryName,
                        PartId = item.PartId,
                        PartFamilyId = item.PartFamilyId,
                        PartNameId = item.PartNameId,
                        Spec = item.Spec,
                        Unit = item.Unit,
                        Min = item.Min,
                        Max = item.Max,
                        Defects = defectCount.ToString(),   // Converted to numeric count
                        DefectRate = string.IsNullOrEmpty(item.DefectRate) ? "0%" : item.DefectRate, // <-- USE DB VALUE
                        Okay = item.Okay,
                        Capa = item.Capa,
                        Method = item.Method,
                        S1 = item.S1,
                        S2 = item.S2,
                        S3 = item.S3,
                        S4 = item.S4,
                        S5 = item.S5,
                        Remarks = item.Remarks
                    };
                });

                return Ok(new { success = true, data = processedData, message = "Records fetched successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while fetching records.", error = ex.Message });
            }
        }









        //[HttpPost("AddOrUpdateParameter")]
        //public async Task<IActionResult> AddOrUpdateParameter([FromBody] InspectionRef model)
        //{
        //    try
        //    {
        //        if (model.InspectionRefId == 0)
        //        {
        //            model.CreatedDate = DateTime.Now;
        //            model.IsActive = true;
        //            model.IsDeleted = false;
        //            model.Okay = true;

        //            _context.Inspectionrefs.Add(model);
        //        }
        //        else
        //        {
        //            var existing = await _context.Inspectionrefs.FindAsync(model.InspectionRefId);
        //            if (existing == null)
        //                return NotFound(new { success = false, message = "Record not found" });

        //            // Update existing fields
        //            existing.ParameterName = model.ParameterName;
        //            existing.Spec = model.Spec;
        //            existing.Min = model.Min;
        //            existing.Max = model.Max;
        //            existing.Method = model.Method;
        //            existing.S1 = model.S1;
        //            existing.S2 = model.S2;
        //            existing.S3 = model.S3;
        //            existing.S4 = model.S4;
        //            existing.S5 = model.S5;
        //            existing.Remarks = model.Remarks;
        //            existing.ModifiedDate = DateTime.Now;

        //            _context.Inspectionrefs.Update(existing);
        //        }

        //        await _context.SaveChangesAsync();
        //        return Ok(new { success = true, message = "Parameter saved successfully." });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { success = false, message = "Error saving parameter", error = ex.Message });
        //    }
        //}









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
                    model.Okay = true;

                    // Convert the numeric UnitId to a string and store it in the Unit column
                    model.Unit = model.UnitId.ToString();

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

                    // Convert and update the Unit column
                    existing.Unit = model.UnitId.ToString();

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
















        public class BulkSampleUpdateDto
        {
            public int InspectionRefId { get; set; }
            public string SampleNumber { get; set; }
            public string? Value { get; set; }
        }

        [HttpPost("UpdateSamples")]
        public async Task<IActionResult> UpdateSamples([FromBody] JsonElement models)
        {
            try
            {

                foreach (var model in models.EnumerateArray())
                {

                    long refId = model.GetProperty("inspectionRefId").GetInt64();
                    string sampleNumber = model.GetProperty("sampleNumber").GetString();


                    string? value = model.GetProperty("value").ValueKind == JsonValueKind.Null
                                    ? null
                                    : model.GetProperty("value").GetString();


                    var existing = await _context.Inspectionrefs.FindAsync(refId);

                    if (existing != null)
                    {

                        switch (sampleNumber?.ToLower())
                        {
                            case "s1": existing.S1 = value; break;
                            case "s2": existing.S2 = value; break;
                            case "s3": existing.S3 = value; break;
                            case "s4": existing.S4 = value; break;
                            case "s5": existing.S5 = value; break;
                        }

                        existing.ModifiedDate = DateTime.Now;
                        _context.Inspectionrefs.Update(existing);
                    }
                }

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Samples updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error updating samples", error = ex.Message });
            }
        }




        [HttpPut("toggle-ok/{id}")]
        public async Task<IActionResult> TogglePublish(long id, bool status)
        {
            var dbItem = await _context.Inspectionrefs.FindAsync(id);
            if (dbItem == null)
                return NotFound(new { Message = "Record not found", Success = false });

            // Explicitly set the value to whatever the Angular checkbox says
            dbItem.Okay = status;

            dbItem.ModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Record OK Status updated.", Success = true });
        }


        //28-june changes 
        [HttpDelete("DeleteParameter/{id}")]
        public async Task<IActionResult> DeleteParameter(long id)
        {
            try
            {
                var existing = await _context.Inspectionrefs.FindAsync(id);
                if (existing == null)
                    return NotFound(new { success = false, message = "Record not found" });

               
                existing.IsDeleted = true;
                existing.DeletedDate = DateTime.Now;
                existing.IsActive = false;  

                _context.Inspectionrefs.Update(existing);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Parameter deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error deleting parameter", error = ex.Message });
            }
        }










        // Add this DTO class inside or outside the controller
        public class UpdateDefectsListDto
        {
            public long InspectionRefId { get; set; }
            public List<string> DefectsList { get; set; }
        }

        //[HttpPost("UpdateDefectsList")]
        //public async Task<IActionResult> UpdateDefectsList([FromBody] UpdateDefectsListDto dto)
        //{
        //    try
        //    {
        //        // Find the existing record
        //        var existingRecord = await _context.Inspectionrefs.FindAsync(dto.InspectionRefId);
        //        if (existingRecord == null)
        //            return NotFound(new { success = false, message = "Record not found." });

        //        // Serialize the List<string> into a JSON array string e.g., '["Defect A", "Defect B"]'
        //        existingRecord.Defects = JsonSerializer.Serialize(dto.DefectsList);
        //        existingRecord.ModifiedDate = DateTime.Now;

        //        _context.Inspectionrefs.Update(existingRecord);
        //        await _context.SaveChangesAsync();

        //        return Ok(new { success = true, message = "Defects uploaded successfully." });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { success = false, message = "Error saving defects", error = ex.Message });
        //    }
        //}



        //[HttpPost("UpdateDefectsList")]
        //public async Task<IActionResult> UpdateDefectsList([FromBody] UpdateDefectsListDto dto)
        //{
        //    try
        //    {
        //        // 1. Find the existing record
        //        var existingRecord = await _context.Inspectionrefs.FindAsync(dto.InspectionRefId);
        //        if (existingRecord == null)
        //            return NotFound(new { success = false, message = "Record not found." });

        //        // 2. Serialize the List<string> into a JSON array string
        //        existingRecord.Defects = JsonSerializer.Serialize(dto.DefectsList);

        //        // 3. Variables to calculate the Defect Rate
        //        int defectCount = 0;
        //        int validSamplesCount = 0;
        //        string defectRateStr = "0%";

        //        // Pool all standard sample cells (S1 - S5) currently in the DB
        //        var allSamples = new List<string>();
        //        if (!string.IsNullOrWhiteSpace(existingRecord.S1)) allSamples.Add(existingRecord.S1);
        //        if (!string.IsNullOrWhiteSpace(existingRecord.S2)) allSamples.Add(existingRecord.S2);
        //        if (!string.IsNullOrWhiteSpace(existingRecord.S3)) allSamples.Add(existingRecord.S3);
        //        if (!string.IsNullOrWhiteSpace(existingRecord.S4)) allSamples.Add(existingRecord.S4);
        //        if (!string.IsNullOrWhiteSpace(existingRecord.S5)) allSamples.Add(existingRecord.S5);

        //        // Add the newly uploaded JSON defects to the same pool for evaluation
        //        if (dto.DefectsList != null && dto.DefectsList.Any())
        //        {
        //            allSamples.AddRange(dto.DefectsList);
        //        }

        //        // 4. Only process if valid numeric limits exist
        //        if (!string.IsNullOrWhiteSpace(existingRecord.Min) && !string.IsNullOrWhiteSpace(existingRecord.Max))
        //        {
        //            if (double.TryParse(existingRecord.Min, out double minVal) && double.TryParse(existingRecord.Max, out double maxVal))
        //            {
        //                foreach (var valStr in allSamples)
        //                {
        //                    // Ensure the sample is an actual number before evaluating
        //                    if (double.TryParse(valStr, out double val))
        //                    {
        //                        validSamplesCount++;

        //                        // It is a defect if it lands strictly OUTSIDE the min/max bounds
        //                        if (val < minVal || val > maxVal)
        //                        {
        //                            defectCount++;
        //                        }
        //                    }
        //                }

        //                // Calculate final percentage based on the combined total of samples
        //                if (validSamplesCount > 0)
        //                {
        //                    double rate = ((double)defectCount / validSamplesCount) * 100;
        //                    defectRateStr = $"{Math.Round(rate, 1)}%";
        //                }
        //            }
        //        }

        //        // 5. Save the final calculated rate and modified date
        //        existingRecord.DefectRate = defectRateStr;
        //        existingRecord.ModifiedDate = DateTime.Now;

        //        _context.Inspectionrefs.Update(existingRecord);
        //        await _context.SaveChangesAsync();

        //        return Ok(new { success = true, message = "Defects uploaded successfully." });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { success = false, message = "Error saving defects", error = ex.Message });
        //    }
        //}




        [HttpPost("UpdateDefectsList")]
        public async Task<IActionResult> UpdateDefectsList([FromBody] UpdateDefectsListDto dto)
        {
            try
            {
                // 1. Find the existing record
                var existingRecord = await _context.Inspectionrefs.FindAsync(dto.InspectionRefId);
                if (existingRecord == null)
                    return NotFound(new { success = false, message = "Record not found." });

                // 2. Serialize the List<string> into a JSON array string
                existingRecord.Defects = JsonSerializer.Serialize(dto.DefectsList);

                // 3. Variables for calculation
                int recedingDefects = 0;
                int exceedingDefects = 0;
                int totalSamplesCount = 0;
                string defectRateStr = "0%";

                // 4. ONLY evaluate the values passed in the payload (Ignore S1-S5)
                if (dto.DefectsList != null && dto.DefectsList.Any())
                {
                    if (!string.IsNullOrWhiteSpace(existingRecord.Min) && !string.IsNullOrWhiteSpace(existingRecord.Max))
                    {
                        if (double.TryParse(existingRecord.Min, out double minVal) && double.TryParse(existingRecord.Max, out double maxVal))
                        {
                            foreach (var valStr in dto.DefectsList)
                            {
                                // Ensure the sample is an actual number before evaluating
                                if (double.TryParse(valStr, out double val))
                                {
                                    totalSamplesCount++; // Count this as a valid sample

                                    // Check if it recedes (below min) or exceeds (above max)
                                    if (val < minVal)
                                    {
                                        recedingDefects++;
                                    }
                                    else if (val > maxVal)
                                    {
                                        exceedingDefects++;
                                    }
                                }
                            }

                            // Formula: (Exceeding + Receding) / Total * 100
                            if (totalSamplesCount > 0)
                            {
                                double rate = ((double)(exceedingDefects + recedingDefects) / totalSamplesCount) * 100;
                                defectRateStr = $"{Math.Round(rate, 1)}%";
                            }
                        }
                    }
                }

                // 5. Save the final calculated rate and modified date
                existingRecord.DefectRate = defectRateStr;
                existingRecord.ModifiedDate = DateTime.Now;

                _context.Inspectionrefs.Update(existingRecord);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Defects uploaded successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error saving defects", error = ex.Message });
            }
        }








        [HttpGet("GetDefectsByInspectionRefId/{inspectionRefId}")]
        public async Task<IActionResult> GetDefectsByInspectionRefId(long inspectionRefId)
        {
            try
            {
                // 1. Find the existing record by InspectionRefId
                var existingRecord = await _context.Inspectionrefs.FindAsync(inspectionRefId);

                if (existingRecord == null)
                {
                    return NotFound(new { success = false, message = "Record not found." });
                }

                // 2. Check if defects exist
                if (string.IsNullOrWhiteSpace(existingRecord.Defects))
                {
                    return Ok(new { success = true, data = new List<string>(), message = "No defects found." });
                }

                // 3. Deserialize the JSON string back to a List<string>
                List<string> defectsList;
                try
                {
                    defectsList = JsonSerializer.Deserialize<List<string>>(existingRecord.Defects);
                }
                catch (JsonException)
                {
                    // Fallback in case the data in the DB is corrupted or not a valid JSON array
                    return StatusCode(500, new { success = false, message = "Failed to parse defects data from the database." });
                }

                return Ok(new { success = true, data = defectsList, message = "Defects fetched successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while fetching defects.", error = ex.Message });
            }
        }

    }
}