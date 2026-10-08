using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Models;

namespace HighSchoolManagementSystem.Repositories.Interfaces;

public enum StudentAssignmentResult { Success, StudentNotFound, StudentInactive, ClassNotFound, YearNotCurrent, AlreadyAssigned }
public enum StudentTransferResult { Success, StudentNotFound, StudentInactive, ClassNotFound, YearNotCurrent, MembershipChanged, SameClass, DifferentGrade }

public interface IStudentRepository
{
    Task<PagedResult<Student>> GetAllAsync(string? search, string? status, int? classId, bool unassignedOnly, int page, int pageSize);
    Task<Student?> GetByIdAsync(int id);
    Task<Student?> GetByUserIdAsync(int userId);
    Task<Student?> GetAcademicHistoryAsync(int id);
    Task<bool> ExistsByCodeAsync(string code, int? excludedId = null);
    Task<Role?> GetStudentRoleAsync();
    Task AddAsync(Student student);
    Task SaveChangesAsync();
    Task<List<Class>> GetCurrentClassesAsync();
    Task<AcademicYear?> GetCurrentYearAsync();
    Task<StudentAssignmentResult> AssignClassAsync(int studentId, int classId);
    Task<StudentTransferResult> TransferClassAsync(int studentId, int membershipId, int classId);
}
