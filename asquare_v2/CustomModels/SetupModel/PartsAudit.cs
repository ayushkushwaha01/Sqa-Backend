
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sqa_core.Models

{
    [Table("tbl_Parts_Aduit_Category")]
    public class PartsAudit
    {
        [Key]
        public long PartId { get; set; }
        public string CategoryName { get; set; }

        public string CategoryCode { get; set; }


        public bool? IsActive { get; set; } = true;
        public bool? IsDeleted { get; set; } = false;

        public long? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public long? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public long? DeletedBy { get; set; }
        public DateTime? DeletedDate { get; set; }
    }


    public class PartsAuditCategoryFilter
    {
        public string? Keyword { get; set; }

        public bool? Status { get; set; }
    }
}
