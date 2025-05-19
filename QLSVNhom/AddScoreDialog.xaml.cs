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
using QLSVNhom.Controller.KeyManager;

namespace QLSVNhom
{
    public partial class AddScoreDialog : Window
    {
        private readonly string _connString;
        private readonly string _tendn;
        private readonly RSAServiceProvider _provider;

        public AddScoreDialog(string connString, string tendn, RSAServiceProvider provider)
        {
            InitializeComponent();
            _connString = connString;
            _tendn      = tendn;
            _provider = provider;
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            using var cn  = new SqlConnection(_connString);
            using var cmd = new SqlCommand("SP_INS_SCORE_ENCRYPTED", cn)
            {
                CommandType = CommandType.StoredProcedure
            };

            try
            {
                cmd.Parameters.AddWithValue("@TENDN", _tendn);
                cmd.Parameters.AddWithValue("@MASV", txtMASV.Text);
                cmd.Parameters.AddWithValue("@MAHP", txtMAHP.Text);
                cmd.Parameters.AddWithValue("@DIEMTHI", _provider.Encrypt<double>(double.Parse(txtDIEMSO.Text)));
            }catch (Exception ex)
            {
                MessageBox.Show("Điểm thi, mã sinh viên hoặc mã học phần không hợp lệ. Vui lòng kiểm tra lại. ");
                DialogResult = false;
                return;
            }

            try
            {
                cn.Open();
                cmd.ExecuteNonQuery();
                cn.Close();
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Có lỗi xảy ra khi thêm điểm: " + ex.Message);
                DialogResult = false;
                cn.Close();
                return;
            }
            DialogResult = true;
        }
    }
}
