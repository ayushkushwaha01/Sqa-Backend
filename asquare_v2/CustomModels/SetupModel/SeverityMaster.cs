using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sqa_core.Models
{
    [Table("Sqa_SeverityMaster")]
    public class SeverityMaster
    {
        [Key] public long SeverityId { get; set; }

        [Required]
        public string SeverityName { get; set; }

        [Required]
        [Range(1, 10, ErrorMessage = "Rating must be between 1 and 10")]
        public int Rating { get; set; }

        public bool? IsActive { get; set; } = true;
        public bool? IsDeleted { get; set; } = false;

        public long CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public long? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public long? DeletedBy { get; set; }
        public DateTime? DeletedDate { get; set; }
    }
}