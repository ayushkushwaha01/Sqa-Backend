using Amazon.S3.Model;
using Amazon.S3;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sqa_core.Configuration;
using sqa_core.Data;
using sqa_core.Models;

namespace sqa_core.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PartsAuditInnerScreenController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly string _awsAccessKey = ConfigKey.Aws.AccessKey;
        private readonly string _awsSecretKey = ConfigKey.Aws.SecretKey;
        private readonly Amazon.RegionEndpoint _awsRegion = Amazon.RegionEndpoint.GetBySystemName(ConfigKey.Aws.Region);
        private readonly string _bucketName = ConfigKey.Aws.BucketName;

        public PartsAuditInnerScreenController(AppDbContext context)
        {
            _context = context;
        }


        private long GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return string.IsNullOrEmpty(userIdClaim) ? 0 : long.Parse(userIdClaim);
        }

        private string GeneratePreSignedUrl(AmazonS3Client s3Client, string key)
        {
            if (string.IsNullOrEmpty(key))
                return "";

            if (key.StartsWith("http"))
                return key;

            var request = new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = key,
                Expires = DateTime.UtcNow.AddMinutes(60)
            };

            return s3Client.GetPreSignedURL(request);
        }




        [HttpPost("upload-docs")]
        public async Task<IActionResult> UploadDocs(
    [FromForm] long partAuditId,
    [FromForm] long auditParameterId,
    [FromForm] IFormFileCollection files)
        {
            if (files == null || files.Count == 0)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "No files selected."
                });
            }

            long currentUserId = GetCurrentUserId();

            using var s3Client = new AmazonS3Client(_awsAccessKey, _awsSecretKey, _awsRegion);

            foreach (var file in files)
            {
                string key = $"parts-audit/docs/{partAuditId}/{Guid.NewGuid()}_{file.FileName}";

                using (var stream = file.OpenReadStream())
                {
                    await s3Client.PutObjectAsync(new PutObjectRequest
                    {
                        BucketName = _bucketName,
                        Key = key,
                        InputStream = stream,
                        ContentType = file.ContentType
                    });
                }

                _context.PartsAuditCapaDocs.Add(new PartsAuditCapaDoc
                {
                    DocTitlte = file.FileName,
                    Docurl = key,
                    PartAuditId = partAuditId,
                    AuditParameterId = auditParameterId,
                    CreatedBy = currentUserId,
                    CreatedDate = DateTime.UtcNow,
                    IsActive = true,
                    IsDeleted = false
                });
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Success = true,
                Message = "Document Uploaded Successfully"
            });
        }


        [HttpGet("get-docs")]
        public async Task<IActionResult> GetDocs(long auditParameterId)
        {
            var data = await _context.PartsAuditCapaDocs
                .Where(x => x.AuditParameterId == auditParameterId &&
                            x.IsDeleted != true)
                .ToListAsync();

            using var s3Client = new AmazonS3Client(_awsAccessKey, _awsSecretKey, _awsRegion);

            foreach (var item in data)
            {
                item.Docurl = GeneratePreSignedUrl(s3Client, item.Docurl);

                // Add this temporarily
                Console.WriteLine(item.Docurl);
            }

            return Ok(new
            {
                Success = true,
                TotalRecords = data.Count,
                Data = data,
               
            });
        }


        [HttpPost("upload-images")]
        public async Task<IActionResult> UploadImages(
    [FromForm] long partAuditId,
    [FromForm] long auditParameterId,
    [FromForm] IFormFileCollection files)
        {
            if (files == null || files.Count == 0)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "No images selected."
                });
            }

            long currentUserId = GetCurrentUserId();

            using var s3Client = new AmazonS3Client(_awsAccessKey, _awsSecretKey, _awsRegion);

            foreach (var file in files)
            {
                string key = $"parts-audit/images/{partAuditId}/{Guid.NewGuid()}_{file.FileName}";

                using (var stream = file.OpenReadStream())
                {
                    await s3Client.PutObjectAsync(new PutObjectRequest
                    {
                        BucketName = _bucketName,
                        Key = key,
                        InputStream = stream,
                        ContentType = file.ContentType
                    });
                }

                _context.PartsAuditCapaImages.Add(new PartsAuditCapaImage
                {
                    ImageTitlte = file.FileName,
                    Imageurl = key,
                    PartAuditId = partAuditId,
                    AuditParameterId = auditParameterId,
                    CreatedBy = currentUserId,
                    CreatedDate = DateTime.UtcNow,
                    IsActive = true,
                    IsDeleted = false
                });
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Success = true,
                Message = "Images Uploaded Successfully"
            });
        }

        [HttpPost("okay-status-change")]
        public async Task<IActionResult> OkayStatusChange([FromBody] PartsAuditParameter model)
        {
            var dbItem = await _context.PartsAuditParameters
                .FirstOrDefaultAsync(x => x.AuditParameterId == model.AuditParameterId);

            if (dbItem == null)
            {
                return NotFound(new
                {
                    Success = false,
                    Message = "Record Not Found"
                });
            }

            dbItem.Okay = !(dbItem.Okay ?? false);
            dbItem.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Success = true,
                Message = " Status Updated Successfully",
                Okay = dbItem.Okay
            });
        }



        [HttpPost("upsert-capa")]
        public async Task<IActionResult> UpsertCapa([FromBody] PartsAuditCapaModel model)
        {
            if (model == null)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "Invalid Data"
                });
            }

            PartsAuditCapa entity;

            if (model.PartAuditCapaId > 0)
            {
                entity = await _context.PartsAuditCapas
                    .FirstOrDefaultAsync(x => x.PartAuditCapaId == model.PartAuditCapaId);

                if (entity == null)
                {
                    return NotFound(new
                    {
                        Success = false,
                        Message = "Record Not Found"
                    });
                }

                entity.ModifiedDate = DateTime.UtcNow;
            }
            else
            {
                entity = new PartsAuditCapa();

                entity.CreatedDate = DateTime.UtcNow;
                entity.IsActive = true;
                entity.IsDeleted = false;

                _context.PartsAuditCapas.Add(entity);
            }

            entity.PartAuditId = model.PartAuditId;
            entity.AuditParameterId = model.AuditParameterId;
            entity.Subject = model.Subject;
            entity.DueDate = model.DueDate;
            entity.CompletedDate = model.CompletedDate;
            entity.PDCAStatus = model.PDCAStatus;
            entity.SeverityId = model.SeverityId;
            entity.Occurrence = model.Occurrence;
            entity.Detection = model.Detection;
            entity.SODScore = model.SODScore;
            entity.RiskRating = model.RiskRating;
            entity.IsResolved = model.IsResolved;
            entity.Class = model.Class;
            entity.ActionType = model.ActionType;
            entity.CapaSubject = model.CapaSubject;
            entity.Observations = model.Observations;
            entity.CorrectiveActions = model.CorrectiveActions;
            entity.SupplierRemarks = model.SupplierRemarks;
            entity.PDFID = model.PDFID;
            entity.ImageID = model.ImageID;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Success = true,
                Message = model.PartAuditCapaId > 0
                    ? "CAPA Updated Successfully"
                    : "CAPA Saved Successfully",
                Data = entity
            });
        }




        [HttpGet("get-capa")]
        public async Task<IActionResult> GetCapa(long? auditParameterId)
        {
            var query = _context.PartsAuditCapas
                .Where(x => x.IsDeleted != true);

            if (auditParameterId.HasValue)
            {
                query = query.Where(x => x.AuditParameterId == auditParameterId.Value);
            }

            var capa = await query
                .OrderByDescending(x => x.PartAuditCapaId)
                .FirstOrDefaultAsync();

            if (capa == null)
            {
                return Ok(new
                {
                    Success = true,
                    Data = (object?)null
                });
            }

            using var s3Client = new AmazonS3Client(_awsAccessKey, _awsSecretKey, _awsRegion);

            var docs = await _context.PartsAuditCapaDocs
                .Where(x => x.AuditParameterId == capa.AuditParameterId && x.IsDeleted != true)
                .OrderByDescending(x => x.DocId)
                .ToListAsync();

            foreach (var item in docs)
            {
                item.Docurl = GeneratePreSignedUrl(s3Client, item.Docurl);
            }

            var images = await _context.PartsAuditCapaImages
                .Where(x => x.AuditParameterId == capa.AuditParameterId && x.IsDeleted != true)
                .OrderByDescending(x => x.ImageId)
                .ToListAsync();

            foreach (var item in images)
            {
                item.Imageurl = GeneratePreSignedUrl(s3Client, item.Imageurl);
            }

            return Ok(new
            {
                Success = true,
                Data = new
                {
                    Capa = capa,
                    Documents = docs,
                    Images = images
                }
            });
        }





        [HttpPost("resolved-status-change")]
        public async Task<IActionResult> ResolvedStatusChange([FromBody] PartsAuditCapa model)
        {
            var dbItem = await _context.PartsAuditCapas
                .FirstOrDefaultAsync(x => x.PartAuditCapaId == model.PartAuditCapaId);

            if (dbItem == null)
            {
                return NotFound(new
                {
                    Success = false,
                    Message = "Record Not Found"
                });
            }

            dbItem.IsResolved = !(dbItem.IsResolved ?? false);
            dbItem.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Success = true,
                Message = "Resolved Status Updated Successfully",
                IsResolved = dbItem.IsResolved
            });
        }




        [HttpGet("get-all-capas")]
        public async Task<IActionResult> GetCapas([FromQuery] PartsAuditCapaFilter filter)
        {
            var query = _context.PartsAuditCapas
                .Where(x => x.IsDeleted != true);

            if (filter.AuditParameterId.HasValue)
            {
                query = query.Where(x => x.AuditParameterId == filter.AuditParameterId);
            }

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var keyword = filter.Keyword.Trim().ToLower();

                query = query.Where(x =>
                    (x.Subject ?? "").ToLower().Contains(keyword) ||
                    (x.CapaSubject ?? "").ToLower().Contains(keyword) ||
                    (x.Observations ?? "").ToLower().Contains(keyword) ||
                    (x.SupplierRemarks ?? "").ToLower().Contains(keyword));
            }

            if (!string.IsNullOrWhiteSpace(filter.ActionType))
            {
                query = query.Where(x => x.ActionType == filter.ActionType);
            }

            if (filter.FromDate.HasValue)
            {
                query = query.Where(x =>
                    x.DueDate.HasValue &&
                    x.DueDate.Value.Date >= filter.FromDate.Value.Date);
            }

            if (filter.ToDate.HasValue)
            {
                query = query.Where(x =>
                    x.DueDate.HasValue &&
                    x.DueDate.Value.Date <= filter.ToDate.Value.Date);
            }

            var data = await (
     from capa in query
     join severity in _context.SeverityMasters
         on capa.SeverityId equals severity.SeverityId into severityGroup
     from severity in severityGroup.DefaultIfEmpty()

     orderby capa.PartAuditCapaId descending

     select new
     {
         capa.PartAuditCapaId,
         capa.PartAuditId,
         capa.AuditParameterId,
         capa.Subject,
         capa.DueDate,
         capa.CompletedDate,
         capa.PDCAStatus,

         capa.SeverityId,
         SeverityName = severity != null ? severity.SeverityName : "",
         SeverityRating = severity != null ? severity.Rating : (int?)null,

         capa.Occurrence,
         capa.Detection,
         capa.SODScore,
         capa.RiskRating,
         capa.IsResolved,
         capa.Class,
         capa.ActionType,
         capa.CapaSubject,
         capa.Observations,
         capa.CorrectiveActions,
         capa.SupplierRemarks,
         capa.PDFID,
         capa.ImageID,
         capa.IsActive,
         capa.IsDeleted,
         capa.CreatedBy,
         capa.CreatedDate,
         capa.StatusId,

         DelayInDays =
             capa.CompletedDate.HasValue && capa.DueDate.HasValue
                 ? EF.Functions.DateDiffDay(capa.DueDate.Value, capa.CompletedDate.Value)
                 : (int?)null,

         DocCount = _context.PartsAuditCapaDocs.Count(d =>
             d.AuditParameterId == capa.AuditParameterId &&
             d.IsDeleted != true),

         ImageCount = _context.PartsAuditCapaImages.Count(i =>
             i.AuditParameterId == capa.AuditParameterId &&
             i.IsDeleted != true)
     }
 ).ToListAsync();

            return Ok(new
            {
                Success = true,
                Data = new
                {
                    TotalRecords = data.Count,
                    Data = data
                }
            });
        }




        [HttpPost("delete-doc")]
        public async Task<IActionResult> DeleteDoc([FromBody] PartsAuditCapaModel model)
        {
            var dbItem = await _context.PartsAuditCapaDocs
                .FindAsync(model.DocId);

            if (dbItem == null)
            {
                return NotFound(new
                {
                    Success = false,
                    Message = "Document Not Found"
                });
            }

            dbItem.IsDeleted = true;
            dbItem.DeletedBy = model.DeletedBy;
            dbItem.DeletedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Success = true,
                Message = "Document Deleted Successfully"
            });
        }



        [HttpPost("delete-capa")]
        public async Task<IActionResult> DeleteCapa([FromBody] PartsAuditCapaModel model)
        {
            var dbItem = await _context.PartsAuditCapas
                .FindAsync(model.PartAuditCapaId);

            if (dbItem == null)
            {
                return NotFound(new
                {
                    Success = false,
                    Message = "CAPA Record Not Found"
                });
            }

            dbItem.IsDeleted = true;
            dbItem.DeletedBy = model.DeletedBy;
            dbItem.DeletedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Success = true,
                Message = "CAPA Deleted Successfully"
            });
        }

        [HttpPost("update-capa-status")]
        public async Task<IActionResult> UpdateCapaStatus([FromBody] PartsAuditCapa model)
        {
            var dbItem = await _context.PartsAuditCapas
                .FirstOrDefaultAsync(x => x.PartAuditCapaId == model.PartAuditCapaId
                                       && x.IsDeleted != true);

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


    }
}