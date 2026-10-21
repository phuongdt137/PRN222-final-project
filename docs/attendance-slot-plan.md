# Kế hoạch điểm danh theo từng slot

Ngày khảo sát: 21/10/2026. Đây là kế hoạch ban đầu; chức năng đã được triển khai tiếp theo yêu cầu của người dùng. Quy tắc đã chốt: chỉ được nhập/sửa trước 00:00 ngày kế tiếp theo giờ Việt Nam, không có ngoại lệ mở lại sau hạn. Xem attendance-implementation.md để biết cách sử dụng và kiểm thử.

## 1. Kết luận từ dự án hiện tại

Dự án `HighSchoolManagementSystem` dùng ASP.NET Core MVC .NET 8, EF Core 8 và SQL Server; chia thành Models → Repositories → Services → MVC. Chức năng mới nên tiếp tục dùng kiến trúc này.

| Thành phần đã có | Hiện trạng và ý nghĩa với điểm danh |
| --- | --- |
| `TimeSlot` | Có mã slot, buổi, số tiết, giờ bắt đầu/kết thúc. Số tiết chỉ được kiểm tra trùng trong cùng buổi; không dùng riêng `PeriodNo` làm khóa. |
| `TimetableEntry` | Gắn phân công, phòng, slot, thứ trong tuần. Đây là mẫu lịch lặp hàng tuần, chưa phải một buổi học có ngày cụ thể. Service hiện chấp nhận thứ 2–7. |
| `TeachingAssignment` | Xác định giáo viên, môn, lớp, học kỳ. Cần phân biệt `Teacher.Id` với `User.Id`. |
| `AttendanceRecord` | Có học sinh, giáo viên ghi nhận, tiết trong thời khóa biểu, ngày, trạng thái, ghi chú và đơn nghỉ. Mặc định hiện là `Present`; `RecordedBy` bắt buộc và tham chiếu `Teacher.Id`. Chưa có unique index chống ghi trùng một học sinh trong một tiết vào một ngày. |
| Tầng xử lý điểm danh | Chưa có AttendanceController, AttendanceService, AttendanceRepository và các màn hình nhập/xem tương ứng. |
| Phân quyền | Cookie authentication và các role Staff, Teacher, Student, Parent, Principal. Các controller lấy `User.Id` từ `ClaimTypes.NameIdentifier`. |
| Học sinh–phụ huynh | `Student.ParentId` liên kết `Parent.ParentId`; mỗi phụ huynh có thể có nhiều học sinh. |
| Lịch sử lớp | `StudentClass` có JoinedAt, LeftAt và Status; chuyển lớp đóng bản ghi cũ, mở bản ghi mới. |
| Nghỉ phép | Đã có luồng Pending/Approved/Rejected/Cancelled; hiện duyệt đơn chưa đồng bộ kết quả điểm danh. Đơn nghỉ theo ngày, chưa theo slot. |
| Giao diện | Menu điểm danh của Teacher/Student/Parent đang là liên kết giả. Dashboard giáo viên và học sinh có số liệu điểm danh tĩnh. |
| Báo cáo | PrincipalService và ConductEvaluationRepository đã đọc AttendanceRecords. Cần cập nhật cùng chức năng mới. |

Các điểm cần sửa khi tích hợp:

- PrincipalService có nhánh coi học sinh đều có mặt khi hôm nay chưa có dữ liệu; các tỷ lệ rỗng có thể trả 100%.
- Báo cáo chuyên cần lấy học kỳ để hiển thị nhưng truy vấn records chưa lọc theo học kỳ đó; bộ lọc khối cũng chưa áp dụng nhất quán cho tổng toàn báo cáo.
- Một số thống kê nhóm theo lớp hiện tại của học sinh; sẽ sai khi học sinh chuyển lớp hoặc xem lịch sử năm trước.
- TimetableService đã chặn xóa lịch có AttendanceRecords, nhưng thao tác sửa vẫn có thể đổi các trường của lịch. Phải bảo vệ cả phiên điểm danh chưa có kết quả và lịch sử đã chốt.
- DbInitializer tự gọi `Database.MigrateAsync()` khi khởi động. Migration mới phải được kiểm tra trước khi chạy ứng dụng với cơ sở dữ liệu thật.
- Solution hiện có bốn project ứng dụng, chưa thấy project kiểm thử trong mã nguồn đã khảo sát. Chưa truy vấn database nên chưa kết luận về số lượng hoặc chất lượng dữ liệu điểm danh thực tế.

## 2. Phạm vi nghiệp vụ đề xuất cho phiên bản đầu

