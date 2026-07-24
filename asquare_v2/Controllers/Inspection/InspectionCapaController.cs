using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sqa_core.Data;
using sqa_core.Models;
using sqa_core.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using System.Collections.Generic;

namespace sqa_core.Controllers
{

    public class CapaUploadRequest
    {
        public string jsonData { get; set; }
        public List<IFormFile> files { get; set; }
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
                dbItem.ModifiedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return Ok(new { Message = "Document deleted successfully", Success = true });
            }

            return BadRequest(new { Message = "Document not found in record", Success = false });
        }
    }
}