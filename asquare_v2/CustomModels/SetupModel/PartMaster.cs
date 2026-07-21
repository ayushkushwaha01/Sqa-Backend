using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sqa_core.Models
{
    [Table("tbl_Part_Master")]
    public class PartMaster
    {
        [Key]
        public long PartMasterId { get; set; }

        public string? PartMasterName { get; set; }

        public string? PartMasterCode { get; set; }

        public long? PartFamilyId { get; set; }

        public long? CommodityId { get; set; }

        public bool? IsActive { get; set; } = true;

        public bool? IsDeleted { get; set; } = false;

        public long? CreatedBy { get; set; }

        public DateTime? CreatedDate { get; set; }

        public long? ModifiedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public long? DeletedBy { get; set; }

        public DateTime? DeletedDate { get; set; }
        public string? SupplierIds { get; set; }
    }

    public class PartMasterFilter
    {
        public string? Keyword { get; set; }

        public bool? Status { get; set; }
    }
}