Một bảng điểm danh tương ứng **một tiết trong thời khóa biểu vào một ngày cụ thể**, ví dụ: lớp 10A1, môn Toán, slot S1, ngày 21/10/2026. Cùng tiết đó tuần sau là bảng khác.

| Vai trò | Quyền |
| --- | --- |
| Staff | Chọn ngày/lớp/học kỳ, xem trước các tiết, tạo một hoặc nhiều bảng; theo dõi đã/chưa điểm danh; hủy/khôi phục và đồng bộ bảng chưa chốt trong ngày hoặc tương lai; không mở quyền sửa sau hạn. |
| Teacher | Xem và điểm danh các phiên được phân công cho mình; xem lại kết quả; sửa trong thời hạn được phép. Không tự tạo bảng. |
| Student | Chỉ xem kết quả của chính mình và trạng thái chưa có kết quả. |
| Parent | Chọn một trong các con đã liên kết và chỉ xem điểm danh của học sinh đó. |
| Principal | Tiếp tục xem báo cáo chuyên cần, không nhập hoặc sửa điểm danh. |

Giáo viên điểm danh là giáo viên bộ môn của slot. Giáo viên chủ nhiệm không tự có quyền sửa mọi tiết của lớp mình.

Mặc định để lập kế hoạch, có thể điều chỉnh theo quy định của trường:

- Staff tạo theo một ngày, có thể chọn nhiều tiết sau bước xem trước. Tạo cả tuần là mở rộng sau.
- Cho phép điểm danh từ giờ bắt đầu tiết đến hết ngày theo giờ Việt Nam. Không nhập kết quả cho tiết chưa diễn ra.
- Giáo viên có thể sửa bảng đã chốt trước 00:00 ngày kế tiếp; sau hạn chỉ được xem, kể cả khi staff muốn mở lại. Học kỳ đã đóng chỉ xem, không sửa trong phiên bản đầu.
- Chưa làm lưu nháp phía server. Giáo viên nhập toàn bộ danh sách rồi xác nhận một lần; việc sửa bảng đã chốt cũng lưu toàn bộ thay đổi trong một transaction.
- Không tự tạo phiên theo lịch chạy nền, không bổ sung nhận diện khuôn mặt/QR, thông báo phụ huynh hoặc chức năng dạy thay trong phiên bản đầu.

## 3. Luồng sử dụng

### 3.1. Staff tạo bảng

1. Vào “Quản lý điểm danh”, chọn học kỳ đang mở, ngày và lớp; có thể lọc giáo viên.
2. Hệ thống tìm TimetableEntries có thứ trùng ngày, học kỳ phù hợp và hiển thị: lớp, môn, giáo viên, slot/buổi, giờ, phòng, số học sinh dự kiến, trạng thái bảng đã có.
3. Staff chọn các tiết thực sự có học. Hiện chưa có danh mục ngày nghỉ, nên bước xem trước phải cho bỏ chọn tiết/ngày nghỉ; không tự suy luận ngày lễ.
4. Khi tạo, server kiểm tra lại dữ liệu hiện tại, tạo phiên và danh sách học sinh trong cùng transaction.
5. Tạo lại không phát sinh bản trùng; trả rõ số bảng mới và các bảng đã tồn tại/không hợp lệ. Bảng đã hủy vẫn được nhận diện là đã tồn tại; muốn dùng lại cần thao tác khôi phục có kiểm tra và lưu lịch sử.

### 3.2. Giáo viên điểm danh

1. Vào “Điểm danh theo tiết”, mặc định danh sách tiết hôm nay được phân công cho tài khoản đang đăng nhập.
2. Mở bảng, xem thông tin tiết và danh sách học sinh đã được xác định cho phiên đó.
3. Chọn từng học sinh: Có mặt (`Present`), Vắng không phép (`Absent`), Đi muộn (`Late`), Vắng có phép (`Excused`); thêm ghi chú tối đa 255 ký tự.
4. Có thể dùng nút “Đánh dấu có mặt cho các học sinh chưa chọn”. Đây là thao tác chủ động; không mặc định cả lớp có mặt khi staff vừa tạo bảng. Không ghi đè những dòng đang được gợi ý nghỉ có phép.
5. Đơn nghỉ Approved bao phủ ngày học được hiển thị như gợi ý `Excused`; giáo viên vẫn xác nhận theo thực tế.
6. Chỉ cho chốt khi toàn bộ học sinh có kết quả hợp lệ. Server lưu danh sách, người ghi nhận, thời điểm và trạng thái phiên trong một transaction.
7. Sau khi chốt, học sinh/phụ huynh thấy kết quả. Gửi lại cùng dữ liệu không tạo thêm bản ghi hoặc nhân đôi số liệu.

