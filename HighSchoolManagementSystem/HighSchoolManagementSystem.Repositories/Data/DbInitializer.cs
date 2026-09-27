using Microsoft.EntityFrameworkCore;
using HighSchoolManagementSystem.Models.Entities;

namespace HighSchoolManagementSystem.Repositories.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext context)
    {
        await context.Database.MigrateAsync();

        var roles = new List<Role>
        {
            new Role { RoleName = "Principal", Description = "Hiệu trưởng - Giám sát, thống kê và báo cáo toàn trường" },
            new Role { RoleName = "Staff", Description = "Nhân viên - Hành chính nhân sự, cán bộ đoàn, quản trị nội bộ" },
            new Role { RoleName = "Teacher", Description = "Giáo viên - Quản lý giảng dạy, điểm số, điểm danh học sinh" },
            new Role { RoleName = "Student", Description = "Học sinh - Tra cứu điểm số, thời khóa biểu, chuyên cần" },
            new Role { RoleName = "Parent", Description = "Phụ huynh - Theo dõi tình hình học tập của con em" }
        };

        foreach (var role in roles)
        {
            if (!await context.Roles.AnyAsync(r => r.RoleName == role.RoleName))
            {
                await context.Roles.AddAsync(role);
            }
        }
        await context.SaveChangesAsync();

        var principalRole = await context.Roles.FirstAsync(r => r.RoleName == "Principal");
        var staffRole = await context.Roles.FirstAsync(r => r.RoleName == "Staff");
        var teacherRole = await context.Roles.FirstAsync(r => r.RoleName == "Teacher");
        var studentRole = await context.Roles.FirstAsync(r => r.RoleName == "Student");
        var parentRole = await context.Roles.FirstAsync(r => r.RoleName == "Parent");

        if (!await context.Users.AnyAsync(u => u.Username == "principal"))
        {
            var principalUser = new User
            {
                Username = "principal",
                Email = "principal@highschool.edu.vn",
                FullName = "Thầy Nguyễn Văn Hiệu Trưởng",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Principal@123", 11),
                PhoneNumber = "0901234567",
                Gender = "Nam",
                IsActive = true,
                CreateAt = DateTime.UtcNow,
                RoleId = principalRole.RoleId
            };
            await context.Users.AddAsync(principalUser);
        }

        if (!await context.Users.AnyAsync(u => u.Username == "staff"))
        {
            var staffUser = new User
            {
                Username = "staff",
                Email = "staff@highschool.edu.vn",
                FullName = "Cô Trần Thị Quản Trị",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Staff@123", 11),
                PhoneNumber = "0912345678",
                Gender = "Nữ",
                IsActive = true,
                CreateAt = DateTime.UtcNow,
                RoleId = staffRole.RoleId
            };
            await context.Users.AddAsync(staffUser);
        }

        if (!await context.Users.AnyAsync(u => u.Username == "teacher"))
        {
            var teacherUser = new User
            {
                Username = "teacher",
                Email = "teacher@highschool.edu.vn",
                FullName = "Thầy Lê Văn Giáo Viên",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Teacher@123", 11),
                PhoneNumber = "0923456789",
                Gender = "Nam",
                IsActive = true,
                CreateAt = DateTime.UtcNow,
                RoleId = teacherRole.RoleId
            };
            await context.Users.AddAsync(teacherUser);
            await context.SaveChangesAsync();

            var teacherProfile = new Teacher
            {
                UserId = teacherUser.Id,
                TeacherCode = "GV001",
                Specialization = "Toán Học",
                Qualification = "Thạc sĩ Sư phạm Toán",
                HireDate = DateTime.UtcNow,
                Status = "Active"
            };
            await context.Teachers.AddAsync(teacherProfile);
        }

        Parent? parentEntity = null;
        if (!await context.Users.AnyAsync(u => u.Username == "parent"))
        {
            var parentUser = new User
            {
                Username = "parent",
                Email = "parent@highschool.edu.vn",
                FullName = "Phụ huynh Phạm Văn Hải",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Parent@123", 11),
                PhoneNumber = "0945678901",
                Gender = "Nam",
                IsActive = true,
                CreateAt = DateTime.UtcNow,
                RoleId = parentRole.RoleId
            };
            await context.Users.AddAsync(parentUser);
            await context.SaveChangesAsync();

            parentEntity = new Parent
            {
                UserId = parentUser.Id,
                Occupation = "Kỹ sư công nghệ",
                EmergencyContact = "0945678901"
            };
            await context.Parents.AddAsync(parentEntity);
            await context.SaveChangesAsync();
        }
        else
        {
            parentEntity = await context.Parents.FirstOrDefaultAsync();
        }

        if (!await context.Users.AnyAsync(u => u.Username == "student"))
        {
            var studentUser = new User
            {
                Username = "student",
                Email = "student@highschool.edu.vn",
                FullName = "Học sinh Phạm Minh Đức",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Student@123", 11),
                PhoneNumber = "0934567890",
                Gender = "Nam",
                IsActive = true,
                CreateAt = DateTime.UtcNow,
                RoleId = studentRole.RoleId
            };
            await context.Users.AddAsync(studentUser);
            await context.SaveChangesAsync();

            var studentProfile = new Student
            {
                UserId = studentUser.Id,
                StudentCode = "HS001",
                Status = "Active",
                AdmissionDate = DateTime.UtcNow,
                Address = "Số 123 Đường Giải Phóng, Hà Nội",
                ParentId = parentEntity?.ParentId
            };
            await context.Students.AddAsync(studentProfile);
        }

        if (!await context.Users.AnyAsync(u => u.Email == "phuongw98yb@gmail.com" || u.Username == "phuongw98yb"))
        {
            var customUser = new User
            {
                Username = "phuongw98yb",
                Email = "phuongw98yb@gmail.com",
                FullName = "Đỗ Thế Phương",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Phuong@123", 11),
                PhoneNumber = "0987654321",
                Gender = "Nam",
                IsActive = true,
                CreateAt = DateTime.UtcNow,
                RoleId = teacherRole.RoleId
            };
            await context.Users.AddAsync(customUser);
            await context.SaveChangesAsync();

            var teacherCustom = new Teacher
            {
                UserId = customUser.Id,
                TeacherCode = "GV999",
                Specialization = "Tin Học",
                Qualification = "Kỹ sư Công nghệ thông tin",
                HireDate = DateTime.UtcNow,
                Status = "Active"
            };
            await context.Teachers.AddAsync(teacherCustom);
        }

        var defaultSubjects = new List<Subject>
        {
            new Subject { SubjectCode = "MATH10", SubjectName = "Toán Học 10", EvaluationType = "Score", Description = "Môn Toán lớp 10 theo chương trình GDPT mới", IsActive = true },
            new Subject { SubjectCode = "PHYS10", SubjectName = "Vật Lý 10", EvaluationType = "Score", Description = "Môn Vật lý cơ bản lớp 10", IsActive = true },
            new Subject { SubjectCode = "CHEM10", SubjectName = "Hóa Học 10", EvaluationType = "Score", Description = "Môn Hóa học cơ bản lớp 10", IsActive = true },
            new Subject { SubjectCode = "BIO10", SubjectName = "Sinh Học 10", EvaluationType = "Score", Description = "Môn Sinh học đại cương lớp 10", IsActive = true },
            new Subject { SubjectCode = "LIT10", SubjectName = "Ngữ Văn 10", EvaluationType = "Score", Description = "Môn Ngữ văn lớp 10", IsActive = true },
            new Subject { SubjectCode = "HIST10", SubjectName = "Lịch Sử 10", EvaluationType = "Score", Description = "Môn Lịch sử Việt Nam và Thế giới lớp 10", IsActive = true },
            new Subject { SubjectCode = "GEOG10", SubjectName = "Địa Lý 10", EvaluationType = "Score", Description = "Môn Địa lý tự nhiên và kinh tế lớp 10", IsActive = true },
            new Subject { SubjectCode = "ENG10", SubjectName = "Tiếng Anh 10", EvaluationType = "Score", Description = "Môn Ngoại ngữ 1 - Tiếng Anh lớp 10", IsActive = true },
            new Subject { SubjectCode = "TIN10", SubjectName = "Tin Học 10", EvaluationType = "Score", Description = "Môn Tin học ứng dụng & thuật toán cơ bản lớp 10", IsActive = true },
            new Subject { SubjectCode = "GDTC10", SubjectName = "Giáo Dục Thể Chất 10", EvaluationType = "PassFail", Description = "Rèn luyện thể lực và thể thao học đường lớp 10", IsActive = true },
            new Subject { SubjectCode = "GDQP10", SubjectName = "Giáo Dục Quốc Phòng - An Ninh 10", EvaluationType = "PassFail", Description = "Giáo dục quốc phòng và an ninh lớp 10", IsActive = true },
            new Subject { SubjectCode = "HDTN10", SubjectName = "Hoạt Động Trải Nghiệm - Hướng Nghiệp 10", EvaluationType = "PassFail", Description = "Hoạt động trải nghiệm, hướng nghiệp lớp 10", IsActive = false }
        };

        foreach (var sub in defaultSubjects)
        {
            if (!await context.Subjects.AnyAsync(s => s.SubjectCode == sub.SubjectCode))
            {
                await context.Subjects.AddAsync(sub);
            }
        }
        await context.SaveChangesAsync();

        AcademicYear? academicYear = await context.AcademicYears.FirstOrDefaultAsync(y => y.YearCode == "2026-2027");
        if (academicYear == null)
        {
            academicYear = new AcademicYear
            {
                YearCode = "2026-2027",
                StartDate = new DateTime(2026, 9, 1),
                EndDate = new DateTime(2027, 5, 31)
            };
            await context.AcademicYears.AddAsync(academicYear);
            await context.SaveChangesAsync();
        }

        Semester? semester = await context.Semesters.FirstOrDefaultAsync(s => s.SemesterCode == "HK1-2026");
        if (semester == null)
        {
            semester = new Semester
            {
                SemesterCode = "HK1-2026",
                SemesterName = "Học kỳ 1",
                StartDate = new DateTime(2026, 9, 1),
                EndDate = new DateTime(2027, 1, 15),
                AcademicYearId = academicYear.Id
            };
            await context.Semesters.AddAsync(semester);
            await context.SaveChangesAsync();
        }

        var grade10 = await context.GradeLevels.FirstOrDefaultAsync(g => g.LevelCode == "K10");
        if (grade10 == null)
        {
            grade10 = new GradeLevel { LevelCode = "K10", LevelName = "Khối 10" };
            await context.GradeLevels.AddAsync(grade10);
        }

        var grade11 = await context.GradeLevels.FirstOrDefaultAsync(g => g.LevelCode == "K11");
        if (grade11 == null)
        {
            grade11 = new GradeLevel { LevelCode = "K11", LevelName = "Khối 11" };
            await context.GradeLevels.AddAsync(grade11);
        }

        var grade12 = await context.GradeLevels.FirstOrDefaultAsync(g => g.LevelCode == "K12");
        if (grade12 == null)
        {
            grade12 = new GradeLevel { LevelCode = "K12", LevelName = "Khối 12" };
            await context.GradeLevels.AddAsync(grade12);
        }
        await context.SaveChangesAsync();

        var teacherGv001 = await context.Teachers.FirstOrDefaultAsync(t => t.TeacherCode == "GV001");
        var teacherGv999 = await context.Teachers.FirstOrDefaultAsync(t => t.TeacherCode == "GV999");

        var class10A1 = await context.Classes.FirstOrDefaultAsync(c => c.ClassName == "10A1");
        if (class10A1 == null)
        {
            class10A1 = new Class
            {
                ClassName = "10A1",
                AcademicYearId = academicYear.Id,
                GradeLevelId = grade10.Id,
                HomeroomTeacherId = teacherGv001?.Id
            };
            await context.Classes.AddAsync(class10A1);
        }

        var class10A2 = await context.Classes.FirstOrDefaultAsync(c => c.ClassName == "10A2");
        if (class10A2 == null)
        {
            class10A2 = new Class
            {
                ClassName = "10A2",
                AcademicYearId = academicYear.Id,
                GradeLevelId = grade10.Id,
                HomeroomTeacherId = teacherGv999?.Id
            };
            await context.Classes.AddAsync(class10A2);
        }
        await context.SaveChangesAsync();

        var math10 = await context.Subjects.FirstOrDefaultAsync(s => s.SubjectCode == "MATH10");
        var tin10 = await context.Subjects.FirstOrDefaultAsync(s => s.SubjectCode == "TIN10");

        if (teacherGv001 != null && math10 != null && class10A1 != null && semester != null)
        {
            if (!await context.TeachingAssignments.AnyAsync(ta => ta.TeacherId == teacherGv001.Id && ta.SubjectId == math10.Id && ta.ClassId == class10A1.Id))
            {
                await context.TeachingAssignments.AddAsync(new TeachingAssignment
                {
                    TeacherId = teacherGv001.Id,
                    SubjectId = math10.Id,
                    ClassId = class10A1.Id,
                    SemesterId = semester.Id,
                    Status = "Active"
                });
            }
            if (class10A2 != null && !await context.TeachingAssignments.AnyAsync(ta => ta.TeacherId == teacherGv001.Id && ta.SubjectId == math10.Id && ta.ClassId == class10A2.Id))
            {
                await context.TeachingAssignments.AddAsync(new TeachingAssignment
                {
                    TeacherId = teacherGv001.Id,
                    SubjectId = math10.Id,
                    ClassId = class10A2.Id,
                    SemesterId = semester.Id,
                    Status = "Active"
                });
            }
        }

        if (teacherGv999 != null && tin10 != null && class10A1 != null && semester != null)
        {
            if (!await context.TeachingAssignments.AnyAsync(ta => ta.TeacherId == teacherGv999.Id && ta.SubjectId == tin10.Id && ta.ClassId == class10A1.Id))
            {
                await context.TeachingAssignments.AddAsync(new TeachingAssignment
                {
                    TeacherId = teacherGv999.Id,
                    SubjectId = tin10.Id,
                    ClassId = class10A1.Id,
                    SemesterId = semester.Id,
                    Status = "Active"
                });
            }
            if (class10A2 != null && !await context.TeachingAssignments.AnyAsync(ta => ta.TeacherId == teacherGv999.Id && ta.SubjectId == tin10.Id && ta.ClassId == class10A2.Id))
            {
                await context.TeachingAssignments.AddAsync(new TeachingAssignment
                {
                    TeacherId = teacherGv999.Id,
                    SubjectId = tin10.Id,
                    ClassId = class10A2.Id,
                    SemesterId = semester.Id,
                    Status = "Active"
                });
            }
        }

        await context.SaveChangesAsync();
    }
}