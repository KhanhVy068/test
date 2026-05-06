Ý nghĩa các thư mục


Thư mục chính: SyncChain.API

Đây là phần backend chính của hệ thống.

```Controllers/```

Chứa các API endpoint.

Mỗi controller xử lý một nhóm chức năng.

- AdminController.cs

Quản lý chức năng admin:

tạo tài khoản nội bộ
reset mật khẩu
quản lý người dùng
AuthController.cs

Xử lý xác thực:

đăng ký
đăng nhập
authentication
OrderController.cs

Quản lý đơn hàng:

tạo đơn hàng
cập nhật trạng thái
xem danh sách đơn

- ProductController.cs

Quản lý sản phẩm:

thêm sản phẩm
cập nhật sản phẩm
import kho
xem sản phẩm
- ReportController.cs

Xuất báo cáo và thống kê:

doanh thu
tồn kho
số lượng đơn hàng
```Data/```

Chứa lớp kết nối database.

- AppDbContext.cs

DbContext chính của Entity Framework Core.

Khai báo:

DbSet<SanPham>
DbSet<DonHang>
DbSet<NguoiDung>

Quản lý kết nối giữa model và database.

```DTOs/```

DTO = Data Transfer Object.

Dùng để:

nhận dữ liệu từ client
trả dữ liệu về client
tránh expose trực tiếp model database
DTOs/Admin/
- ResetPasswordDTO.cs

Dữ liệu reset mật khẩu.

Ví dụ:

{
  "newPassword": "123456"
}
UpdateInternalUserDTO.cs

Dữ liệu cập nhật người dùng nội bộ.

DTOs/Product/
- CreateProductDTO.cs

Dữ liệu thêm sản phẩm mới.

- ImportStockDTO.cs

Dữ liệu nhập kho.

Ví dụ:

{
  "productId": 1,
  "quantity": 100
}
UpdateProductDTO.cs

Dữ liệu cập nhật sản phẩm.

DTO khác
- CreateInternalUserDTO.cs

Tạo tài khoản nhân viên/admin.

- CreateOrderDTO.cs

Tạo đơn hàng mới.

- LoginDTO.cs

Dữ liệu đăng nhập.

- OrderItemDTO.cs

Thông tin từng sản phẩm trong đơn hàng.

- RegisterDTO.cs

Dữ liệu đăng ký tài khoản.

```Migrations/```

Chứa migration của Entity Framework Core.

Dùng để:

tạo bảng
cập nhật schema database
- 20260428132426_AddTrangThaiToDonHang.cs

Migration thêm trạng thái cho đơn hàng.

Ví dụ:

TrangThai
- AppDbContextModelSnapshot.cs

Snapshot schema hiện tại của database.

EF Core dùng file này để so sánh thay đổi.

```Models/```

Chứa entity/model ánh xạ với database.

- ChiTietDonHang.cs

Model chi tiết đơn hàng.

Quan hệ:

đơn hàng
sản phẩm
số lượng
- DonHang.cs

Model đơn hàng.

Thông tin:

khách hàng
ngày tạo
trạng thái
- GiaoDichKho.cs

Lưu lịch sử nhập/xuất kho.

- NguoiDung.cs

Model người dùng.

Thông tin:

username
password hash
role
- PhanQuyen.cs

Model phân quyền người dùng.

Ví dụ:

Admin
Staff
Warehouse
- SanPham.cs

Model sản phẩm.

Thông tin:

tên sản phẩm
giá
số lượng tồn kho
```Services/```

Chứa business logic của hệ thống.

Ví dụ:

xử lý JWT
xử lý nghiệp vụ đơn hàng
validate dữ liệu
tính toán báo cáo

## Cách chạy 
- build:
```dotnet build app\SyncChain.Desktop\SyncChain.Desktop.csproj```
- run: 
```dotnet run --project app\SyncChain.Desktop\SyncChain.Desktop.csproj```
- backend:
```dotnet run --project SyncChain.API\SyncChain.API.csproj```