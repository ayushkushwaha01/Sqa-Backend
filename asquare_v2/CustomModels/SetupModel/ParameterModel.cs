using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sqa_core.Models
{
    [Table("tbl_Parmeters")]
    public class ParameterModel
    {
        [Key]
        public long ParameterId { get; set; }

        public string? ParmeterName { get; set; }
        public string? Spec { get; set; }
        public string? Min { get; set; }
        public string? Max { get; set; }
        public string? Method { get; set; }

        //public string? S1 { get; set; }
        //public string? S2 { get; set; }
        //public string? S3 { get; set; }
        //public string? S4 { get; set; }
        //public string? S5 { get; set; }
        public decimal? S1 { get; set; }
        public decimal? S2 { get; set; }
        public decimal? S3 { get; set; }
        public decimal? S4 { get; set; }
        public decimal? S5 { get; set; }

        public string? Remarks { get; set; }

        public long? PartId { get; set; }
        public long? PartFamilyId { get; set; }
        public long? PartMasterId { get; set; }

        public bool? Okay { get; set; }

        public bool? IsActive { get; set; } = true;
        public bool? IsDeleted { get; set; } = false;

        public long? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }

        public long? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }

        public long? DeletedBy { get; set; }
        public DateTime? DeletedDate { get; set; }
        public long? UnitId { get; set; }
    }

    public class ParameterFilter
    {
        public long? PartFamilyId { get; set; }
        public long? PartMasterId { get; set; }
        public long? PartAuditId { get; set; }
        public string? Keyword { get; set; }

        public bool? Status { get; set; }
    }
}