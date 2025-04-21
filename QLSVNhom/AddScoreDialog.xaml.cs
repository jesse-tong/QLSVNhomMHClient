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
using Microsoft.Data.SqlClient;

namespace QLSVNhom
{
    public partial class AddScoreDialog : Window
    {
        private readonly string _connString;
        private readonly string _tendn;

        public AddScoreDialog(string connString, string tendn)
        {
            InitializeComponent();
            _connString = connString;
            _tendn      = tendn;
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            using var cn  = new SqlConnection(_connString);
            using var cmd = new SqlCommand("SP_INS_SCORE_ENCRYPTED", cn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@TENDN",   _tendn);
            cmd.Parameters.AddWithValue("@MASV",    txtMASV.Text);
            cmd.Parameters.AddWithValue("@MAHP",    txtMAHP.Text);
            cmd.Parameters.AddWithValue("@DIEMTHI", double.Parse(txtDIEMSO.Text));

            cn.Open();
            cmd.ExecuteNonQuery();
            MessageBox.Show("Nhập điểm thành công.");
            DialogResult = true;
        }
    }
}
