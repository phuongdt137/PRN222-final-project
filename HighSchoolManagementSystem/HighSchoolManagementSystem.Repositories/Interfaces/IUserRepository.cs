using HighSchoolManagementSystem.Models.Entities;

namespace HighSchoolManagementSystem.Repositories.Interfaces;

public interface IUserRepository
{
    Task<List<User>> GetAllAsync(string? search = null, int? roleId = null, bool? isActive = null);
    Task<User?> GetByUsernameOrEmailAsync(string usernameOrEmail);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetUserFullProfileByIdAsync(int id);
    Task<Role?> GetRoleByIdAsync(int roleId);
    Task<List<Role>> GetRolesAsync();
    Task<bool> ExistsByUsernameAsync(string username);
    Task<bool> ExistsByEmailAsync(string email, int? excludedUserId = null);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
    Task SaveChangesAsync();
    Task<string> GenerateNextStudentCodeAsync();
    Task<string> GenerateNextTeacherCodeAsync();
}
