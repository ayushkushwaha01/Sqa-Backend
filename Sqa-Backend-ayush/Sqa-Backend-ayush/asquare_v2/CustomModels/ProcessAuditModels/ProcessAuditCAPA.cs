using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sqa_core.Models
{
    [Table("Sqa_ProcessAuditCAPA")]
    public class ProcessAuditCAPA
    {
        [Key]
        public long CapaId { get; set; }

        [Required]
        public long ProcessAuditId { get; set; }

        public long? ProcessCategoryId { get; set; }
        public long? ChecklistId { get; set; }

        public string? Rating { get; set; }
        public long? SeverityId { get; set; }
        public int? Occurrence { get; set; }
        public int? Detection { get; set; }
        public int? SodScore { get; set; }

        // AWS S3 File URLs
        public string? PdfDocs { get; set; }
        public string? ImageDocs { get; set; }

        public string? Compliance { get; set; }

        // --- CAPA Fields (Populated if Compliance is 'Fail') ---
        public string? ReferenceNo { get; set; }
        public string? Class { get; set; }
        public string? CapaSubject { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string? PdcaStatus { get; set; }
        public bool? IsResolved { get; set; }
        public string? ActionType { get; set; }
        public string? Remarks { get; set; }
        public string? CorrectiveActions { get; set; }
        public string? SupplierRemarks { get; set; }

        // --- Standard Audit Fields ---
        public bool? IsActive { get; set; } = true;
        public bool? IsDeleted { get; set; } = false;

        [Required]
        public long CreatedBy { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; }

        public long? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public long? DeletedBy { get; set; }
        public DateTime? DeletedDate { get; set; }

        public string? Status { get; set; } = "Open"; // Default to Open when created
    }
}