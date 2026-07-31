using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sqa_core.Data;
using sqa_core.Models;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace sqa_core.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DataTableController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DataTableController(AppDbContext context)
        {
            _context = context;
        }



        //[HttpGet("get-all-records")]
        //public async Task<IActionResult> GetAllInspections()
        //{
        //    try
        //    {
        //        // 1. Fetch base inspection data from the database
        //        var rawData = await (from i in _context.Inspections
        //                             join stage in _context.Lookups on i.StageId equals stage.LookupId into stageGroup
        //                             from stage in stageGroup.DefaultIfEmpty()
        //                             join shift in _context.Lookups on i.ShiftId equals shift.LookupId into shiftGroup
        //                             from shift in shiftGroup.DefaultIfEmpty()
        //                             join inspector in _context.Users on i.InspectorId equals inspector.UserId into inspectorGroup
        //                             from inspector in inspectorGroup.DefaultIfEmpty()
        //                             join partFamily in _context.PartFamilies on i.PartFamilyId equals partFamily.PartFamilyId into pfGroup
        //                             from partFamily in pfGroup.DefaultIfEmpty()
        //                             join partCode in _context.PartMasters on i.PartCodeId equals partCode.PartMasterId into pcGroup
        //                             from partCode in pcGroup.DefaultIfEmpty()
        //                             join batch in _context.BatchMasters on i.BatchNumberId equals batch.BatchId into batchGroup
        //                             from batch in batchGroup.DefaultIfEmpty()
        //                             where i.IsDeleted != true && i.IsArchive != true
        //                             orderby i.CreatedDate descending
        //                             select new
        //                             {
        //                                 i.InspectionId,
        //                                 i.ReferenceId,
        //                                 i.InspectionDate,
        //                                 i.Time,
        //                                 i.Remarks,
        //                                 i.ErrorRate,
        //                                 i.Publish,
        //                                 i.BatchQuantity,
        //                                 i.SampleQuantity,
        //                                 i.StageId,
        //                                 i.SupplierId,
        //                                 i.ShiftId,
        //                                 i.InspectorId,
        //                                 i.PartFamilyId,
        //                                 i.PartCodeId,
        //                                 i.BatchNumberId,
        //                                 StageName = stage != null ? stage.LookupName : null,
        //                                 ShiftName = shift != null ? shift.LookupName : null,
        //                                 InspectorName = inspector != null ? inspector.UserName : null,
        //                                 PartFamilyName = partFamily != null ? partFamily.PartFamilyName : null,
        //                                 PartMasterCode = partCode != null ? partCode.PartMasterCode : null,
        //                                 BatchNumber = batch != null ? batch.BatchNumber : null
        //                             }).ToListAsync();

        //        var inspectionIds = rawData.Select(x => x.InspectionId).ToList();


        //        var paramCounts = await _context.Inspectionrefs
        //            .Where(r => inspectionIds.Contains(r.InspectionId) && r.IsDeleted != true)
        //            .GroupBy(r => r.InspectionId)
        //            .Select(g => new { InspectionId = g.Key, Count = g.Count() })
        //            .ToDictionaryAsync(k => k.InspectionId, v => v.Count);


        //        var defectsData = await _context.InspectionDefects
        //            .Where(d => inspectionIds.Contains(d.InspectionId))
        //            .ToDictionaryAsync(k => k.InspectionId, v => v.Status);


        //        var finalData = rawData.Select(d =>
        //        {
        //            // Extract Parameter count
        //            int pCount = paramCounts.ContainsKey(d.InspectionId) ? paramCounts[d.InspectionId] : 0;


        //            string defectsFraction = "0/0";
        //            if (defectsData.ContainsKey(d.InspectionId) && !string.IsNullOrEmpty(defectsData[d.InspectionId]))
        //            {
        //                try
        //                {
        //                    var statusDict = JsonSerializer.Deserialize<Dictionary<string, int>>(defectsData[d.InspectionId]);
        //                    if (statusDict != null && statusDict.Count > 0)
        //                    {
        //                        int totalDefects = statusDict.Count;
        //                        int redcount = statusDict.Values.Count(v => v == 5); // 1 = Green Status
        //                        defectsFraction = $"{redcount}/{totalDefects}";
        //                    }
        //                }
        //                catch { /* Ignore invalid JSON */ }
        //            }

        //            return new
        //            {
        //                inspectionId = d.InspectionId,
        //                referenceId = d.ReferenceId,
        //                inspectionDate = d.InspectionDate,
        //                time = d.Time,
        //                remarks = d.Remarks,
        //                defects = defectsFraction,          // Overrides the DB NULL with dynamic string
        //                parameters = pCount.ToString(),     // Overrides the DB NULL with dynamic count
        //                errorRate = d.ErrorRate,
        //                publish = d.Publish,
        //                batchQuantity = d.BatchQuantity,
        //                sampleQuantity = d.SampleQuantity,
        //                stageId = d.StageId,
        //                supplierId = d.SupplierId,
        //                shiftId = d.ShiftId,
        //                inspectorId = d.InspectorId,
        //                partFamilyId = d.PartFamilyId,
        //                partCodeId = d.PartCodeId,
        //                batchNumberId = d.BatchNumberId,
        //                stageName = d.StageName,
        //                shiftName = d.ShiftName,
        //                inspectorName = d.InspectorName,
        //                partFamilyName = d.PartFamilyName,
        //                partMasterCode = d.PartMasterCode,
        //                batchNumber = d.BatchNumber
        //            };
        //        }).ToList();

        //        return Ok(new { Data = finalData, Success = true });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { Success = false, Message = ex.Message });
        //    }
        //}




























        [HttpGet("get-all-records")]
        public async Task<IActionResult> GetAllInspections()
        {
            try
            {
                // 1. Fetch base inspection data from the database
                var rawData = await (from i in _context.Inspections
                                     join stage in _context.Lookups on i.StageId equals stage.LookupId into stageGroup
                                     from stage in stageGroup.DefaultIfEmpty()
                                     join shift in _context.Lookups on i.ShiftId equals shift.LookupId into shiftGroup
                                     from shift in shiftGroup.DefaultIfEmpty()
                                     join inspector in _context.Users on i.InspectorId equals inspector.UserId into inspectorGroup
                                     from inspector in inspectorGroup.DefaultIfEmpty()
                                     join partFamily in _context.PartFamilies on i.PartFamilyId equals partFamily.PartFamilyId into pfGroup
                                     from partFamily in pfGroup.DefaultIfEmpty()
                                     join partCode in _context.PartMasters on i.PartCodeId equals partCode.PartMasterId into pcGroup
                                     from partCode in pcGroup.DefaultIfEmpty()
                                     join batch in _context.BatchMasters on i.BatchNumberId equals batch.BatchId into batchGroup
                                     from batch in batchGroup.DefaultIfEmpty()
                                     where i.IsDeleted != true && i.IsArchive != true
                                     orderby i.CreatedDate descending
                                     select new
                                     {
                                         i.InspectionId,
                                         i.ReferenceId,
                                         i.InspectionDate,
                                         i.Time,
                                         i.Remarks,
                                         i.Publish,
                                         i.BatchQuantity,
                                         i.SampleQuantity,
                                         i.StageId,
                                         i.SupplierId,
                                         i.ShiftId,
                                         i.InspectorId,
                                         i.PartFamilyId,
                                         i.PartCodeId,
                                         i.BatchNumberId,
                                         StageName = stage != null ? stage.LookupName : null,
                                         ShiftName = shift != null ? shift.LookupName : null,
                                         InspectorName = inspector != null ? inspector.UserName : null,
                                         PartFamilyName = partFamily != null ? partFamily.PartFamilyName : null,
                                         PartMasterCode = partCode != null ? partCode.PartMasterCode : null,
                                         BatchNumber = batch != null ? batch.BatchNumber : null
                                     }).ToListAsync();

                var inspectionIds = rawData.Select(x => x.InspectionId).ToList();

                // 2. Fetch Parameter counts
                var paramCounts = await _context.Inspectionrefs
                    .Where(r => inspectionIds.Contains(r.InspectionId) && r.IsDeleted != true)
                    .GroupBy(r => r.InspectionId)
                    .Select(g => new { InspectionId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(k => k.InspectionId, v => v.Count);

                // 3. Fetch Defects Data
                var defectsData = await _context.InspectionDefects
                    .Where(d => inspectionIds.Contains(d.InspectionId))
                    .ToDictionaryAsync(k => k.InspectionId, v => v.Status);

                // 4. NEW: Fetch DefectRates from Inspectionrefs and calculate the average per InspectionId
                var refRates = await _context.Inspectionrefs
                    .Where(r => inspectionIds.Contains(r.InspectionId) && r.IsDeleted != true && r.DefectRate != null)
                    .Select(r => new { r.InspectionId, r.DefectRate })
                    .ToListAsync();

                var avgRatesDict = refRates
                    .GroupBy(r => r.InspectionId)
                    .ToDictionary(
                        g => g.Key,
                        g =>
                        {
                            // Parse rates safely, ignoring nulls or empty strings
                            var parsedRates = g.Select(x =>
                            {
                                string cleanString = x.DefectRate.Replace("%", "").Trim();
                                return double.TryParse(cleanString, out double val) ? val : 0.0;
                            }).ToList();

                            if (parsedRates.Any())
                            {
                                double average = parsedRates.Average();
                                return $"{Math.Round(average, 1)}%";
                            }
                            return "0%";
                        }
                    );

                // 5. Build Final Response Data
                var finalData = rawData.Select(d =>
                {
                    // Extract Parameter count
                    int pCount = paramCounts.ContainsKey(d.InspectionId) ? paramCounts[d.InspectionId] : 0;

                    string defectsFraction = "0/0";
                    if (defectsData.ContainsKey(d.InspectionId) && !string.IsNullOrEmpty(defectsData[d.InspectionId]))
                    {
                        try
                        {
                            var statusDict = JsonSerializer.Deserialize<Dictionary<string, int>>(defectsData[d.InspectionId]);
                            if (statusDict != null && statusDict.Count > 0)
                            {
                                int totalDefects = statusDict.Count;
                                int redcount = statusDict.Values.Count(v => v == 5); // 5 = Status map for red/bad
                                defectsFraction = $"{redcount}/{totalDefects}";
                            }
                        }
                        catch { /* Ignore invalid JSON */ }
                    }

                    // Extract Average Defect Rate
                    string avgErrorRate = avgRatesDict.ContainsKey(d.InspectionId) ? avgRatesDict[d.InspectionId] : "0%";

                    return new
                    {
                        inspectionId = d.InspectionId,
                        referenceId = d.ReferenceId,
                        inspectionDate = d.InspectionDate,
                        time = d.Time,
                        remarks = d.Remarks,
                        defects = defectsFraction,          // Overrides the DB NULL with dynamic string
                        parameters = pCount.ToString(),     // Overrides the DB NULL with dynamic count
                        errorRate = avgErrorRate,           // OVERRIDDEN: Now uses the calculated average
                        publish = d.Publish,
                        batchQuantity = d.BatchQuantity,
                        sampleQuantity = d.SampleQuantity,
                        stageId = d.StageId,
                        supplierId = d.SupplierId,
                        shiftId = d.ShiftId,
                        inspectorId = d.InspectorId,
                        partFamilyId = d.PartFamilyId,
                        partCodeId = d.PartCodeId,
                        batchNumberId = d.BatchNumberId,
                        stageName = d.StageName,
                        shiftName = d.ShiftName,
                        inspectorName = d.InspectorName,
                        partFamilyName = d.PartFamilyName,
                        partMasterCode = d.PartMasterCode,
                        batchNumber = d.BatchNumber
                    };
                }).ToList();

                return Ok(new { Data = finalData, Success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }







        [HttpPost("add-record")]
        public async Task<IActionResult> AddInspection([FromBody] Inspection model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { Success = false, Errors = ModelState });

            // Use a transaction to safely handle multiple database operations
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. ADD NEW RECORD (ID is 0)
                if (model.InspectionId == 0)
                {
                    model.CreatedDate = DateTime.Now;
                    model.IsActive = true;
                    model.IsDeleted = false;
                    model.IsArchive = false;

                    // This tells EF to create a NEW record
                    _context.Inspections.Add(model);
                    await _context.SaveChangesAsync(); // SQL generates the new InspectionId here

                    // Generate ReferenceId using the new DB-generated ID
                    model.ReferenceId = $"{model.CreatedDate.Year}/{model.InspectionId:D5}";
                    _context.Inspections.Update(model);

                    // Fetch and insert parameters for the new record
                    await GenerateInspectionRefs(model);

                    // Fetch and insert defects for the new record based on PartFamily
                    await GenerateInspectionDefects(model);
                }
                // 2. EDIT EXISTING RECORD (ID is > 0)
                else
                {
                    var existingRecord = await _context.Inspections
                        .FirstOrDefaultAsync(x => x.InspectionId == model.InspectionId);

                    if (existingRecord == null)
                        return NotFound(new { Success = false, Message = "Record not found." });

                    // Check if Part has changed to rebuild Parameters and Defects
                    bool partChanged = existingRecord.PartCodeId != model.PartCodeId ||
                                       existingRecord.PartFamilyId != model.PartFamilyId;

                    // Map frontend updates to the existing DB record
                    existingRecord.StageId = model.StageId;
                    existingRecord.SupplierId = model.SupplierId;
                    existingRecord.InspectionDate = model.InspectionDate;
                    existingRecord.ShiftId = model.ShiftId;
                    existingRecord.Time = model.Time;
                    existingRecord.InspectorId = model.InspectorId;
                    existingRecord.PartFamilyId = model.PartFamilyId;
                    existingRecord.PartCodeId = model.PartCodeId;
                    existingRecord.BatchNumberId = model.BatchNumberId;
                    existingRecord.Remarks = model.Remarks;
                    existingRecord.BatchQuantity = model.BatchQuantity;
                    existingRecord.SampleQuantity = model.SampleQuantity;
                    existingRecord.ModifiedDate = DateTime.Now;

                    // This tells EF to UPDATE the existing record, NOT insert a new one
                    _context.Inspections.Update(existingRecord);

                    if (partChanged)
                    {
                        // Remove old References
                        var oldRefs = await _context.Inspectionrefs
                            .Where(x => x.InspectionId == existingRecord.InspectionId)
                            .ToListAsync();
                        _context.Inspectionrefs.RemoveRange(oldRefs);
                        await GenerateInspectionRefs(existingRecord);

                        // Remove old Defects mapping
                        var oldDefects = await _context.InspectionDefects
                            .Where(x => x.InspectionId == existingRecord.InspectionId)
                            .ToListAsync();
                        _context.InspectionDefects.RemoveRange(oldDefects);
                        await GenerateInspectionDefects(existingRecord);
                    }
                }

                // Commit all changes to the database
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    Success = true,
                    Data = model,
                    Message = model.InspectionId == 0 ? "Inspection added successfully." : "Inspection updated successfully."
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                var errorMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;

                return StatusCode(500, new
                {
                    Success = false,
                    Message = errorMessage,
                    DetailedError = ex.ToString()
                });
            }
        }

        // --- Existing Parameters Helper ---
        private async Task GenerateInspectionRefs(Inspection model)
        {
            List<ParameterModel> parameters = new();

            if (model.PartCodeId.HasValue)
            {
                parameters = await _context.Parameters
                    .Where(x => x.IsDeleted != true && x.IsActive == true && x.PartMasterId == model.PartCodeId)
                    .ToListAsync();
            }

            if (parameters.Count == 0 && model.PartFamilyId.HasValue)
            {
                parameters = await _context.Parameters
                    .Where(x => x.IsDeleted != true && x.IsActive == true && x.PartFamilyId == model.PartFamilyId)
                    .ToListAsync();
            }

            foreach (var p in parameters)
            {
                _context.Inspectionrefs.Add(new InspectionRef
                {
                    InspectionId = model.InspectionId,
                    PartNameId = p.ParameterId,
                    PartMasterId = model.PartCodeId,
                    ParameterName = p.ParmeterName,
                    PartFamilyId = p.PartFamilyId ?? model.PartFamilyId,
                    PartId = p.PartId,
                    Spec = p.Spec,
                    Min = p.Min,
                    Max = p.Max,
                    Method = p.Method,
                    Defects = null,
                    Okay = false,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedBy = model.CreatedBy,
                    CreatedDate = DateTime.Now
                });
            }
        }

        // --- NEW Defects Helper ---
        //private async Task GenerateInspectionDefects(Inspection model)
        //{
        //    if (model.PartFamilyId.HasValue)
        //    {
        //        // Fetch the PartFamily to get the default JSON array of defects
        //        var partFamily = await _context.PartFamilies
        //            .FirstOrDefaultAsync(pf => pf.PartFamilyId == model.PartFamilyId);

        //        // Check if defects string exists and is not null/empty
        //        if (partFamily != null && !string.IsNullOrEmpty(partFamily.Defects))
        //        {
        //            try
        //            {
        //                // Deserialize "[4,2,3,1]" into a list of integers
        //                var defectIds = JsonSerializer.Deserialize<List<int>>(partFamily.Defects);

        //                if (defectIds != null && defectIds.Any())
        //                {
        //                    // Initialize status for all fetched defects to 5 (Gray)
        //                    // Example output: {"4": 5, "2": 5, "3": 5, "1": 5}
        //                    var initialStatuses = defectIds.ToDictionary(id => id.ToString(), id => 5);

        //                    var newInspectionDefect = new InspectionDefects
        //                    {
        //                        InspectionId = model.InspectionId,
        //                        DefectsId = partFamily.Defects,
        //                        Status = JsonSerializer.Serialize(initialStatuses)
        //                    };

        //                    _context.InspectionDefects.Add(newInspectionDefect);
        //                }
        //            }
        //            catch (JsonException ex)
        //            {
        //                // Log serialization errors if the database holds invalid JSON formats
        //                Console.WriteLine($"Error parsing defects for PartFamilyId {model.PartFamilyId}: {ex.Message}");
        //            }
        //        }
        //    }
        //}







        private async Task GenerateInspectionDefects(Inspection model)
        {
            if (model.PartFamilyId.HasValue)
            {
                // Fetch the PartFamily to get the default JSON array of defects
                var partFamily = await _context.PartFamilies
                    .FirstOrDefaultAsync(pf => pf.PartFamilyId == model.PartFamilyId);

                // Check if defects string exists and is not null/empty
                if (partFamily != null && !string.IsNullOrEmpty(partFamily.Defects))
                {
                    try
                    {
                        // Deserialize "[4,2,3,1]" into a list of integers
                        var defectIds = JsonSerializer.Deserialize<List<int>>(partFamily.Defects);

                        if (defectIds != null && defectIds.Any())
                        {
                            // Initialize status for all fetched defects to 5 (Gray)
                            var initialStatuses = defectIds.ToDictionary(id => id.ToString(), id => 5);

                            var newInspectionDefect = new InspectionDefects
                            {
                                InspectionId = model.InspectionId,
                                DefectsId = partFamily.Defects,
                                Status = JsonSerializer.Serialize(initialStatuses),

                                // --- FIX: ADDED MISSING AUDIT FIELDS ---
                                IsActive = true,
                                IsDeleted = false,
                                CreatedBy = 0, // Carried over from the main Inspection model
                                CreatedDate = DateTime.Now
                            };

                            _context.InspectionDefects.Add(newInspectionDefect);
                        }
                    }
                    catch (JsonException ex)
                    {
                        // Log serialization errors if the database holds invalid JSON formats
                        Console.WriteLine($"Error parsing defects for PartFamilyId {model.PartFamilyId}: {ex.Message}");
                    }
                }
            }
        }







        [HttpPut("update-record/{id}")]
        public async Task<IActionResult> UpdateInspection(long id, [FromBody] Inspection model)
        {
            var existingRecord = await _context.Inspections.FindAsync(id);
            if (existingRecord == null || existingRecord.IsDeleted)
                return NotFound(new { Success = false, Message = "Record not found" });

             
            existingRecord.StageId = model.StageId;
            existingRecord.SupplierId = model.SupplierId;
            existingRecord.InspectionDate = model.InspectionDate;
            existingRecord.ShiftId = model.ShiftId;
            existingRecord.Time = model.Time;
            existingRecord.InspectorId = model.InspectorId;
            existingRecord.PartFamilyId = model.PartFamilyId;
            existingRecord.PartCodeId = model.PartCodeId;
            existingRecord.BatchNumberId = model.BatchNumberId;
            existingRecord.Remarks = model.Remarks;
            existingRecord.BatchQuantity = model.BatchQuantity;
            existingRecord.SampleQuantity = model.SampleQuantity;

            existingRecord.ModifiedDate = DateTime.Now;
            // existingRecord.ModifiedBy = model.ModifiedBy; //  

            await _context.SaveChangesAsync();
            return Ok(new { Success = true, Message = "Record updated successfully." });
        }

        
        [HttpDelete("delete-record/{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var dbItem = await _context.Inspections.FindAsync(id);
            if (dbItem == null)
                return NotFound(new { Message = "Record not found", Success = false });

            dbItem.IsDeleted = true;
            dbItem.DeletedDate = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Record successfully deleted.", Success = true });
        }

        
        [HttpPut("archive-record/{id}")]
        public async Task<IActionResult> ToggleArchive(long id)
        {
            var dbItem = await _context.Inspections.FindAsync(id);
            if (dbItem == null)
                return NotFound(new { Message = "Record not found", Success = false });

            // Set to true, or toggle with: dbItem.IsArchive = !dbItem.IsArchive;
            dbItem.IsArchive = !dbItem.IsArchive;
            dbItem.ModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Record successfully archived.", Success = true });
        }










        [HttpPut("toggle-publish/{id}/{status}")]
        public async Task<IActionResult> TogglePublish(long id, bool status)
        {
            var dbItem = await _context.Inspections.FindAsync(id);
            if (dbItem == null)
                return NotFound(new { Message = "Record not found", Success = false });

            // Explicitly set the value to whatever the Angular checkbox says
            dbItem.Publish = status;

            dbItem.ModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Record publish status updated.", Success = true });
        }












        [HttpGet("get-all-archive")]
        public async Task<IActionResult> GetAllarchives()
        {
            try
            {
                // 1. Fetch base inspection data from the database
                var rawData = await (from i in _context.Inspections
                                     join stage in _context.Lookups on i.StageId equals stage.LookupId into stageGroup
                                     from stage in stageGroup.DefaultIfEmpty()
                                     join shift in _context.Lookups on i.ShiftId equals shift.LookupId into shiftGroup
                                     from shift in shiftGroup.DefaultIfEmpty()
                                     join inspector in _context.Users on i.InspectorId equals inspector.UserId into inspectorGroup
                                     from inspector in inspectorGroup.DefaultIfEmpty()
                                     join partFamily in _context.PartFamilies on i.PartFamilyId equals partFamily.PartFamilyId into pfGroup
                                     from partFamily in pfGroup.DefaultIfEmpty()
                                     join partCode in _context.PartMasters on i.PartCodeId equals partCode.PartMasterId into pcGroup
                                     from partCode in pcGroup.DefaultIfEmpty()
                                     join batch in _context.BatchMasters on i.BatchNumberId equals batch.BatchId into batchGroup
                                     from batch in batchGroup.DefaultIfEmpty()
                                     where i.IsDeleted != true && i.IsArchive != false
                                     orderby i.CreatedDate descending
                                     select new
                                     {
                                         i.InspectionId,
                                         i.ReferenceId,
                                         i.InspectionDate,
                                         i.Time,
                                         i.Remarks,
                                         i.ErrorRate,
                                         i.Publish,
                                         i.BatchQuantity,
                                         i.SampleQuantity,
                                         i.StageId,
                                         i.SupplierId,
                                         i.ShiftId,
                                         i.InspectorId,
                                         i.PartFamilyId,
                                         i.PartCodeId,
                                         i.BatchNumberId,
                                         StageName = stage != null ? stage.LookupName : null,
                                         ShiftName = shift != null ? shift.LookupName : null,
                                         InspectorName = inspector != null ? inspector.UserName : null,
                                         PartFamilyName = partFamily != null ? partFamily.PartFamilyName : null,
                                         PartMasterCode = partCode != null ? partCode.PartMasterCode : null,
                                         BatchNumber = batch != null ? batch.BatchNumber : null
                                     }).ToListAsync();

                var inspectionIds = rawData.Select(x => x.InspectionId).ToList();


                var paramCounts = await _context.Inspectionrefs
                    .Where(r => inspectionIds.Contains(r.InspectionId) && r.IsDeleted != true)
                    .GroupBy(r => r.InspectionId)
                    .Select(g => new { InspectionId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(k => k.InspectionId, v => v.Count);


                var defectsData = await _context.InspectionDefects
                    .Where(d => inspectionIds.Contains(d.InspectionId))
                    .ToDictionaryAsync(k => k.InspectionId, v => v.Status);


                var finalData = rawData.Select(d =>
                {
                    // Extract Parameter count
                    int pCount = paramCounts.ContainsKey(d.InspectionId) ? paramCounts[d.InspectionId] : 0;


                    string defectsFraction = "0/0";
                    if (defectsData.ContainsKey(d.InspectionId) && !string.IsNullOrEmpty(defectsData[d.InspectionId]))
                    {
                        try
                        {
                            var statusDict = JsonSerializer.Deserialize<Dictionary<string, int>>(defectsData[d.InspectionId]);
                            if (statusDict != null && statusDict.Count > 0)
                            {
                                int totalDefects = statusDict.Count;
                                int greenCount = statusDict.Values.Count(v => v == 1); // 1 = Green Status
                                defectsFraction = $"{greenCount}/{totalDefects}";
                            }
                        }
                        catch { /* Ignore invalid JSON */ }
                    }

                    return new
                    {
                        inspectionId = d.InspectionId,
                        referenceId = d.ReferenceId,
                        inspectionDate = d.InspectionDate,
                        time = d.Time,
                        remarks = d.Remarks,
                        defects = defectsFraction,          // Overrides the DB NULL with dynamic string
                        parameters = pCount.ToString(),     // Overrides the DB NULL with dynamic count
                        errorRate = d.ErrorRate,
                        publish = d.Publish,
                        batchQuantity = d.BatchQuantity,
                        sampleQuantity = d.SampleQuantity,
                        stageId = d.StageId,
                        supplierId = d.SupplierId,
                        shiftId = d.ShiftId,
                        inspectorId = d.InspectorId,
                        partFamilyId = d.PartFamilyId,
                        partCodeId = d.PartCodeId,
                        batchNumberId = d.BatchNumberId,
                        stageName = d.StageName,
                        shiftName = d.ShiftName,
                        inspectorName = d.InspectorName,
                        partFamilyName = d.PartFamilyName,
                        partMasterCode = d.PartMasterCode,
                        batchNumber = d.BatchNumber
                    };
                }).ToList();

                return Ok(new { Data = finalData, Success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }



         





    }












     
    }