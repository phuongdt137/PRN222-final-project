using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Models;
using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.DTOs;

namespace HighSchoolManagementSystem.Services.Interfaces;

public interface IStudentService
{
    Task<ServiceResult<PagedResult<Student>>> GetStudentsAsync(string? search = null, string? status = null, int? classId = null, bool unassignedOnly = false, int page = 1, int pageSize = 25);
    Task<ServiceResult<Student>> GetStudentAsync(int id);
    Task<ServiceResult<Student>> GetAcademicHistoryAsync(int id);
    Task<ServiceResult<Student>> CreateStudentAsync(CreateStudentDto dto);
    Task<ServiceResult<Student>> UpdateStudentAsync(int id, UpdateStudentDto dto);
    Task<ServiceResult<List<Class>>> GetCurrentClassesAsync();
    Task<ServiceResult<AcademicYear?>> GetCurrentYearAsync();
    Task<ServiceResult> AssignClassAsync(int studentId, int classId);
    Task<ServiceResult> TransferClassAsync(int studentId, TransferStudentClassDto dto);
}
