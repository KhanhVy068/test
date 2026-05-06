# NT106_SyncChain

SyncChain là ứng dụng quản lý bán hàng và tồn kho gồm:

- Backend API viết bằng ASP.NET Core.
- Ứng dụng desktop viết bằng .NET MAUI.
- Cơ sở dữ liệu SQLite lưu trong thư mục `database`.
- Swagger/Postman để kiểm thử API.

## Chức năng chính

- Đăng ký, đăng nhập và xác thực bằng JWT.
- Phân quyền người dùng theo vai trò: `customer`, `staff`, `manager`, `admin`.
- Quản lý sản phẩm, giá bán, giá nhập, hình ảnh, tồn kho và trạng thái bán hàng.
- Tạo đơn hàng, xem chi tiết đơn hàng và cập nhật trạng thái xử lý.
- Nhập kho và xem lịch sử giao dịch kho.
- Dashboard báo cáo doanh thu, đơn hàng, sản phẩm bán chạy và cảnh báo tồn kho thấp.
- Quản trị tài khoản nội bộ dành cho admin.

## Yêu cầu môi trường

- Windows 10 trở lên.
- .NET SDK có hỗ trợ:
  - `net10.0` cho `SyncChain.API`.
  - `net9.0-windows10.0.19041.0` và workload MAUI cho `SyncChain.Desktop`.
- Visual Studio 2022 hoặc JetBrains Rider/VS Code có hỗ trợ .NET MAUI nếu muốn chạy app desktop bằng IDE.
- Node.js chỉ cần nếu muốn chạy thử phần script mẫu trong `src`.

Cài MAUI workload nếu máy chưa có:

```powershell
dotnet workload install maui
```

## Cách chạy dự án

### 1. Clone hoặc mở thư mục dự án

```powershell
cd NT106_SyncChain
```

### 2. Restore package .NET

```powershell
dotnet restore NT106_SyncChain.sln
```

### 3. Chạy Backend API

```powershell
dotnet run --project SyncChain.API\SyncChain.API.csproj
```

API mặc định chạy tại:

- `http://localhost:5292`
- Swagger: `http://localhost:5292/swagger`

Khi API khởi động, chương trình sẽ tự đảm bảo database tồn tại, bổ sung một số cột/bảng còn thiếu và seed các vai trò mặc định.

Tài khoản admin mặc định:

```text
Email: admin@gmail.com
Password: 123456
```

### 4. Chạy ứng dụng Desktop

Mở terminal khác và chạy:

```powershell
dotnet run --project app\SyncChain.Desktop\SyncChain.Desktop.csproj
```

Lưu ý: app desktop đang gọi API tại `http://localhost:5292/`, vì vậy cần chạy API trước khi đăng nhập hoặc sử dụng dữ liệu thật.

## Cấu trúc thư mục

```text
NT106_SyncChain/
├── app/
│   └── SyncChain.Desktop/
├── database/
├── postman/
├── .postman/
├── src/
├── SyncChain.API/
├── ui/
├── NT106_SyncChain.sln
├── package.json
├── package-lock.json
├── index.json
├── LICENSE
└── README.md
```

### `SyncChain.API/`

Backend chính của hệ thống, viết bằng ASP.NET Core Web API.

- `Program.cs`: cấu hình service, SQLite, JWT, Swagger, phân quyền và seed dữ liệu ban đầu.
- `Controllers/`: chứa các API controller như đăng nhập, sản phẩm, đơn hàng, báo cáo, admin.
- `Services/`: chứa logic nghiệp vụ như xác thực, sản phẩm, đơn hàng.
- `Models/`: định nghĩa entity ánh xạ với bảng database.
- `DTOs/`: định nghĩa dữ liệu request/response cho API.
- `Data/AppDbContext.cs`: DbContext của Entity Framework Core.
- `Migrations/`: migration của Entity Framework Core.
- `wwwroot/uploads/products/`: nơi lưu ảnh sản phẩm được upload.
- `appsettings.json`: cấu hình logging và JWT.
- `Properties/launchSettings.json`: cấu hình URL chạy local.

### `app/SyncChain.Desktop/`

Ứng dụng giao diện desktop viết bằng .NET MAUI.

