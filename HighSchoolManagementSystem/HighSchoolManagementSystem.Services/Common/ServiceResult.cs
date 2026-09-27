namespace HighSchoolManagementSystem.Services.Common;

/// <summary>
/// Lớp đóng gói kết quả trả về chung cho toàn bộ tầng Services (Result Pattern).
/// Dùng cho các thao tác không có dữ liệu trả về (Create, Update, Delete, ToggleStatus...).
/// </summary>
public class ServiceResult
{
    public bool Success { get; set; }
    public string? ErrorCode { get; set; }
    public string? Message { get; set; }
    public List<string> Errors { get; set; } = new();

    public static ServiceResult Ok(string? message = null) => new()
    {
        Success = true,
        Message = message
    };

    public static ServiceResult Fail(string errorCode, string message, List<string>? errors = null) => new()
    {
        Success = false,
        ErrorCode = errorCode,
        Message = message,
        Errors = errors ?? new List<string>()
    };

    public static ServiceResult Fail(string errorCode, string message, string singleError) => new()
    {
        Success = false,
        ErrorCode = errorCode,
        Message = message,
        Errors = new List<string> { singleError }
    };
}

/// <summary>
/// Lớp đóng gói kết quả trả về chung có kèm dữ liệu (Generic Result Pattern).
/// Dùng cho các thao tác lấy dữ liệu, đăng nhập, hoặc trả về đối tượng vừa tạo/sửa.
/// </summary>
/// <typeparam name="T">Kiểu dữ liệu của đối tượng trả về</typeparam>
public class ServiceResult<T> : ServiceResult
{
    public T? Data { get; set; }

    public static ServiceResult<T> Ok(T data, string? message = null) => new()
    {
        Success = true,
        Data = data,
        Message = message
    };

    public new static ServiceResult<T> Fail(string errorCode, string message, List<string>? errors = null) => new()
    {
        Success = false,
        ErrorCode = errorCode,
        Message = message,
        Errors = errors ?? new List<string>()
    };

    public new static ServiceResult<T> Fail(string errorCode, string message, string singleError) => new()
    {
        Success = false,
        ErrorCode = errorCode,
        Message = message,
        Errors = new List<string> { singleError }
    };
}
