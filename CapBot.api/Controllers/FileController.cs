using App.BLL.Interfaces;
using App.Commons.BaseAPI;
using App.Commons.Utils;
using App.Entities.DTOs.Files;
using App.Entities.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Security.Cryptography;

namespace CapBot.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileController : BaseAPIController
    {
        private readonly IFileService _fileService;
        private readonly ILogger<FileController> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly IHostEnvironment _hostEnvironment;

        private readonly long _fileSizeLimit;

        public FileController(IFileService fileService,
            ILogger<FileController> logger,
            IWebHostEnvironment environment,
            IConfiguration configuration,
            IHostEnvironment hostEnvironment)
        {
            this._fileService = fileService;
            this._logger = logger;
            this._environment = environment;
            this._configuration = configuration;
            this._hostEnvironment = hostEnvironment;
            this._fileSizeLimit = Helpers.FromMB(int.Parse(configuration["AppSettings:FileSizeLimit"] ?? "20"));
        }

        [Authorize]
        [HttpPost("upload-image")]
        public async Task<ActionResult> UploadImage(IFormFile file)
        {
            try
            {
                if (file == null)
                    return SaveError("file upload không được để trống");

                var uploaded = await ProcessUploadImage(file, IsAdmin);
                if (uploaded == null)
                    return GetError("Lỗi xử lý file");

                var createFileDTO = new CreateFileDTO
                {
                    FileName = uploaded.FileName,
                    FilePath = uploaded.FilePath,
                    Url = uploaded.Url,
                    ThumbnailUrl = uploaded.ThumbnailUrl,
                    FileSize = uploaded.FileSize,
                    MimeType = uploaded.MimeType,
                    Alt = uploaded.Alt,
                    FileType = FileType.Image,
                    Width = uploaded.Width,
                    Height = uploaded.Height,
                    Checksum = uploaded.Checksum,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = UserName
                };

                var saved = await _fileService.SaveFileInfoAsync(createFileDTO);

                return SaveSuccess(new
                {
                    Success = true,
                    FileId = saved.Id,
                    Url = saved.Url,
                    ThumbnailUrl = saved.ThumbnailUrl
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error uploading image: {ex.Message}", ex);
                return GetError(ex.Message);
            }
        }

        [Authorize]
        [HttpPost("upload")]
        public async Task<ActionResult> UploadFile(IFormFile file)
        {
            try
            {
                if (file == null)
                    return SaveError("file upload không được để trống");

                var uploaded = await ProcessUploadGeneric(file, IsAdmin);
                if (uploaded == null)
                    return GetError("Lỗi xử lý file");

                var fileType = DetermineFileType(Path.GetExtension(uploaded.FileName), uploaded.MimeType);

                var createFileDTO = new CreateFileDTO
                {
                    FileName = uploaded.FileName,
                    FilePath = uploaded.FilePath,
                    Url = uploaded.Url,
                    ThumbnailUrl = uploaded.ThumbnailUrl,
                    FileSize = uploaded.FileSize,
                    MimeType = uploaded.MimeType,
                    Alt = uploaded.Alt,
                    FileType = fileType,
                    Width = uploaded.Width,
                    Height = uploaded.Height,
                    Checksum = uploaded.Checksum,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = UserName
                };

                var saved = await _fileService.SaveFileInfoAsync(createFileDTO);

                return SaveSuccess(new
                {
                    Success = true,
                    FileId = saved.Id,
                    Url = saved.Url,
                    ThumbnailUrl = saved.ThumbnailUrl
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error uploading file: {ex.Message}", ex);
                return GetError(ex.Message);
            }
        }

        #region PRIVATE

        private async Task<UploadedFileInfo> ProcessUploadImage(IFormFile file, bool isAdmin)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is required");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var validExtensions = new string[] { ".jpg", ".png", ".svg", ".jpeg", ".dng", ".webp" };

            if (!validExtensions.Contains(extension))
                throw new ArgumentException("Định dạng file ảnh không được hỗ trợ");

            if (file.Length > _fileSizeLimit)
                throw new ArgumentException($"Kích thước ảnh vượt quá quy định cho phép ({Helpers.FormatFileSize(_fileSizeLimit)})");

            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                var bytes = memoryStream.ToArray();

                // Use original file name (sanitized)
                var originalFileName = Path.GetFileNameWithoutExtension(file.FileName);
                var sanitizedName = SanitizeFileName(originalFileName);
                var isSvg = extension.Equals(".svg", StringComparison.OrdinalIgnoreCase);

                var targetFileName = sanitizedName + extension;
                var guildStringPath = new string[] { "images", isAdmin ? string.Empty : "entities", targetFileName };
                var path = Path.Combine(_hostEnvironment.ContentRootPath, "wwwroot", Helpers.PathCombine(guildStringPath));

                // Ensure uniqueness if file exists
                path = EnsureUniqueFilePath(path, ref guildStringPath);

                string directory = Path.GetDirectoryName(path) ?? _hostEnvironment.ContentRootPath;
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                await System.IO.File.WriteAllBytesAsync(path, bytes);

                string cdnhost = _configuration.GetSection("AppSettings").GetValue<string>("CdnUrl") ?? string.Empty;
                string url = $"{cdnhost}{Helpers.UrlCombine(guildStringPath)}";

                // Tạo thumbnail
                string thumbnailUrl = isSvg ? url : $"{cdnhost}{CompressThumbnailWithNew(guildStringPath, path)}";

                int? width = null, height = null;
                if (!isSvg)
                {
                    using var img = Image.Load(path);
                    width = img.Width;
                    height = img.Height;
                }

                return new UploadedFileInfo
                {
                    FileName = Path.GetFileName(path),
                    FilePath = path,
                    Url = url,
                    ThumbnailUrl = thumbnailUrl,
                    FileSize = file.Length,
                    MimeType = file.ContentType,
                    Alt = Path.GetFileNameWithoutExtension(file.FileName),
                    Width = width,
                    Height = height,
                    Checksum = ComputeChecksum(bytes)
                };
            }
        }

        private async Task<UploadedFileInfo> ProcessUploadGeneric(IFormFile file, bool isAdmin)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is required");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            // Cho phép nhiều loại phổ biến, có thể lấy từ config
            var validExtensions = new string[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".csv", ".zip", ".rar", ".7z", ".mp3", ".mp4", ".mov", ".avi", ".png", ".jpg", ".jpeg", ".svg", ".webp" };

            if (!validExtensions.Contains(extension))
                throw new ArgumentException("Định dạng file không được hỗ trợ");

            if (file.Length > _fileSizeLimit)
                throw new ArgumentException($"Kích thước file vượt quá quy định cho phép ({Helpers.FormatFileSize(_fileSizeLimit)})");

            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                var bytes = memoryStream.ToArray();

                // Use original file name (sanitized)
                var originalFileName = Path.GetFileNameWithoutExtension(file.FileName);
                var sanitizedName = SanitizeFileName(originalFileName);

                var targetFileName = sanitizedName + extension;
                var guildStringPath = new string[] { "files", isAdmin ? string.Empty : "entities", targetFileName };
                var path = Path.Combine(_hostEnvironment.ContentRootPath, "wwwroot", Helpers.PathCombine(guildStringPath));

                // Ensure uniqueness if file exists
                path = EnsureUniqueFilePath(path, ref guildStringPath);

                string directory = Path.GetDirectoryName(path) ?? _hostEnvironment.ContentRootPath;
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                await System.IO.File.WriteAllBytesAsync(path, bytes);

                string cdnhost = _configuration.GetSection("AppSettings").GetValue<string>("CdnUrl") ?? string.Empty;
                string url = $"{cdnhost}{Helpers.UrlCombine(guildStringPath)}";

                return new UploadedFileInfo
                {
                    FileName = Path.GetFileName(path),
                    FilePath = path,
                    Url = url,
                    ThumbnailUrl = null,
                    FileSize = file.Length,
                    MimeType = file.ContentType,
                    Alt = Path.GetFileNameWithoutExtension(file.FileName),
                    Width = null,
                    Height = null,
                    Checksum = ComputeChecksum(bytes)
                };
            }
        }

        // Sanitize original filename to remove invalid chars and trim length
        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                name = "file";

            // Remove invalid path chars
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '-');
            }

            // Collapse multiple hyphens
            while (name.Contains("--"))
                name = name.Replace("--", "-");

            // Trim to reasonable length (keep room for suffixes/extensions)
            var maxLen = 100;
            if (name.Length > maxLen)
                name = name.Substring(0, maxLen);

            return name;
        }

        // If the target path already exists, append a timestamp to filename and update guildStringPath accordingly
        private string EnsureUniqueFilePath(string path, ref string[] guildStringPath)
        {
            var directory = Path.GetDirectoryName(path) ?? _hostEnvironment.ContentRootPath;
            var fileName = Path.GetFileName(path);

            if (!System.IO.File.Exists(path))
                return path;

            var name = Path.GetFileNameWithoutExtension(fileName);
            var ext = Path.GetExtension(fileName);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
            var newFileName = $"{name}_{timestamp}{ext}";

            guildStringPath[guildStringPath.Length - 1] = newFileName;

            var newPath = Path.Combine(directory, newFileName);
            return newPath;
        }

        private FileType DetermineFileType(string extension, string? mime)
        {
            extension = (extension ?? string.Empty).ToLowerInvariant();

            if (new[] { ".jpg", ".jpeg", ".png", ".svg", ".webp", ".gif", ".bmp", ".tiff", ".dng" }.Contains(extension))
                return FileType.Image;
            if (new[] { ".pdf" }.Contains(extension))
                return FileType.Pdf;
            if (new[] { ".xls", ".xlsx", ".csv" }.Contains(extension))
                return FileType.Spreadsheet;
            if (new[] { ".ppt", ".pptx" }.Contains(extension))
                return FileType.Presentation;
            if (new[] { ".doc", ".docx", ".txt" }.Contains(extension))
                return FileType.Document;
            if (new[] { ".zip", ".rar", ".7z" }.Contains(extension))
                return FileType.Archive;
            if (new[] { ".mp3", ".wav", ".aac" }.Contains(extension))
                return FileType.Audio;
            if (new[] { ".mp4", ".mov", ".avi", ".mkv" }.Contains(extension))
                return FileType.Video;

            return FileType.Unknown;
        }

        private static string ComputeChecksum(byte[] bytes)
        {
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        private string CompressThumbnailWithNew(string[] guildStringPath, string originalImagePath)
        {
            var originalFileName = guildStringPath.Last();
            var thumbnailFileName = Path.GetFileNameWithoutExtension(originalFileName) + "_thumb" +
                                    Path.GetExtension(originalFileName);

            var thumbnailGuildStringPath = guildStringPath.Take(guildStringPath.Length - 1)
                .Concat(new[] { thumbnailFileName })
                .ToArray();

            var thumbnailPhysicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot",
                Helpers.PathCombine(thumbnailGuildStringPath));

            string directory = Path.GetDirectoryName(thumbnailPhysicalPath) ?? _hostEnvironment.ContentRootPath;
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            using (var image = Image.Load(originalImagePath))
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(150, 0)
                }));
                image.Save(thumbnailPhysicalPath);
            }

            var thumbnailUrlPath = Helpers.UrlCombine(thumbnailGuildStringPath);
            return thumbnailUrlPath;
        }

        // Model hỗ trợ cho việc lưu thông tin upload
        private class UploadedFileInfo
        {
            public string FileName { get; set; } = string.Empty;
            public string FilePath { get; set; } = string.Empty;
            public string Url { get; set; } = string.Empty;
            public string? ThumbnailUrl { get; set; }
            public long FileSize { get; set; }
            public string MimeType { get; set; } = string.Empty;
            public string Alt { get; set; } = string.Empty;
            public int? Width { get; set; }
            public int? Height { get; set; }
            public string? Checksum { get; set; }
        }

        #endregion
    }
}
