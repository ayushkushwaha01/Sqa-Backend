using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sqa_core.Models
{
    [Table("Sqa_DefectMaster")]
    public class DefectMaster
    {
        [Key] public long DefectId { get; set; }
        [Required] public string DefectName { get; set; }

        public bool? IsDeleted { get; set; } = false;

        public long CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public long? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public long? DeletedBy { get; set; }
        public DateTime? DeletedDate { get; set; }
    }
}