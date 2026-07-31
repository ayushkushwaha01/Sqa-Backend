using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sqa_core.Models
{
    [Table("tbl_inspection_ref")]
    public class InspectionRef
    {
        [Key]
        public long InspectionRefId { get; set; }

        public long InspectionId { get; set; }

        public long? PartNameId { get; set; } // Note: Marked as nullable in DB image

        public long? PartFamilyId { get; set; }

        [StringLength(50)]
        public string? ParameterName { get; set; }

        public long? PartMasterId { get; set; }

        public long? PartId { get; set; }

        [StringLength(255)]
        public string? Spec { get; set; }

        [NotMapped]
        public long? UnitId { get; set; }


        [StringLength(50)]
       
        public string? Unit { get; set; }

        [StringLength(255)]
        public string? Min { get; set; }

        [StringLength(255)]
        public string? Max { get; set; }

        // FIX: Changed from long? to double? to match SQL float
        public String? Defects { get; set; }

        public bool? Okay { get; set; }

        // FIX: Changed from string? to bool? to match SQL bit
        public bool? CAPA { get; set; }

        [StringLength(255)]
        public string? Method { get; set; }

        [StringLength(255)]
        public string? S1 { get; set; }

        [StringLength(255)]
        public string? S2 { get; set; }

        [StringLength(255)]
        public string? S3 { get; set; }

        [StringLength(255)]
        public string? S4 { get; set; }

        [StringLength(255)]
        public string? S5 { get; set; }

        public string? Remarks { get; set; }

        public bool? IsActive { get; set; }

        public long? CreatedBy { get; set; }

        public DateTime? CreatedDate { get; set; }

        // FIX: Changed from int? to long? to match SQL bigint
        public long? ModifiedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }

        // FIX: Changed from int? to long? to match SQL bigint
        public long? DeletedBy { get; set; }

        public DateTime? DeletedDate { get; set; }

        public bool? IsDeleted { get; set; }

        public string? DefectRate { get; set; }
    }
}