using System;
using App.Entities.Entities.App;
using App.Entities.Enums;

namespace App.Entities.DTOs.Files;

public class FileDetailDTO
{
    public long Id { get; set; }
    public string FilePath { get; set; } = null!;
    public string FileName { get; set; } = null!;
    public string Url { get; set; } = null!;
    public string? ThumbnailUrl { get; set; }
    public long FileSize { get; set; }
    public string? MimeType { get; set; }
    public string? Alt { get; set; }

    public FileType FileType { get; set; } = FileType.Unknown;
    public string? Checksum { get; set; } // MD5/SHA-256 để chống trùng/kiểm tra toàn vẹn
    public int? Width { get; set; }
    public int? Height { get; set; }

    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }

    public FileDetailDTO(AppFile file)
    {
        this.Id = file.Id;
        this.FilePath = file.FilePath;
        this.FileName = file.FileName;
        this.Url = file.Url;
        this.ThumbnailUrl = file.ThumbnailUrl;
        this.FileSize = file.FileSize;
        this.MimeType = file.MimeType;
        this.Alt = file.Alt;
        this.FileType = file.FileType;
        this.Checksum = file.Checksum;
        this.Width = file.Width;
        this.Height = file.Height;
        this.CreatedAt = file.CreatedAt;
        this.CreatedBy = file.CreatedBy;
    }
}
