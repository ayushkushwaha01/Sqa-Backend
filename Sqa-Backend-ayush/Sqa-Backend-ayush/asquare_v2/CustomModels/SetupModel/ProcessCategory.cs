using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sqa_core.Models
{
    [Table("Sqa_ProcessCategory")]
    public class ProcessCategory
    {
        [Key]
        public long ProcessCategoryId { get; set; }
        [Required] public string Name { get; set; }
        [Required] public string Code { get; set; }

        public bool? IsActive { get; set; } = true;
        public bool? IsDeleted { get; set; } = false;

        public long CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public long? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public long? DeletedBy { get; set; }
        public DateTime? DeletedDate { get; set; }
    }

    [Table("Sqa_Checklist")]
    public class Checklist
    {
        [Key]
        public long ChecklistId { get; set; }
        public long ProcessCategoryId { get; set; }
        [Required] public string Question { get; set; }
        public string? Guideline { get; set; }

        public bool? IsMandatory { get; set; } = false;
        public bool? IsPriority { get; set; } = false;

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