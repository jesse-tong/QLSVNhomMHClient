using Microsoft.Data.SqlClient;
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
    public partial class AddClassDialog : Window
    {
        private readonly string _connString;
        private readonly string _tendn;

        public AddClassDialog(string connString, string tendn)
        {
            InitializeComponent();
            _connString = connString;
            _tendn      = tendn;
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            using var cn  = new SqlConnection(_connString);
            using var cmd = new SqlCommand("SP_INS_LOP", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@MALOP", txtMALOP.Text);
            cmd.Parameters.AddWithValue("@TENLOP", txtTENLOP.Text);
            cmd.Parameters.AddWithValue("@TENDN",  _tendn);

            cn.Open();
            cmd.ExecuteNonQuery();
            MessageBox.Show("Thêm lớp thành công.");
            DialogResult = true;
        }
    }
}
