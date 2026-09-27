namespace HighSchoolManagementSystem.Services.DTOs;

public class UpdateProfileDto
{
    public string? PhoneNumber { get; set; }
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? AvatarUrl { get; set; }
}
