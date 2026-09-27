using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.DTOs;

namespace HighSchoolManagementSystem.Services.Interfaces;

public interface IUserService
{
    Task<ServiceResult<User>> GetUserProfileAsync(int userId);
    Task<ServiceResult<User>> UpdateProfileAsync(int userId, UpdateProfileDto dto);
    Task<ServiceResult<bool>> ChangePasswordAsync(int userId, ChangePasswordDto dto);
}
