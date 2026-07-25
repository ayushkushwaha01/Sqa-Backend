using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sqa_core.Data;
using sqa_core.Models;
using sqa_core.Configuration;
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

        // AWS S3 Configuration — sourced from ConfigKey (git-ignored file)
        private readonly string _awsAccessKey = ConfigKey.Aws.AccessKey;
        private readonly string _awsSecretKey = ConfigKey.Aws.SecretKey;
        private readonly Amazon.RegionEndpoint _awsRegion = Amazon.RegionEndpoint.GetBySystemName(ConfigKey.Aws.Region);
        private readonly string _bucketName = ConfigKey.Aws.BucketName;

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

            if (key.StartsWith("http")) return key;

            var request = new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = key,
                Expires = DateTime.UtcNow.AddMinutes(60)
            };
            return s3Client.GetPreSignedURL(request);
        }

        [HttpGet("get-response")]
        public async Task<IActionResult> GetResponse([FromQuery] long processAuditId, [FromQuery] long checklistId)
        {
            var data = await _context.ProcessAuditCAPAs
                .FirstOrDefaultAsync(x => x.ProcessAuditId == processAuditId && x.ChecklistId == checklistId && x.IsDeleted != true);

            if (data != null)
            {
                using (var s3Client = new AmazonS3Client(_awsAccessKey, _awsSecretKey, _awsRegion))
                {
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

        [HttpPost("save-checklist-response")]
        public async Task<IActionResult> SaveChecklistResponse([FromForm] string jsonData, [FromForm] IFormFileCollection files)
        {
            var model = JsonSerializer.Deserialize<ProcessAuditCAPA>(jsonData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (model == null) return BadRequest(new { Message = "Invalid data payload.", Success = false });

            long currentUserId = GetCurrentUserId();

            string sodString = $"{model.SeverityId ?? 0}{model.Occurrence ?? 0}{model.Detection ?? 0}";
            model.SodScore = int.TryParse(sodString, out int sod) ? sod : 0;

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

            var existingRecord = await _context.ProcessAuditCAPAs
                .FirstOrDefaultAsync(x => x.ProcessAuditId == model.ProcessAuditId && x.ChecklistId == model.ChecklistId && x.IsDeleted != true);

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

                            await s3Client.PutObjectAsync(putRequest);
                        }

                        if (file.ContentType.Contains("image")) imageKeysList.Add(s3Key);
                        else pdfKeysList.Add(s3Key);
                    }
                }

                newPdfKeys = string.Join(",", pdfKeysList);
                newImageKeys = string.Join(",", imageKeysList);
            }

            if (existingRecord != null)
            {
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

                if (!string.IsNullOrEmpty(newPdfKeys))
                    existingRecord.PdfDocs = string.IsNullOrEmpty(existingRecord.PdfDocs) ? newPdfKeys : existingRecord.PdfDocs + "," + newPdfKeys;
                if (!string.IsNullOrEmpty(newImageKeys))
                    existingRecord.ImageDocs = string.IsNullOrEmpty(existingRecord.ImageDocs) ? newImageKeys : existingRecord.ImageDocs + "," + newImageKeys;

                existingRecord.ModifiedBy = currentUserId;
                existingRecord.ModifiedDate = DateTime.UtcNow;

                if (existingRecord.Compliance == "Fail" && string.IsNullOrEmpty(existingRecord.ReferenceNo))
                {
                    existingRecord.ReferenceNo = $"{DateTime.UtcNow.Year}/CAPA/{existingRecord.CapaId:D6}";
                }

                await _context.SaveChangesAsync();
                return Ok(new { Message = "Audit Response Updated Successfully", Success = true });
            }
            else
            {
                model.PdfDocs = newPdfKeys;
                model.ImageDocs = newImageKeys;
                model.CreatedBy = currentUserId;
                model.CreatedDate = DateTime.UtcNow;
                model.IsActive = true;

                _context.ProcessAuditCAPAs.Add(model);
                await _context.SaveChangesAsync();

                if (model.Compliance == "Fail")
                {
                    model.ReferenceNo = $"{DateTime.UtcNow.Year}/CAPA/{model.CapaId:D6}";
                    await _context.SaveChangesAsync();
                }

                return Ok(new { Message = "Audit Response Saved Successfully", Success = true });
            }
        }

        //[HttpGet("get-all-capas")]
        //public async Task<IActionResult> GetAllCapas()
        //{
        //    var data = await (from c in _context.ProcessAuditCAPAs
        //                      join a in _context.ProcessAudits on c.ProcessAuditId equals a.ProcessAuditId
        //                      join s in _context.SupplierMasters on a.SupplierId equals s.SupplierId into asup
        //                      from s in asup.DefaultIfEmpty()
        //                      join pc in _context.ProcessCategories on c.ProcessCategoryId equals pc.ProcessCategoryId into apc
        //                      from pc in apc.DefaultIfEmpty()
        //                      where c.IsDeleted != true && c.Compliance == "Fail"
        //                      orderby c.CreatedDate descending
        //                      select new
        //                      {
        //                          c.CapaId,
        //                          c.ProcessAuditId,
        //                          c.ProcessCategoryId,
        //                          c.ChecklistId,
        //                          Status = string.IsNullOrEmpty(c.Status) ? "Open" : c.Status,
        //                          Resolved = c.IsResolved ?? false,
        //                          Docs = (string.IsNullOrEmpty(c.PdfDocs) ? 0 : c.PdfDocs.Split(',', StringSplitOptions.RemoveEmptyEntries).Length) +
        //                                 (string.IsNullOrEmpty(c.ImageDocs) ? 0 : c.ImageDocs.Split(',', StringSplitOptions.RemoveEmptyEntries).Length),
        //                          Reference = c.ReferenceNo,
        //                          ActionSubject = c.CapaSubject,
        //                          SupplierName = s != null ? s.SupplierName : "",
        //                          c.ActionType,
        //                          a.AuditReference,
        //                          ProcessCategory = pc != null ? pc.Name : "",
        //                          Description = c.Remarks,
        //                          c.SupplierRemarks,
        //                          LogDate = c.CreatedDate,
        //                          c.DueDate,
        //                          Completion = c.CompletedDate,
        //                          DelayInDays = c.DueDate.HasValue && c.CompletedDate == null && c.DueDate.Value < DateTime.UtcNow
        //                                        ? (DateTime.UtcNow - c.DueDate.Value).Days : 0,
        //                          Severity = c.SeverityId,
        //                          c.Occurrence,
        //                          c.Detection,
        //                          RiskRating = (c.SodScore >= 800) ? "High" : (c.SodScore >= 400) ? "Medium" : "Low",
        //                          c.Rating,
        //                          c.PdcaStatus
        //                      }).ToListAsync();

        //    return Ok(new { Data = data, Success = true });
        //}

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

            string RemoveKey(string existingKeys, string urlToRemove)
            {
                if (string.IsNullOrEmpty(existingKeys)) return existingKeys;

                var keys = existingKeys.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
                var keyToRemove = keys.FirstOrDefault(k => urlToRemove.Contains(Uri.EscapeDataString(k)) || urlToRemove.Contains(k));

                if (keyToRemove != null)
                {
                    keys.Remove(keyToRemove);
                    removed = true;
                }
                return string.Join(",", keys);
            }

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

        //supplier

        private string GetCurrentUserType()
        {
            var userTypeClaim = User.FindFirstValue("UserType");
            return string.IsNullOrEmpty(userTypeClaim) ? "Internal" : userTypeClaim;
        }

        [HttpGet("get-all-capas")]
        public async Task<IActionResult> GetAllCapas()
        {
            long currentUserId = GetCurrentUserId();
            string userType = GetCurrentUserType(); // 🔥 Get the UserType

            var query = from c in _context.ProcessAuditCAPAs
                        join a in _context.ProcessAudits on c.ProcessAuditId equals a.ProcessAuditId
                        join s in _context.SupplierMasters on a.SupplierId equals s.SupplierId into asup
                        from s in asup.DefaultIfEmpty()
                        join pc in _context.ProcessCategories on c.ProcessCategoryId equals pc.ProcessCategoryId into apc
                        from pc in apc.DefaultIfEmpty()
                        where c.IsDeleted != true && c.Compliance == "Fail"
                        select new { c, a, s, pc };

            // 🔥 If logged in as Supplier, only return CAPAs tied to audits assigned to their SupplierId
            if (userType == "Supplier")
            {
                query = query.Where(x => x.a.SupplierId == currentUserId);
            }

            var data = await query.OrderByDescending(x => x.c.CreatedDate).Select(x => new
            {
                x.c.CapaId,
                x.c.ProcessAuditId,
                x.c.ProcessCategoryId,
                x.c.ChecklistId,
                Status = string.IsNullOrEmpty(x.c.Status) ? "Open" : x.c.Status,
                Resolved = x.c.IsResolved ?? false,
                Docs = (string.IsNullOrEmpty(x.c.PdfDocs) ? 0 : x.c.PdfDocs.Split(',', StringSplitOptions.RemoveEmptyEntries).Length) +
                       (string.IsNullOrEmpty(x.c.ImageDocs) ? 0 : x.c.ImageDocs.Split(',', StringSplitOptions.RemoveEmptyEntries).Length),
                Reference = x.c.ReferenceNo,
                ActionSubject = x.c.CapaSubject,
                SupplierName = x.s != null ? x.s.SupplierName : "",
                x.c.ActionType,
                x.a.AuditReference,
                ProcessCategory = x.pc != null ? x.pc.Name : "",
                Description = x.c.Remarks,
                x.c.SupplierRemarks,
                LogDate = x.c.CreatedDate,
                x.c.DueDate,
                Completion = x.c.CompletedDate,
                DelayInDays = x.c.DueDate.HasValue && x.c.CompletedDate == null && x.c.DueDate.Value < DateTime.UtcNow
                              ? (DateTime.UtcNow - x.c.DueDate.Value).Days : 0,
                Severity = x.c.SeverityId,
                x.c.Occurrence,
                x.c.Detection,
                RiskRating = (x.c.SodScore >= 800) ? "High" : (x.c.SodScore >= 400) ? "Medium" : "Low",
                x.c.Rating,
                x.c.PdcaStatus
            }).ToListAsync();

            return Ok(new { Data = data, Success = true });
        }
    }
}