using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("tbl_User_Grid_Settings")]
public class UserGridSetting
{
    [Key]
    public long SettingId { get; set; }

    public long UserId { get; set; }

    public string GridType { get; set; }

    public string? SelectedColumnsJSON { get; set; }

    public bool? IsActive { get; set; }

    public bool? IsDeleted { get; set; }

    public long? CreatedBy { get; set; }

    public DateTime? CreatedDate { get; set; }

    public long? ModifiedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }
}


public class UserGridColumnsModel
{
    public long UserId { get; set; }

    public string GridType { get; set; }

    public string? SelectedColumnsJSON { get; set; }
}