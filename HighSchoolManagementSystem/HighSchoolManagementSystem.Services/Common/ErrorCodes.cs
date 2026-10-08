namespace HighSchoolManagementSystem.Services.Common;

/// <summary>
/// Định nghĩa mã lỗi toàn hệ thống (System-wide Error Codes).
/// Quy ước tiền tố theo module:
/// - SYS_: Hệ thống chung & Dữ liệu
/// - AUTH_: Xác thực & Phân quyền
/// - STU_: Nghiệp vụ Học sinh
/// - TEA_: Nghiệp vụ Giáo viên & Giảng dạy
/// - CLS_: Lớp học & Năm học
/// - SCR_: Điểm số & Đánh giá
/// - ATT_: Chuyên cần & Nghỉ phép
/// </summary>
public static class ErrorCodes
{
    // =========================================================================
    // 1. HỆ THỐNG CHUNG & DỮ LIỆU (SYS_*)
    // =========================================================================
    public const string SystemError            = "SYS_001"; // Lỗi hệ thống không xác định
    public const string ValidationFailed       = "SYS_002"; // Dữ liệu đầu vào không hợp lệ
    public const string ResourceNotFound       = "SYS_003"; // Không tìm thấy tài nguyên yêu cầu
    public const string ResourceAlreadyExists   = "SYS_004"; // Tài nguyên đã tồn tại trong hệ thống
    public const string OperationFailed        = "SYS_005"; // Thao tác xử lý dữ liệu thất bại
    public const string DatabaseError          = "SYS_006"; // Lỗi kết nối hoặc thực thi CSDL

    // =========================================================================
    // 2. XÁC THỰC & PHÂN QUYỀN (AUTH_*)
    // =========================================================================
    public const string AuthInvalidCredentials = "AUTH_001"; // Tên đăng nhập hoặc mật khẩu không chính xác
    public const string AuthAccountLocked      = "AUTH_002"; // Tài khoản bị khóa / tạm dừng hoạt động
    public const string AuthAccountNotFound    = "AUTH_003"; // Tài khoản không tồn tại
    public const string AuthUnauthorized       = "AUTH_004"; // Người dùng chưa đăng nhập
    public const string AuthForbidden          = "AUTH_005"; // Không có thẩm quyền truy cập phân hệ
    public const string AuthPasswordMismatch   = "AUTH_006"; // Mật khẩu xác nhận không khớp
    public const string AuthPasswordTooWeak    = "AUTH_007"; // Mật khẩu chưa đủ độ phức tạp
    public const string AuthUsernameDuplicate  = "AUTH_008"; // Tên đăng nhập đã được sử dụng
    public const string AuthEmailDuplicate     = "AUTH_009"; // Email đã được sử dụng

    // =========================================================================
    // 3. HỌC SINH (STU_*)
    // =========================================================================
    public const string StudentNotFound        = "STU_001"; // Không tìm thấy học sinh
    public const string StudentCodeDuplicate   = "STU_002"; // Mã học sinh đã tồn tại
    public const string StudentClassAssigned   = "STU_003"; // Học sinh chưa được xếp vào lớp nào
    public const string StudentInactive        = "STU_004"; // Trạng thái học sinh không hoạt động (đã chuyển trường/thôi học)

    // =========================================================================
    // 4. GIÁO VIÊN & GIẢNG DẠY (TEA_*)
    // =========================================================================
    public const string TeacherNotFound        = "TEA_001"; // Không tìm thấy giáo viên
    public const string TeacherCodeDuplicate   = "TEA_002"; // Mã giáo viên đã tồn tại
    public const string TeachingConflict       = "TEA_003"; // Trùng lịch giảng dạy hoặc đã vượt quá tiết/tuần

    // =========================================================================
    // 5. LỚP HỌC & NĂM HỌC (CLS_*)
    // =========================================================================
    public const string ClassNotFound          = "CLS_001"; // Không tìm thấy lớp học
    public const string ClassCapacityExceeded  = "CLS_002"; // Lớp đã đủ sĩ số tối đa
    public const string AcademicYearClosed     = "CLS_003"; // Năm học đã kết thúc, không thể chỉnh sửa
    public const string SemesterNotFound       = "CLS_004"; // Không tìm thấy học kỳ
    public const string RoomNotFound           = "CLS_005"; // Không tìm thấy phòng học
    public const string RoomCodeDuplicate      = "CLS_006"; // Mã phòng học đã tồn tại
    public const string TimeSlotNotFound       = "CLS_007"; // Không tìm thấy tiết học
    public const string TimeSlotCodeDuplicate  = "CLS_008"; // Mã tiết học đã tồn tại
    public const string TimeSlotPeriodDuplicate = "CLS_009"; // Số tiết đã tồn tại trong cùng buổi