### 3.3. Học sinh và phụ huynh xem

- Học sinh vào “Chuyên cần & Điểm danh”; phụ huynh có thêm bộ chọn con.
- Bộ lọc: học kỳ, khoảng ngày, môn và trạng thái; có phân trang.
- Mỗi dòng thể hiện ngày, buổi/slot, giờ, lớp tại thời điểm học, môn, kết quả, ghi chú công khai và giáo viên ghi nhận.
- Bảng chưa chốt chỉ thể hiện “Chưa có kết quả”, không công bố kết quả tạm, đơn nghỉ đính kèm hoặc dữ liệu các bạn cùng lớp.
- Thống kê số tiết có mặt/đi muộn/vắng không phép/vắng có phép, tổng tiết đã chốt và số tiết đang chờ. Bảng đã hủy không tính vào thống kê.
- Có thể xem lịch sử sau khi chuyển lớp; kết quả không phụ thuộc việc hiện tại còn ở lớp cũ hay không.

## 4. Thiết kế dữ liệu

### 4.1. Thêm AttendanceSession — phần đầu của bảng điểm danh

Các trường chính:

| Trường | Ý nghĩa |
| --- | --- |
| Id | Khóa phiên |
| TimetableEntryId | Tham chiếu mẫu tiết gốc để truy vết |
| AttendanceDate | Ngày học Việt Nam, SQL `date` |
| ClassId, SubjectId, SemesterId | Phạm vi học tập được chụp tại lúc tạo |
| AssignedTeacherId | `Teacher.Id` được giao điểm danh phiên này |
| TimeSlotId, RoomId | Slot và phòng tại lúc tạo |
| SlotCodeSnapshot, PeriodNoSnapshot, SessionSnapshot | Nhãn slot/buổi tại lúc tạo; nhận diện bằng TimeSlotId, không chỉ số tiết |
| StartTimeSnapshot, EndTimeSnapshot | Giờ học tại lúc tạo |
| Status | `Open`, `Submitted`, `Cancelled` |
| CreatedByUserId, CreatedAt | Staff tạo và thời điểm UTC |
| SubmittedByTeacherId, SubmittedAt | Giáo viên và thời điểm chốt lần đầu; nullable trước khi chốt |
| LastUpdatedAt | Thời điểm cập nhật gần nhất |
| RowVersion | SQL Server rowversion chống ghi đè khi có nhiều thao tác cùng lúc |

Không tạo thêm một bảng Slot riêng. Giữ TimeSlot và TimetableEntry hiện có; các trường snapshot giúp sửa thời khóa biểu cho tương lai không làm thay đổi lịch sử phiên đã tạo.

`AttendanceSession.Status` là nguồn quyết định bảng đã được công bố hay chưa. Phiên Submitted vẫn được sửa trong ngày nhưng không đổi về Open; kết quả đang công bố chỉ đổi khi giáo viên lưu sửa thành công trước hạn.

### 4.2. Điều chỉnh AttendanceRecord — từng học sinh trong bảng

- Thêm `AttendanceSessionId` bắt buộc sau khi backfill xong.
- Bỏ mặc định `Present`, thay bằng `NotMarked` để dùng bản ghi làm danh sách học sinh của phiên.
- Đổi `RecordedBy` thành nullable vì staff tạo bảng chưa đồng nghĩa giáo viên đã điểm danh. Khi có kết quả, giá trị vẫn là `Teacher.Id`.
- Thêm `RecordedAt` và `UpdatedAt`; chưa được ghi nhận thì RecordedAt để trống.
- Giữ StudentId, Status, Comment, LeaveRequestId.
- Giữ TimetableEntryId và AttendanceDate trong giai đoạn tương thích vì báo cáo và kiểm tra liên kết đang sử dụng. Mọi đường ghi phải lấy chúng từ session, không lấy từ form. Ràng buộc khóa ngoại tổng hợp tới session bảo đảm cặp tiết/ngày không lệch giữa hai bảng.
- Chỉ record thuộc session Submitted và có trạng thái kết quả hợp lệ mới được dùng để thống kê.

Ràng buộc và chỉ mục:

