# Điểm danh theo lịch học — cập nhật 11/11/2026

## Phạm vi

Module attendance gồm nghiệp vụ điểm danh, tiến trình tự động đăng ký trong Program.cs và phần dữ liệu điểm danh trên Dashboard giáo viên/học sinh/phụ huynh. Các action và giao diện ngoài phần điểm danh của Dashboard được giữ nguyên. Mã nguồn các module thời khóa biểu, đơn xin nghỉ, thông báo, email và điểm số giữ nguyên. Attendance đọc dữ liệu qua AttendanceRepository hiện có, gọi lại INotificationService và IEmailService hiện có.

## Dashboard qua controller và ViewModel

Đã bỏ AttendanceDashboardViewComponent. Action Dashboard của TeacherController, StudentController và ParentController gọi IAttendanceService, đưa dữ liệu/kết quả lỗi vào ViewModel riêng của từng vai trò rồi trả về View. View không gọi service, không dùng Component.InvokeAsync để lấy dữ liệu điểm danh.

- TeacherDashboardViewModel chứa kết quả điểm danh hôm nay; controller chỉ truy vấn một lần, dùng chung cho thẻ và bảng.
- StudentDashboardViewModel và ParentDashboardViewModel chứa chuyên cần từ đầu tháng đến hôm nay và bảng điểm danh hôm nay. Mỗi phần có thông báo lỗi riêng; một phần tải lỗi không làm mất dữ liệu phần còn lại.
- Các partial chỉ hiển thị dữ liệu, nằm tại Views/Shared/AttendanceDashboard: _TeacherCard.cshtml, _TeacherTable.cshtml, _StudentCard.cshtml và _StudentTable.cshtml. Học sinh và phụ huynh dùng chung hai partial Student.
- Controller dùng user ID từ claim đã xác thực; thiếu/sai ID hoặc service trả về không có quyền thì trả Forbid. Phân quyền dữ liệu học sinh/con vẫn do AttendanceService hiện có kiểm tra.
- Các thao tác mở danh sách, mở bảng và lưu điểm danh tiếp tục đi qua AttendanceController. Nghiệp vụ tạo bảng, kiểm tra đơn nghỉ, lưu log và gửi email ở service/tiến trình nền.

## Luồng vận hành

- Hệ thống tạo bảng cho các tiết **trong ngày hiện tại theo UTC+7**, lúc ứng dụng chạy và kiểm tra lại mỗi phút. Trang danh sách/bảng điểm danh cũng thực hiện đồng bộ trước khi đọc. Không còn trang hay API tạo bảng thủ công, đồng bộ/hủy/khôi phục thủ công của staff.
- Chỉ tạo khi ngày thuộc học kỳ đang mở, phân công/giáo viên/môn/phòng hợp lệ và có học sinh thuộc lớp tại giờ bắt đầu tiết. Hai lịch trùng giờ của lớp, giáo viên hoặc phòng không được tự chọn ngẫu nhiên một lịch để tạo bảng.
- Bảng chưa được giáo viên xác nhận tự cập nhật lịch và danh sách. Nếu không còn lịch/danh sách hợp lệ thì tự hủy; nếu lịch hợp lệ trở lại thì tự khôi phục. Những thay đổi tự động đều có lý do và dữ liệu trước/sau trong log.
- Bảng đã xác nhận giữ lịch và danh sách lịch sử, không bị đồng bộ nền ghi đè. Không tự tạo bù ngày quá khứ nếu ứng dụng chưa chạy ngày đó; không suy diễn kết quả học sinh.
- Giáo viên đang hoạt động, được phân công đúng tiết, nhập kết quả cho đủ danh sách. Không cho lưu thiếu học sinh, trùng học sinh, học sinh lạ hoặc trạng thái chưa chọn.
- Có đơn nghỉ **Approved**, đúng học sinh, bao phủ ngày học: không được ghi **Vắng không phép**. Nếu học sinh thực tế đến học thì vẫn cho ghi **Có mặt/Đi muộn**, không gán đơn nghỉ vào hai kết quả này.
- Chọn **Vắng có phép** bắt buộc tự chọn một đơn Approved phù hợp trong danh sách. Ghi chú không thay thế đơn; đơn Pending/Rejected/Cancelled, khác học sinh hoặc ngoài ngày đều bị từ chối tại server, kể cả gửi POST trực tiếp.
- Staff chỉ sửa kết quả **sau khi giáo viên đã điểm danh đầy đủ tiết đó**, không nhập điểm danh lần đầu. Staff giữ nguyên giáo viên ghi nhận ban đầu; danh tính staff chỉnh sửa nằm trong audit log.
- Mọi chỉnh sửa sau xác nhận, kể cả chỉ đổi ghi chú hay đơn nghỉ, phải có nội dung/lý do không rỗng, tối đa 500 ký tự. Log lưu người thao tác, thời gian UTC, lý do, dữ liệu trước/sau. Giao diện hiển thị học sinh, kết quả, ghi chú và đơn nghỉ thay đổi. **Chỉ Staff được xem lịch sử thao tác**; service không truy vấn/trả audit history cho giáo viên và giao diện cũng kiểm tra vai trò Staff. Việc ghi log của mọi người thao tác vẫn được giữ nguyên.
- Giữ quy tắc thời gian hiện có: nhập/sửa từ giờ bắt đầu tiết đến **trước 00:00 ngày kế tiếp theo UTC+7**. Áp dụng cho cả giáo viên và staff; kiểm tra trước/sau khi lưu. Không mở khóa sau hạn.
- Dùng transaction Serializable, unique index và rowversion hiện có để tránh tạo trùng, cập nhật đè và lưu một phần kết quả. Kết quả và log lưu trong cùng giao dịch.

