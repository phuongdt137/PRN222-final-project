using HighSchoolManagementSystem.Services.Common;

namespace HighSchoolManagementSystem.Services.Interfaces;

public interface IFileStorageService
{
    Task<ServiceResult<string>> SaveFileAsync(Stream fileStream, string originalFileName, string subFolder);
    Task<ServiceResult<bool>> DeleteFileAsync(string? relativeFilePath);
}
