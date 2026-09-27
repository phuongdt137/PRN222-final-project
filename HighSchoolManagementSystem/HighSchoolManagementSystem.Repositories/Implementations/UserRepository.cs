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

    public async Task<bool> ExistsByUsernameAsync(string username)
    {
        var normalized = username.Trim().ToLower();
        return await _context.Users.AnyAsync(u => u.Username.ToLower() == normalized);
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        var normalized = email.Trim().ToLower();
        return await _context.Users.AnyAsync(u => u.Email.ToLower() == normalized);
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
}