## Thông báo và email

- Khi một học sinh chuyển sang **Vắng không phép** hoặc **Đi muộn**, lưu thành công rồi mới gửi thông báo cho học sinh/phụ huynh và email cho phụ huynh.
- Đợi hoàn tất các lời gọi gửi; không dùng tác vụ chạy rời trên DbContext của request. Nếu lưu/commit thất bại thì không gọi dịch vụ gửi.
- Không gửi lặp khi lưu lại kết quả y hệt hoặc chỉ sửa ghi chú/đơn nghỉ mà trạng thái cảnh báo không đổi. Nếu đổi sang một trạng thái cảnh báo khác thì gửi cảnh báo mới.
- Hàm email dùng tên trạng thái cũ `AbsentWithoutPermission`: attendance chuyển `Absent` sang tên này ở chỗ gọi để email hiển thị đúng **VẮNG MẶT KHÔNG PHÉP**, giữ nguyên mã nguồn email dùng chung.
- Thiếu email phụ huynh hoặc dịch vụ gửi lỗi: giữ kết quả điểm danh đã lưu, ghi lỗi có mã tiết/học sinh vào application log, trả cảnh báo cho người lưu. Chưa có hàng đợi gửi lại bền vững; không cam kết gửi đúng một lần khi ứng dụng dừng giữa lưu và gửi.

## Cơ sở dữ liệu

Migration mới: `20261111091302_AttendanceSystemAuditActor`. Chỉ đổi `attendance_audit_logs.actor_user_id` sang nullable để log hệ thống có actor NULL, không giả danh giáo viên/staff. Không sửa dữ liệu hay schema của module khác. Snapshot chỉ thay phần AttendanceAuditLog. Rollback dừng nếu còn log hệ thống để tránh mất lịch sử/gán sai người thao tác.

Chạy từ thư mục `HighSchoolManagementSystem`:

```powershell
dotnet ef database update --project HighSchoolManagementSystem.Repositories --startup-project HighSchoolManagementSystem.MVC
dotnet run --project HighSchoolManagementSystem.MVC
```

Chưa chạy migration vào database ứng dụng trong quá trình sửa. Cơ chế DbInitializer hiện có của dự án có thể áp dụng migration khi chạy ứng dụng; không thay đổi cơ chế đó.

## Kiểm thử

```powershell
dotnet test HighSchoolManagementSystem.Attendance.Tests/HighSchoolManagementSystem.Attendance.Tests.csproj
dotnet ef migrations has-pending-model-changes --project HighSchoolManagementSystem.Repositories --startup-project HighSchoolManagementSystem.MVC --no-build
```

Bộ kiểm thử attendance dùng SQLite in-memory với repository và transaction thật, giả lập rowversion do SQLite không có rowversion tự sinh như SQL Server. Email/thông báo dùng spy để xác minh được gọi sau commit; không gửi thư thật và không kết nối database ứng dụng. Các ca bao gồm tạo tự động/idempotent, lịch trùng/không hợp lệ, đồng bộ/hủy/khôi phục tự động, bảo toàn lịch sử, đơn nghỉ, staff, lý do chỉnh sửa, nội dung audit, khóa 00:00, rowversion, rollback và lỗi gửi email. Cần kiểm tra tiếp cấu hình SMTP và SQL Server thực tế khi triển khai.

### Kết quả xác minh 11/11/2026

- Build MVC và bộ test thành công, 0 warning, 0 error.
- **63/63 ca kiểm thử đạt**, chạy trực tiếp cùng các phương thức/assertion xUnit. VSTest trong môi trường sửa code bị timeout khi kết nối testhost, nên bổ sung runner trong tiến trình làm phương án dự phòng.
- EF xác nhận không có thay đổi model chưa có migration. SQL migration sinh ra chỉ ALTER cột actor_user_id của attendance_audit_logs và ghi lịch sử migration; không thao tác bảng nghiệp vụ khác.
- Gồm 46 ca nghiệp vụ/phân quyền attendance và 17 ca Dashboard/controller: đúng user/ngày, dùng chung dữ liệu giáo viên, lỗi từng phần và phân quyền.
- Chưa chạy SQL Server thật, chưa kiểm tra trình duyệt hoặc gửi SMTP thật.

Chạy dự phòng khi VSTest không kết nối được testhost, từ thư mục solution:

```powershell
dotnet build HighSchoolManagementSystem.Attendance.Tests/HighSchoolManagementSystem.Attendance.Tests.csproj -m:1
dotnet HighSchoolManagementSystem.Attendance.Tests/bin/Debug/net8.0/HighSchoolManagementSystem.Attendance.Tests.dll
```
