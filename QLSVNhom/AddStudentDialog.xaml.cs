using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Numerics;
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
    public partial class AddStudentDialog : Window
    {
        private readonly string _connString;
        private readonly string _tendn;
        private readonly string? _mahp;

        public AddStudentDialog(string connString, string tendn)
        {
            InitializeComponent();
            _connString = connString;
            _tendn = tendn;
        }

        private void Update_Click(object sender, RoutedEventArgs e)
        {
            using var cn = new SqlConnection(_connString);
            using var cmd = new SqlCommand("SP_INS_SINHVIEN_LOP", cn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@TENDN", _tendn);
            cmd.Parameters.AddWithValue("@MALOP", txtMALOP.Text);
            cmd.Parameters.AddWithValue("@MASV", txtSV.Text);
            cmd.Parameters.AddWithValue("@HOTEN", txtHOTEN.Text);
            cmd.Parameters.AddWithValue("@DIACHI", txtDIACHI.Text);
            cmd.Parameters.AddWithValue("@TENDNSV", txtTENDNSV.Text);
            cmd.Parameters.AddWithValue("@NGAYSINH", dateNGAYSINH.SelectedDate != null ? dateNGAYSINH.SelectedDate : null);
            cmd.Parameters.AddWithValue("@MK", txtMATKHAUSV.Password);
            var ketqua = new SqlParameter("@KETQUA", SqlDbType.NVarChar, 200) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(ketqua);

            cn.Open();
            try
            {
                cmd.ExecuteNonQuery();
                string? KETQUA = ketqua.Value as string;
                MessageBox.Show(KETQUA);
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Cập nhật điểm thất bại, có lỗi xảy ra ở server.");
            }
            finally
            {
                cn.Close();
            }
            DialogResult = true;
        }
    }
}
