using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sqa_core.Models
{
    [Table("tbl_inspection_capa")]
    public class InspectionCapa
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]  
        public long CapaId { get; set; } 

        public long? InspectionRefId { get; set; }  

        public long? SeverityId { get; set; } 

        [StringLength(255)]
        public string? Subject { get; set; }

        public DateTime? DueDate { get; set; }

        public DateTime? CompletedDate { get; set; }

        [StringLength(50)]
        public string? PdcaStatus { get; set; }

        public int? Occurrence { get; set; }

        [StringLength(50)]
        public string? RiskRating { get; set; }

        [StringLength(50)]
        public string? Class { get; set; }

        [StringLength(100)]
        public string? ActionType { get; set; }

        public string? CapaSubject { get; set; }
        public string? Observations { get; set; }
        public string? CorrectiveActions { get; set; }
        public string? SupplierRemarks { get; set; }
        public string? PdfDocs { get; set; }
        public string? ImageDocs { get; set; }

        public int? Detection { get; set; }
        public int? SodScore { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;

        public int? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime? DeletedDate { get; set; }



        public long? Status { get; set; }
        public bool? Resolved { get; set; }
        public string? Description { get; set; }
        public DateTime? EtaDate { get; set; }
        public string? AuditorRemarks { get; set; }
        public string? AuditeeResponse { get; set; }
    }
}