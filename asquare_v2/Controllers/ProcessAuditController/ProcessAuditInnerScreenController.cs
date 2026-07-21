using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sqa_core.Data;
using sqa_core.Models;
using System.Security.Claims;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model; // Required for PutObjectRequest and GetPreSignedUrlRequest
using System.Linq;

namespace sqa_core.Controllers
{


    public class DeleteDocDto
    {
        public long ProcessAuditId { get; set; }
        public long ChecklistId { get; set; }
        public string FileUrl { get; set; }
    }


    [Route("api/[controller]")]
    [ApiController]
    public class ProcessAuditInnerScreenController : ControllerBase
    {
        private readonly AppDbContext _context;

        // AWS S3 Configuration
        private readonly string _awsAccessKey = "AKIA4S6IFT5TOJC7B5BS";
        private readonly string _awsSecretKey = "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";
        private readonly Amazon.RegionEndpoint _awsRegion = Amazon.RegionEndpoint.APSouth1;
        private readonly string _bucketName = "projects-pcmx-2026";

        public ProcessAuditInnerScreenController(AppDbContext context)
        {
            _context = context;
        }

        private long GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return string.IsNullOrEmpty(userIdClaim) ? 0 : long.Parse(userIdClaim);
        }

        // Helper Method to generate 60-minute links for frontend viewing
        private string GeneratePreSignedUrl(AmazonS3Client s3Client, string key)
        {
            if (string.IsNullOrEmpty(key)) return "";

            // Fallback: If it's already a full HTTP url from previous tests, just return it
            if (key.StartsWith("http")) return key;

            var request = new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = key,
                Expires = DateTime.UtcNow.AddMinutes(60) // Link expires in 1 hour
            };
            return s3Client.GetPreSignedURL(request);
        }

        // 🔥 GET API: Fetches existing data so the form isn't empty 🔥
        [HttpGet("get-response")]
        public async Task<IActionResult> GetResponse([FromQuery] long processAuditId, [FromQuery] long checklistId)
        {
            var data = await _context.ProcessAuditCAPAs
                .FirstOrDefaultAsync(x => x.ProcessAuditId == processAuditId && x.ChecklistId == checklistId && x.IsDeleted != true);

            if (data != null)
            {
                using (var s3Client = new AmazonS3Client(_awsAccessKey, _awsSecretKey, _awsRegion))
                {
                    // Convert stored S3 Keys to temporary Pre-Signed URLs for the frontend
                    if (!string.IsNullOrEmpty(data.ImageDocs))
                    {
                        var keys = data.ImageDocs.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        data.ImageDocs = string.Join(",", keys.Select(k => GeneratePreSignedUrl(s3Client, k)));
                    }

                    if (!string.IsNullOrEmpty(data.PdfDocs))
                    {
                        var keys = data.PdfDocs.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        data.PdfDocs = string.Join(",", keys.Select(k => GeneratePreSignedUrl(s3Client, k)));
                    }
                }
            }

            return Ok(new { Data = data, Success = true });
        }

        // 🔥 POST API: Saves or Updates the Record and Uploads to S3 🔥
        [HttpPost("save-checklist-response")]
        public async Task<IActionResult> SaveChecklistResponse([FromForm] string jsonData, [FromForm] IFormFileCollection files)
        {
            var model = JsonSerializer.Deserialize<ProcessAuditCAPA>(jsonData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (model == null) return BadRequest(new { Message = "Invalid data payload.", Success = false });

            long currentUserId = GetCurrentUserId();

            // 1. Calculate SOD Score safely
            string sodString = $"{model.SeverityId ?? 0}{model.Occurrence ?? 0}{model.Detection ?? 0}";
            model.SodScore = int.TryParse(sodString, out int sod) ? sod : 0;

            // 2. Wipe CAPA fields if Compliance is 'Pass'
            if (model.Compliance == "Pass")
            {
                model.Class = null;
                model.CapaSubject = null;
                model.DueDate = null;
                model.CompletedDate = null;
                model.PdcaStatus = null;
                model.IsResolved = false;
                model.ActionType = null;
                model.Remarks = null;
                model.CorrectiveActions = null;
                model.SupplierRemarks = null;
            }

            // 3. Check if a record for this Audit AND Checklist Question already exists
            var existingRecord = await _context.ProcessAuditCAPAs
                .FirstOrDefaultAsync(x => x.ProcessAuditId == model.ProcessAuditId && x.ChecklistId == model.ChecklistId && x.IsDeleted != true);

            // 4. AWS S3 Upload Logic
            string newPdfKeys = "";
            string newImageKeys = "";

            if (files != null && files.Count > 0)
            {
                List<string> pdfKeysList = new List<string>();
                List<string> imageKeysList = new List<string>();

                using (var s3Client = new AmazonS3Client(_awsAccessKey, _awsSecretKey, _awsRegion))
                {
                    foreach (var file in files)
                    {
                        // Create a clean key to store in the DB (e.g., process-audit-docs/5/filename.jpg)
                        string s3Key = $"process-audit-docs/{model.ProcessAuditId}/{Guid.NewGuid()}_{file.FileName}";

                        using (var stream = file.OpenReadStream())
                        {
                            var putRequest = new PutObjectRequest
                            {
                                BucketName = _bucketName,
                                Key = s3Key,
                                InputStream = stream,
                                ContentType = file.ContentType
                            };

                            // Notice: NO CannedACL here. This fixes the AWS 500 Crash!
                            await s3Client.PutObjectAsync(putRequest);
                        }

                        // Store only the keys, not the full URLs
                        if (file.ContentType.Contains("image")) imageKeysList.Add(s3Key);
                        else pdfKeysList.Add(s3Key);
                    }
                }

                newPdfKeys = string.Join(",", pdfKeysList);
                newImageKeys = string.Join(",", imageKeysList);
            }

            // 5. UPSERT LOGIC (Update if exists, Insert if new)
            if (existingRecord != null)
            {
                // UPDATE EXISTING RECORD
                existingRecord.Rating = model.Rating;
                existingRecord.SeverityId = model.SeverityId;
                existingRecord.Occurrence = model.Occurrence;
                existingRecord.Detection = model.Detection;
                existingRecord.SodScore = model.SodScore;
                existingRecord.Compliance = model.Compliance;

                existingRecord.Class = model.Class;
                existingRecord.CapaSubject = model.CapaSubject;
                existingRecord.DueDate = model.DueDate;
                existingRecord.CompletedDate = model.CompletedDate;
                existingRecord.PdcaStatus = model.PdcaStatus;
                existingRecord.IsResolved = model.IsResolved;
                existingRecord.ActionType = model.ActionType;
                existingRecord.Remarks = model.Remarks;
                existingRecord.CorrectiveActions = model.CorrectiveActions;
                existingRecord.SupplierRemarks = model.SupplierRemarks;

                // Append new files to existing files (comma separated)
                if (!string.IsNullOrEmpty(newPdfKeys))
                    existingRecord.PdfDocs = string.IsNullOrEmpty(existingRecord.PdfDocs) ? newPdfKeys : existingRecord.PdfDocs + "," + newPdfKeys;
                if (!string.IsNullOrEmpty(newImageKeys))
                    existingRecord.ImageDocs = string.IsNullOrEmpty(existingRecord.ImageDocs) ? newImageKeys : existingRecord.ImageDocs + "," + newImageKeys;

                existingRecord.ModifiedBy = currentUserId;
                existingRecord.ModifiedDate = DateTime.UtcNow;

                // Ensure a Reference No gets generated if they changed Compliance from Pass to Fail
                if (existingRecord.Compliance == "Fail" && string.IsNullOrEmpty(existingRecord.ReferenceNo))
                {
                    existingRecord.ReferenceNo = $"{DateTime.UtcNow.Year}/CAPA/{existingRecord.CapaId:D6}";
                }

                await _context.SaveChangesAsync();
                return Ok(new { Message = "Audit Response Updated Successfully", Success = true });
            }
            else
            {
                // INSERT NEW RECORD
                model.PdfDocs = newPdfKeys;
                model.ImageDocs = newImageKeys;
                model.CreatedBy = currentUserId;
                model.CreatedDate = DateTime.UtcNow;
                model.IsActive = true;

                _context.ProcessAuditCAPAs.Add(model);
                await _context.SaveChangesAsync();

                // Generate ReferenceNo if it's a Fail
                if (model.Compliance == "Fail")
                {
                    model.ReferenceNo = $"{DateTime.UtcNow.Year}/CAPA/{model.CapaId:D6}";
                    await _context.SaveChangesAsync();
                }

                return Ok(new { Message = "Audit Response Saved Successfully", Success = true });
            }
        }



        // 🔥 1. Fetches all CAPA records for the Grid 🔥
        [HttpGet("get-all-capas")]
        public async Task<IActionResult> GetAllCapas()
        {
            var data = await (from c in _context.ProcessAuditCAPAs
                              join a in _context.ProcessAudits on c.ProcessAuditId equals a.ProcessAuditId
                              join s in _context.SupplierMasters on a.SupplierId equals s.SupplierId into asup
                              from s in asup.DefaultIfEmpty()
                              join pc in _context.ProcessCategories on c.ProcessCategoryId equals pc.ProcessCategoryId into apc
                              from pc in apc.DefaultIfEmpty()
                              where c.IsDeleted != true && c.Compliance == "Fail" // ONLY SHOW FAILED COMPLIANCE
                              orderby c.CreatedDate descending
                              select new
                              {
                                  c.CapaId,
                                  c.ProcessAuditId,       
                                  c.ProcessCategoryId,   
                                  c.ChecklistId,        
                                  Status = string.IsNullOrEmpty(c.Status) ? "Open" : c.Status,
                                  Resolved = c.IsResolved ?? false,

                                  // Count total attached files to show in "Docs" column
                                  Docs = (string.IsNullOrEmpty(c.PdfDocs) ? 0 : c.PdfDocs.Split(',', StringSplitOptions.RemoveEmptyEntries).Length) +
                                         (string.IsNullOrEmpty(c.ImageDocs) ? 0 : c.ImageDocs.Split(',', StringSplitOptions.RemoveEmptyEntries).Length),

                                  Reference = c.ReferenceNo,
                                  ActionSubject = c.CapaSubject,
                                  SupplierName = s != null ? s.SupplierName : "",
                                  c.ActionType,
                                  a.AuditReference,
                                  ProcessCategory = pc != null ? pc.Name : "",
                                  Description = c.Remarks,
                                  c.SupplierRemarks,
                                  LogDate = c.CreatedDate,
                                  c.DueDate,
                                  Completion = c.CompletedDate,

                                  // Calculate delay in days safely
                                  DelayInDays = c.DueDate.HasValue && c.CompletedDate == null && c.DueDate.Value < DateTime.UtcNow
                                                ? (DateTime.UtcNow - c.DueDate.Value).Days : 0,

                                  Severity = c.SeverityId,
                                  c.Occurrence,
                                  c.Detection,
                                  RiskRating = (c.SodScore >= 800) ? "High" : (c.SodScore >= 400) ? "Medium" : "Low",
                                  c.Rating,
                                  c.PdcaStatus  
                              }).ToListAsync();

            return Ok(new { Data = data, Success = true });
        }

        // 🔥 2. Instantly updates Status or Resolved checkbox from the Grid 🔥
        [HttpPost("update-capa-status")]
        public async Task<IActionResult> UpdateCapaStatus([FromBody] ProcessAuditCAPA model)
        {
            var dbItem = await _context.ProcessAuditCAPAs.FindAsync(model.CapaId);
            if (dbItem == null) return NotFound(new { Message = "CAPA not found", Success = false });

            dbItem.Status = model.Status;
            dbItem.IsResolved = model.IsResolved;
            dbItem.ModifiedBy = GetCurrentUserId(); 
            dbItem.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Status Updated", Success = true });
        }


        [HttpPost("delete-document")]
        public async Task<IActionResult> DeleteDocument([FromBody] DeleteDocDto request)
        {
            var dbItem = await _context.ProcessAuditCAPAs
                .FirstOrDefaultAsync(x => x.ProcessAuditId == request.ProcessAuditId && x.ChecklistId == request.ChecklistId && x.IsDeleted != true);

            if (dbItem == null) return NotFound(new { Message = "Record not found", Success = false });

            bool removed = false;

            // Safely removes the S3 key from the comma-separated string
            string RemoveKey(string existingKeys, string urlToRemove)
            {
                if (string.IsNullOrEmpty(existingKeys)) return existingKeys;

                var keys = existingKeys.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

                // Match the Pre-Signed URL with the stored S3 key
                var keyToRemove = keys.FirstOrDefault(k => urlToRemove.Contains(Uri.EscapeDataString(k)) || urlToRemove.Contains(k));

                if (keyToRemove != null)
                {
                    keys.Remove(keyToRemove);
                    removed = true;
                }
                return string.Join(",", keys);
            }

            // Try removing from PDF list first, if not found, try Image list
            dbItem.PdfDocs = RemoveKey(dbItem.PdfDocs, request.FileUrl);
            if (!removed) dbItem.ImageDocs = RemoveKey(dbItem.ImageDocs, request.FileUrl);

            if (removed)
            {
                dbItem.ModifiedBy = GetCurrentUserId();
                dbItem.ModifiedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return Ok(new { Message = "Document deleted successfully", Success = true });
            }

            return BadRequest(new { Message = "Document not found in record", Success = false });
        }
    }
}