using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sqa_core.Models
{
    [Table("Sqa_SupplierMaster")]
    public class SupplierMaster
    {
        [Key]
        public long SupplierId { get; set; }

        public string SupplierName { get; set; }

        public string ContactPerson { get; set; }

        public long StateId { get; set; }

        public long CityId { get; set; }

        public string Address { get; set; }

        public bool? IsActive { get; set; } = true;

        public DateTime? CreatedDate { get; set; }

        public long? CreatedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public long? ModifiedBy { get; set; }

        public DateTime? DeletedDate { get; set; }

        public long? DeletedBy { get; set; }

        public bool? IsDeleted { get; set; } = false;


    }
}