- `App.xaml`, `AppShell.xaml`: cấu hình app và điều hướng chính.
- `Views/Pages/`: các màn hình giao diện như đăng nhập, dashboard, sản phẩm, đơn hàng, nhập kho, quản trị người dùng.
- `Services/SyncChainApiClient.cs`: client gọi API backend.
- `Models/AppModels.cs`: model dùng cho giao diện.
- `Converters/`: converter phục vụ binding trong XAML.
- `Resources/`: font, ảnh, icon, splash screen, style và màu sắc.
- `Platforms/`: cấu hình riêng cho từng nền tảng MAUI như Windows, Android, iOS, MacCatalyst.

### `database/`

Chứa dữ liệu SQLite và script tạo bảng.

- `SyncChain.db`: database chính của ứng dụng.
- `SyncChain.db-wal`, `SyncChain.db-shm`: file phụ của SQLite khi dùng WAL mode.
- `TaoBang.sql`: script tạo bảng ban đầu.

Không nên xóa `SyncChain.db` nếu muốn giữ dữ liệu hiện tại. Nếu cần reset dữ liệu, dừng API trước, sao lưu database, rồi tạo lại database mới.

### `postman/` và `.postman/`

Chứa cấu hình dùng cho Postman để kiểm thử API. Có thể import collection/environment nếu cần gọi API thủ công.

### `src/`

Chứa script Node.js thử nghiệm với SQLite, gồm các hàm mẫu về thêm sản phẩm và đặt hàng bằng transaction. Đây không phải backend chính của app desktop.

Nếu muốn thử script Node.js:

```powershell
npm install
node src\index.js
```

### `ui/`

Thư mục dành cho tài nguyên hoặc thử nghiệm giao diện. Trong phiên bản hiện tại, app desktop chính nằm trong `app/SyncChain.Desktop`.

### File ở thư mục gốc

- `NT106_SyncChain.sln`: solution chính để mở toàn bộ dự án bằng Visual Studio.
- `package.json`, `package-lock.json`: package Node.js phục vụ script thử nghiệm trong `src`.
- `index.json`: file dữ liệu/cấu hình phụ của dự án.
- `LICENSE`: giấy phép dự án.
- `.gitignore`: danh sách file/thư mục không đưa vào Git.

## Phân quyền

Hệ thống có 4 vai trò chính:

- `customer`: tài khoản khách hàng, có thể xem sản phẩm và tạo đơn hàng.
- `staff`: nhân viên, có thể quản lý/xử lý đơn hàng.
- `manager`: quản lý, có thể quản lý sản phẩm và đơn hàng.
- `admin`: quản trị viên, có toàn quyền, bao gồm quản lý tài khoản nội bộ.

## Một số API thường dùng

Các endpoint có thể xem đầy đủ trong Swagger tại `http://localhost:5292/swagger`.

- `POST /api/Auth/register`: đăng ký khách hàng.
- `POST /api/Auth/login`: đăng nhập và nhận JWT.
- `GET /api/Auth/profile`: xem hồ sơ người dùng đang đăng nhập.
- `GET /api/Product`: lấy danh sách sản phẩm.
- `POST /api/Product`: tạo sản phẩm mới.
- `POST /api/Product/upload-image`: upload ảnh sản phẩm.
- `GET /api/Order`: lấy danh sách đơn hàng của người dùng.
- `POST /api/Order`: tạo đơn hàng.
- `GET /api/Report/dashboard`: lấy dữ liệu dashboard.
- `GET /api/admin/users`: lấy danh sách tài khoản nội bộ.

Với các API yêu cầu đăng nhập, thêm header:

```text
Authorization: Bearer <token>
```

## Ghi chú khi phát triển

- API dùng SQLite với đường dẫn `../database/SyncChain.db` tính từ thư mục chạy của project API.
- Ứng dụng desktop đang hard-code API base URL trong `app/SyncChain.Desktop/Services/SyncChainApiClient.cs`.
- Ảnh sản phẩm upload được lưu trong `SyncChain.API/wwwroot/uploads/products`.
- Nếu thay đổi schema database, nên cập nhật cả migration và cân nhắc dữ liệu cũ trong `database/SyncChain.db`.
- Nếu chạy app desktop không kết nối được backend, kiểm tra API đã chạy ở `http://localhost:5292` chưa.

## Build kiểm tra

Build API:

```powershell
dotnet build SyncChain.API\SyncChain.API.csproj
```

Build desktop:

```powershell
dotnet build app\SyncChain.Desktop\SyncChain.Desktop.csproj
```

Build toàn solution:

```powershell
dotnet build NT106_SyncChain.sln
```
