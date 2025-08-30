using System;
using App.BLL.Interfaces;
using App.Commons.Utils;
using App.DAL.UnitOfWork;
using App.Entities.DTOs.Files;
using App.Entities.Entities.App;
using App.Entities.Enums;
using Microsoft.Extensions.Configuration;

namespace App.BLL.Implementations;

public class FileService : IFileService
{
    private readonly IUnitOfWork _unitOfWork;

    private readonly long _fileSizeLimit;

    public FileService(IUnitOfWork unitOfWork, IConfiguration configuration)
    {
        this._unitOfWork = unitOfWork;
        this._fileSizeLimit = Helpers.FromMB(int.Parse(configuration["AppSettings:FileSizeLimit"] ?? "20"));
    }

    public async Task<FileDetailDTO> SaveFileInfoAsync(CreateFileDTO dto)
    {
        var repo = _unitOfWork.GetRepo<AppFile>();

        var file = new AppFile
        {
            FileName = dto.FileName,
            FilePath = dto.FilePath,
            Url = dto.Url,
            ThumbnailUrl = dto.ThumbnailUrl,
            FileSize = dto.FileSize,
            MimeType = dto.MimeType,
            Alt = dto.Alt,
            FileType = dto.FileType,
            Width = dto.Width,
            Height = dto.Height,
            Checksum = dto.Checksum,
            CreatedAt = dto.CreatedAt ?? DateTime.UtcNow,
            CreatedBy = dto.CreatedBy,
            IsActive = true,
            DeletedAt = null
        };

        await repo.CreateAsync(file);
        await _unitOfWork.SaveChangesAsync();

        return new FileDetailDTO(file);
    }

    public async Task<EntityFile> LinkFileToEntityAsync(
        long fileId,
        EntityType entityType,
        long entityId,
        bool isPrimary = false,
        string? caption = null
    )
    {
        var repo = _unitOfWork.GetRepo<EntityFile>();

        var link = new EntityFile
        {
            FileId = fileId,
            EntityType = entityType,
            EntityId = entityId,
            IsPrimary = isPrimary,
            Caption = caption,
            CreatedAt = DateTime.UtcNow
        };

        await repo.CreateAsync(link);
        await _unitOfWork.SaveChangesAsync();

        return link;
    }
}
