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
    public class DataTableController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DataTableController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("get-all-records")]
        public async Task<IActionResult> GetAllInspections()
        {
            try
            {
                var query = from i in _context.Inspections
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
                                InspectionId = i.InspectionId,
                                ReferenceId = i.ReferenceId,
                                InspectionDate = i.InspectionDate,
                                Time = i.Time,
                                Remarks = i.Remarks,
                                Defects = i.Defects,
                                Parameters = i.Parameters,
                                ErrorRate = i.ErrorRate,
                                Publish = i.Publish,
                                BatchQuantity = i.BatchQuantity,
                                SampleQuantity = i.SampleQuantity,

                                
                                StageId = i.StageId,
                                SupplierId = i.SupplierId,
                                ShiftId = i.ShiftId,
                                InspectorId = i.InspectorId,
                                PartFamilyId = i.PartFamilyId,
                                PartCodeId = i.PartCodeId,
                                BatchNumberId = i.BatchNumberId,

                               
                                StageName = stage != null ? stage.LookupName : null,
                                ShiftName = shift != null ? shift.LookupName : null,
                                InspectorName = inspector != null ? inspector.UserName : null,
                                PartFamilyName = partFamily != null ? partFamily.PartFamilyName : null,
                                PartMasterCode = partCode != null ? partCode.PartMasterCode : null,
                                BatchNumber = batch != null ? batch.BatchNumber : null
                            };

                var data = await query.ToListAsync();
                return Ok(new { Data = data, Success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }


        //[HttpPost("add-record")]
        //public async Task<IActionResult> AddInspection([FromBody] Inspection model)
        //{
        //    if (!ModelState.IsValid)
        //        return BadRequest(new { Success = false, Errors = ModelState });

        //    try
        //    {
        //        model.CreatedDate = DateTime.Now;
        //        model.IsActive = true;
        //        model.IsDeleted = false;
        //        model.IsArchive = false;

        //        await _context.Inspections.AddAsync(model);
        //        await _context.SaveChangesAsync();

        //        return Ok(new { Data = model, Success = true, Message = "Record added successfully." });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { Success = false, Message = $"Internal server error: {ex.Message}" });
        //    }
        //}


        //[HttpPost("add-record")]
        //public async Task<IActionResult> AddInspection([FromBody] Inspection model)
        //{
        //    if (!ModelState.IsValid)
        //        return BadRequest(new { Success = false, Errors = ModelState });

        //    try
        //    {
        //        model.CreatedDate = DateTime.Now;
        //        model.IsActive = true;
        //        model.IsDeleted = false;
        //        model.IsArchive = false;

        //        _context.Inspections.Add(model);
        //        await _context.SaveChangesAsync();

        //        // Fetch Parameters
        //        List<ParameterModel> parameters = new();

        //        // First check PartCodeId
        //        if (model.PartCodeId.HasValue)
        //        {
        //            parameters = await _context.Parameters
        //                .Where(x =>
        //                    x.IsDeleted != true &&
        //                    x.IsActive == true &&
        //                    x.PartMasterId == model.PartCodeId)   // PartCodeId -> PartMasterId
        //                .ToListAsync();
        //        }

        //        // If not found, check PartFamilyId
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
        //            _context.Inspectionrefs.Add(new InspectionRef
        //            {
        //                InspectionId = (int)model.InspectionId,

        //                // Copy from Parameter table
        //                PartNameId = p.ParameterId,
        //                PartFamilyId = p.PartFamilyId,
        //                PartId = p.PartMasterId,
        //                Spec = p.Spec,
        //               // Unit = p.Unit,
        //                Min = p.Min,
        //                Max = p.Max,
        //                Method = p.Method,

        //                // Default values
        //                Defects = 0,
        //                Okay = false,
        //                CAPA = null,
        //                S1 = null,
        //                S2 = null,
        //                S3 = null,
        //                S4 = null,
        //                S5 = null,
        //                Remarks = null,

        //                IsActive = true,
        //                IsDeleted = false,
        //                CreatedBy = (int?)model.CreatedBy,
        //                CreatedDate = DateTime.Now
        //            });
        //        }

        //        await _context.SaveChangesAsync();

        //        return Ok(new
        //        {
        //            Success = true,
        //            Data = model,
        //            Message = "Inspection added successfully."
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            Success = false,
        //            Message = ex.Message
        //        });
        //    }
        //}







        //[HttpPost("add-record")]
        //public async Task<IActionResult> AddInspection([FromBody] Inspection model)
        //{
        //    if (!ModelState.IsValid)
        //        return BadRequest(new { Success = false, Errors = ModelState });

        //    try
        //    {
        //        model.CreatedDate = DateTime.Now;
        //        model.IsActive = true;
        //        model.IsDeleted = false;
        //        model.IsArchive = false;

        //        _context.Inspections.Add(model);
        //        await _context.SaveChangesAsync();

        //        // Fetch Parameters
        //        List<ParameterModel> parameters = new();

        //        // First check PartCodeId
        //        if (model.PartCodeId.HasValue)
        //        {
        //            parameters = await _context.Parameters
        //                .Where(x =>
        //                    x.IsDeleted != true &&
        //                    x.IsActive == true &&
        //                    x.PartMasterId == model.PartCodeId)
        //                .ToListAsync();
        //        }

        //        // If not found, check PartFamilyId
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
        //            _context.Inspectionrefs.Add(new InspectionRef
        //            {
        //                InspectionId = model.InspectionId,

        //                // --- FK MAPPING FIX ---
        //                // PartNameId MUST be a valid Part Master ID to satisfy the SQL FK.
        //                // Your payload sends this in PartCodeId.
        //                PartNameId = p.ParameterId,

        //                // Populate the newly added missing columns
        //                PartMasterId = model.PartCodeId,
        //                ParameterName = p.ParmeterName, // Adjust property name if it differs in ParameterModel
        //                                                 // ----------------------

        //                PartFamilyId = p.PartFamilyId ?? model.PartFamilyId,
        //                PartId = p.PartId,

        //                Spec = p.Spec,
        //                //Unit = p.Unit, // Uncommented Unit since it exists in SQL
        //                Min = p.Min,
        //                Max = p.Max,
        //                Method = p.Method,

        //                // Default values
        //                Defects = 0,
        //                Okay = false,
        //                CAPA = null,
        //                S1 = null,
        //                S2 = null,
        //                S3 = null,
        //                S4 = null,
        //                S5 = null,
        //                Remarks = null,

        //                IsActive = true,
        //                IsDeleted = false,
        //                CreatedBy = model.CreatedBy,
        //                CreatedDate = DateTime.Now
        //            });
        //        }

        //        await _context.SaveChangesAsync();

        //        return Ok(new
        //        {
        //            Success = true,
        //            Data = model,
        //            Message = "Inspection added successfully."
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        var errorMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;

        //        return StatusCode(500, new
        //        {
        //            Success = false,
        //            Message = errorMessage,
        //            DetailedError = ex.ToString()
        //        });
        //    }
        //}








        //[HttpPost("add-record")]
        //public async Task<IActionResult> AddInspection([FromBody] Inspection model)
        //{
        //    if (!ModelState.IsValid)
        //        return BadRequest(new { Success = false, Errors = ModelState });

        //    try
        //    {
        //        model.CreatedDate = DateTime.Now;
        //        model.IsActive = true;
        //        model.IsDeleted = false;
        //        model.IsArchive = false;

        //        _context.Inspections.Add(model);

        //        // 1. Save to database to generate the new model.InspectionId
        //        await _context.SaveChangesAsync();

        //        // 2. Generate ReferenceId
        //        model.ReferenceId = $"{model.CreatedDate.Year}/{model.InspectionId:D5}";

        //        // 3. EXPLICITLY mark the model as updated so EF knows to save the new ReferenceId
        //        _context.Inspections.Update(model);

        //        // Fetch Parameters
        //        List<ParameterModel> parameters = new();

        //        if (model.PartCodeId.HasValue)
        //        {
        //            parameters = await _context.Parameters
        //                .Where(x =>
        //                    x.IsDeleted != true &&
        //                    x.IsActive == true &&
        //                    x.PartMasterId == model.PartCodeId)
        //                .ToListAsync();
        //        }

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
        //            _context.Inspectionrefs.Add(new InspectionRef
        //            {
        //                InspectionId = model.InspectionId,

        //                // mapped properly for SQL FK constraints
        //                PartNameId = p.ParameterId,

        //                PartMasterId = model.PartCodeId,
        //                ParameterName = p.ParmeterName,

        //                PartFamilyId = p.PartFamilyId ?? model.PartFamilyId,
        //                PartId = p.PartId,

        //                Spec = p.Spec,
        //                //Unit = p.Unit, 
        //                Min = p.Min,
        //                Max = p.Max,
        //                Method = p.Method,

        //                Defects = 0,
        //                Okay = false,
        //                CAPA = null,
        //                S1 = null,
        //                S2 = null,
        //                S3 = null,
        //                S4 = null,
        //                S5 = null,
        //                Remarks = null,

        //                IsActive = true,
        //                IsDeleted = false,
        //                CreatedBy = model.CreatedBy,
        //                CreatedDate = DateTime.Now
        //            });
        //        }

        //        // 4. Second save updates the main Inspection (with ReferenceId) AND inserts InspectionRefs
        //        await _context.SaveChangesAsync();

        //        return Ok(new
        //        {
        //            Success = true,
        //            Data = model,
        //            Message = "Inspection added successfully."
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        var errorMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;

        //        return StatusCode(500, new
        //        {
        //            Success = false,
        //            Message = errorMessage,
        //            DetailedError = ex.ToString()
        //        });
        //    }
        //}







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
                }
                // 2. EDIT EXISTING RECORD (ID is > 0)
                else
                {
                    var existingRecord = await _context.Inspections
                        .FirstOrDefaultAsync(x => x.InspectionId == model.InspectionId);

                    if (existingRecord == null)
                        return NotFound(new { Success = false, Message = "Record not found." });

                    // Check if Part has changed to rebuild Parameters
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
                        var oldRefs = await _context.Inspectionrefs
                            .Where(x => x.InspectionId == existingRecord.InspectionId)
                            .ToListAsync();

                        _context.Inspectionrefs.RemoveRange(oldRefs);

                        // Pass existingRecord to generate new references against the updated Part ID
                        await GenerateInspectionRefs(existingRecord);
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

        // Extracted helper method
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
                    Defects = 0,
                    Okay = false,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedBy = model.CreatedBy,
                    CreatedDate = DateTime.Now
                });
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
            dbItem.IsArchive = true;
            dbItem.ModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Record successfully archived.", Success = true });
        }


        //[HttpPut("toggle-publish/{id}")]
        //public async Task<IActionResult> TogglePublish(long id)
        //{
        //    var dbItem = await _context.Inspections.FindAsync(id);
        //    if (dbItem == null)
        //        return NotFound(new { Message = "Record not found", Success = false });

        //    // Toggle the publish status (if true becomes false, if false becomes true)
        //    dbItem.Publish = !dbItem.Publish;
        //    dbItem.ModifiedDate = DateTime.Now;

        //    await _context.SaveChangesAsync();
        //    return Ok(new { Message = $"Record publish status updated.", Success = true });
        //}









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
    }
}