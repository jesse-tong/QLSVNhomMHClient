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

namespace QLSVNhom
{
    public partial class LoginDialog : Window
    {
        private readonly string _connString;
        public string TENDN { get; private set; }
        public string MATKHAU { get; private set; }
        public string MANV { get; private set; }
        public string HOTEN { get; private set; }

        public LoginDialog(string connString)
        {
            _connString = connString;
            InitializeComponent();
        }
        private void Login_Click(object sender, RoutedEventArgs e)
        {
            var tendn = txtTENDN.Text;
            var mk = txtMATKHAU.Password;
            using var cn = new SqlConnection(_connString);
            using var cmd = new SqlCommand("SP_LOGIN", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@TENDN", tendn);
            cmd.Parameters.AddWithValue("@MK", mk);

            var pManv = new SqlParameter("@MANV", SqlDbType.VarChar, 20) { Direction = ParameterDirection.Output };
            var pHoten = new SqlParameter("@HOTEN", SqlDbType.NVarChar, 100) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(pManv);
            cmd.Parameters.Add(pHoten);

            cn.Open();
            cmd.ExecuteNonQuery();

            MANV = pManv.Value as string;
            HOTEN = pHoten.Value as string;
      
            if (!string.IsNullOrEmpty(MANV))
            {
                TENDN = tendn;
                MATKHAU = mk;
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("Đăng nhập thất bại. Vui lòng kiểm tra lại tên đăng nhập và mật khẩu.");
            }
        }
    }
}
