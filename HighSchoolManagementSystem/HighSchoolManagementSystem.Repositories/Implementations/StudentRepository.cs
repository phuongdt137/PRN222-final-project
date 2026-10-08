using System.Data;
using HighSchoolManagementSystem.Models;
using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Repositories.Data;
using HighSchoolManagementSystem.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HighSchoolManagementSystem.Repositories.Implementations;

public class StudentRepository(AppDbContext context) : IStudentRepository
{
    private IQueryable<Student> WithDetails() => context.Students
        .Include(s => s.User).ThenInclude(u => u.Role)
        .Include(s => s.Parent).ThenInclude(p => p!.User)
        .Include(s => s.StudentClasses.Where(sc => sc.Class.AcademicYear.IsCurrent))
            .ThenInclude(sc => sc.Class).ThenInclude(c => c.AcademicYear)
        .Include(s => s.StudentClasses.Where(sc => sc.Class.AcademicYear.IsCurrent))
            .ThenInclude(sc => sc.Class).ThenInclude(c => c.GradeLevel);

    public async Task<PagedResult<Student>> GetAllAsync(string? search, string? status, int? classId, bool unassignedOnly, int page, int pageSize)
    {
        var query = WithDetails().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(s => s.StudentCode.ToLower().Contains(term) || s.User.FullName.ToLower().Contains(term)
                || s.User.Username.ToLower().Contains(term) || s.User.Email.ToLower().Contains(term)
                || (s.User.PhoneNumber != null && s.User.PhoneNumber.Contains(term)));
        }
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(s => s.Status == status);
        if (classId.HasValue) query = query.Where(s => s.StudentClasses.Any(sc => sc.ClassId == classId
            && sc.Class.AcademicYear.IsCurrent && sc.Status == "Enrolled" && sc.LeftAt == null));
        if (unassignedOnly) query = query.Where(s => !s.StudentClasses.Any(sc => sc.Class.AcademicYear.IsCurrent));
        var total = await query.CountAsync();
        page = Math.Clamp(page, 1, Math.Max(1, (int)Math.Ceiling((double)total / pageSize)));
        var items = await query.OrderBy(s => s.StudentCode).ThenBy(s => s.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return new(items, total, page, pageSize);
    }

    public Task<Student?> GetByIdAsync(int id) => WithDetails().FirstOrDefaultAsync(s => s.Id == id);
    public Task<Student?> GetByUserIdAsync(int userId) => WithDetails().FirstOrDefaultAsync(s => s.UserId == userId);
    // Separate collection queries avoid multiplying memberships, scores and reports in one SQL join.
    public Task<Student?> GetAcademicHistoryAsync(int id) => context.Students.AsNoTracking().AsSplitQuery()
        .Include(s => s.User)
        .Include(s => s.StudentClasses).ThenInclude(sc => sc.Class).ThenInclude(c => c.AcademicYear)
        .Include(s => s.StudentClasses).ThenInclude(sc => sc.Class).ThenInclude(c => c.GradeLevel)
        .Include(s => s.SemesterReports).ThenInclude(r => r.Semester).ThenInclude(s => s.AcademicYear)
        .Include(s => s.SemesterSubjectResults).ThenInclude(r => r.Subject)
        .Include(s => s.SemesterSubjectResults).ThenInclude(r => r.Semester).ThenInclude(s => s.AcademicYear)
        .Include(s => s.ConductEvaluations).ThenInclude(r => r.Semester).ThenInclude(s => s.AcademicYear)
        .Include(s => s.StudentScores).ThenInclude(r => r.Assessment).ThenInclude(a => a.TeachingAssignment).ThenInclude(t => t.Subject)
        .Include(s => s.StudentScores).ThenInclude(r => r.Assessment).ThenInclude(a => a.TeachingAssignment).ThenInclude(t => t.Class)
        .Include(s => s.StudentScores).ThenInclude(r => r.Assessment).ThenInclude(a => a.TeachingAssignment).ThenInclude(t => t.Semester).ThenInclude(s => s.AcademicYear)
        .SingleOrDefaultAsync(s => s.Id == id);
    public Task<bool> ExistsByCodeAsync(string code, int? excludedId = null) => context.Students
        .AnyAsync(s => s.StudentCode.ToLower() == code.ToLower() && (!excludedId.HasValue || s.Id != excludedId.Value));
    public Task<Role?> GetStudentRoleAsync() => context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Student");
    public async Task AddAsync(Student student) => await context.Students.AddAsync(student);
    public async Task SaveChangesAsync() => await context.SaveChangesAsync();
    public Task<AcademicYear?> GetCurrentYearAsync() => context.AcademicYears.AsNoTracking().FirstOrDefaultAsync(y => y.IsCurrent);
    public Task<List<Class>> GetCurrentClassesAsync() => context.Classes.AsNoTracking()
        .Include(c => c.AcademicYear).Include(c => c.GradeLevel)
        .Where(c => c.AcademicYear.IsCurrent).OrderBy(c => c.GradeLevel.LevelCode).ThenBy(c => c.ClassName).ToListAsync();

