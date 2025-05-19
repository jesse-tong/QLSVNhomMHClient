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

using QLSVNhom.Controller.KeyManager;
using Microsoft.Data.Sqlite;
using System.Security.Cryptography;
using System.Data.SqlTypes;
using Org.BouncyCastle.Asn1.X509.Qualified;


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
        }
    }
    public partial class MainWindow : Window
    {
        private readonly string _connString = "Server=.;Database=QLSVNhom;User Id=21120263;Password=21120263;TrustServerCertificate=True;";
        public string TENDN { get; private set; }
        public string MANV { get; private set; }
        public string PUBKEY { get; private set; }
        private DataTable _scores, _classes, _students, _courses, _employees;
        public Visibility isManagingClass = Visibility.Visible;

        public RSAKeyManager _rsaKeyClient;
        public RSAServiceProvider _rsaServiceProvider;

        private string? _mahp = null;
        private string _user;
        private string _pwd;

        public MainWindow()
        {
            _rsaKeyClient = new RSAKeyManager();

            var loginDialog = new LoginDialog(_connString, _rsaKeyClient);
            if (loginDialog.ShowDialog() == true)
            {
                TENDN = loginDialog.TENDN;
                MANV = loginDialog.MANV;
                PUBKEY = loginDialog.PUBKEY;
                _user = loginDialog.TENDN;
                _pwd = loginDialog.MATKHAU;

                try
                {
                    _rsaServiceProvider = _rsaKeyClient.getKeyProvider(MANV, PUBKEY);
                }
                catch(SqliteException ex)
                {
                    (string pubKey, string privKey) = RsaHelper.GenerateRsaKeys();
                    _rsaKeyClient.SaveKeys(MANV, pubKey, privKey);
                    MessageBox.Show("Lưu khóa thành công.");
                }
            }
            else
            {
                Application.Current.Shutdown();
                return;
            }

            InitializeComponent();
            LoadEmployees();
            LoadCourses();
            LoadClasses();
            InitScoreGrid(_mahp);
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

        private DataTable DecryptDataTable(DataTable dataTable, string columnName, int convertToNumber = 0)
        {
            // Copy the data table to a new one
            DataTable dt = new DataTable();
            if (convertToNumber == 1)
            {
                dt.Columns.Add(columnName, typeof(int));
            }else if (convertToNumber == 2)
            {
                dt.Columns.Add(columnName, typeof(float));
            }else
            {
                dt.Columns.Add(columnName, typeof(string));
            }
            
            for (int i = 0; i < dataTable.Rows.Count; i++)
            {
                var row = dataTable.Rows[i];
                byte[] encryptedData;

                if (row[columnName].GetType() == typeof(SqlBytes))
                {
                    encryptedData = ((SqlBytes)row[columnName]).Value;
                }
                else if (row[columnName].GetType() == typeof(SqlBinary))
                {
                    encryptedData = ((SqlBinary)row[columnName]).Value;
                }
                else if (row[columnName].GetType() == typeof(string))
                {
                    encryptedData = Convert.FromBase64String((string)row[columnName]);
                }
                else
                {
                    encryptedData = (byte[])row[columnName];
                }


                var decryptedData =  _rsaServiceProvider.Decrypt<string>(encryptedData);
                if (convertToNumber == 1)
                {
                    dt.Rows.Add([int.Parse(decryptedData)]);
                }
                else if (convertToNumber == 2)
                {
                    dt.Rows.Add([float.Parse(decryptedData)]);
                }else
                {
                    dt.Rows.Add([decryptedData]);
                }
            }
            dataTable.Columns.Remove(columnName);
            dataTable.Columns.Add(columnName, typeof(string));
            for (int i = 0; i < dataTable.Rows.Count; i++)
            {
                dataTable.Rows[i][columnName] = dt.Rows[i][columnName].ToString();
            }
            return dataTable;
        }

        #endregion

        #region Employees Tab
        private void LoadEmployees()
        {
            using var cn = NewConn();
            cn.Open();
            var cmd = new SqlCommand("SP_SEL_PUBLIC_ALL_NHANVIEN", cn);
            using var reader = cmd.ExecuteReader();
            _employees = new DataTable();
            _employees.Columns.Add("LUONG", typeof(int));
            _employees.Load(reader);

            using var sha1 = System.Security.Cryptography.SHA1.Create();
            byte[] mkHash = sha1.ComputeHash(Encoding.UTF8.GetBytes(_pwd));


            var mkParam = new SqlParameter("@MK", SqlDbType.VarBinary, -1) { Value = mkHash };
            var employeeDataAndSalary = ExecSP("SP_SEL_PUBLIC_ENCRYPT_NHANVIEN", new SqlParameter("@TENDN", _user), mkParam);

            for (int i = 0; i < _employees.Rows.Count; i++)
            {
                var row = _employees.Rows[i];
                if (row["MANV"].ToString() == employeeDataAndSalary.Rows[0]["MANV"].ToString())
                {
                    var salary = employeeDataAndSalary.Rows[0]["LUONG"];
                    int salaryInt = _rsaServiceProvider.Decrypt<int>((byte[])salary);
                    _employees.Rows[i]["LUONG"] = salaryInt;
                }
                else
                {
                    row["LUONG"] = DBNull.Value;
                }
            }

            dataGridEmployees.ItemsSource = _employees.DefaultView;
        }

        private void AddNewEmployee_Click(object sender, RoutedEventArgs e)
        {
            var addEmployeeDialog = new AddEmployee(_connString, _rsaKeyClient, _rsaServiceProvider);
            if (addEmployeeDialog.ShowDialog() == true)
            {
                MessageBox.Show("Thêm nhân viên thành công.");
                LoadEmployees();
            }
            else
            {
                MessageBox.Show("Thêm nhân viên không thành công.");
            }
        }

        private void SaveEmployee_Click(object sender, RoutedEventArgs e)
        {
            if (dataGridEmployees.SelectedItem is DataRowView drv)
            {
                AddEmployee updateEmployeeDialog = new AddEmployee(_connString, _rsaKeyClient, _rsaServiceProvider, (string?)drv["MANV"]);
                if (updateEmployeeDialog.ShowDialog() == true)
                {
                    MessageBox.Show("Cập nhật nhân viên thành công.");
                    LoadEmployees();
                }
                else
                {
                    MessageBox.Show("Cập nhật nhân viên không thành công.");
                }
            } 
        }
        #endregion

        #region Scores Tab
        private void InitScoreGrid(string? mahp = null)
        {
            if (mahp == null)
            {
                _scores = ExecSP("SP_SEL_SCORE_ENCRYPTED",
                    new SqlParameter("@TENDN", _user));
            } else
            {
                _scores = ExecSP("SP_SEL_SCORE_ENCRYPTED",
                    new SqlParameter("@TENDN", _user),
                    new SqlParameter("@MAHP", mahp));
            }
            
            if (_scores == null)
            {
                MessageBox.Show("Failed to load scores.");
                return;
            }

            _scores = DecryptDataTable(_scores, "DIEMTHI", 2);
            dataGridScores.ItemsSource = _scores.DefaultView;
            dataGridScores.Columns[0].Header = "Mã học phần";
            dataGridScores.Columns[1].Header = "Mã lớp";
            dataGridScores.Columns[2].Header = "Mã sinh viên";
            dataGridScores.Columns[3].Header = "Tên sinh viên";
            dataGridScores.Columns[4].Header = "Điểm số";
        }

        private void DataGridCoursesScoreTab_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dataGridCoursesScoreTab.SelectedItem is DataRowView drv)
            {
                string? mahp = drv["MAHP"] != null && drv["MAHP"] != DBNull.Value ? drv["MAHP"].ToString(): null;
                _mahp = mahp;
                InitScoreGrid(_mahp);
            }
        }
        private void AddNewRow_Click(object sender, RoutedEventArgs e)
        {
            var addScoreDialog = new AddScoreDialog(_connString, _user, _rsaServiceProvider);
            if (addScoreDialog.ShowDialog() != true)
            {
                MessageBox.Show("Nhập điểm không thành công.");
                InitScoreGrid(_mahp);
            }
            else
            {
                MessageBox.Show("Nhập điểm thành công.");
                InitScoreGrid(_mahp);
                
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

            var updateScoreDialog = new UpdateScoreDialog(_connString, _rsaServiceProvider, _user, masv, mahp);
            if (updateScoreDialog.ShowDialog() == true)
            {
                InitScoreGrid(_mahp);
            }
            else
            {
                MessageBox.Show("Cập nhật điểm không thành công.");
                InitScoreGrid(_mahp);
            }
        }

        private void DeleteScoreRow_Click(object sender, RoutedEventArgs e)
        {
            var drv = (DataRowView)((Button)sender).DataContext;
            string masv = drv["MASV"].ToString();
            string mahp = drv["MAHP"].ToString();
            try
            {
                using var cmd = ExecSP("SP_DEL_SCORE_ENCRYPTED",
                new SqlParameter("@TENDN", _user),
                new SqlParameter("@MASV", masv),
                new SqlParameter("@MAHP", mahp));
                _scores.Rows.Remove(drv.Row);
            }
            catch(SqlException ex)
            {
                MessageBox.Show("Có lỗi xảy ra khi xóa điểm: " + ex.Message);
            }
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
                LoadClasses();
            }
        }

        private void SaveClass_Click(object sender, RoutedEventArgs e)
        {
            dataGridClasses.CommitEdit(DataGridEditingUnit.Row, true);
            var drv = (DataRowView)((Button)sender).DataContext;
            try
            {
                ExecNonQuery("SP_UPDATE_LOP",
                    new SqlParameter("@MALOP", drv["MALOP"]),
                    new SqlParameter("@TENLOP", drv["TENLOP"]),
                    new SqlParameter("@TENDN", _user));
                MessageBox.Show("Cập nhật thông tin lớp thành công");
                LoadClasses();
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Có lỗi xảy ra khi cập nhật lớp: " + ex.Message);
                return;
            }
            
        }

        private void DeleteClass_Click(object sender, RoutedEventArgs e)
        {
            var drv = (DataRowView)((Button)sender).DataContext;
            try
            {
                ExecNonQuery("SP_DEL_LOP",
                    new SqlParameter("@MALOP", drv["MALOP"]));
                MessageBox.Show("Xóa lớp thành công.");

            }
            catch (SqlException ex)
            {
                MessageBox.Show("Có lỗi xảy ra khi xóa lớp: " + ex.Message);
                return;
            }
            LoadClasses();
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

            DataTable coursesScoreTab = _courses.Copy();
            DataRow selectAllCoursesRow = coursesScoreTab.NewRow();
            selectAllCoursesRow["MAHP"] = DBNull.Value;
            selectAllCoursesRow["TENHP"] = "Tất cả học phần";
            selectAllCoursesRow["SOTC"] = DBNull.Value;
            coursesScoreTab.Rows.InsertAt(selectAllCoursesRow, 0);
            dataGridCoursesScoreTab.ItemsSource = coursesScoreTab.DefaultView;
        }

        private void UpdateCourse_Click(object sender, RoutedEventArgs e)
        {
            dataGridCourses.CommitEdit(DataGridEditingUnit.Row, true);
            var drv = (DataRowView)((Button)sender).DataContext;
            try
            {
                ExecNonQuery("SP_UPDATE_HOCPHAN",
                    new SqlParameter("@MAHP", drv["MAHP"]),
                    new SqlParameter("@TENHP", drv["TENHP"]),
                    new SqlParameter("@SOTC", drv["SOTC"]));
                MessageBox.Show("Cập nhật học phần thành công");
            }
            catch(SqlException ex)
            {
                MessageBox.Show("Có lỗi xảy ra khi cập nhật học phần: " + ex.Message);
            }
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
                LoadCourses();
            }
        }
        #endregion
    }
}
