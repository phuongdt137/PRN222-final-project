using System.Data;
using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Repositories.Data;
using HighSchoolManagementSystem.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HighSchoolManagementSystem.Repositories.Implementations;

public class ParentRepository(AppDbContext context) : IParentRepository
{
    public async Task<List<Parent>> GetAllAsync(string? search, bool? isActive)
    {
        var query = context.Parents.AsNoTracking().Include(p => p.User).ThenInclude(u => u.Role)
            .Include(p => p.Students).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p => p.User.FullName.ToLower().Contains(term)
                || p.User.Username.ToLower().Contains(term) || p.User.Email.ToLower().Contains(term)
                || (p.User.PhoneNumber != null && p.User.PhoneNumber.Contains(term)));
        }
        if (isActive.HasValue) query = query.Where(p => p.User.IsActive == isActive.Value);
        return await query.OrderBy(p => p.User.FullName).ThenBy(p => p.ParentId).ToListAsync();
    }

    public Task<Parent?> GetByIdAsync(int id) => context.Parents
        .Include(p => p.User).ThenInclude(u => u.Role)
        .Include(p => p.Students).ThenInclude(s => s.User)
        .FirstOrDefaultAsync(p => p.ParentId == id);

    public Task<Parent?> GetByUserIdAsync(int userId) => context.Parents
        .Include(p => p.User).ThenInclude(u => u.Role)
        .Include(p => p.Students).ThenInclude(s => s.User)
        .FirstOrDefaultAsync(p => p.UserId == userId);

    public Task<Role?> GetParentRoleAsync() => context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Parent");

    public async Task AddAsync(Parent parent) => await context.Parents.AddAsync(parent);
    public async Task SaveChangesAsync() => await context.SaveChangesAsync();

    public async Task<List<Student>> GetAvailableStudentsAsync(string? search)
    {
        var query = context.Students.AsNoTracking().Include(s => s.User)
            .Where(s => s.ParentId == null);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(s => s.StudentCode.ToLower().Contains(term) || s.User.FullName.ToLower().Contains(term));
        }
        return await query.OrderBy(s => s.StudentCode).ToListAsync();
    }

    public async Task<ParentLinkResult> LinkStudentsAsync(int parentId, IReadOnlyCollection<int> studentIds)
    {
        // Keep account authorization, ownership checks and the whole batch in one transaction.
        // Serializable prevents two staff requests from silently replacing each other's links.
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var parent = await context.Parents.AsNoTracking().Include(p => p.User).ThenInclude(u => u.Role)
            .FirstOrDefaultAsync(p => p.ParentId == parentId);
        if (parent == null) return ParentLinkResult.ParentNotFound;
        if (!parent.User.IsActive || parent.User.Role.RoleName != "Parent") return ParentLinkResult.AccountNotAuthorized;

        var ids = studentIds.Distinct().ToArray();
        var students = await context.Students.Where(s => ids.Contains(s.Id)).ToListAsync();
        if (students.Count != ids.Length) return ParentLinkResult.StudentNotFound;
        if (students.Any(s => s.ParentId.HasValue && s.ParentId != parentId)) return ParentLinkResult.AlreadyLinkedElsewhere;

        foreach (var student in students) student.ParentId = parentId;
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return ParentLinkResult.Success;
    }
}