    public async Task<StudentAssignmentResult> AssignClassAsync(int studentId, int classId)
    {
        // Serialize decisions for the same student, including simultaneous assignments to different classes.
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var student = await context.Students.FromSqlInterpolated($"SELECT * FROM students WITH (UPDLOCK, HOLDLOCK) WHERE id = {studentId}")
            .AsNoTracking().SingleOrDefaultAsync();
        if (student == null) return StudentAssignmentResult.StudentNotFound;
        if (student.Status != "Active") return StudentAssignmentResult.StudentInactive;
        var target = await context.Classes.AsNoTracking().Include(c => c.AcademicYear).SingleOrDefaultAsync(c => c.Id == classId);
        if (target == null) return StudentAssignmentResult.ClassNotFound;
        if (!target.AcademicYear.IsCurrent) return StudentAssignmentResult.YearNotCurrent;
        // Even a closed membership prevents this endpoint being used as a class-transfer operation.
        if (await context.StudentClasses.AnyAsync(sc => sc.StudentId == studentId && sc.Class.AcademicYearId == target.AcademicYearId))
            return StudentAssignmentResult.AlreadyAssigned;
        context.StudentClasses.Add(new StudentClass { StudentId = studentId, ClassId = classId, Status = "Enrolled", JoinedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return StudentAssignmentResult.Success;
    }

    public async Task<StudentTransferResult> TransferClassAsync(int studentId, int membershipId, int classId)
    {
        // Use the same student lock as assignment; close and open memberships atomically.
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var student = await context.Students.FromSqlInterpolated($"SELECT * FROM students WITH (UPDLOCK, HOLDLOCK) WHERE id = {studentId}")
            .AsNoTracking().SingleOrDefaultAsync();
        if (student == null) return StudentTransferResult.StudentNotFound;
        if (student.Status != "Active") return StudentTransferResult.StudentInactive;
        var target = await context.Classes.AsNoTracking().Include(c => c.AcademicYear).SingleOrDefaultAsync(c => c.Id == classId);
        if (target == null) return StudentTransferResult.ClassNotFound;
        if (!target.AcademicYear.IsCurrent) return StudentTransferResult.YearNotCurrent;
        var memberships = await context.StudentClasses.Include(sc => sc.Class)
            .Where(sc => sc.StudentId == studentId && sc.Class.AcademicYearId == target.AcademicYearId
                && sc.Status == "Enrolled" && sc.LeftAt == null).ToListAsync();
        if (memberships.Count != 1 || memberships[0].Id != membershipId)
            return StudentTransferResult.MembershipChanged;
        var current = memberships[0];
        if (current.ClassId == classId) return StudentTransferResult.SameClass;
        if (current.Class.GradeLevelId != target.GradeLevelId) return StudentTransferResult.DifferentGrade;
        var now = DateTime.UtcNow;
        if (current.JoinedAt > now) return StudentTransferResult.MembershipChanged;
        current.Status = "Transferred";
        current.LeftAt = now;
        // Preserve old memberships and all scores, even when returning to a previously attended class.
        context.StudentClasses.Add(new StudentClass { StudentId = studentId, ClassId = classId, Status = "Enrolled", JoinedAt = now });
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return StudentTransferResult.Success;
    }
}
