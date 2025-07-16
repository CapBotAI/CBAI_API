namespace App.Commons;

public class CommonDataModel
{
    public bool IsActive { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTime LastModifiedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
