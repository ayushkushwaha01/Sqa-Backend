using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace sqa_core.Models
{
    public class PartsAuditInnerScreen
{
}


    [Table("tbl_Parts_audit_capa")]
    public class PartsAuditCapa
    {
        [Key]
        public long PartAuditCapaId { get; set; }

        public long? PartAuditId { get; set; }

        public long? AuditParameterId { get; set; }

        public string? Subject { get; set; }

        public DateTime? DueDate { get; set; }

        public DateTime? CompletedDate { get; set; }

        public string? PDCAStatus { get; set; }

        public long? SeverityId { get; set; }

        public long? Occurrence { get; set; }

        public long? Detection { get; set; }

        public long? SODScore { get; set; }

        public string? RiskRating { get; set; }

        public bool? IsResolved { get; set; }

        public string? Class { get; set; }

        public string? ActionType { get; set; }

        public string? CapaSubject { get; set; }

        public string? Observations { get; set; }

        public string? CorrectiveActions { get; set; }

        public string? SupplierRemarks { get; set; }

        public long? PDFID { get; set; }

        public long? ImageID { get; set; }

        public bool? IsActive { get; set; }

        public bool? IsDeleted { get; set; }

        public long? CreatedBy { get; set; }

        public DateTime? CreatedDate { get; set; }

        public long? ModifiedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public long? DeletedBy { get; set; }

        public DateTime? DeletedDate { get; set; }
        public long? StatusId { get; set; }
    }
    public class PartsAuditCapaModel
    {
        public long PartAuditCapaId { get; set; }

        public long DocId { get; set; }

        public long? PartAuditId { get; set; }

        public long? AuditParameterId { get; set; }

        public string? Subject { get; set; }

        public DateTime? DueDate { get; set; }

        public DateTime? CompletedDate { get; set; }

        public string? PDCAStatus { get; set; }

        public long? SeverityId { get; set; }

        public long? Occurrence { get; set; }

        public long? Detection { get; set; }

        public long? SODScore { get; set; }

        public string? RiskRating { get; set; }

        public bool? IsResolved { get; set; }

        public string? Class { get; set; }

        public string? ActionType { get; set; }

        public string? CapaSubject { get; set; }

        public string? Observations { get; set; }

        public string? CorrectiveActions { get; set; }

        public string? SupplierRemarks { get; set; }

        public long? PDFID { get; set; }

        public long? ImageID { get; set; }
        public long? DeletedBy { get; set; }

    }

    public class PartsAuditCapaFilter
    {
        public long? AuditParameterId { get; set; }
        public string? Keyword { get; set; }
        public string? ActionType { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
