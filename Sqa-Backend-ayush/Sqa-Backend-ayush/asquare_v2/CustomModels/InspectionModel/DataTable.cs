using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sqa_core.Models
{
    [Table("tbl_Inspections")]
    public class Inspection
    {
         
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long InspectionId { get; set; }

         
        [StringLength(100)]
        public string? ReferenceId { get; set; }

        //[Column(TypeName = "date")]
        public DateTime? InspectionDate { get; set; }

        public TimeSpan? Time { get; set; }

        public string? Remarks { get; set; }

        
        public long? StageId { get; set; }

        public long? SupplierId { get; set; }

        public long? ShiftId { get; set; }

        public long? InspectorId { get; set; }

        public long? PartFamilyId { get; set; }

        public long? PartCodeId { get; set; }

        public long? BatchNumberId { get; set; }

       
        public bool? Publish { get; set; }

        public int? Defects { get; set; }

        public int? Parameters { get; set; }

        public double? ErrorRate { get; set; }

        public bool? IsArchive { get; set; }

  
        public bool IsActive { get; set; }

        public long CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; }

        public long? ModifiedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public long? DeletedBy { get; set; }

        public DateTime? DeletedDate { get; set; }

        public bool IsDeleted { get; set; }

        public int? BatchQuantity { get; set; }


        public int? SampleQuantity { get; set; }

    }
}