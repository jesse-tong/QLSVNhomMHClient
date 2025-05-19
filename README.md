# QLSVNhom (mã hóa Client)

## HCMUS - Môn: Bảo mật CSDL 
- Lab 04: Bảo mật CSDL bên phía client.
- Đây là chương trình đơn giản cho Lab 04 để thực thi các stored procedure bên phía server, tuy nhiên private key được lưu tại client và các hàm băm và mã hóa thực hiện ở Client. Sử dụng framework là WPF (.NET) do vậy chỉ chạy trên Windows.

## Các sinh viên thực hiện
| MSSV     | Họ và tên              | Email sinh viên               |
|----------|------------------------|-------------------------------|
| 22120211 | Quách Ngọc Minh        | 22120211@student.hcmus.edu.vn |
| 21120469 | Nguyễn Phúc Huy        | 21120469@student.hcmus.edu.vn |
| 21120546 | Nguyễn Thanh Sang      | 21120546@student.hcmus.edu.vn |
| 21120263 | Tống Nguyễn Minh Khang | 21120263@student.hcmus.edu.vn |

## Cách chạy
- Cài đặt các package cần thiết.
- Trong MainWindow.xaml.cs, trong _connString, đổi database thành CSDL muốn truy cập, user_id và password thành tài khoản (SQL Auth) của login có quyền truy cập vào CSDL muốn truy cập.
- Chạy các file SQL trong thư mục con Lab04 (lưu ý là nếu đã có cơ sở dữ liệu QLSVNhom ở Lab03, cần phải xóa CSDL và tạo mới và chạy đoạn script, hoặc tạo CSDL mới rồi đổi connection string).
- Nếu như mà biên dịch lại từ Debug -> Release hoặc ngược lại, nếu không muốn reset lại CSDL, thì đưa privateKeys.db từ thư mục chương trình đã biên dịch (với Debug là {Thư mục chứa project} -> bin -> Debug -> net{version}-{platform} (ví dụ net8.0-windows), Release là {Thư mục chứa project} -> bin -> Release -> net{version}-{platform} (ví dụ net8.0-windows)) sang thư mục mới.
- Biên dịch lại chương trình và chạy.
- Nếu chạy lần đầu, ở màn hình đăng nhập chọn "Đăng ký tài khoản nhân viên" chứ không thực hiện các stored procedure ở server do client cần thực hiện tạo và lưu cặp khóa RSA.

## Các package cần để biên dịch và chạy chương trình (NuGet)
- BouncyCastle.Cryptography
- Microsoft.Data.SqlClient (cần cho thực hiện các truy vấn trên SQL Server)
- Microsoft.Data.Sqlite (cần để lưu và truy vấn private key từ client, lưu và mã hóa ở client).
