using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.DTOs;

namespace HighSchoolManagementSystem.Services.Interfaces;

public interface ITeacherService
{
    Task<ServiceResult<IEnumerable<Teacher>>> GetAllTeachersAsync(string? searchTerm = null, string? specialization = null, string? status = null);
    Task<ServiceResult<Teacher>> GetTeacherByIdAsync(int id);
    Task<ServiceResult<Teacher>> GetTeacherDetailsAsync(int id);
    Task<ServiceResult<Teacher>> CreateTeacherAsync(CreateTeacherDto dto);
    Task<ServiceResult<Teacher>> UpdateTeacherAsync(UpdateTeacherDto dto);
    Task<ServiceResult<bool>> ToggleTeacherStatusAsync(int id);
    Task<ServiceResult<IEnumerable<string>>> GetSpecializationsAsync();
}
