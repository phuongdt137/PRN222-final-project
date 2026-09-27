using HighSchoolManagementSystem.Models.Entities;

namespace HighSchoolManagementSystem.Repositories.Interfaces;

public interface ITeacherRepository
{
    Task<IEnumerable<Teacher>> GetAllAsync(string? searchTerm = null, string? specialization = null, string? status = null);
    Task<Teacher?> GetByIdAsync(int id);
    Task<Teacher?> GetByIdWithDetailsAsync(int id);
    Task<Teacher?> GetByCodeAsync(string teacherCode);
    Task<bool> ExistsByCodeAsync(string teacherCode, int? excludeId = null);
    Task<bool> ExistsByEmailAsync(string email, int? excludeUserId = null);
    Task<bool> ExistsByUsernameAsync(string username, int? excludeUserId = null);
    Task<Role?> GetTeacherRoleAsync();
    Task AddAsync(Teacher teacher);
    Task UpdateAsync(Teacher teacher);
    Task SaveChangesAsync();
}
