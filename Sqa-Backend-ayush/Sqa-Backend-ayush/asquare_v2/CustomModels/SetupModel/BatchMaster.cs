using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sqa_core.Models
{
    [Table("tbl_Batch_Master")]
    public class BatchMaster
    {
        [Key]
        public long BatchId { get; set; }

        public string? BatchNumber { get; set; }

        public long? PartFamilyId { get; set; }

        public long? PartMasterId { get; set; }

        public DateTime? BatchDate { get; set; }

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

    public class BatchMasterFilter
    {
        public string? Keyword { get; set; }

        public bool? Status { get; set; }

        public long? PartFamilyId { get; set; }

        public long? PartMasterId { get; set; }
    }
}