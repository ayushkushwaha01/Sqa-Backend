using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sqa_core.Models
{
    [Table("tbl_Parts_audit_capa_images")]
    public class PartsAuditCapaImage
    {
        [Key]
        public long ImageId { get; set; }

        public string? ImageTitlte { get; set; }

        public string? Imageurl { get; set; }

        public long? PartAuditId { get; set; }

        public long? AuditParameterId { get; set; }

        public bool? IsActive { get; set; }

        public bool? IsDeleted { get; set; }

        public long? CreatedBy { get; set; }

        public DateTime? CreatedDate { get; set; }

        public long? ModifiedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public long? DeletedBy { get; set; }

        public DateTime? DeletedDate { get; set; }
    }
}