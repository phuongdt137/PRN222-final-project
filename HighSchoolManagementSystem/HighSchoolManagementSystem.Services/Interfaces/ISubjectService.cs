using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.DTOs;

namespace HighSchoolManagementSystem.Services.Interfaces;

public interface ISubjectService
{
    Task<ServiceResult<IEnumerable<Subject>>> GetAllSubjectsAsync(string? searchTerm = null, string? evaluationType = null, bool? isActive = null);
    Task<ServiceResult<Subject>> GetSubjectByIdAsync(int id);
    Task<ServiceResult<Subject>> GetSubjectDetailsAsync(int id);
    Task<ServiceResult<Subject>> CreateSubjectAsync(CreateSubjectDto dto);
    Task<ServiceResult<Subject>> UpdateSubjectAsync(UpdateSubjectDto dto);
    Task<ServiceResult<bool>> ToggleSubjectStatusAsync(int id);
    Task<ServiceResult<bool>> DeleteSubjectAsync(int id);
    Task<ServiceResult<IEnumerable<Class>>> GetAvailableClassesAsync();
    Task<ServiceResult<IEnumerable<Semester>>> GetAvailableSemestersAsync();
    Task<ServiceResult<bool>> AssignTeacherAsync(AssignTeacherDto dto);
    Task<ServiceResult<bool>> UnassignTeacherAsync(int assignmentId);
    Task<ServiceResult<bool>> CheckSubjectHasScoresAsync(int id);
}
