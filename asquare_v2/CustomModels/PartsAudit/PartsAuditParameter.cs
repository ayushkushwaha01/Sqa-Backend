using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sqa_core.Models
{
    [Table("tbl_Parts_Audit_Parameters")]
    public class PartsAuditParameter
    {
        [Key]
        public long AuditParameterId { get; set; }

        public long PartAuditId { get; set; }

        public long ParameterId { get; set; }

        public string? ParmeterName { get; set; }

        public string? Spec { get; set; }

        public string? Min { get; set; }

        public string? Max { get; set; }

        public string? Method { get; set; }

        public string? S1 { get; set; }

        public string? S2 { get; set; }

        public string? S3 { get; set; }

        public string? S4 { get; set; }

        public string? S5 { get; set; }

        public string? Remarks { get; set; }

        public bool? Okay { get; set; }

        public bool? IsActive { get; set; }

        public bool? IsDeleted { get; set; }

        public DateTime? CreatedDate { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public long? PartMasterId { get; set; }
        public long? PartFamilyId { get; set; }
        public long? PartId { get; set; }
        public long? UnitId { get; set; }


    }

    public class AuditParameterModel
    {
        public long AuditParameterId { get; set; }

        public long PartAuditId { get; set; }

        public long ParameterId { get; set; }

        public long PartId { get; set; }

        public long? PartFamilyId { get; set; }

        public long? PartMasterId { get; set; }

        public string? ParmeterName { get; set; }

        public string? Spec { get; set; }

        public string? Min { get; set; }

        public string? Max { get; set; }

        public string? Method { get; set; }

        public string? S1 { get; set; }

        public string? S2 { get; set; }

        public string? S3 { get; set; }

        public string? S4 { get; set; }

        public string? S5 { get; set; }

        public string? Remarks { get; set; }

        public bool Okay { get; set; }
        public long? UnitId { get; set; }
    }
}