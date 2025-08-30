using System;
using App.Entities.DTOs.Files;
using App.Entities.Entities.App;
using App.Entities.Enums;

namespace App.BLL.Interfaces;

public interface IFileService
{
    Task<FileDetailDTO> SaveFileInfoAsync(CreateFileDTO dto);
    Task<EntityFile> LinkFileToEntityAsync(
        long fileId,
        EntityType entityType,
        long entityId,
        bool isPrimary = false,
        string? caption = null
    );
}