1. Unique session `(TimetableEntryId, AttendanceDate)` chống staff tạo trùng, kể cả hai yêu cầu đồng thời.
2. Unique có điều kiện `(ClassId, AttendanceDate, TimeSlotId)` với session chưa hủy, chống cùng lớp/slot/ngày phát sinh hai bảng qua hai mẫu lịch khác nhau.
3. Unique record `(AttendanceSessionId, StudentId)`; thêm unique `(StudentId, TimetableEntryId, AttendanceDate)` để bảo vệ dữ liệu tương thích.
4. FK tổng hợp record `(AttendanceSessionId, TimetableEntryId, AttendanceDate)` → alternate key tương ứng của session.
5. Check constraint cho trạng thái; NotMarked đi cùng RecordedBy/RecordedAt null, trạng thái đã ghi nhận phải có người và thời điểm. Điều kiện “Submitted phải đủ toàn bộ danh sách” kiểm tra trong transaction ở service.
6. Index phục vụ session theo giáo viên/ngày, lớp/ngày, học kỳ/ngày; record theo học sinh/ngày. Không xóa cascade lịch sử.

### 4.3. Thêm AttendanceAuditLog

Lưu SessionId, AttendanceRecordId nếu có, loại thao tác, dữ liệu trước/sau cần thiết, ActorUserId, thời gian UTC và lý do. Theo dõi tạo/hủy/khôi phục bảng, đổi danh sách trước điểm danh, sửa kết quả/ghi chú/liên kết đơn nghỉ.

Người thao tác audit là `User.Id`; người thực hiện điểm danh ở record là `Teacher.Id`. Không dùng lẫn hai loại khóa.

## 5. Quy tắc bắt buộc tại service

### 5.1. Lịch, ngày và danh sách học sinh

- Ngày tạo thuộc học kỳ được chọn, học kỳ thuộc năm học của lớp; ngày trong phạm vi semester và đúng thứ của mẫu lịch. Chuyển `DateTime.DayOfWeek` sang quy ước thứ 2–7 của dự án một cách tường minh.
- Kiểm tra phân công còn hợp lệ, giáo viên/tài khoản có thể sử dụng, môn/phòng phù hợp; chuẩn hóa cách hiểu các trạng thái Assigned/Active hiện đang xuất hiện trong mã nguồn.
- Dùng SchoolDate cho “hôm nay”; giờ Việt Nam UTC+7 cho ngày học và thời hạn nghiệp vụ; thời điểm audit lưu UTC. Không phụ thuộc múi giờ máy chủ.
- Xác định học sinh từ lịch sử StudentClass tại thời điểm bắt đầu tiết: `JoinedAt <= lessonStartUtc` và `LeftAt == null || lessonStartUtc < LeftAt`. Không chỉ lọc Status=Enrolled/LeftAt=null khi xem ngày quá khứ. Không suy ngược trạng thái Active hiện tại để loại học sinh khỏi lịch sử.
- Danh sách tạo cho ngày tương lai là dự kiến. Trước điểm danh phải đối chiếu lại membership; nếu thay đổi thì yêu cầu staff cập nhật bảng có hiển thị chênh lệch. Giáo viên không tự thêm/xóa học sinh. Sau Submitted, danh sách được giữ cố định.
- Với dữ liệu mẫu JoinedAt được seed tại thời điểm chạy, không tự đổi ngày vào lớp để làm đủ danh sách quá khứ; trường hợp thiếu lịch sử phải báo rõ để sửa dữ liệu nguồn.
- Bảng không có học sinh không được tạo/chốt. Cần kiểm tra cả trường hợp học sinh trùng do dữ liệu membership không nhất quán.

### 5.2. Phân quyền và kiểm tra yêu cầu

- Controller dùng `[Authorize]` theo role, service kiểm tra tiếp quyền với đối tượng cụ thể cho cả GET và POST.
- Lấy người dùng từ claims, tra Teacher/Student/Parent tương ứng. Không tin TeacherId, RecordedBy, role hoặc cờ override do trình duyệt gửi.
- Giáo viên chỉ có quyền với `AttendanceSession.AssignedTeacherId`. Không dựa trên giáo viên hiện tại của mẫu lịch để sửa dữ liệu cũ.
- StudentId của học sinh tự lấy từ tài khoản; phụ huynh phải được kiểm tra `student.ParentId == currentParent.ParentId` trước khi truy vấn kết quả.
- Nhận danh sách học sinh từ form phải khớp chính xác roster của session: không thiếu, không lặp, không thêm học sinh ngoài lớp. Kiểm tra trạng thái và độ dài ghi chú tại server.
- Tất cả thao tác thay đổi dùng POST và ValidateAntiForgeryToken; dùng DTO chuyên biệt để tránh cập nhật các trường ngoài ý muốn.
- Với Student/Parent, không cung cấp endpoint chi tiết toàn lớp và không dựa vào việc ẩn nút trên giao diện để bảo vệ dữ liệu.

### 5.3. Đơn nghỉ

