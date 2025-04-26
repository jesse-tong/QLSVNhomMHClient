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
    public partial class UpdateScoreDialog : Window
    {
        private readonly string _connString;
        private readonly string _tendn;
        private readonly string _masv;
        private readonly string? _mahp;

        public UpdateScoreDialog(string connString, string tendn, string masv, string? mahp = null)
        {
            InitializeComponent();
            _connString = connString;
            _tendn      = tendn;
            _masv       = masv;
            _mahp       = mahp;
            txtMAHP.Text = mahp != null ? mahp : string.Empty;
            // e.g. txtMASV.Text = _masv;
        }

        private void Update_Click(object sender, RoutedEventArgs e)
        {
            using var cn  = new SqlConnection(_connString);
            using var cmd = new SqlCommand("SP_INS_SCORE_ENCRYPTED", cn)
            {
                CommandType = CommandType.StoredProcedure
            };
            try
            {
                cmd.Parameters.AddWithValue("@TENDN", _tendn);
                cmd.Parameters.AddWithValue("@MASV", _masv);
                cmd.Parameters.AddWithValue("@MAHP", txtMAHP.Text);
                cmd.Parameters.AddWithValue("@DIEMTHI", double.Parse(txtDIEMSO.Text));
            }catch (Exception ex)
            {
                MessageBox.Show("Điểm thi nhập vào không hợp lệ.");
            }

            cn.Open();
            try
            {
                cmd.ExecuteNonQuery();
                MessageBox.Show("Cập nhật điểm thành công.");
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Cập nhật điểm thất bại, có lỗi: " + ex.Message);
            }
            finally
            {
                cn.Close();
            }
            DialogResult = true;
        }
    }
}

