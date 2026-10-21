# Điểm danh theo slot — hướng dẫn sử dụng và triển khai

Chức năng được bổ sung ngày 21/10/2026 trên kiến trúc MVC → Services → Repositories → Models hiện có.

## Quy tắc đã triển khai

- Một bảng ứng với một tiết trong thời khóa biểu và một ngày cụ thể. Slot sáng/chiều có cùng số tiết vẫn là hai slot riêng.
- Staff tạo bảng; giáo viên phụ trách nhập kết quả; học sinh xem chính mình; phụ huynh chỉ xem học sinh đã liên kết với tài khoản.
- Giáo viên được nhập và sửa từ giờ bắt đầu tiết học đến **trước 00:00 ngày kế tiếp, theo giờ Việt Nam (UTC+7)**. Ví dụ tiết ngày 21/10 được sửa đến 23:59:59 ngày 21/10; từ 00:00 ngày 22/10 chỉ được xem.
- Máy chủ kiểm tra hạn khi lưu, kể cả biểu mẫu đã mở từ trước. Giao diện cũng tự khóa khi hết hạn. Không có thao tác mở lại sau hạn.
- Học sinh mới trong bảng có trạng thái **Chưa điểm danh**. Chỉ khi giáo viên xác nhận đủ danh sách mới công bố kết quả.
- Kết quả gồm Có mặt, Vắng không phép, Đi muộn, Vắng có phép. Đơn nghỉ đã duyệt đúng học sinh/ngày được gợi ý; vắng có phép không có đơn cần ghi lý do.
- Khi sửa kết quả đã xác nhận, giáo viên phải nhập lý do. Hệ thống lưu người thao tác, thời gian và nội dung thay đổi.
- Bảng lưu lớp, giáo viên, môn và slot tại lúc tạo; chuyển lớp hoặc sửa thời khóa biểu không tự thay đổi lịch sử đã xác nhận.
- Khi lịch hoặc danh sách của bảng chưa xác nhận thay đổi, staff phải đồng bộ trước khi giáo viên điểm danh.
- Chống tạo trùng và lưu đè biểu mẫu cũ. Nếu có thao tác đồng thời gây xung đột, hệ thống yêu cầu tải lại; không lưu một phần danh sách.

## Cách dùng

### Staff

Điều kiện: học kỳ đang mở phải có phân công giảng dạy và các tiết trong thời khóa biểu. Nếu trang tạo bảng chưa có tiết, thực hiện **Phân công giảng dạy → Quản lý thời khóa biểu → chọn giáo viên → Bật Xếp Lịch (+) → thêm tiết**, rồi quay lại chọn ngày học tương ứng. Trang sẽ thông báo riêng khi chưa có lịch, khi ngày đã chọn không có tiết hoặc khi chưa mở học kỳ; không hiển thị khung lỗi trống.

1. Mở **Quản lý điểm danh** trong menu, chọn tạo bảng.
2. Chọn ngày, học kỳ và lớp; xem trước các tiết khớp ngày trong thời khóa biểu.
3. Chọn tiết rồi tạo bảng. Có thể tạo trước cho ngày tương lai trong học kỳ đang mở. Tạo lại cùng tiết/ngày không nhân đôi bảng.
4. Mở bảng để theo dõi trạng thái. Bảng chưa xác nhận có thể đồng bộ danh sách/lịch, hủy hoặc khôi phục, kèm lý do, trước khi hết ngày học. Staff không nhập kết quả thay giáo viên.

### Giáo viên

1. Mở **Điểm danh** để xem các bảng được phân công; dùng bộ lọc ngày để xem lại lịch sử.
2. Mở bảng của tiết học, chọn kết quả từng học sinh và ghi chú khi cần. Nút đánh dấu có mặt chỉ điền các học sinh chưa chọn kết quả.
3. Xác nhận toàn bộ bảng. Muốn sửa trong cùng ngày, mở lại bảng, thay đổi kết quả và ghi lý do.
4. Từ 00:00 ngày kế tiếp, bảng tự chuyển sang chỉ xem; yêu cầu lưu gửi trực tiếp cũng bị từ chối.

