using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sqa_core.Configuration;
using sqa_core.Data;
using sqa_core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace sqa_core.Controllers
{


    public class CapaUploadRequest
    {
        public string jsonData { get; set; }
        public List<IFormFile>? files { get; set; }
    }
    public class CapaDeleteDocDto
    {
        public long CapaId { get; set; }
        public string FileUrl { get; set; }
    }

    [Route("api/[controller]")]
    [ApiController]
    public class InspectionCapaController : ControllerBase
    {
        private readonly AppDbContext _context;

        // AWS S3 Configuration
        private readonly string _awsAccessKey = ConfigKey.Aws.AccessKey;
        private readonly string _awsSecretKey = ConfigKey.Aws.SecretKey;
        private readonly Amazon.RegionEndpoint _awsRegion = Amazon.RegionEndpoint.GetBySystemName(ConfigKey.Aws.Region);
        private readonly string _bucketName = ConfigKey.Aws.BucketName;




        private long GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return string.IsNullOrEmpty(userIdClaim) ? 0 : long.Parse(userIdClaim);
        }


        private string GetCurrentUserType()
        {
            var userTypeClaim = User.FindFirstValue("UserType");
            return string.IsNullOrEmpty(userTypeClaim) ? "Internal" : userTypeClaim;
        }
        public InspectionCapaController(AppDbContext context)
        {
            _context = context;
        }

        // Helper Method to generate 60-minute links for frontend viewing
        private string GeneratePreSignedUrl(AmazonS3Client s3Client, string key)
        {
            if (string.IsNullOrEmpty(key)) return "";
            if (key.StartsWith("http")) return key; // Safety check

            var request = new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = key,
                Expires = DateTime.UtcNow.AddMinutes(60)
            };
            return s3Client.GetPreSignedURL(request);
        }

        [HttpGet("GetCapaByInspectionId/{inspectionId}")]
        public async Task<IActionResult> GetCapaByInspectionId(int inspectionId)
        {
            try
            {
                var capaRecords = await _context.InspectionCapas
                    .Where(c => c.InspectionRefId == inspectionId && c.IsDeleted == false)
                    .Select(c => new
                    {
                        Id = c.CapaId,
                        Subject = c.Subject,
                        CapaSubject = c.CapaSubject,
                        ActionType = c.ActionType,
                        DueDate = c.DueDate,
                        CompletedDate = c.CompletedDate,
                        Status = c.PdcaStatus,
                        RiskRating = c.RiskRating,
                        SeverityId = c.SeverityId,
                        Occurrence = c.Occurrence,
                        Detection = c.Detection,
                        SodScore = c.SodScore,
                        Class = c.Class,
                        Observations = c.Observations,
                        CorrectiveActions = c.CorrectiveActions,
                        SupplierRemarks = c.SupplierRemarks,
                        PdfDocs = c.PdfDocs,
                        ImageDocs = c.ImageDocs
                    }).ToListAsync();

                // Generate pre-signed URLs for the fetched records
                using (var s3Client = new AmazonS3Client(_awsAccessKey, _awsSecretKey, _awsRegion))
                {
                    var updatedRecords = capaRecords.Select(c =>
                    {
                        var pdfUrls = string.IsNullOrEmpty(c.PdfDocs) ? "" : string.Join(",", c.PdfDocs.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(k => GeneratePreSignedUrl(s3Client, k)));
                        var imageUrls = string.IsNullOrEmpty(c.ImageDocs) ? "" : string.Join(",", c.ImageDocs.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(k => GeneratePreSignedUrl(s3Client, k)));

                        return new
                        {
                            c.Id,
                            c.Subject,
                            c.CapaSubject,
                            c.ActionType,
                            c.DueDate,
                            c.CompletedDate,
                            c.Status,
                            c.RiskRating,
                            c.SeverityId,
                            c.Occurrence,
                            c.Detection,
                            c.SodScore,
                            c.Class,
                            c.Observations,
                            c.CorrectiveActions,
                            c.SupplierRemarks,
                            PdfDocs = pdfUrls,
                            ImageDocs = imageUrls
                        };
                    }).ToList();

                    return Ok(updatedRecords);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }





        [HttpPost("SaveCapa")]
        [DisableRequestSizeLimit] // Removes the overall Kestrel request size limit for this endpoint
        [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = int.MaxValue)] // Removes the form-data specific limits
        public async Task<IActionResult> SaveCapa([FromForm] CapaUploadRequest request)
        {
            try
            {
                // Extract the data from the request object
                var jsonData = request.jsonData;
                var files = request.files;

                var capa = JsonSerializer.Deserialize<InspectionCapa>(jsonData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (capa == null) return BadRequest("Invalid JSON data payload");
                if (capa.InspectionRefId == null || capa.InspectionRefId <= 0) return BadRequest("InspectionRefId is required.");

                string newPdfKeys = "";
                string newImageKeys = "";

                // Process File Uploads
                if (files != null && files.Count > 0)
                {
                    List<string> pdfKeysList = new List<string>();
                    List<string> imageKeysList = new List<string>();

                    using (var s3Client = new AmazonS3Client(_awsAccessKey, _awsSecretKey, _awsRegion))
                    {
                        foreach (var file in files)
                        {
                            // Storing the Key in format matching the database image requirement
                            string s3Key = $"inspection-capa-docs/{capa.InspectionRefId}/{Guid.NewGuid()}_{file.FileName}";

                            using (var stream = file.OpenReadStream())
                            {
                                var putRequest = new PutObjectRequest
                                {
                                    BucketName = _bucketName,
                                    Key = s3Key,
                                    InputStream = stream,
                                    ContentType = file.ContentType
                                };
                                await s3Client.PutObjectAsync(putRequest);
                            }

                            if (file.ContentType.Contains("image")) imageKeysList.Add(s3Key);
                            else pdfKeysList.Add(s3Key);
                        }
                    }

                    newPdfKeys = string.Join(",", pdfKeysList);
                    newImageKeys = string.Join(",", imageKeysList);
                }

                // Update Existing or Insert New
                if (capa.CapaId > 0)
                {
                    var existingCapa = await _context.InspectionCapas
                        .FirstOrDefaultAsync(c => c.CapaId == capa.CapaId && c.InspectionRefId == capa.InspectionRefId && c.IsDeleted == false);

                    if (existingCapa == null) return NotFound("Active CAPA not found.");

                    existingCapa.SeverityId = capa.SeverityId;
                    existingCapa.Subject = capa.Subject;
                    existingCapa.DueDate = capa.DueDate;
                    existingCapa.CompletedDate = capa.CompletedDate;
                    existingCapa.PdcaStatus = capa.PdcaStatus;
                    existingCapa.Occurrence = capa.Occurrence;
                    existingCapa.RiskRating = capa.RiskRating;
                    existingCapa.Class = capa.Class;
                    existingCapa.ActionType = capa.ActionType;
                    existingCapa.CapaSubject = capa.CapaSubject;
                    existingCapa.Observations = capa.Observations;
                    existingCapa.CorrectiveActions = capa.CorrectiveActions;
                    existingCapa.SupplierRemarks = capa.SupplierRemarks;
                    existingCapa.Detection = capa.Detection;
                    existingCapa.SodScore = capa.SodScore;

                    // Append new file keys (Keeps existing keys intact)
                    if (!string.IsNullOrEmpty(newPdfKeys))
                        existingCapa.PdfDocs = string.IsNullOrEmpty(existingCapa.PdfDocs) ? newPdfKeys : existingCapa.PdfDocs + "," + newPdfKeys;
                    if (!string.IsNullOrEmpty(newImageKeys))
                        existingCapa.ImageDocs = string.IsNullOrEmpty(existingCapa.ImageDocs) ? newImageKeys : existingCapa.ImageDocs + "," + newImageKeys;

                    existingCapa.ModifiedDate = DateTime.Now;
                    existingCapa.ModifiedBy = capa.ModifiedBy;

                    _context.InspectionCapas.Update(existingCapa);
                    await _context.SaveChangesAsync();

                    return Ok(new { Message = "CAPA updated successfully", CapaId = existingCapa.CapaId });
                }
                else
                {
                    capa.PdfDocs = newPdfKeys;
                    capa.ImageDocs = newImageKeys;
                    capa.CreatedDate = DateTime.Now;
                    capa.IsActive = true;
                    capa.IsDeleted = false;

                    _context.InspectionCapas.Add(capa);
                    await _context.SaveChangesAsync();

                    return Ok(new { Message = "CAPA inserted successfully", CapaId = capa.CapaId });
                }
            }
            catch (Exception ex)
            {
                var errorMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return StatusCode(500, $"Database Error: {errorMessage}");
            }
        }






        //[HttpPost("delete-document")]
        //public async Task<IActionResult> DeleteDocument([FromBody] CapaDeleteDocDto request)
        //{
        //    var dbItem = await _context.InspectionCapas
        //        .FirstOrDefaultAsync(x => x.CapaId == request.CapaId && x.IsDeleted == false);

        //    if (dbItem == null) return NotFound(new { Message = "Record not found", Success = false });

        //    bool removed = false;

        //    // Matches the full PreSigned URL passed from the frontend to the base Key stored in the DB
        //    string RemoveKey(string existingKeys, string urlToRemove)
        //    {
        //        if (string.IsNullOrEmpty(existingKeys)) return existingKeys;

        //        var keys = existingKeys.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        //        var keyToRemove = keys.FirstOrDefault(k => urlToRemove.Contains(Uri.EscapeDataString(k)) || urlToRemove.Contains(k));

        //        if (keyToRemove != null)
        //        {
        //            keys.Remove(keyToRemove);
        //            removed = true;
        //        }
        //        return string.Join(",", keys);
        //    }

        //    dbItem.PdfDocs = RemoveKey(dbItem.PdfDocs, request.FileUrl);
        //    if (!removed) dbItem.ImageDocs = RemoveKey(dbItem.ImageDocs, request.FileUrl);

        //    if (removed)
        //    {
        //        dbItem.ModifiedDate = DateTime.UtcNow;
        //        await _context.SaveChangesAsync();
        //        return Ok(new { Message = "Document deleted successfully", Success = true });
        //    }

        //    return BadRequest(new { Message = "Document not found in record", Success = false });
        //}


        [HttpPost("delete-document")]
        public async Task<IActionResult> DeleteDocument([FromBody] CapaDeleteDocDto request)
        {
            var dbItem = await _context.InspectionCapas
                .FirstOrDefaultAsync(x => x.CapaId == request.CapaId && x.IsDeleted == false);

            if (dbItem == null) return NotFound(new { Message = "Record not found", Success = false });

            bool removed = false;

            // Matches the full PreSigned URL passed from the frontend to the base Key stored in the DB
            string RemoveKey(string existingKeys, string urlToRemove)
            {
                if (string.IsNullOrEmpty(existingKeys)) return existingKeys;

                var keys = existingKeys.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

                // Decode the URL sent from the frontend so %20 becomes a space, %28 becomes (, etc.
                string decodedUrl = Uri.UnescapeDataString(urlToRemove);

                // Find the key by checking if the fully decoded URL contains the raw DB string
                var keyToRemove = keys.FirstOrDefault(k => decodedUrl.Contains(k));

                if (keyToRemove != null)
                {
                    keys.Remove(keyToRemove);
                    removed = true;
                }
                return string.Join(",", keys);
            }

            dbItem.PdfDocs = RemoveKey(dbItem.PdfDocs, request.FileUrl);

            // Only try to remove from ImageDocs if it wasn't already found and removed from PdfDocs
            if (!removed)
            {
                dbItem.ImageDocs = RemoveKey(dbItem.ImageDocs, request.FileUrl);
            }

            if (removed)
            {
                dbItem.ModifiedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return Ok(new { Message = "Document deleted successfully", Success = true });
            }

            return BadRequest(new { Message = "Document not found in record", Success = false });
        }












 













       







        public class CapaInlineUpdateRequest
        {
            public long CapaId { get; set; }
            public long? Status { get; set; }
            public bool? Resolved { get; set; }
 
            public String? RiskRating { get; set; }
        }

        // 2. Add the API Endpoint
        [HttpPut("UpdateInlineStatus")]
        public async Task<IActionResult> UpdateInlineStatus([FromBody] CapaInlineUpdateRequest request)
        {
            try
            {
                var capa = await _context.InspectionCapas.FindAsync(request.CapaId);

                if (capa == null)
                {
                    return NotFound(new { success = false, message = "CAPA record not found." });
                }

                // Update fields
                capa.Status = request.Status;
                capa.Resolved = request.Resolved;
                capa.RiskRating = request.RiskRating;
                capa.ModifiedDate = DateTime.Now;

                _context.InspectionCapas.Update(capa);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Record updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error updating record", error = ex.Message });
            }
        }













































        //[HttpGet("GetPendingCapaRecords")]
        //public async Task<IActionResult> GetPendingCapaRecords()
        //{
        //    try
        //    {
        //        // 1. Fetch raw data with all joins first
        //        var rawQuery = await (from capa in _context.InspectionCapas

        //                                  // Join InspectionRef
        //                              join ir in _context.Inspectionrefs on capa.InspectionRefId equals ir.InspectionRefId

        //                              // Join Inspections to get ReferenceId
        //                              join ins in _context.Inspections on ir.InspectionId equals ins.InspectionId into insJoin
        //                              from ins in insJoin.DefaultIfEmpty()

        //                                  // Join SupplierMaster
        //                              join sup in _context.SupplierMasters on ins.SupplierId equals sup.SupplierId into supJoin
        //                              from sup in supJoin.DefaultIfEmpty()

        //                              where ir.Okay == false && ir.IsDeleted == false && capa.IsDeleted == false
        //                              select new
        //                              {
        //                                  capa.CapaId,
        //                                  capa.InspectionRefId, // <-- ADDED THIS LINE

        //                                  Status = capa.Status ?? 2,
        //                                  Resolved = capa.Resolved ?? false,
        //                                  capa.Description,
        //                                  capa.EtaDate,
        //                                  capa.AuditorRemarks,
        //                                  capa.AuditeeResponse,
        //                                  capa.PdfDocs,
        //                                  capa.ImageDocs,
        //                                  Reference = ins != null ? ins.ReferenceId : "N/A",
        //                                  ActionSubject = capa.Subject,
        //                                  SupplierName = sup != null ? sup.SupplierName : "N/A",
        //                                  capa.ActionType,
        //                                  AuditReference = ir.PartId.ToString(),
        //                                  ProcessCategory = capa.Class,
        //                                  capa.SupplierRemarks,
        //                                  LogDate = capa.CreatedDate,
        //                                  capa.DueDate,
        //                                  Completion = capa.CompletedDate,
        //                                  Severity = capa.SeverityId,
        //                                  capa.Occurrence,
        //                                  capa.Detection,
        //                                  capa.RiskRating,
        //                                  Rating = capa.SodScore,
        //                                  capa.PdcaStatus
        //                              }).ToListAsync();

        //        // 2. Perform in-memory calculation for the Docs count
        //        var query = rawQuery.Select(x => new
        //        {
        //            x.CapaId,
        //            x.InspectionRefId, // <-- ADDED THIS LINE

        //            x.Status,
        //            x.Resolved,
        //            x.Description,
        //            x.EtaDate,
        //            x.AuditorRemarks,
        //            x.AuditeeResponse,

        //            // Sum of Docs and Photos
        //            Docs = (string.IsNullOrEmpty(x.PdfDocs) ? 0 : x.PdfDocs.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Length) +
        //                   (string.IsNullOrEmpty(x.ImageDocs) ? 0 : x.ImageDocs.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Length),

        //            x.Reference,
        //            x.ActionSubject,
        //            x.SupplierName,
        //            x.ActionType,
        //            x.AuditReference,
        //            x.ProcessCategory,
        //            x.SupplierRemarks,
        //            x.LogDate,
        //            x.DueDate,
        //            x.Completion,
        //            x.Severity,
        //            x.Occurrence,
        //            x.Detection,
        //            x.RiskRating,
        //            x.Rating,
        //            x.PdcaStatus
        //        });

        //        return Ok(new { success = true, data = query, message = "Pending CAPA fetched successfully." });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { success = false, message = "Error fetching CAPA records", error = ex.Message });
        //    }
        //}





        [HttpGet("GetPendingCapaRecords")]
        // 🔥 Added [FromQuery] to accept the supplierId from the Angular Service
        public async Task<IActionResult> GetPendingCapaRecords([FromQuery] long? supplierId)
        {
            try
            {
                // 🔥 Get the Current User Id and Type from claims
                long currentUserId = GetCurrentUserId();
                string userType = GetCurrentUserType();

                // 1. Define the base query without executing it yet
                var query = from capa in _context.InspectionCapas
                                // Join InspectionRef
                            join ir in _context.Inspectionrefs on capa.InspectionRefId equals ir.InspectionRefId
                            // Join Inspections to get ReferenceId and SupplierId
                            join ins in _context.Inspections on ir.InspectionId equals ins.InspectionId into insJoin
                            from ins in insJoin.DefaultIfEmpty()
                                // Join SupplierMaster
                            join sup in _context.SupplierMasters on ins.SupplierId equals sup.SupplierId into supJoin
                            from sup in supJoin.DefaultIfEmpty()
                                // 🔥 Exclude deleted records (Already handled here)
                            where ir.Okay == false && ir.IsDeleted == false && capa.IsDeleted == false
                            select new { capa, ir, ins, sup };

                // 🔥 FILTERING LOGIC
                if (userType.Equals("Supplier", StringComparison.OrdinalIgnoreCase))
                {
                    // If they are a supplier based on token, strictly lock it to their ID
                    query = query.Where(x => x.ins != null && x.ins.SupplierId == currentUserId);
                }
                else if (supplierId.HasValue && supplierId.Value > 0)
                {
                    // If it's an Admin/Internal passing the supplierId from the frontend, filter by that
                    query = query.Where(x => x.ins != null && x.ins.SupplierId == supplierId.Value);
                }

                // 2. Project the data to the expected format and execute the query (fetch from DB)
                var rawData = await query.Select(x => new
                {
                    x.capa.CapaId,
                    x.capa.InspectionRefId,
                    Status = x.capa.Status ?? 2,
                    Resolved = x.capa.Resolved ?? false,
                    x.capa.Description,
                    x.capa.EtaDate,
                    x.capa.AuditorRemarks,
                    x.capa.AuditeeResponse,
                    x.capa.PdfDocs,
                    x.capa.ImageDocs,
                    Reference = x.ins != null ? x.ins.ReferenceId : "N/A",
                    ActionSubject = x.capa.Subject,
                    SupplierName = x.sup != null ? x.sup.SupplierName : "N/A",
                    x.capa.ActionType,
                    AuditReference = x.ir.PartId.ToString(),
                    ProcessCategory = x.capa.Class,
                    x.capa.SupplierRemarks,
                    LogDate = x.capa.CreatedDate,
                    x.capa.DueDate,
                    Completion = x.capa.CompletedDate,
                    Severity = x.capa.SeverityId,
                    x.capa.Occurrence,
                    x.capa.Detection,
                    x.capa.RiskRating,
                    Rating = x.capa.SodScore,
                    x.capa.PdcaStatus
                }).ToListAsync();

                // 3. Perform in-memory calculation for the Docs count
                var finalData = rawData.Select(x => new
                {
                    x.CapaId,
                    x.InspectionRefId,
                    x.Status,
                    x.Resolved,
                    x.Description,
                    x.EtaDate,
                    x.AuditorRemarks,
                    x.AuditeeResponse,

                    // Sum of Docs and Photos
                    Docs = (string.IsNullOrEmpty(x.PdfDocs) ? 0 : x.PdfDocs.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Length) +
                           (string.IsNullOrEmpty(x.ImageDocs) ? 0 : x.ImageDocs.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Length),

                    x.Reference,
                    x.ActionSubject,
                    x.SupplierName,
                    x.ActionType,
                    x.AuditReference,
                    x.ProcessCategory,
                    x.SupplierRemarks,
                    x.LogDate,
                    x.DueDate,
                    x.Completion,
                    x.Severity,
                    x.Occurrence,
                    x.Detection,
                    x.RiskRating,
                    x.Rating,
                    x.PdcaStatus
                });

                return Ok(new { success = true, data = finalData, message = "Pending CAPA fetched successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error fetching CAPA records", error = ex.Message });
            }
        }


























        // --- NEW API TO FETCH INDIVIDUAL DOCUMENTS FOR THE POPUP ---
        // --- UPDATED API TO FETCH INDIVIDUAL DOCUMENTS FOR THE POPUP ---
        [HttpGet("GetCapaDocuments/{capaId}")]
        public async Task<IActionResult> GetCapaDocuments(long capaId)
        {
            try
            {
                var capa = await _context.InspectionCapas.FindAsync(capaId);

                if (capa == null)
                {
                    return NotFound(new { success = false, message = "CAPA record not found." });
                }

                var documentList = new List<object>();
                string uploadDate = capa.CreatedDate.ToString("MM/dd/yyyy");

                // Initialize S3 Client to generate PreSigned URLs
                using (var s3Client = new AmazonS3Client(_awsAccessKey, _awsSecretKey, _awsRegion))
                {
                    // Parse PDF Docs
                    if (!string.IsNullOrEmpty(capa.PdfDocs))
                    {
                        var pdfs = capa.PdfDocs.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var pdf in pdfs)
                        {
                            string cleanKey = pdf.Trim();
                            string fileName = GetFileNameFromKey(cleanKey);
                            string url = GeneratePreSignedUrl(s3Client, cleanKey); // Generate AWS Link

                            documentList.Add(new { title = fileName, date = uploadDate, type = "document", url = url });
                        }
                    }

                    // Parse Image Docs
                    if (!string.IsNullOrEmpty(capa.ImageDocs))
                    {
                        var images = capa.ImageDocs.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var img in images)
                        {
                            string cleanKey = img.Trim();
                            string fileName = GetFileNameFromKey(cleanKey);
                            string url = GeneratePreSignedUrl(s3Client, cleanKey); // Generate AWS Link

                            documentList.Add(new { title = fileName, date = uploadDate, type = "image", url = url });
                        }
                    }
                }

                return Ok(new { success = true, data = documentList, message = "Documents fetched successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error fetching documents", error = ex.Message });
            }
        }

        // --- HELPER METHOD TO EXTRACT CLEAN FILE NAME FROM S3 KEY ---
        private string GetFileNameFromKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return "Unknown Document";

            // 1. Get the last segment after '/' (removes folder paths)
            var lastSegment = key.Split('/').LastOrDefault();
            if (string.IsNullOrEmpty(lastSegment)) return key;

            // 2. Remove the GUID (everything before the first '_')
            var firstUnderscoreIndex = lastSegment.IndexOf('_');

            // Ensure the underscore exists and isn't the very last character
            if (firstUnderscoreIndex >= 0 && firstUnderscoreIndex < lastSegment.Length - 1)
            {
                return lastSegment.Substring(firstUnderscoreIndex + 1);
            }

            return lastSegment; // Fallback if no underscore is found
        }













        public class CapaEditRequestDto
        {
            public long CapaId { get; set; }
            public DateTime? DueDate { get; set; }
            public DateTime? EtaDate { get; set; }
            public DateTime? CompletedDate { get; set; }
            public string? AuditorRemarks { get; set; }
            public string? AuditeeResponse { get; set; }
        }


        [HttpPut("UpdateCapaDetails")]
        public async Task<IActionResult> UpdateCapaDetails([FromBody] CapaEditRequestDto request)
        {
            try
            {
                // 1. Find the existing CAPA record
                var capa = await _context.InspectionCapas.FindAsync(request.CapaId);

                if (capa == null || capa.IsDeleted)
                {
                    return NotFound(new { success = false, message = "CAPA record not found." });
                }

                // 2. Update the specific fields from the UI
                capa.DueDate = request.DueDate;
                capa.EtaDate = request.EtaDate;
                capa.CompletedDate = request.CompletedDate;
                capa.AuditorRemarks = request.AuditorRemarks;
                capa.AuditeeResponse = request.AuditeeResponse;

                // 3. Update the tracking timestamp
                capa.ModifiedDate = DateTime.Now;

                // 4. Save changes to the database
                _context.InspectionCapas.Update(capa);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "CAPA details updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error updating CAPA details", error = ex.Message });
            }
        }











        [HttpDelete("DeleteCapa/{id}")]
        public async Task<IActionResult> DeleteCapa(long id)
        {
            try
            {
                // 1. Find the existing CAPA record
                var capa = await _context.InspectionCapas.FindAsync(id);

                // 2. Check if it exists and hasn't already been deleted
                if (capa == null || capa.IsDeleted)
                {
                    return NotFound(new { success = false, message = "CAPA record not found." });
                }

                
                capa.IsDeleted = true;
                capa.ModifiedDate = DateTime.Now;

                // 4. Save changes to the database
                _context.InspectionCapas.Update(capa);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "CAPA deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error deleting CAPA", error = ex.Message });
            }
        }


    }





















    }