- Chỉ gợi ý Excused khi đơn thuộc đúng học sinh, đã Approved và khoảng ngày bao phủ ngày học. Đơn Pending/Rejected/Cancelled không được coi là đã có phép.
- Khi lưu, kiểm tra lại LeaveRequestId. Chỉ liên kết đơn với record Excused; giáo viên có thể ghi nhận Excused thủ công nhưng bắt buộc ghi lý do nếu không có đơn đã duyệt.
- Nếu thực tế có mặt/đi muộn dù có đơn, giáo viên ghi nhận theo thực tế; không tự chuyển lại thành Excused.
- Đơn được duyệt sau khi bảng đã chốt chỉ tạo gợi ý cần đối chiếu. Không tự ghi đè kết quả; sửa theo thời hạn và lưu audit.

### 5.4. Nhất quán và thay đổi lịch

- Tạo phiên + roster là một transaction. Chốt/sửa toàn bộ kết quả + audit + RowVersion cũng là một transaction; lỗi một dòng thì rollback toàn bộ.
- Khi RowVersion đã đổi, trả thông báo yêu cầu tải lại, không ghi đè. Mọi thao tác sửa roster/hủy cũng phải cập nhật RowVersion.
- Dùng unique index để chống tạo trùng; kiểm tra trước ở service chỉ nhằm hiển thị thông báo. POST lặp cùng kết quả là no-op hợp lệ.
- Hủy chỉ áp dụng Open; muốn sửa phiên Submitted phải do giáo viên được giao thực hiện trước 00:00 ngày kế tiếp, không hủy để làm biến mất lịch sử.
- Khôi phục Cancelled dùng lại phiên cũ sau khi kiểm tra lịch/roster và các ràng buộc trùng, có audit; không tạo bản ghi mới cùng tiết/ngày.
- Khi sửa lịch/phân công/slot, liệt kê các phiên Open tương lai bị ảnh hưởng và yêu cầu staff đồng bộ rõ ràng; không tự đổi phiên đã chốt. Snapshot lịch sử tiếp tục được dùng khi hiển thị và báo cáo.
- Không cho chốt phiên Open có nguồn lịch đã đổi nhưng chưa được đối chiếu. Khi đồng bộ phải kiểm tra lại quyền, xung đột lớp/giáo viên, membership và ngày học; phiên không còn hợp lệ phải hủy.
- Chặn xóa TimetableEntry/TeachingAssignment/TimeSlot đang được bất kỳ session tham chiếu, kể cả chưa có kết quả. Các kiểm tra HasAttendance và SubjectService hiện tại cần mở rộng sang session.
- Thay giáo viên ở TeachingAssignment không tự chuyển quyền các phiên đã tạo. Trước khi sửa cần giải quyết phiên Open tương lai; phiên Submitted giữ nguyên người được giao và người ghi nhận. Dạy thay cho từng phiên là mở rộng riêng.

## 6. Thành phần triển khai

Các đường dẫn dưới đây tính từ thư mục `HighSchoolManagementSystem/`.

