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
    public partial class RegisterDialog
        : Window
    {
        private readonly string _connString;
        private RSAKeyManager _rsaManager;
        public RegisterDialog(string connString, RSAKeyManager rsaManager)
        {
            _connString = connString;
            _rsaManager = rsaManager;
            InitializeComponent();
        }
        private void Add_Click(object sender, RoutedEventArgs e)
        {
            var manv = txtMANV.Text;
            var hoten = txtHOTEN.Text;
            var email = txtEMAIL.Text;
            var luongStr = txtLUONGCB.Text;
            int luong;
            var tendn = txtTENDN.Text;
            var mk = txtMATKHAU.Password;

            try
            {
                luong = int.Parse(luongStr);
            }catch(Exception ex)
            {
                MessageBox.Show("Lương không hợp lệ. Vui lòng nhập lại.");
                DialogResult = false;
                return;
            }
            if (string.IsNullOrEmpty(manv) || string.IsNullOrEmpty(hoten) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(tendn) || string.IsNullOrEmpty(mk))
            {
                MessageBox.Show("Vui lòng điền đầy đủ thông tin.");
                DialogResult = false;
                return;
            }
            (string publicKey, string privateKey) = RsaHelper.GenerateRsaKeys();
            _rsaManager.SaveKeys(manv, publicKey, privateKey);
            RSAServiceProvider _rsaProvider = _rsaManager.getKeyProvider(manv, publicKey);
            var luongEncrypted = _rsaProvider.Encrypt<int>(luong);
            //Băm mật khẩu
            using var sha1 = System.Security.Cryptography.SHA1.Create();
            var mkHash = sha1.ComputeHash(Encoding.UTF8.GetBytes(mk));

            using var cn = new SqlConnection(_connString);
            using var cmd = new SqlCommand("SP_INS_PUBLIC_ENCRYPT_NHANVIEN", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@TENDN", tendn);
            cmd.Parameters.AddWithValue("@MK", mkHash);
            cmd.Parameters.AddWithValue("@MANV", manv);
            cmd.Parameters.AddWithValue("@HOTEN", hoten);
            cmd.Parameters.AddWithValue("@EMAIL", email);
            cmd.Parameters.AddWithValue("@LUONGCB", luongEncrypted);
            cmd.Parameters.AddWithValue("@PUB", publicKey);

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
