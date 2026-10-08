using HighSchoolManagementSystem.Models.Entities;

namespace HighSchoolManagementSystem.Repositories.Interfaces;

public enum ParentLinkResult { Success, ParentNotFound, AccountNotAuthorized, StudentNotFound, AlreadyLinkedElsewhere }

public interface IParentRepository
{
    Task<List<Parent>> GetAllAsync(string? search, bool? isActive);
    Task<Parent?> GetByIdAsync(int id);
    Task<Parent?> GetByUserIdAsync(int userId);
    Task<Role?> GetParentRoleAsync();
    Task AddAsync(Parent parent);
    Task SaveChangesAsync();
    Task<List<Student>> GetAvailableStudentsAsync(string? search);
    Task<ParentLinkResult> LinkStudentsAsync(int parentId, IReadOnlyCollection<int> studentIds);
}
