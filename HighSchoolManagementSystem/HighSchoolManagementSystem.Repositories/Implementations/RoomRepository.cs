using Microsoft.EntityFrameworkCore;
using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Repositories.Data;
using HighSchoolManagementSystem.Repositories.Interfaces;

namespace HighSchoolManagementSystem.Repositories.Implementations;

public class RoomRepository : IRoomRepository
{
    private readonly AppDbContext _context;

    public RoomRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Room>> GetByFiltersAsync(string? status, string? roomType)
    {
        var query = _context.Rooms.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(r => r.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(roomType))
        {
            query = query.Where(r => r.RoomType == roomType);
        }

        return await query
            .OrderBy(r => r.Building)
            .ThenBy(r => r.RoomCode)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<string>> GetDistinctRoomTypesAsync()
    {
        return await _context.Rooms
            .AsNoTracking()
            .Select(r => r.RoomType)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync();
    }

    public async Task<Room?> GetByIdAsync(int id)
    {
        return await _context.Rooms
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<Room?> GetTrackedByIdAsync(int id)
    {
        return await _context.Rooms
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<bool> ExistsByCodeAsync(string roomCode, int? excludeId = null)
    {
        var normalized = roomCode.Trim().ToLower();
        var query = _context.Rooms
            .Where(r => r.RoomCode.ToLower() == normalized);

        if (excludeId.HasValue)
        {
            query = query.Where(r => r.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task AddAsync(Room room)
    {
        await _context.Rooms.AddAsync(room);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
