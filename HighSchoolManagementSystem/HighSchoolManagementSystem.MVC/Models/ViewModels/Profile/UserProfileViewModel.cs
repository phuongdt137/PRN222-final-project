namespace HighSchoolManagementSystem.MVC.Models.ViewModels.Profile;

public class UserProfileViewModel
{
    // Thông tin tài khoản & cơ bản
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? AvatarUrl { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string? RoleDescription { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreateAt { get; set; }

    public string InitialLetter => !string.IsNullOrWhiteSpace(FullName)
        ? FullName.Trim().Substring(0, 1).ToUpper()
        : (!string.IsNullOrWhiteSpace(Username) ? Username.Substring(0, 1).ToUpper() : "U");

    // Thông tin mở rộng dành cho Giáo viên (Teacher)
    public TeacherProfileSection? TeacherSection { get; set; }

    // Thông tin mở rộng dành cho Học sinh (Student)
    public StudentProfileSection? StudentSection { get; set; }

    // Thông tin mở rộng dành cho Phụ huynh (Parent)
    public ParentProfileSection? ParentSection { get; set; }
}

public class TeacherProfileSection
{
    public string TeacherCode { get; set; } = string.Empty;
    public string? Specialization { get; set; }
    public string? Qualification { get; set; }
    public DateTime? HireDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? MaxPeriodsPerWeek { get; set; }
    public List<string> HomeroomClassNames { get; set; } = new();
}

public class StudentProfileSection
{
    public string StudentCode { get; set; } = string.Empty;
    public DateTime? AdmissionDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? CurrentClassName { get; set; }
    public string? ParentFullName { get; set; }
    public string? ParentPhone { get; set; }
}

public class ParentProfileSection
{
    public string? Occupation { get; set; }
    public string? EmergencyContact { get; set; }
    public List<StudentChildSummary> Children { get; set; } = new();
}

public class StudentChildSummary
{
    public string FullName { get; set; } = string.Empty;
    public string StudentCode { get; set; } = string.Empty;
    public string? CurrentClassName { get; set; }
}