| Tầng | Tạo mới | Cập nhật |
| --- | --- | --- |
| Models | Entities/AttendanceSession.cs, Entities/AttendanceAuditLog.cs; hằng số trạng thái | Entities/AttendanceRecord.cs và navigation liên quan |
| Repositories | Interfaces/IAttendanceRepository.cs, Implementations/AttendanceRepository.cs; migration | Data/AppDbContext.cs; TimetableRepository, SubjectRepository; truy vấn báo cáo chuyên cần |
| Services | DTOs/AttendanceDtos.cs, Interfaces/IAttendanceService.cs, Implementations/AttendanceService.cs | Common/ErrorCodes.cs; TimetableService, SubjectService, TimeSlotService; PrincipalService và ConductEvaluationRepository theo phạm vi dữ liệu |
| MVC | Controllers/AttendanceController.cs, Models/ViewModels/Attendance/AttendanceViewModels.cs, Views/Attendance/* | Program.cs đăng ký DI; _Sidebar.cshtml; các liên kết trong thời khóa biểu và dashboard liên quan |
| Kiểm thử | Project kiểm thử service và kiểm thử tích hợp database | Bổ sung vào solution khi thực hiện |

Contract nghiệp vụ dự kiến, tiếp tục trả `ServiceResult<T>`:

- PreviewSessionsAsync: staff xem trước lịch theo ngày/bộ lọc.
- CreateSessionsAsync: staff tạo các phiên được chọn, kiểm tra lại dữ liệu nguồn trong transaction.
- GetStaffSessionsAsync / GetTeacherSessionsAsync: danh sách có bộ lọc và phân trang.
- GetTeacherSheetAsync: kiểm tra quyền và trả roster, thông tin tiết, đơn nghỉ gợi ý, RowVersion.
- SubmitAttendanceAsync / UpdateSubmittedAttendanceAsync: xác nhận toàn bảng hoặc sửa kết quả, có RowVersion.
- RefreshRosterAsync / CancelSessionAsync / RestoreSessionAsync: tác vụ riêng cho staff, bắt buộc kiểm tra trạng thái và audit.
- GetMyAttendanceAsync: kết quả học sinh từ tài khoản hiện tại.
- GetChildAttendanceAsync / GetMyChildrenAsync: dữ liệu con thuộc phụ huynh hiện tại.

Các action MVC đề xuất:

| Method | Đường dẫn | Quyền và chức năng |
| --- | --- | --- |
| GET | /Attendance/Manage, /Attendance/Create | Staff quản lý và xem trước |
| POST | /Attendance/Create | Staff tạo bảng |
| POST | /Attendance/RefreshRoster, /Attendance/Cancel, /Attendance/Restore | Staff cập nhật danh sách, hủy/khôi phục |
| GET | /Attendance/MyTeachingSessions, /Attendance/Take/{id} | Teacher xem tiết của mình và mở bảng |
| POST | /Attendance/Submit, /Attendance/Update | Teacher xác nhận hoặc sửa toàn bảng |
| GET | /Attendance/MyAttendance | Student xem cá nhân |
| GET | /Attendance/ChildAttendance?studentId=... | Parent xem một con được liên kết |

Không cần thêm REST API độc lập hoặc tách dịch vụ cho phạm vi này.

## 7. Giao diện và thống kê

- Staff: bảng ngày/lớp/môn/buổi/slot/giáo viên, trạng thái, số lượng học sinh, xem chi tiết và các tác vụ được phép. Sau tạo hiển thị kết quả rõ từng bảng.
- Teacher: mặc định hôm nay, lọc khoảng ngày; phân biệt chưa mở giờ điểm danh, Open, Submitted, hết hạn sửa và Cancelled. Form có tổng số từng trạng thái và lỗi tại từng dòng nếu có.
- Student/Parent: bảng lịch sử và thống kê; không cho gửi thao tác sửa. Xử lý rõ chưa liên kết con, chưa có lịch, có bảng chưa chốt và không có kết quả trong bộ lọc.
- Thay menu giả bằng action thật. Chuyển phần lịch/tiến độ điểm danh trên dashboard giáo viên và thẻ chuyên cần của học sinh/phụ huynh sang số liệu thật trong phạm vi chức năng; không làm lại toàn bộ dashboard.
- Nếu thêm nút vào lịch tuần, phải truyền `TimetableEntryId + ngày của ô`; map và hiển thị slot theo TimeSlotId/buổi, tránh nhầm tiết 1 sáng với tiết 1 chiều. Grid hiện đang gộp theo PeriodNo nên cần sửa phần này khi tích hợp.

Quy ước thống kê đề xuất:

- Đơn vị là **lượt học sinh–tiết**, không phải số ngày/buổi hoặc số học sinh duy nhất.
- `Tổng tiết đã ghi nhận = Present + Late + Absent + Excused` trong các phiên Submitted.
- `Tỷ lệ hiện diện = (Present + Late) / Tổng tiết đã ghi nhận × 100%`. Excused vẫn nằm trong mẫu số, nhưng hiển thị riêng với Absent. Đây là đề xuất nghiệp vụ, không phải kết luận về quy định của trường.
- Chưa có tiết Submitted thì tỷ lệ là null và hiển thị “Chưa có dữ liệu”; không trả 0% hoặc 100% như một kết quả đã xác nhận.
- Phiên Open/Cancelled và NotMarked không vào tử/mẫu số. “Chưa có kết quả” là một số đếm riêng.
- Báo cáo lọc đúng Session.SemesterId, ClassId/khối và ngày học; lớp/khối lịch sử lấy qua lớp của session, không qua lớp hiện tại của học sinh.
- Thống kê hạnh kiểm phải ghi rõ số tiết nghỉ. Không tự đổi quy tắc xếp loại hạnh kiểm theo số ngày nếu chưa có yêu cầu.
- Có thể dùng cùng một projection/truy vấn kết quả Submitted để các màn hình không tính khác nhau; không mặc định thêm tầng abstraction nếu chưa cần.

## 8. Migration và dữ liệu cũ

1. Chạy khảo sát chỉ đọc trên database đích khi bắt đầu triển khai: số record, bản trùng học sinh/tiết/ngày, trạng thái lạ, ngày có phần giờ, người ghi nhận, quan hệ nguồn lịch và membership không hợp lệ.
2. Tạo bảng session/audit và cột AttendanceSessionId tạm nullable; điều chỉnh RecordedBy nullable, trạng thái mặc định NotMarked và các cột thời điểm.
3. Với record cũ hợp lệ, nhóm theo TimetableEntryId + ngày học chuẩn hóa để tạo session Submitted và gắn record hiện có. Không tự thêm các học sinh đang thiếu để coi họ có mặt/vắng, không chế ra thời điểm hoặc staff tạo dữ liệu cũ; trường metadata không biết được phép null cho dữ liệu import.
4. Trường hợp không thể suy ra chính xác lớp/giáo viên lịch sử do lịch từng được sửa, record bị trùng hoặc có trạng thái bất thường phải xuất danh sách đối chiếu. Không lấy lịch hiện tại làm lịch sử chắc chắn hoặc tự xóa một bản trùng. Chưa giải quyết xong thì dừng trước bước áp ràng buộc cuối cùng.
5. Sau kiểm tra toàn vẹn mới đặt AttendanceSessionId NOT NULL, chuẩn hóa ngày về SQL date và tạo unique/check/FK. Nếu ngày cũ có UTC/time không rõ nghĩa thì phải đối chiếu trước khi chuẩn hóa.
6. So sánh số record và tổng từng trạng thái trước/sau; đối chiếu báo cáo theo học kỳ/lớp/ngày. Có bản sao lưu và phương án khôi phục database thử nghiệm.
7. Kiểm thử trên SQL Server tách biệt trước khi đưa migration vào luồng tự chạy của DbInitializer. Không khởi động ứng dụng trỏ database thật để thử migration.

Các timestamp không thể khôi phục từ record cũ không được coi là bằng chứng về giờ điểm danh. Audit mới bắt đầu từ khi chức năng được đưa vào sử dụng; ghi rõ nguồn import.

## 9. Thứ tự triển khai và điều kiện hoàn thành

| Giai đoạn | Công việc | Điều kiện hoàn thành |
| --- | --- | --- |
| 1. Chốt quy tắc và dữ liệu | Xác nhận các mặc định tại mục 2; khảo sát database; thiết kế migration/DTO | Có dữ liệu mẫu kiểm thử, quy tắc thời hạn, roster, nghỉ phép và thống kê nhất quán |
| 2. Nền dữ liệu | Session, Record, Audit, indexes, backfill và kiểm tra toàn vẹn | Migration chạy trên database rỗng và bản sao dữ liệu cũ; không mất/nhân đôi record |
| 3. Staff tạo bảng | Repository/service, danh sách, preview, tạo hàng loạt theo ngày, hủy/khôi phục, cập nhật roster | Staff tạo đúng tiết/ngày/học sinh; tạo lặp hoặc đồng thời không trùng; chưa tạo kết quả có mặt giả |
| 4. Teacher điểm danh | Danh sách tiết, form toàn lớp, xác nhận/sửa, thời hạn, đơn nghỉ, RowVersion, audit | Chỉ đúng giáo viên thao tác; lưu nguyên tử; lỗi hoặc xung đột không ghi dở dang |
| 5. Student/Parent và báo cáo | Tra cứu cá nhân/con, thống kê, menu, tích hợp lịch/dashboard; sửa Principal/Conduct queries | Không lộ dữ liệu người khác; số liệu khớp theo slot, học kỳ và lớp lịch sử |
| 6. Kiểm thử và bàn giao | Kiểm thử nghiệp vụ/phân quyền/tích hợp, kiểm tra giao diện, hướng dẫn chạy migration | Toàn bộ kịch bản dưới đây đạt trên môi trường thử nghiệm; có hướng dẫn vận hành và giới hạn đã biết |

Ưu tiên hoàn thành luồng dọc Staff tạo → Teacher chốt → Student/Parent xem trước, sau đó hoàn thiện tác vụ sửa, đối chiếu lịch, các thống kê và tình huống cạnh tranh. Không đưa bản chưa có bảo vệ dữ liệu và phân quyền vào dùng thực tế.

## 10. Kịch bản nghiệm thu

1. Hai slot cùng ngày tạo hai bảng riêng; cùng một mẫu tiết ở hai tuần tạo hai bảng riêng. Slot sáng/chiều cùng số tiết không bị gộp.
2. Hai staff tạo cùng lúc một tiết/ngày chỉ có một session và một record/học sinh; báo rõ bảng đã tồn tại.
3. Sai thứ, ngoài học kỳ, học kỳ đóng, thiếu phân công hoặc không có học sinh bị từ chối tại server.
4. Staff tạo bảng xong chưa phát sinh tỷ lệ có mặt; Student/Parent thấy chưa có kết quả, không thấy roster toàn lớp.
5. Giáo viên A đổi id trên URL hoặc gửi POST tới bảng của B đều bị từ chối; giáo viên chủ nhiệm không được sửa tiết người khác chỉ vì đang chủ nhiệm lớp đó.
6. Gửi thừa/thiếu/trùng StudentId, trạng thái lạ hoặc RecordedBy giả không làm thay đổi dữ liệu.
7. Lưu lỗi một học sinh thì cả bảng và audit không bị ghi một phần; POST lại không tạo record trùng.
8. Hai tab cùng mở bảng: tab lưu sau với RowVersion cũ nhận thông báo tải lại, không làm mất dữ liệu tab trước.
9. Chưa tới tiết không cho điểm danh; đúng 00:00 ngày kế tiếp hết quyền lưu/sửa; staff không được cấp ngoại lệ. Thử sát nửa đêm UTC+7 với máy chủ ở múi giờ khác.
10. Đơn Approved đúng ngày gợi ý Excused; Pending/Rejected/Cancelled không gợi ý; duyệt sau không ghi đè điểm danh đã chốt.
11. Học sinh đổi StudentId không xem được bạn khác; phụ huynh chọn học sinh không liên kết không xem được dữ liệu; phụ huynh nhiều con có kết quả tách riêng.
12. Chuyển lớp trước/sau giờ bắt đầu tiết được xử lý theo membership; bảng tương lai phát hiện roster đã đổi; lịch sử lớp cũ vẫn đúng.
13. Sửa lịch/phân công/giờ slot không đổi lịch sử Submitted; phiên Open bị ảnh hưởng phải được đối chiếu; xóa nguồn đang có session bị chặn.
14. Một học sinh có hai tiết Submitted, một Present và một Absent → hai kết quả, tỷ lệ 50%; thêm một Open không đổi tỷ lệ. Một Present + một Late → hiện diện 100%, vẫn hiển thị một lần đi muộn.
15. Lọc học kỳ/khối/lớp/ngày áp dụng đồng nhất trên bảng chi tiết, tổng số và báo cáo Principal/Conduct. Không có dữ liệu hiển thị “Chưa có dữ liệu”.
16. Migration dữ liệu cũ không thay đổi số record/trạng thái, không tự lấp kết quả thiếu; dữ liệu trùng hoặc lịch sử không thể suy ra được chặn để đối chiếu.

Kiểm thử ràng buộc unique, transaction, filtered index và rowversion phải chạy với SQL Server; EF InMemory không đủ để chứng minh các hành vi này. Kiểm thử service tập trung vào quyền, thời gian, roster và trạng thái. Chi tiết triển khai, migration và các lệnh kiểm thử nằm trong attendance-implementation.md.

## 11. Các tệp nguồn đã đối chiếu

- `HighSchoolManagementSystem.Models/Entities/AttendanceRecord.cs`, `TimetableEntry.cs`, `TimeSlot.cs`, `TeachingAssignment.cs`, `StudentClass.cs`, `Student.cs`, `Parent.cs`, `LeaveRequest.cs`, `Semester.cs`, `Class.cs`.
- `HighSchoolManagementSystem.Repositories/Data/AppDbContext.cs`, `DbInitializer.cs`, `Migrations/AppDbContextModelSnapshot.cs`.
- `HighSchoolManagementSystem.Repositories/Implementations/TimetableRepository.cs`, `StudentRepository.cs`, `ConductEvaluationRepository.cs` và phần tải phân công của `SubjectRepository.cs`.
- `HighSchoolManagementSystem.Services/Implementations/TimetableService.cs`, `TimeSlotService.cs`, `LeaveRequestService.cs`, phần cập nhật phân công trong `SubjectService.cs`, phần dashboard/báo cáo chuyên cần trong `PrincipalService.cs` và các truy vấn chuyên cần trong `ConductEvaluationService.cs`.
- `HighSchoolManagementSystem.Services/Common/SchoolDate.cs`, `ServiceResult.cs`, `ErrorCodes.cs`, các interfaces/DTO liên quan.
- `HighSchoolManagementSystem.MVC/Program.cs`, `Controllers/TimetableController.cs`, `TeacherController.cs`, `StudentController.cs`, `ParentController.cs`, `PrincipalController.cs`.
- `HighSchoolManagementSystem.MVC/Views/Shared/_Sidebar.cshtml`, `Views/Timetable/_WeeklyGrid.cshtml`, `Views/Teacher/Dashboard.cshtml`, `Views/Student/Dashboard.cshtml`.
