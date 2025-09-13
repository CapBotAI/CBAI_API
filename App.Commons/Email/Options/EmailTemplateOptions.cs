namespace App.Commons.Email.Options;

public class EmailTemplateOptions
{
    public string RootPath { get; set; } = "wwwroot";
    
    // Thêm thuộc tính mới để chỉ định đường dẫn tuyệt đối
    public string? BasePath { get; set; }
}