    // =========================================================================
    // 6. ĐIỂM SỐ & ĐÁNH GIÁ (SCR_*)
    // =========================================================================
    public const string ScoreOutOfRange        = "SCR_001"; // Điểm số không nằm trong thang quy định (0 - 10)
    public const string ScoreLocked            = "SCR_002"; // Điểm số đã bị khóa, không thể chỉnh sửa
    public const string AssessmentNotFound     = "SCR_003"; // Không tìm thấy đầu điểm / bài kiểm tra
    public const string AssessmentMaxEntriesExceeded = "SCR_004"; // Đã đạt số lượng bài kiểm tra tối đa cho loại điểm này
    public const string AssessmentNameRequired = "SCR_005"; // Tên bài kiểm tra không được để trống
    public const string AssessmentTypeNotFound = "SCR_006"; // Không tìm thấy loại điểm đánh giá
    public const string AssessmentAssignmentNotFound = "SCR_007"; // Không tìm thấy phân công giảng dạy
    public const string AssessmentHasLockedScores = "SCR_008"; // Không thể thay đổi loại kiểm tra khi đã có điểm bị khóa
    public const string ScoreNotFound          = "SCR_009"; // Không tìm thấy bản ghi điểm số
    public const string ScoreReasonRequired    = "SCR_010"; // Cần nhập lý do khi điều chỉnh điểm đã có
    public const string ScoreStudentNotInClass = "SCR_011"; // Học sinh không thuộc danh sách lớp học
    public const string AssessmentSubmissionIncomplete = "SCR_012"; // Vui lòng nhập đầy đủ điểm cho toàn bộ học sinh trước khi chốt nộp
    public const string ScoreCommentRequired          = "SCR_013"; // Tất cả học sinh đều phải có nhận xét trước khi chốt nộp
    public const string AssessmentAlreadyLockedForTeacher = "SCR_014"; // Bài kiểm tra đã chốt nộp và bị khóa. Chỉ Giáo vụ mới có quyền điều chỉnh
    public const string FeedbackNotFound       = "FDB_001"; // Không tìm thấy phản hồi / nhận xét
    public const string FeedbackContentRequired = "FDB_002"; // Nội dung phản hồi không được để trống

    // =========================================================================
    // 7. CHUYÊN CẦN & ĐƠN NGHỈ PHÉP (ATT_*)
    // =========================================================================
    public const string AttendanceAlreadyTaken = "ATT_001"; // Tiết học này đã được điểm danh trước đó
    public const string LeaveRequestReviewed   = "ATT_002"; // Đơn nghỉ phép đã được duyệt, không thể thay đổi
    public const string LeaveRequestNotFound   = "ATT_003"; // Không tìm thấy đơn xin nghỉ phép

    // =========================================================================
    // 8. NGƯỜI DÙNG & HỒ SƠ (USR_*)
    // =========================================================================
    public const string UserNotFound           = "USR_001"; // Không tìm thấy người dùng
    public const string UserPhoneInvalid       = "USR_002"; // Số điện thoại không hợp lệ
    public const string UserDobInvalid         = "USR_003"; // Ngày sinh không hợp lệ hoặc lớn hơn hiện tại
    public const string UserProfileUpdateFail  = "USR_004"; // Cập nhật hồ sơ cá nhân thất bại
    public const string UserEmailDuplicate     = "USR_005"; // Email đã được sử dụng
    public const string UserRoleInvalid        = "USR_006"; // Vai trò không tồn tại

    // =========================================================================
    // 9. QUẢN LÝ TỆP TIN & TẢI LÊN (FILE_*)
    // =========================================================================
    public const string FileEmpty              = "FILE_001"; // Tệp tải lên rỗng
    public const string FileTooLarge           = "FILE_002"; // Dung lượng tệp vượt quá giới hạn (tối đa 2MB)
    public const string FileInvalidExtension   = "FILE_003"; // Định dạng tệp không được hỗ trợ (chỉ chấp nhận .jpg, .jpeg, .png, .webp)
    public const string FileSaveFailed         = "FILE_004"; // Lỗi khi lưu tệp tin lên hệ thống

    // =========================================================================
    // 10. ĐẶT LẠI MẬT KHẨU & EMAIL (MAIL_*)
    // =========================================================================
    public const string AuthResetTokenInvalid  = "AUTH_010"; // Mã đặt lại mật khẩu không hợp lệ hoặc đã qua sử dụng
    public const string AuthResetTokenExpired  = "AUTH_011"; // Mã đặt lại mật khẩu đã hết hạn
    public const string EmailSendFailed        = "MAIL_001"; // Gửi email thất bại
    public const string EmailNotConfigured     = "MAIL_002"; // Hệ thống email chưa được cấu hình tài khoản gửi

    public const string SubjectNotFound        = "SUB_001";
    public const string SubjectCodeDuplicate   = "SUB_002";
    public const string SubjectCodeInvalid     = "SUB_003";
    public const string SubjectNameRequired    = "SUB_004";
    public const string SubjectHasAssignments  = "SUB_005";

    public const string TeacherEmailDuplicate  = "TEA_004";
    public const string TeacherUsernameDuplicate = "TEA_005";
    public const string TeacherHasAssignments  = "TEA_006";
    // =========================================================================
    // 12. ĐÁNH GIÁ HẠNH KIỂM (CND_*)
    // =========================================================================
    public const string ConductNotHomeroomTeacher      = "CND_001"; // Chỉ Giáo viên chủ nhiệm mới có quyền đánh giá hạnh kiểm cho lớp này
    public const string ConductNotFound               = "CND_002"; // Không tìm thấy bản ghi đánh giá hạnh kiểm
    public const string ConductInvalidGrade           = "CND_003"; // Mức xếp loại hạnh kiểm không hợp lệ (Chỉ chấp nhận: Tốt, Khá, Đạt, Chưa đạt)
    public const string ConductStudentNotInClass      = "CND_004"; // Học sinh không thuộc danh sách lớp chủ nhiệm này
}
