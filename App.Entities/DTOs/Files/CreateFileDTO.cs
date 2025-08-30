using System;
using App.Entities.Enums;

namespace App.Entities.DTOs.Files;

public class CreateFileDTO
{
    public string FileName { get; set; } = null!;
    public string FilePath { get; set; } = null!;
    public string Url { get; set; } = null!;
    public string? ThumbnailUrl { get; set; }
    public long FileSize { get; set; }
    public string? MimeType { get; set; }
    public string? Alt { get; set; }
    public FileType FileType { get; set; }

    public int? Width { get; set; }
    public int? Height { get; set; }

    public string? Checksum { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
}
