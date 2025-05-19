using System;
using System.Collections.Generic;
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
using System;
using System.Data;
using Microsoft.Data.SqlClient;
using QLSVNhom.Controller.KeyManager;

namespace QLSVNhom
{
    public partial class LoginDialog : Window
    {
        private readonly string _connString;
        private readonly RSAKeyManager _rsaKeyManager;
        public string TENDN { get; private set; }
        public string MATKHAU { get; private set; }
        public string MANV { get; private set; }
        public string HOTEN { get; private set; }
        public string PUBKEY { get; private set; }

        public LoginDialog(string connString, RSAKeyManager rsaKeyManager)
        {
            _connString = connString;
            _rsaKeyManager = rsaKeyManager;
            InitializeComponent();
        }
        private void Register_Click(object sender, RoutedEventArgs e)
        {
            RegisterDialog registerDialog = new RegisterDialog(_connString, _rsaKeyManager);
            registerDialog.Owner = this;

            if (registerDialog.ShowDialog() == true)
            {
                MessageBox.Show("Đăng ký thành công. Vui lòng đăng nhập.");
            }
        }
        private void Login_Click(object sender, RoutedEventArgs e)
        {
            var tendn = txtTENDN.Text;
            var mk = txtMATKHAU.Password;

            using var sha1 = System.Security.Cryptography.SHA1.Create();
            var mkHash = sha1.ComputeHash(Encoding.UTF8.GetBytes(mk));

            using var cn = new SqlConnection(_connString);
            using var cmd = new SqlCommand("SP_LOGIN_ENCRYPTED", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@TENDN", tendn);
            cmd.Parameters.AddWithValue("@MK", mkHash);

            var pManv = new SqlParameter("@MANV", SqlDbType.VarChar, 20) { Direction = ParameterDirection.Output };
            var pHoten = new SqlParameter("@HOTEN", SqlDbType.NVarChar, 100) { Direction = ParameterDirection.Output };
            var pubKey = new SqlParameter("@PUB", SqlDbType.VarChar, -1) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(pManv);
            cmd.Parameters.Add(pHoten);
            cmd.Parameters.Add(pubKey);

            cn.Open();
            try
            {
                cmd.ExecuteNonQuery();
            }catch(SqlException ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }

            MANV = pManv.Value as string;
            HOTEN = pHoten.Value as string;
            PUBKEY = pubKey.Value as string;

            if (!string.IsNullOrEmpty(MANV))
            {
                TENDN = tendn;
                MATKHAU = mk;
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("Đăng nhập thất bại. Vui lòng kiểm tra lại tên đăng nhập và mật khẩu.");
                DialogResult = false;
                return;
            }
        }
    }
}