### Học sinh và phụ huynh

Mở mục điểm danh để xem kết quả, lọc theo khoảng ngày, học kỳ, môn và trạng thái. Phụ huynh chọn con trong danh sách liên kết. Màn hình không cung cấp bảng điểm danh của học sinh khác.

Dashboard đã dùng dữ liệu điểm danh thực tế; thời khóa biểu có liên kết xem điểm danh theo tuần. Báo cáo và thống kê chuyên cần chỉ tính bảng đã xác nhận, dùng lớp/học kỳ của buổi học. Không có kết quả thì hiển thị chưa có dữ liệu thay vì tự coi là 100% có mặt. Tỷ lệ có mặt tính cả đi muộn.

## Cơ sở dữ liệu và khởi chạy

Migration mới: `20261021065337_AddSlotAttendanceSessions` trong project Repositories. Migration thêm bảng phiên điểm danh, nhật ký thao tác và liên kết các bản ghi cũ với phiên tương ứng.

Migration giữ kết quả cũ; các thông tin người tạo/thời gian không có nguồn xác thực được để trống. Migration dừng để yêu cầu rà soát nếu phát hiện bản ghi trùng, trạng thái/ngày không hợp lệ, giáo viên/học kỳ/thứ/lịch sử lớp không khớp hoặc nhiều tiết chiếm cùng lớp/ngày/slot. Không tự xóa hay đoán lại dữ liệu cũ.

Chạy từ thư mục `HighSchoolManagementSystem`, dùng kết nối `ConnectionStrings:DefaultConnection` của ứng dụng (hoặc biến môi trường `ConnectionStrings__DefaultConnection`):

```powershell
dotnet ef database update --project HighSchoolManagementSystem.Repositories --startup-project HighSchoolManagementSystem.MVC
dotnet run --project HighSchoolManagementSystem.MVC
```

Ứng dụng cũng có cơ chế tự chạy migration khi khởi động qua `DbInitializer`. Factory dành cho công cụ EF chỉ đọc cấu hình, không khởi chạy ứng dụng hoặc seed. Cơ sở dữ liệu ứng dụng thực tế chưa được cập nhật trong quá trình thực hiện chức năng; các lần kiểm thử dùng cơ sở dữ liệu riêng.

## Kiểm thử

Kết quả ngày 21/10/2026: **23/23 bài kiểm thử đạt**, không bỏ qua bài nào; build thành công. EF xác nhận không có thay đổi mô hình chưa được đưa vào migration. Luồng MVC được kiểm tra qua máy chủ thử nghiệm với cookie và biểu mẫu thật; chưa kiểm tra trực quan bằng trình duyệt trên dữ liệu ứng dụng chính.

Chạy từ thư mục solution:

```powershell
dotnet test HighSchoolManagementSystem.Tests/HighSchoolManagementSystem.Tests.csproj --disable-build-servers -m:1
dotnet ef migrations has-pending-model-changes --project HighSchoolManagementSystem.Repositories --startup-project HighSchoolManagementSystem.MVC --no-build
```

Kiểm thử tích hợp mặc định dùng SQL Server LocalDB `(localdb)\MSSQLLocalDB`. Có thể đặt `ATTENDANCE_TEST_SQL` để chọn SQL Server khác có quyền tạo database. Mỗi bài kiểm thử tạo database tên `CodexAttendanceTests_<guid>` riêng rồi xóa đúng database đó; không dùng tên database ứng dụng trong cấu hình.

Bộ kiểm thử bao gồm ranh giới giờ bắt đầu/00:00 theo UTC+7, phân quyền bốn vai trò, chống xem học sinh khác, gửi đủ danh sách, đơn nghỉ, dữ liệu cũ, cập nhật đồng thời, lịch sử chuyển lớp, báo cáo và luồng biểu mẫu MVC thực với cookie đăng nhập/chống giả mạo yêu cầu.
