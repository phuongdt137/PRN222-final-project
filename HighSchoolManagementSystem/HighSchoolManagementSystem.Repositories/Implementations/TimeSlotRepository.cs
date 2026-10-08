using Microsoft.EntityFrameworkCore;
using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Repositories.Data;
using HighSchoolManagementSystem.Repositories.Interfaces;

namespace HighSchoolManagementSystem.Repositories.Implementations;

public class TimeSlotRepository : ITimeSlotRepository
{
    private readonly AppDbContext _context;

    public TimeSlotRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<TimeSlot>> GetByFiltersAsync(string? session)
    {
        var query = _context.TimeSlots.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(session))
        {
            query = query.Where(t => t.Session == session);
        }

        return await query
            .OrderBy(t => t.PeriodNo)
            .ThenBy(t => t.StartTime)
            .ToListAsync();
    }

    public async Task<TimeSlot?> GetByIdAsync(int id)
    {
        return await _context.TimeSlots
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<TimeSlot?> GetTrackedByIdAsync(int id)
    {
        return await _context.TimeSlots
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<bool> ExistsByCodeAsync(string slotCode, int? excludeId = null)
    {
        var normalized = slotCode.Trim().ToLower();
        var query = _context.TimeSlots
            .Where(t => t.SlotCode.ToLower() == normalized);

        if (excludeId.HasValue)
        {
            query = query.Where(t => t.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<bool> ExistsByPeriodInSessionAsync(int periodNo, string session, int? excludeId = null)
    {
        var query = _context.TimeSlots
            .Where(t => t.PeriodNo == periodNo && t.Session == session);

        if (excludeId.HasValue)
        {
            query = query.Where(t => t.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task AddAsync(TimeSlot timeSlot)
    {
        await _context.TimeSlots.AddAsync(timeSlot);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
