using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sqa_core.Models // Using your current namespace
{
    [Table("Sqa-RoleMaster")]
    public class RoleMaster
    {
        [Key]
        public long RoleId { get; set; }

        [Required]
        [MaxLength(255)]
        public string RoleName { get; set; }

        public bool? IsActive { get; set; } = true;
        public bool? IsDeleted { get; set; } = false;

        public DateTime? CreatedDate { get; set; }
        public long? CreatedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }
        public long? ModifiedBy { get; set; }

        public DateTime? DeletedDate { get; set; }
        public long? DeletedBy { get; set; }
        //thisbbis test
    }
}