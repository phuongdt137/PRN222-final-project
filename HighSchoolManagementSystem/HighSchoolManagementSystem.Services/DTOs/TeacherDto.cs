namespace HighSchoolManagementSystem.Services.DTOs;

public class CreateTeacherDto
{
    public string TeacherCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Specialization { get; set; }
    public string? Qualification { get; set; }
    public DateTime? HireDate { get; set; }
    public int? MaxPeriodsPerWeek { get; set; } = 20;
    public string Status { get; set; } = "Active";
}

public class UpdateTeacherDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string TeacherCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Specialization { get; set; }
    public string? Qualification { get; set; }
    public DateTime? HireDate { get; set; }
    public int? MaxPeriodsPerWeek { get; set; }
    public string Status { get; set; } = "Active";
}
