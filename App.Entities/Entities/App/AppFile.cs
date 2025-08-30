using System;
using App.Commons;
using App.Entities.Enums;

namespace App.Entities.Entities.App;

public class AppFile : CommonDataModel
{
    public long Id { get; set; }
    public string FilePath { get; set; } = null!;
    public string FileName { get; set; } = null!;
    // Sử dụng như URL truy cập công khai cho cả ảnh và file
    public string Url { get; set; } = null!;
    // Có thể null nếu không phải ảnh/video hoặc không tạo thumbnail
    public string? ThumbnailUrl { get; set; }
    public long FileSize { get; set; }
    public string? MimeType { get; set; }
    // Dùng như tiêu đề/alt text (tùy loại file)
    public string? Alt { get; set; }

    // ==== Mở rộng để hỗ trợ mọi loại file ====
    public FileType FileType { get; set; } = FileType.Unknown;
    public string? Checksum { get; set; } // MD5/SHA-256 để chống trùng/kiểm tra toàn vẹn
    // Nếu là ảnh, lưu kích thước để client tối ưu hiển thị
    public int? Width { get; set; }
    public int? Height { get; set; }
    // =========================================

    public virtual ICollection<EntityFile> EntityFiles { get; set; } = new List<EntityFile>();
}
