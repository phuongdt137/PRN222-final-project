using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.Interfaces;

namespace HighSchoolManagementSystem.Services.Implementations;

public class FileStorageService : IFileStorageService
{
    private readonly string _webRootPath;
    private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxFileSizeBytes = 2 * 1024 * 1024; // 2MB

    public FileStorageService(string? webRootPath = null)
    {
        _webRootPath = !string.IsNullOrWhiteSpace(webRootPath)
            ? webRootPath
            : Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
    }

    public async Task<ServiceResult<string>> SaveFileAsync(Stream fileStream, string originalFileName, string subFolder)
    {
        if (fileStream == null || (fileStream.CanSeek && fileStream.Length == 0))
        {
            return ServiceResult<string>.Fail(ErrorCodes.FileEmpty, "Tệp tải lên không có dữ liệu.");
        }

        var ext = Path.GetExtension(originalFileName).ToLowerInvariant();
        if (!AllowedImageExtensions.Contains(ext))
        {
            return ServiceResult<string>.Fail(ErrorCodes.FileInvalidExtension, 
                "Định dạng tệp không được hỗ trợ. Chỉ chấp nhận các định dạng .jpg, .jpeg, .png, .webp.");
        }

        if (fileStream.CanSeek && fileStream.Length > MaxFileSizeBytes)
        {
            return ServiceResult<string>.Fail(ErrorCodes.FileTooLarge, 
                "Dung lượng tệp vượt quá giới hạn tối đa cho phép (2MB).");
        }

        try
        {
            var cleanSubFolder = subFolder.TrimStart('/', '\\')
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);

            var targetDirPath = Path.Combine(_webRootPath, cleanSubFolder);
            if (!Directory.Exists(targetDirPath))
            {
                Directory.CreateDirectory(targetDirPath);
            }

            var uniqueFileName = $"{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(targetDirPath, uniqueFileName);

            if (fileStream.CanSeek)
            {
                fileStream.Position = 0;
            }

            using (var destinationStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await fileStream.CopyToAsync(destinationStream);
            }

            var relativeUrl = "/" + cleanSubFolder.Replace(Path.DirectorySeparatorChar, '/') + "/" + uniqueFileName;
            return ServiceResult<string>.Ok(relativeUrl, "Tải lên tệp tin thành công.");
        }
        catch (Exception ex)
        {
            return ServiceResult<string>.Fail(ErrorCodes.FileSaveFailed, $"Lỗi lưu tệp tin: {ex.Message}");
        }
    }

    public Task<ServiceResult<bool>> DeleteFileAsync(string? relativeFilePath)
    {
        if (string.IsNullOrWhiteSpace(relativeFilePath))
        {
            return Task.FromResult(ServiceResult<bool>.Ok(true));
        }

        try
        {
            var cleanPath = relativeFilePath.TrimStart('/', '\\')
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);

            var fullPath = Path.GetFullPath(Path.Combine(_webRootPath, cleanPath));

            // Đảm bảo an toàn không xóa ngoài thư mục wwwroot
            if (fullPath.StartsWith(_webRootPath, StringComparison.OrdinalIgnoreCase) && File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            return Task.FromResult(ServiceResult<bool>.Ok(true, "Xóa tệp tin thành công."));
        }
        catch
        {
            // Không chặn tiến trình nếu xóa file cũ thất bại
            return Task.FromResult(ServiceResult<bool>.Ok(false, "Không thể xóa tệp tin cũ."));
        }
    }
}
