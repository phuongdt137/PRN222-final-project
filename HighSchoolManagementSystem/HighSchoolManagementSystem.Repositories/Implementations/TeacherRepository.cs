using Microsoft.EntityFrameworkCore;
using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Repositories.Data;
using HighSchoolManagementSystem.Repositories.Interfaces;

namespace HighSchoolManagementSystem.Repositories.Implementations;

public class TeacherRepository : ITeacherRepository
{
    private readonly AppDbContext _context;

    public TeacherRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Teacher>> GetAllAsync(string? searchTerm = null, string? specialization = null, string? status = null)
    {
        var query = _context.Teachers
            .Include(t => t.User)
            .Include(t => t.TeachingAssignments)
            .Include(t => t.HomeroomClasses)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(t => 
                t.TeacherCode.ToLower().Contains(term) ||
                t.User.FullName.ToLower().Contains(term) ||
                t.User.Email.ToLower().Contains(term) ||
                (t.User.PhoneNumber != null && t.User.PhoneNumber.Contains(term)) ||
                (t.Specialization != null && t.Specialization.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(specialization) && specialization != "All")
        {
            query = query.Where(t => t.Specialization == specialization);
        }

        if (!string.IsNullOrWhiteSpace(status) && status != "All")
        {
            query = query.Where(t => t.Status == status);
        }

        return await query.OrderBy(t => t.TeacherCode).ToListAsync();
    }

    public async Task<Teacher?> GetByIdAsync(int id)
    {
        return await _context.Teachers
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<Teacher?> GetByIdWithDetailsAsync(int id)
    {
        return await _context.Teachers
            .Include(t => t.User)
            .Include(t => t.HomeroomClasses)
                .ThenInclude(c => c.AcademicYear)
            .Include(t => t.TeachingAssignments)
                .ThenInclude(ta => ta.Class)
            .Include(t => t.TeachingAssignments)
                .ThenInclude(ta => ta.Subject)
            .Include(t => t.TeachingAssignments)
                .ThenInclude(ta => ta.Semester)
                    .ThenInclude(s => s.AcademicYear)
            .Include(t => t.TeachingAssignments)
                .ThenInclude(ta => ta.TimetableEntries)
                    .ThenInclude(te => te.TimeSlot)
            .Include(t => t.TeachingAssignments)
                .ThenInclude(ta => ta.TimetableEntries)
                    .ThenInclude(te => te.Room)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<Teacher?> GetByCodeAsync(string teacherCode)
    {
        var code = teacherCode.Trim().ToLower();
        return await _context.Teachers
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TeacherCode.ToLower() == code);
    }

    public async Task<bool> ExistsByCodeAsync(string teacherCode, int? excludeId = null)
    {
        var code = teacherCode.Trim().ToLower();
        return await _context.Teachers
            .AnyAsync(t => t.TeacherCode.ToLower() == code && (!excludeId.HasValue || t.Id != excludeId.Value));
    }

    public async Task<bool> ExistsByEmailAsync(string email, int? excludeUserId = null)
    {
        var mail = email.Trim().ToLower();
        return await _context.Users
            .AnyAsync(u => u.Email.ToLower() == mail && (!excludeUserId.HasValue || u.Id != excludeUserId.Value));
    }

    public async Task<bool> ExistsByUsernameAsync(string username, int? excludeUserId = null)
    {
        var name = username.Trim().ToLower();
        return await _context.Users
            .AnyAsync(u => u.Username.ToLower() == name && (!excludeUserId.HasValue || u.Id != excludeUserId.Value));
    }

    public async Task<Role?> GetTeacherRoleAsync()
    {
        return await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Teacher");
    }

    public async Task AddAsync(Teacher teacher)
    {
        await _context.Teachers.AddAsync(teacher);
    }

    public Task UpdateAsync(Teacher teacher)
    {
        _context.Teachers.Update(teacher);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
