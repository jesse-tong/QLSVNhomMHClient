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
using Microsoft.Data.SqlClient;
using System.Data;
using System.Linq.Expressions;

namespace QLSVNhom
{
    public partial class AddCourseDialog : Window
    {
        private readonly string _connString;

        public AddCourseDialog(string connString)
        {
            InitializeComponent();
            _connString = connString;
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            using var cn  = new SqlConnection(_connString);
            using var cmd = new SqlCommand("SP_INS_HOCPHAN", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@MAHP",  txtMAHP.Text);
            cmd.Parameters.AddWithValue("@TENHP", txtTENHP.Text);
            cmd.Parameters.AddWithValue("@SOTC",  int.Parse(txtSOTC.Text));

            cn.Open();
            try
            {
                cmd.ExecuteNonQuery();
                cn.Close();
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Có lỗi xảy ra khi thêm học phần: " +  ex.Message);
                DialogResult = false;
                cn.Close();
                return;
            }
            MessageBox.Show("Thêm học phần thành công.");
            DialogResult = true;
        }
    }
}
