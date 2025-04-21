using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Diagnostics.Eventing.Reader;
using System.Linq.Expressions;
using System.Globalization;


namespace QLSVNhom
{
    public class BoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;

            // Converting back rarely happens, a lot of the converters will throw an exception
            //throw new NotImplementedException();
        }
    }
    public partial class MainWindow : Window
    {
        private readonly string _connString = "Server=.;Database=QLSVNhom;User Id=21120263;Password=21120263;Trusted_Connection=True;TrustServerCertificate=True;";
        public string TENDN { get; private set; }
        public string MANV { get; private set; }
        private DataTable _scores, _classes, _students, _courses;
        public Visibility isManagingClass = Visibility.Visible;
        private string _user;
        private string _pwd;

        public MainWindow()
        {
            var loginDialog = new LoginDialog(_connString);
            if (loginDialog.ShowDialog() == true)
            {
                TENDN = loginDialog.TENDN;
                MANV = loginDialog.MANV;
                _user = loginDialog.TENDN;
                _pwd = loginDialog.MATKHAU;
            }
            else
            {
                Application.Current.Shutdown();
                return;
            }

            InitializeComponent();
            LoadCourses();
            LoadClasses();
            InitScoreGrid();
        }

        #region Helpers
        private SqlConnection NewConn() => new SqlConnection(_connString);

        private DataTable ExecSP(string sp, params SqlParameter[] ps)
        {
            using var cn = NewConn();
            using var cmd = new SqlCommand(sp, cn) { CommandType = CommandType.StoredProcedure };
            if (ps != null) cmd.Parameters.AddRange(ps);
            using var da = new SqlDataAdapter(cmd);
            var dt = new DataTable();
            da.Fill(dt);
            return dt;
        }

        private DataTable? ExecSPMultiDatasetsGetSecondDataset(string sp, params SqlParameter[] ps)
        {
            using var cn = NewConn();
            using var cmd = new SqlCommand(sp, cn) { CommandType = CommandType.StoredProcedure };
            if (ps != null) cmd.Parameters.AddRange(ps);

            cn.Open();
            using var reader = cmd.ExecuteReader();

            // Skip the first result set
            if (reader.NextResult())
            {
                var dt = new DataTable();
                dt.Load(reader);
                return dt;
            }

            return null;
        }

        private void ExecNonQuery(string sp, params SqlParameter[] ps)
        {
            using var cn = NewConn();
            using var cmd = new SqlCommand(sp, cn) { CommandType = CommandType.StoredProcedure };
            if (ps != null) cmd.Parameters.AddRange(ps);
            cn.Open();
            cmd.ExecuteNonQuery();
        }
        #endregion

        #region Scores Tab
        private void InitScoreGrid()
        {
            /*_scores = new DataTable();
            _scores.Columns.Add("MAHP", typeof(string));
            _scores.Columns.Add("MALOP", typeof(string));
            _scores.Columns.Add("MASV", typeof(string));
            _scores.Columns.Add("TENSV", typeof(string));
            _scores.Columns.Add("DIEMSO", typeof(double));*/
            _scores = ExecSPMultiDatasetsGetSecondDataset("SP_SEL_SCORE_ENCRYPTED", new SqlParameter("@TENDN", _user), new SqlParameter("@MK", _pwd));
            
            if (_scores == null)
            {
                MessageBox.Show("Failed to load scores.");
                return;
            }
            dataGridScores.ItemsSource = _scores.DefaultView;
            dataGridScores.Columns[0].Header = "Mã học phần";
            dataGridScores.Columns[1].Header = "Mã lớp";
            dataGridScores.Columns[2].Header = "Mã sinh viên";
            dataGridScores.Columns[3].Header = "Tên sinh viên";
            dataGridScores.Columns[4].Header = "Điểm số";
        }

        private void AddNewRow_Click(object sender, RoutedEventArgs e)
        {
            var addScoreDialog = new AddScoreDialog(_connString, _user);
            if (addScoreDialog.ShowDialog() != true)
            {
                MessageBox.Show("Nhập điểm không thành công.");
                InitScoreGrid();
            }
            else
            {
                InitScoreGrid();
                MessageBox.Show("Nhập điểm thành công.");
            }
        }

        private void SaveScores_Click(object sender, RoutedEventArgs e)
        {
            var drv = (DataRowView)((Button)sender).DataContext;
            string masv = drv["MASV"].ToString();
            string? mahp;
            bool hasMAHPInRowView = drv.Row.Table.Columns.Contains("MAHP");
            if (hasMAHPInRowView && drv["MAHP"] != null && drv["MAHP"].ToString() != ""){
                mahp = drv["MAHP"].ToString();
            }
            else{
                mahp = null;
            }

            var updateScoreDialog = new UpdateScoreDialog(_connString, _user, masv, mahp);
            if (updateScoreDialog.ShowDialog() == true)
            {
                InitScoreGrid();
            }
            else
            {
                MessageBox.Show("Cập nhật điểm không thành công.");
                InitScoreGrid();
            }
        }

        private void DeleteScoreRow_Click(object sender, RoutedEventArgs e)
        {
            var drv = (DataRowView)((Button)sender).DataContext;
            string masv = drv["MASV"].ToString();
            string mahp = drv["MAHP"].ToString();
            using var cn = NewConn();
            using var cmd = new SqlCommand(
                "DELETE FROM BANGDIEM WHERE MASV=@m AND MAHP=@h", cn);
            cmd.Parameters.AddWithValue("@m", masv);
            cmd.Parameters.AddWithValue("@h", mahp);
            cn.Open(); cmd.ExecuteNonQuery();
            _scores.Rows.Remove(drv.Row);
        }
        #endregion

        #region Classes & Students Tab
        private void LoadClasses()
        {
            _classes = ExecSP("SP_SELECT_LOP", new SqlParameter("@TENDN", _user));
            dataGridClasses.ItemsSource = _classes.DefaultView;
        }

        private void DataGridClasses_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dataGridClasses.SelectedItem is DataRowView drv)
            {
                string malop = drv["MALOP"].ToString();
                isManagingClass = ( drv.Row.Table.Columns.Contains("DANGQL") && drv["DANGQL"] != null
                                    && (drv["DANGQL"].ToString().ToLower().Equals("true") || drv["DANGQL"].ToString().ToLower().Equals("1")) ) 
                                    ? Visibility.Visible : Visibility.Collapsed; 

                _students = ExecSP("SP_SEL_SINHVIEN_LOP",
                    new SqlParameter("@TENDN", _user),
                    new SqlParameter("@MALOP", malop));
                dataGridStudents.ItemsSource = _students.DefaultView;
                dataGridStudents.Visibility = Visibility.Visible;
            }
        }

        private void AddClass_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddClassDialog(_connString, _user);
            if (dialog.ShowDialog() == true)
            {
                MessageBox.Show("New class added successfully.");
                LoadClasses();
            }
        }

        private void SaveStudentEdit_Click(object sender, RoutedEventArgs e)
        {
            // Dòng này là để lưu lại chỉnh sửa của người dùng trên dòng thông tin sinh viên trước khi thực hiện cập nhật trên CSDL
            dataGridStudents.CommitEdit(DataGridEditingUnit.Row, true);
            var drv = (DataRowView)((Button)sender).DataContext;
            try
            {
                ExecNonQuery("SP_UPDATE_SINHVIEN_LOP",
                    new SqlParameter("@TENDN", _user),
                    new SqlParameter("@MALOP", drv["MALOP"]),
                    new SqlParameter("@MASV", drv["MASV"]),
                    new SqlParameter("@HOTEN", drv["HOTEN"]),
                    new SqlParameter("@NGAYSINH", drv["NGAYSINH"]),
                    new SqlParameter("@DIACHI", drv["DIACHI"]));
            }catch(SqlException ex)
            {
                MessageBox.Show("Cập nhật sinh viên thất bại, lỗi: " + ex.Message);
                return;
            }
            MessageBox.Show("Cập nhật sinh viên thành công.");
        }

        private void AddStudent_Click(object sender, RoutedEventArgs e)
        {
            var addStudentDialog = new AddStudentDialog(_connString, _user);
            if (addStudentDialog.ShowDialog() == true)
            {
                LoadClasses();
            }
            else
            {
                MessageBox.Show("Thêm sinh viên không thành công.");
            }

        }

        private void UpdateScores_Click(object sender, RoutedEventArgs e)
        {
            SaveScores_Click(sender, e);
        }
        #endregion

        #region Courses Tab
        private void LoadCourses()
        {
            _courses = ExecSP("SP_SEL_HOCPHAN");
            dataGridCourses.ItemsSource = _courses.DefaultView;
        }

        private void UpdateCourse_Click(object sender, RoutedEventArgs e)
        {
            var drv = (DataRowView)((Button)sender).DataContext;
            ExecNonQuery("SP_UPDATE_HOCPHAN",
                new SqlParameter("@MAHP", drv["MAHP"]),
                new SqlParameter("@SOTC", drv["SOTC"]));
            LoadCourses();
        }

        private void DeleteCourse_Click(object sender, RoutedEventArgs e)
        {
            var drv = (DataRowView)((Button)sender).DataContext;
            try
            {
                ExecNonQuery("SP_DEL_HOCPHAN",
                    new SqlParameter("@MAHP", drv["MAHP"]));
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Không thể xóa học phần này vì có bảng điểm của sinh viên hoặc lỗi từ server.");
                return;
            }
            LoadCourses();
        }

        private void AddCourse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddCourseDialog(_connString);
            if (dialog.ShowDialog() == true)
            {
                MessageBox.Show("Học phần thêm thành công.");
                LoadCourses();
            }
        }
        #endregion
    }
}
