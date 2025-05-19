# QLSVNhom (mã hóa Client)

## HCMUS - Môn: Bảo mật CSDL 
- Lab 04: Bảo mật CSDL bên phía client.
- Đây là chương trình đơn giản cho Lab 04 để thực thi các stored procedure bên phía server, tuy nhiên private key được lưu tại client và các hàm băm và mã hóa thực hiện ở Client. Sử dụng framework là WPF (.NET) do vậy chỉ chạy trên Windows.

## Các sinh viên thực hiện

## Cách chạy
- Trong MainWindow.xaml.cs, trong _connString, đổi database thành CSDL muốn truy cập, user_id và password thành tài khoản (SQL Auth) của login có quyền truy cập vào CSDL muốn truy cập.
- Biên dịch lại chương trình và chạy.
