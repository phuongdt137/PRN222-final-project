using Microsoft.EntityFrameworkCore;
using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Repositories.Data;
using HighSchoolManagementSystem.Repositories.Interfaces;

namespace HighSchoolManagementSystem.Repositories.Implementations;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<User>> GetAllAsync(string? search = null, int? roleId = null, bool? isActive = null)
    {
        var query = _context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLower();
            query = query.Where(u => u.Username.ToLower().Contains(normalizedSearch)
                || u.Email.ToLower().Contains(normalizedSearch)
                || u.FullName.ToLower().Contains(normalizedSearch));
        }

        if (roleId.HasValue)
        {
            query = query.Where(u => u.RoleId == roleId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        return await query
            .OrderBy(u => u.FullName)
            .ThenBy(u => u.Username)
            .ToListAsync();
    }

    public async Task<User?> GetByUsernameOrEmailAsync(string usernameOrEmail)
    {
        var normalized = usernameOrEmail.Trim().ToLower();
        return await _context.Users
            .Include(u => u.Role)
            .Include(u => u.Teacher)
            .Include(u => u.Student)
            .Include(u => u.Parent)
            .FirstOrDefaultAsync(u => u.Username.ToLower() == normalized || u.Email.ToLower() == normalized);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        var normalized = email.Trim().ToLower();
        return await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalized);
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        return await _context.Users
            .Include(u => u.Role)
            .Include(u => u.Teacher)
            .Include(u => u.Student)
            .Include(u => u.Parent)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User?> GetUserFullProfileByIdAsync(int id)
    {
        return await _context.Users
            .Include(u => u.Role)
            .Include(u => u.Teacher)
                .ThenInclude(t => t!.HomeroomClasses)
            .Include(u => u.Student)
                .ThenInclude(s => s!.Parent)
                    .ThenInclude(p => p!.User)
            .Include(u => u.Student)
                .ThenInclude(s => s!.StudentClasses)
                    .ThenInclude(sc => sc.Class)
            .Include(u => u.Parent)
                .ThenInclude(p => p!.Students)
                    .ThenInclude(s => s.User)
            .Include(u => u.Parent)
                .ThenInclude(p => p!.Students)
                    .ThenInclude(s => s.StudentClasses)
                        .ThenInclude(sc => sc.Class)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<Role?> GetRoleByIdAsync(int roleId)
    {
        return await _context.Roles.FirstOrDefaultAsync(r => r.RoleId == roleId);
    }

    public async Task<List<Role>> GetRolesAsync()
    {
        return await _context.Roles
            .AsNoTracking()
            .OrderBy(r => r.RoleName)
            .ToListAsync();
    }

    public async Task<bool> ExistsByUsernameAsync(string username)
    {
        var normalized = username.Trim().ToLower();
        return await _context.Users.AnyAsync(u => u.Username.ToLower() == normalized);
    }

    public async Task<bool> ExistsByEmailAsync(string email, int? excludedUserId = null)
    {
        var normalized = email.Trim().ToLower();
        return await _context.Users.AnyAsync(u => u.Email.ToLower() == normalized
            && (!excludedUserId.HasValue || u.Id != excludedUserId.Value));
    }

    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
    }

    public async Task UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<string> GenerateNextStudentCodeAsync()
    {
        var existingCodes = await _context.Students
            .Select(s => s.StudentCode)
            .ToListAsync();

        int maxNum = 0;
        foreach (var code in existingCodes)
        {
            if (!string.IsNullOrWhiteSpace(code) && code.StartsWith("HS", StringComparison.OrdinalIgnoreCase))
            {
                var numPart = code.Substring(2);
                if (int.TryParse(numPart, out var n) && n > maxNum)
                {
                    maxNum = n;
                }
            }
        }

        int nextNum = maxNum + 1;
        var candidate = $"HS{nextNum:D3}";
        while (existingCodes.Any(c => string.Equals(c, candidate, StringComparison.OrdinalIgnoreCase)))
        {
            nextNum++;
            candidate = $"HS{nextNum:D3}";
        }

        return candidate;
    }

    public async Task<string> GenerateNextTeacherCodeAsync()
    {
        var existingCodes = await _context.Teachers
            .Select(t => t.TeacherCode)
            .ToListAsync();

        int maxNum = 0;
        foreach (var code in existingCodes)
        {
            if (!string.IsNullOrWhiteSpace(code) && code.StartsWith("GV", StringComparison.OrdinalIgnoreCase))
            {
                var numPart = code.Substring(2);
                if (int.TryParse(numPart, out var n) && n < 900 && n > maxNum)
                {
                    maxNum = n;
                }
            }
        }

        int nextNum = maxNum + 1;
        var candidate = $"GV{nextNum:D3}";
        while (existingCodes.Any(c => string.Equals(c, candidate, StringComparison.OrdinalIgnoreCase)))
        {
            nextNum++;
            candidate = $"GV{nextNum:D3}";
        }

        return candidate;
    }
}
