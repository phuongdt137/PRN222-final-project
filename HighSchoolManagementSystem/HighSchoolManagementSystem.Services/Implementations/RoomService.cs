using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Repositories.Interfaces;
using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.Interfaces;

namespace HighSchoolManagementSystem.Services.Implementations;

public class RoomService : IRoomService
{
    private static readonly HashSet<string> AllowedRoomTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Classroom", "Lab", "ComputerLab", "Library", "Meeting", "Gym"
    };

    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Available", "Occupied", "Maintenance"
    };

    private readonly IRoomRepository _roomRepository;

    public RoomService(IRoomRepository roomRepository)
    {
        _roomRepository = roomRepository;
    }

    public async Task<ServiceResult<IReadOnlyList<Room>>> GetByFiltersAsync(string? status, string? roomType)
    {
        try
        {
            var rooms = await _roomRepository.GetByFiltersAsync(status, roomType);
            return ServiceResult<IReadOnlyList<Room>>.Ok(rooms);
        }
        catch
        {
            return ServiceResult<IReadOnlyList<Room>>.Fail(
                ErrorCodes.DatabaseError,
                "Không thể tải danh sách phòng học. Vui lòng thử lại sau.");
        }
    }

    public async Task<ServiceResult<IReadOnlyList<string>>> GetRoomTypesAsync()
    {
        try
        {
            var types = await _roomRepository.GetDistinctRoomTypesAsync();
            return ServiceResult<IReadOnlyList<string>>.Ok(types);
        }
        catch
        {
            return ServiceResult<IReadOnlyList<string>>.Fail(
                ErrorCodes.DatabaseError,
                "Không thể tải danh sách loại phòng học.");
        }
    }

    public async Task<ServiceResult<Room>> GetByIdAsync(int id)
    {
        try
        {
            var room = await _roomRepository.GetByIdAsync(id);
            if (room == null)
            {
                return ServiceResult<Room>.Fail(
                    ErrorCodes.RoomNotFound,
                    "Không tìm thấy phòng học.");
            }

            return ServiceResult<Room>.Ok(room);
        }
        catch
        {
            return ServiceResult<Room>.Fail(
                ErrorCodes.DatabaseError,
                "Không thể tải thông tin phòng học. Vui lòng thử lại sau.");
        }
    }

    public async Task<ServiceResult<Room>> CreateAsync(
        string roomCode,
        string roomName,
        string? building,
        string roomType,
        int capacity,
        string status)
    {
        var validation = ValidateRoomInput(roomCode, roomName, building, roomType, capacity, status);
        if (validation != null)
        {
            return validation;
        }

        roomCode = roomCode.Trim();
        roomName = roomName.Trim();
        building = string.IsNullOrWhiteSpace(building) ? null : building.Trim();
        roomType = NormalizeRoomType(roomType);
        status = NormalizeStatus(status);

        try
        {
            if (await _roomRepository.ExistsByCodeAsync(roomCode))
            {
                return ServiceResult<Room>.Fail(
                    ErrorCodes.RoomCodeDuplicate,
                    $"Mã phòng \"{roomCode}\" đã tồn tại.");
            }

            var room = new Room
            {
                RoomCode = roomCode,
                RoomName = roomName,
                Building = building,
                RoomType = roomType,
                Capacity = capacity,
                Status = status
            };

            await _roomRepository.AddAsync(room);
            await _roomRepository.SaveChangesAsync();

            return ServiceResult<Room>.Ok(room, "Tạo phòng học thành công.");
        }
        catch
        {
            return ServiceResult<Room>.Fail(
                ErrorCodes.DatabaseError,
                "Không thể tạo phòng học. Vui lòng thử lại sau.");
        }
    }

    public async Task<ServiceResult<Room>> UpdateAsync(
        int id,
        string roomCode,
        string roomName,
        string? building,
        string roomType,
        int capacity,
        string status)
    {
        var validation = ValidateRoomInput(roomCode, roomName, building, roomType, capacity, status);
        if (validation != null)
        {
            return validation;
        }

        roomCode = roomCode.Trim();
        roomName = roomName.Trim();
        building = string.IsNullOrWhiteSpace(building) ? null : building.Trim();
        roomType = NormalizeRoomType(roomType);
        status = NormalizeStatus(status);

        try
        {
            var room = await _roomRepository.GetTrackedByIdAsync(id);
            if (room == null)
            {
                return ServiceResult<Room>.Fail(
                    ErrorCodes.RoomNotFound,
                    "Không tìm thấy phòng học cần cập nhật.");
            }

            if (await _roomRepository.ExistsByCodeAsync(roomCode, excludeId: id))
            {
                return ServiceResult<Room>.Fail(
                    ErrorCodes.RoomCodeDuplicate,
                    $"Mã phòng \"{roomCode}\" đã tồn tại.");
            }

            room.RoomCode = roomCode;
            room.RoomName = roomName;
            room.Building = building;
            room.RoomType = roomType;
            room.Capacity = capacity;
            room.Status = status;

            await _roomRepository.SaveChangesAsync();

            return ServiceResult<Room>.Ok(room, "Cập nhật phòng học thành công.");
        }
        catch
        {
            return ServiceResult<Room>.Fail(
                ErrorCodes.DatabaseError,
                "Không thể cập nhật phòng học. Vui lòng thử lại sau.");
        }
    }

    private static ServiceResult<Room>? ValidateRoomInput(
        string roomCode,
        string roomName,
        string? building,
        string roomType,
        int capacity,
        string status)
    {
        if (string.IsNullOrWhiteSpace(roomCode))
        {
            return ServiceResult<Room>.Fail(ErrorCodes.ValidationFailed, "Vui lòng nhập mã phòng.");
        }

        if (roomCode.Trim().Length > 20)
        {
            return ServiceResult<Room>.Fail(ErrorCodes.ValidationFailed, "Mã phòng không được vượt quá 20 ký tự.");
        }

        if (string.IsNullOrWhiteSpace(roomName))
        {
            return ServiceResult<Room>.Fail(ErrorCodes.ValidationFailed, "Vui lòng nhập tên phòng.");
        }

        if (roomName.Trim().Length > 50)
        {
            return ServiceResult<Room>.Fail(ErrorCodes.ValidationFailed, "Tên phòng không được vượt quá 50 ký tự.");
        }

        if (!string.IsNullOrWhiteSpace(building) && building.Trim().Length > 50)
        {
            return ServiceResult<Room>.Fail(ErrorCodes.ValidationFailed, "Tên tòa nhà không được vượt quá 50 ký tự.");
        }

        if (string.IsNullOrWhiteSpace(roomType) || !AllowedRoomTypes.Contains(roomType.Trim()))
        {
            return ServiceResult<Room>.Fail(ErrorCodes.ValidationFailed, "Loại phòng không hợp lệ.");
        }

        var maxCapacity = roomType.Trim().Equals("Gym", StringComparison.OrdinalIgnoreCase) ? 5000 : 500;
        if (capacity < 1 || capacity > maxCapacity)
        {
            return ServiceResult<Room>.Fail(
                ErrorCodes.ValidationFailed,
                roomType.Trim().Equals("Gym", StringComparison.OrdinalIgnoreCase)
                    ? "Sức chứa nhà đa năng phải từ 1 đến 5000."
                    : "Sức chứa phòng phải từ 1 đến 500.");
        }

        if (string.IsNullOrWhiteSpace(status) || !AllowedStatuses.Contains(status.Trim()))
        {
            return ServiceResult<Room>.Fail(ErrorCodes.ValidationFailed, "Trạng thái phòng không hợp lệ.");
        }

        return null;
    }

    private static string NormalizeRoomType(string roomType)
    {
        return AllowedRoomTypes.First(t => t.Equals(roomType.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeStatus(string status)
    {
        return AllowedStatuses.First(s => s.Equals(status.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
