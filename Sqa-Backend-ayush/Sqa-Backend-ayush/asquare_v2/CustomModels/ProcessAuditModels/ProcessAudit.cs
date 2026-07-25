using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sqa_core.Models
{
    [Table("Sqa_ProcessAudit")]
    public class ProcessAudit
    {
        [Key] public long ProcessAuditId { get; set; }
        public string? AuditReference { get; set; }
        [Required] public long CommodityId { get; set; }
        [Required] public long SupplierId { get; set; }
        [Required] public long StateId { get; set; }
        [Required] public long CityId { get; set; }
        [Required] public long AuditorId { get; set; }
        [Required] public DateTime AuditDate { get; set; }
        [Required] public string Remarks { get; set; }

        public long? StatusId { get; set; }
        public bool? IsDone { get; set; } = false;

        // NO MORE HARDCODED DEFAULTS
        public string? CAPA { get; set; }
        public string? Report { get; set; }

        public bool? IsDeleted { get; set; } = false;
        public long CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public long? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public long? DeletedBy { get; set; }
        public DateTime? DeletedDate { get; set; }

        public string? Status { get; set; } = "Open"; // Default to Open when created

         
    }

}