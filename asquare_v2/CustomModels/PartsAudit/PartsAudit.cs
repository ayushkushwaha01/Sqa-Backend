using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sqa_core.Models
{
    [Table("tbl_Parts_Audits")]
    public class PartsAudits
    {
        [Key]
        public long PartAuditId { get; set; }

        public long? CommodityId { get; set; }

        public long? PartFamilyId { get; set; }

        public long? PartMasterId { get; set; }

        public long? SupplierId { get; set; }

        public long? CityId { get; set; }

        public long? StateId { get; set; }

        public long? AuditorId { get; set; }

        public DateTime? AuditDate { get; set; }

        public bool? Done { get; set; }

        public long? StatusId { get; set; }

        public string? Remakrs { get; set; }

        public bool? IsActive { get; set; } = true;

        public bool? IsDeleted { get; set; } = false;

        public long? CreatedBy { get; set; }

        public DateTime? CreatedDate { get; set; }

        public long? ModifiedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public long? DeletedBy { get; set; }

        public DateTime? DeletedDate { get; set; }
    }

    public class PartsAuditFilter
    {
        public string? Keyword { get; set; }

        public int? CommodityId { get; set; }

        public int? PartFamilyId { get; set; }

        public int? PartMasterId { get; set; }

        public int? SupplierId { get; set; }

        public int? AuditorId { get; set; }

        public int? StateId { get; set; }

        public int? CityId { get; set; }

        public int? StatusId { get; set; }

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        public bool? Status { get; set; }   // IsActive

        public bool? Done { get; set; }
    }
}