using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sqa_core.Models
{
    [Table("tbl_SupplierMaster")]
    public class SupplierMaster
    {
        [Key] public long SupplierId { get; set; }
        [Required] public string UserName { get; set; }
        public string? Password { get; set; }
        [Required] public string SupplierName { get; set; }
        [Required] public string ContactPerson { get; set; }
        [Required] public string Email { get; set; }
        [Required] public string Phone { get; set; }
        public long StateId { get; set; }
        public long CityId { get; set; }
        [Required] public string Address { get; set; }

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