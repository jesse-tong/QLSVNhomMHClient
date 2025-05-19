using Microsoft.Data.SqlClient;
using QLSVNhom.Controller.KeyManager;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace QLSVNhom
{
    /// <summary>
    /// Interaction logic for AddEmployee.xaml
    /// </summary>
    public partial class AddEmployee : Window
    {
        private readonly string _connString;
        private RSAKeyManager _rsaManager;
        private RSAServiceProvider _rsaProvider;
        private string? _manv = null;
        public AddEmployee(string connString, RSAKeyManager rsaManager, RSAServiceProvider rsaProvider, string? manv = null)
        {
            _connString = connString;
            _rsaManager = rsaManager;
            _rsaProvider = rsaProvider;
            _manv = manv;
            InitializeComponent();
            if (manv != null)
            {
                // Nếu có manv thì đây là update, mã nhân viên đã được truyền vào
                // Đồng thời không hiển thị input LUONGCB (do tài khoản có thể không có private key của tài khoản cần cập nhật)
                txtMANV.Text = manv;
                txtMANV.IsEnabled = false;
                AddEmployee_TENDN.Text = "Tên đăng nhập mới: ";
                AddEmployee_MATKHAU.Text = "Mật khẩu mới: ";
                AddEmployee_LUONGCB.Visibility = Visibility.Collapsed;
                AddEmployee_LUONGCB.IsEnabled = false;
                txtLUONGCB.Visibility = Visibility.Collapsed;
                txtLUONGCB.IsEnabled = false;

                //Đổi title thành "Cập nhật nhân viên"
                Title = "Cập nhật nhân viên";
            }
            else
            {
                txtMANV.Text = string.Empty;
            }
            
        }
        private void Add_Click(object sender, RoutedEventArgs e)
        {
            // Nếu có manv thì đây là update
            var manv = _manv != null ? _manv: txtMANV.Text;
            var hoten = txtHOTEN.Text;
            var email = txtEMAIL.Text;
            var luongStr = txtLUONGCB.Text;
            int? luong = null;
            var tendn = txtTENDN.Text;
            var mk = txtMATKHAU.Password;

            if (_manv == null)
            {
                try
                {
                    luong = int.Parse(luongStr); //Nếu không có manv thì không cần phải parse lương
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lương không hợp lệ. Vui lòng nhập lại.");
                    DialogResult = false;

                    return;
                }
            }
            
            if (string.IsNullOrEmpty(manv) || string.IsNullOrEmpty(hoten) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(tendn) || string.IsNullOrEmpty(mk))
            {
                MessageBox.Show("Vui lòng điền đầy đủ thông tin.");
                DialogResult = false;
                return;
            }

            using var sha1 = System.Security.Cryptography.SHA1.Create();
            var mkHash = sha1.ComputeHash(Encoding.UTF8.GetBytes(mk));
            using var cn = new SqlConnection(_connString);
            SqlCommand cmd;

            if (_manv == null)
            {
                //Nếu không truyền mã nhân viên trước, thì sẽ thêm nhân viên
                (string publicKey, string privateKey) = RsaHelper.GenerateRsaKeys();
                _rsaManager.SaveKeys(manv, publicKey, privateKey);

                cmd = new SqlCommand("SP_INS_PUBLIC_ENCRYPT_NHANVIEN", cn) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@TENDN", tendn);
                cmd.Parameters.AddWithValue("@MK", mkHash);
                cmd.Parameters.AddWithValue("@MANV", manv);
                cmd.Parameters.AddWithValue("@HOTEN", hoten);
                cmd.Parameters.AddWithValue("@EMAIL", email);
                cmd.Parameters.AddWithValue("@LUONGCB", _rsaProvider.Encrypt<int>((int)luong));
                cmd.Parameters.AddWithValue("@PUB", publicKey);
            }else
            {
                cmd = new SqlCommand("SP_UPDATE_PUBLIC_ENCRYPT_NHANVIEN", cn) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@TENDN", tendn);
                cmd.Parameters.AddWithValue("@MK", mkHash);
                cmd.Parameters.AddWithValue("@MANV", manv);
                cmd.Parameters.AddWithValue("@HOTEN", hoten);
                cmd.Parameters.AddWithValue("@EMAIL", email);
            }

            cn.Open();
            try
            {
                cmd.ExecuteNonQuery();
                DialogResult = true;

            }
            catch(SqlException ex)
            {
                MessageBox.Show(ex.Message);
                DialogResult = false;
            }
        }
    }
}
