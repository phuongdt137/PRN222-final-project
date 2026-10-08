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

        // Khởi tạo 20 học sinh mẫu
        await SeedSampleStudentsAsync(context, studentRole, parentEntity);

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

        var defaultAssessmentTypes = new List<AssessmentType>
        {
            new AssessmentType { TypeCode = "TX", TypeName = "Đánh giá thường xuyên", Weight = 1.0m, MinEntries = 1, MaxEntries = 5 },
            new AssessmentType { TypeCode = "GK", TypeName = "Đánh giá giữa kỳ", Weight = 2.0m, MinEntries = 1, MaxEntries = 2 },
            new AssessmentType { TypeCode = "CK", TypeName = "Đánh giá cuối kỳ", Weight = 3.0m, MinEntries = 1, MaxEntries = 1 }
        };

        foreach (var type in defaultAssessmentTypes)
        {
            if (!await context.AssessmentTypes.AnyAsync(at => at.TypeCode == type.TypeCode))
            {
                await context.AssessmentTypes.AddAsync(type);
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

        var class11A1 = await context.Classes.FirstOrDefaultAsync(c => c.ClassName == "11A1" && c.AcademicYearId == academicYear.Id);
        if (class11A1 == null)
        {
            await context.Classes.AddAsync(new Class
            {
                ClassName = "11A1",
                AcademicYearId = academicYear.Id,
                GradeLevelId = grade11.Id,
                HomeroomTeacherId = teacherGv001?.Id
            });
        }

        var class12A1 = await context.Classes.FirstOrDefaultAsync(c => c.ClassName == "12A1" && c.AcademicYearId == academicYear.Id);
        if (class12A1 == null)
        {
            await context.Classes.AddAsync(new Class
            {
                ClassName = "12A1",
                AcademicYearId = academicYear.Id,
                GradeLevelId = grade12.Id,
                HomeroomTeacherId = teacherGv999?.Id
            });
        }
        await context.SaveChangesAsync();

        if (!await context.Rooms.AnyAsync())
        {
            await context.Rooms.AddRangeAsync(
                new Room { RoomCode = "A101", RoomName = "Phòng học A101", Building = "Tòa A", RoomType = "Classroom", Capacity = 45, Status = "Available" },
                new Room { RoomCode = "A102", RoomName = "Phòng học A102", Building = "Tòa A", RoomType = "Classroom", Capacity = 45, Status = "Available" },
                new Room { RoomCode = "A201", RoomName = "Phòng học A201", Building = "Tòa A", RoomType = "Classroom", Capacity = 40, Status = "Occupied" },
                new Room { RoomCode = "B101", RoomName = "Phòng thí nghiệm Hóa", Building = "Tòa B", RoomType = "Lab", Capacity = 30, Status = "Available" },
                new Room { RoomCode = "B102", RoomName = "Phòng thí nghiệm Lý", Building = "Tòa B", RoomType = "Lab", Capacity = 30, Status = "Maintenance" },
                new Room { RoomCode = "C101", RoomName = "Phòng máy tính 1", Building = "Tòa C", RoomType = "ComputerLab", Capacity = 40, Status = "Available" },
                new Room { RoomCode = "C102", RoomName = "Phòng máy tính 2", Building = "Tòa C", RoomType = "ComputerLab", Capacity = 40, Status = "Available" },
                new Room { RoomCode = "TV01", RoomName = "Thư viện tầng 1", Building = "Tòa thư viện", RoomType = "Library", Capacity = 80, Status = "Available" },
                new Room { RoomCode = "H01", RoomName = "Phòng họp hội đồng", Building = "Tòa hành chính", RoomType = "Meeting", Capacity = 20, Status = "Available" },
                new Room { RoomCode = "TD01", RoomName = "Nhà thể dục đa năng", Building = "Khu thể thao", RoomType = "Gym", Capacity = 200, Status = "Available" }
            );
            await context.SaveChangesAsync();
        }

        if (!await context.TimeSlots.AnyAsync())
        {
            await context.TimeSlots.AddRangeAsync(
                new TimeSlot { SlotCode = "T1", PeriodNo = 1, StartTime = new TimeSpan(7, 0, 0), EndTime = new TimeSpan(7, 45, 0), Session = "Morning" },
                new TimeSlot { SlotCode = "T2", PeriodNo = 2, StartTime = new TimeSpan(7, 50, 0), EndTime = new TimeSpan(8, 35, 0), Session = "Morning" },
                new TimeSlot { SlotCode = "T3", PeriodNo = 3, StartTime = new TimeSpan(8, 40, 0), EndTime = new TimeSpan(9, 25, 0), Session = "Morning" },
                new TimeSlot { SlotCode = "T4", PeriodNo = 4, StartTime = new TimeSpan(9, 40, 0), EndTime = new TimeSpan(10, 25, 0), Session = "Morning" },
                new TimeSlot { SlotCode = "T5", PeriodNo = 5, StartTime = new TimeSpan(10, 30, 0), EndTime = new TimeSpan(11, 15, 0), Session = "Morning" },
                new TimeSlot { SlotCode = "T6", PeriodNo = 6, StartTime = new TimeSpan(13, 0, 0), EndTime = new TimeSpan(13, 45, 0), Session = "Afternoon" },
                new TimeSlot { SlotCode = "T7", PeriodNo = 7, StartTime = new TimeSpan(13, 50, 0), EndTime = new TimeSpan(14, 35, 0), Session = "Afternoon" },
                new TimeSlot { SlotCode = "T8", PeriodNo = 8, StartTime = new TimeSpan(14, 40, 0), EndTime = new TimeSpan(15, 25, 0), Session = "Afternoon" },
                new TimeSlot { SlotCode = "T9", PeriodNo = 9, StartTime = new TimeSpan(15, 40, 0), EndTime = new TimeSpan(16, 25, 0), Session = "Afternoon" },
                new TimeSlot { SlotCode = "T10", PeriodNo = 10, StartTime = new TimeSpan(16, 30, 0), EndTime = new TimeSpan(17, 15, 0), Session = "Afternoon" }
            );
            await context.SaveChangesAsync();
        }

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

        // Xếp 20 học sinh mẫu vào lớp 10A1
        if (class10A1 != null)
        {
            var studentsToEnroll = await context.Students.ToListAsync();
            foreach (var st in studentsToEnroll)
            {
                if (!await context.StudentClasses.AnyAsync(sc => sc.StudentId == st.Id && sc.ClassId == class10A1.Id))
                {
                    await context.StudentClasses.AddAsync(new StudentClass
                    {
                        StudentId = st.Id,
                        ClassId = class10A1.Id,
                        JoinedAt = DateTime.UtcNow,
                        Status = "Enrolled"
                    });
                }
            }
            await context.SaveChangesAsync();
        }

        // 4. Seed năm học mẫu (để Staff xem danh sách)
        await SeedAcademicYearsAsync(context);
    }

    private static async Task SeedAcademicYearsAsync(AppDbContext context)
    {
        var sampleYears = new List<AcademicYear>
        {
            new AcademicYear
            {
                YearCode = "2024-2025",
                StartDate = new DateTime(2024, 9, 5),
                EndDate = new DateTime(2025, 5, 31)
            },
            new AcademicYear
            {
                YearCode = "2025-2026",
                StartDate = new DateTime(2025, 9, 5),
                EndDate = new DateTime(2026, 5, 31)
            },
            new AcademicYear
            {
                YearCode = "2026-2027",
                StartDate = new DateTime(2026, 9, 5),
                EndDate = new DateTime(2027, 5, 31),
                IsCurrent = true
            }
        };

        foreach (var year in sampleYears)
        {
            if (!await context.AcademicYears.AnyAsync(y => y.YearCode == year.YearCode))
            {
                await context.AcademicYears.AddAsync(year);
            }
        }

        await context.SaveChangesAsync();

        // Đảm bảo luôn có đúng 1 năm hiện hành (ưu tiên 2026-2027 nếu chưa chọn)
        if (!await context.AcademicYears.AnyAsync(y => y.IsCurrent))
        {
            var preferred = await context.AcademicYears.FirstOrDefaultAsync(y => y.YearCode == "2026-2027")
                            ?? await context.AcademicYears.OrderByDescending(y => y.StartDate).FirstOrDefaultAsync();
            if (preferred != null)
            {
                preferred.IsCurrent = true;
                await context.SaveChangesAsync();
            }
        }

        var year2025 = await context.AcademicYears.FirstAsync(y => y.YearCode == "2025-2026");
        var year2026 = await context.AcademicYears.FirstAsync(y => y.YearCode == "2026-2027");

        var sampleSemesters = new List<Semester>
        {
            new Semester
            {
                SemesterCode = "HK1-2526",
                SemesterName = "Học kỳ 1",
                StartDate = new DateTime(2025, 9, 5),
                EndDate = new DateTime(2026, 1, 15),
                AcademicYearId = year2025.Id,
                IsActive = false
            },
            new Semester
            {
                SemesterCode = "HK2-2526",
                SemesterName = "Học kỳ 2",
                StartDate = new DateTime(2026, 1, 20),
                EndDate = new DateTime(2026, 5, 31),
                AcademicYearId = year2025.Id,
                IsActive = false
            },
            new Semester
            {
                SemesterCode = "HK1-2627",
                SemesterName = "Học kỳ 1",
                StartDate = new DateTime(2026, 9, 5),
                EndDate = new DateTime(2027, 1, 15),
                AcademicYearId = year2026.Id,
                IsActive = true
            }
        };

        foreach (var semester in sampleSemesters)
        {
            if (!await context.Semesters.AnyAsync(s => s.SemesterCode == semester.SemesterCode))
            {
                await context.Semesters.AddAsync(semester);
            }
        }

        await context.SaveChangesAsync();

        // Đảm bảo có đúng 1 học kỳ đang mở (ưu tiên HK1-2627)
        if (!await context.Semesters.AnyAsync(s => s.IsActive))
        {
            var preferred = await context.Semesters.FirstOrDefaultAsync(s => s.SemesterCode == "HK1-2627")
                            ?? await context.Semesters.OrderByDescending(s => s.StartDate).FirstOrDefaultAsync();
            if (preferred != null)
            {
                preferred.IsActive = true;
                await context.SaveChangesAsync();
            }
        }
    }

    private static async Task SeedSampleStudentsAsync(
        AppDbContext context,
        Role studentRole,
        Parent? parentEntity)
    {
        var sampleStudents = new (string Code, string Username, string Email, string FullName, string Gender, string Phone, string Address, DateTime Dob)[]
        {
            ("HS001", "student", "student@highschool.edu.vn", "Phạm Minh Đức", "Nam", "0934567890", "Số 123 Giải Phóng, Hai Bà Trưng, Hà Nội", new DateTime(2010, 3, 15)),
            ("HS002", "student02", "student02@highschool.edu.vn", "Trần Thị Bích", "Nữ", "0901000002", "Số 45 Hoàn Kiếm, Hà Nội", new DateTime(2010, 5, 20)),
            ("HS003", "student03", "student03@highschool.edu.vn", "Lê Hoàng Cường", "Nam", "0901000003", "Số 78 Đống Đa, Hà Nội", new DateTime(2010, 1, 10)),
            ("HS004", "student04", "student04@highschool.edu.vn", "Phạm Thu Dung", "Nữ", "0901000004", "Số 15 Cầu Giấy, Hà Nội", new DateTime(2010, 8, 22)),
            ("HS005", "student05", "student05@highschool.edu.vn", "Hoàng Gia Huy", "Nam", "0901000005", "Số 89 Hai Bà Trưng, Hà Nội", new DateTime(2010, 11, 5)),
            ("HS006", "student06", "student06@highschool.edu.vn", "Vũ Mai Linh", "Nữ", "0901000006", "Số 33 Thanh Xuân, Hà Nội", new DateTime(2010, 4, 18)),
            ("HS007", "student07", "student07@highschool.edu.vn", "Đặng Quang Minh", "Nam", "0901000007", "Số 67 Hoàng Mai, Hà Nội", new DateTime(2010, 9, 30)),
            ("HS008", "student08", "student08@highschool.edu.vn", "Bùi Lan Anh", "Nữ", "0901000008", "Số 22 Tây Hồ, Hà Nội", new DateTime(2010, 2, 14)),
            ("HS009", "student09", "student09@highschool.edu.vn", "Đỗ Tuấn Kiệt", "Nam", "0901000009", "Số 90 Long Biên, Hà Nội", new DateTime(2010, 7, 7)),
            ("HS010", "student10", "student10@highschool.edu.vn", "Hồ Bảo Ngọc", "Nữ", "0901000010", "Số 54 Hà Đông, Hà Nội", new DateTime(2010, 12, 1)),
            ("HS011", "student11", "student11@highschool.edu.vn", "Ngô Phương Thảo", "Nữ", "0901000011", "Số 19 Nam Từ Liêm, Hà Nội", new DateTime(2010, 6, 25)),
            ("HS012", "student12", "student12@highschool.edu.vn", "Dương Minh Trí", "Nam", "0901000012", "Số 81 Bắc Từ Liêm, Hà Nội", new DateTime(2010, 10, 12)),
            ("HS013", "student13", "student13@highschool.edu.vn", "Lý Thanh Tùng", "Nam", "0901000013", "Số 37 Gia Lâm, Hà Nội", new DateTime(2010, 3, 3)),
            ("HS014", "student14", "student14@highschool.edu.vn", "Trịnh Thùy Trang", "Nữ", "0901000014", "Số 62 Đông Anh, Hà Nội", new DateTime(2010, 8, 8)),
            ("HS015", "student15", "student15@highschool.edu.vn", "Mai Văn Hưng", "Nam", "0901000015", "Số 11 Sóc Sơn, Hà Nội", new DateTime(2010, 11, 28)),
            ("HS016", "student16", "student16@highschool.edu.vn", "Cao Thục Anh", "Nữ", "0901000016", "Số 48 Thanh Trì, Hà Nội", new DateTime(2010, 1, 19)),
            ("HS017", "student17", "student17@highschool.edu.vn", "Lương Đức Phúc", "Nam", "0901000017", "Số 73 Thường Tín, Hà Nội", new DateTime(2010, 5, 4)),
            ("HS018", "student18", "student18@highschool.edu.vn", "Phan Gia Khánh", "Nam", "0901000018", "Số 29 Phú Xuyên, Hà Nội", new DateTime(2010, 9, 16)),
            ("HS019", "student19", "student19@highschool.edu.vn", "Tạ Nhật Minh", "Nam", "0901000019", "Số 85 Hoài Đức, Hà Nội", new DateTime(2010, 7, 23)),
            ("HS020", "student20", "student20@highschool.edu.vn", "Đoàn Yến Nhi", "Nữ", "0901000020", "Số 41 Đan Phượng, Hà Nội", new DateTime(2010, 4, 9))
        };

        string? defaultPasswordHash = null;

        foreach (var item in sampleStudents)
        {
            var studentUser = await context.Users.FirstOrDefaultAsync(u => u.Username == item.Username || u.Email == item.Email);
            if (studentUser == null)
            {
                defaultPasswordHash ??= BCrypt.Net.BCrypt.HashPassword("Student@123", 11);
                studentUser = new User
                {
                    Username = item.Username,
                    Email = item.Email,
                    FullName = item.FullName,
                    PasswordHash = defaultPasswordHash,
                    PhoneNumber = item.Phone,
                    Gender = item.Gender,
                    DateOfBirth = item.Dob,
                    IsActive = true,
                    CreateAt = DateTime.UtcNow,
                    RoleId = studentRole.RoleId
                };
                await context.Users.AddAsync(studentUser);
                await context.SaveChangesAsync();
            }

            var studentProfile = await context.Students.FirstOrDefaultAsync(s => s.UserId == studentUser.Id || s.StudentCode == item.Code);
            if (studentProfile == null)
            {
                studentProfile = new Student
                {
                    UserId = studentUser.Id,
                    StudentCode = item.Code,
                    Status = "Active",
                    AdmissionDate = new DateTime(2026, 9, 1),
                    Address = item.Address,
                    ParentId = parentEntity?.ParentId
                };
                await context.Students.AddAsync(studentProfile);
                await context.SaveChangesAsync();
            }
        }

        // =========================================================================
        // SEED CONDUCT EVALUATIONS (HẠNH KIỂM MẪU)
        // =========================================================================
        if (!await context.ConductEvaluations.AnyAsync())
        {
            var classesWithHomeroom = await context.Classes
                .Include(c => c.HomeroomTeacher)
                .Include(c => c.StudentClasses).ThenInclude(sc => sc.Student)
                .Where(c => c.HomeroomTeacherId != null)
                .ToListAsync();

            var activeSemester = await context.Semesters.FirstOrDefaultAsync(s => s.IsActive)
                                ?? await context.Semesters.FirstOrDefaultAsync();

            if (activeSemester != null && classesWithHomeroom.Any())
            {
                var sampleEvals = new List<ConductEvaluation>();
                var sampleReports = new List<SemesterReport>();

                foreach (var cls in classesWithHomeroom)
                {
                    var students = cls.StudentClasses
                        .Where(sc => sc.Status == "Enrolled" && sc.LeftAt == null)
                        .Select(sc => sc.Student)
                        .ToList();

                    int sIdx = 0;
                    foreach (var st in students)
                    {
                        string grade = (sIdx % 4 == 0) ? "Tốt" : ((sIdx % 4 == 1) ? "Tốt" : ((sIdx % 4 == 2) ? "Khá" : "Đạt"));
                        string comment = grade == "Tốt"
                            ? "Chăm ngoan, lễ phép, gương mẫu trong học tập và tích cực tham gia các phong trào thi đua của lớp."
                            : (grade == "Khá"
                                ? "Ngoan ngoãn, chấp hành tốt nội quy trường lớp, có ý thức kỷ luật tốt."
                                : "Cơ bản chấp hành nội quy, đôi lúc còn mất trật tự, cần rèn luyện thêm tính tự giác.");

                        var eval = new ConductEvaluation
                        {
                            StudentId = st.Id,
                            SemesterId = activeSemester.Id,
                            HomeroomTeacherId = cls.HomeroomTeacherId!.Value,
                            ConductGrade = grade,
                            TeacherComment = comment,
                            EvaluatedAt = DateTime.UtcNow.AddDays(-2)
                        };
                        sampleEvals.Add(eval);

                        var rep = await context.SemesterReports.FirstOrDefaultAsync(r => r.StudentId == st.Id && r.SemesterId == activeSemester.Id);
                        if (rep != null)
                        {
                            rep.ConductGrade = grade;
                            rep.TeacherComments = comment;
                        }
                        else
                        {
                            sampleReports.Add(new SemesterReport
                            {
                                StudentId = st.Id,
                                SemesterId = activeSemester.Id,
                                ConductGrade = grade,
                                TeacherComments = comment
                            });
                        }

                        sIdx++;
                    }
                }

                if (sampleEvals.Any())
                {
                    await context.ConductEvaluations.AddRangeAsync(sampleEvals);
                }
                if (sampleReports.Any())
                {
                    await context.SemesterReports.AddRangeAsync(sampleReports);
                }
                await context.SaveChangesAsync();
            }
        }
    }
}