using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sqa_core.Models
{
    [Table("tbl_InspectionDefects")]
    public class InspectionDefects
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long InspectionDefectId { get; set; }

        public long InspectionId { get; set; }

        // Stores JSON array of defect IDs, e.g., "[1, 2, 3]"
        public string? DefectsId { get; set; }

        // Stores JSON dictionary of statuses, e.g., "{"1": 5, "2": 5}"
        public string? Status { get; set; }
    }
}