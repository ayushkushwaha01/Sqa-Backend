using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sqa_core.Models
{
    [Table("Sqa_UserMaster")]
    public class UserMaster
    {
        [Key]
        public long UserId { get; set; }

        [Required]
        [MaxLength(255)]
        public string UserName { get; set; }

        [Required]
        [MaxLength(255)]
        public string Email { get; set; }

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }  

        [MaxLength(255)]
        public string? Password { get; set; }  

        public long RoleId { get; set; }

        [MaxLength(100)]
        public string? Department { get; set; }  

        [MaxLength(255)]
        public string? Manager { get; set; }  

        public bool? IsHod { get; set; } = false;
        public bool? IsInspector { get; set; } = false;
        public bool? IsAuditor { get; set; } = false;
        public bool? IsManagerialRole { get; set; } = false;
        public bool? TwoFactor { get; set; } = false;

        public bool? IsActive { get; set; } = true;
        public bool? IsDeleted { get; set; } = false;

        public long? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public long? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public long? DeletedBy { get; set; }
        public DateTime? DeletedDate { get; set; }

        public string? ResetToken { get; set; }
        public DateTime? ResetTokenExpires { get; set; }

    }
}