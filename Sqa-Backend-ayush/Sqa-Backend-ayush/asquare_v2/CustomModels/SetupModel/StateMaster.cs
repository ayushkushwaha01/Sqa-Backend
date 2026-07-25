using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sqa_core.Models
{

    [Table("Sqa_StateMaster")]
    public class StateMaster
    {
        [Key]
        public long StateId { get; set; }

        public string StateName { get; set; }

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
