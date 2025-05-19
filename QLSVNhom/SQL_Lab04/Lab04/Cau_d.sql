USE QLSVNhom
GO


/*i. Stored procedure dùng để thêm mới dữ liệu (Insert) vào table NHANVIEN*/
CREATE OR ALTER PROCEDURE SP_INS_PUBLIC_ENCRYPT_NHANVIEN
    @MANV VARCHAR(20),
    @HOTEN NVARCHAR(100),
    @EMAIL VARCHAR(100),
    @LUONGCB VARBINARY(MAX),
    @TENDN NVARCHAR(100),
    @MK VARBINARY(MAX),
	@PUB VARCHAR(MAX)
AS
BEGIN
	
	IF EXISTS (SELECT * FROM NHANVIEN WHERE MANV = @MANV)
	BEGIN
		RAISERROR(N'Loi: Ma nhan vien %s da ton tai', 16, 1, @MANV);
		RETURN;
	END

	IF EXISTS(SELECT * FROM NHANVIEN WHERE TENDN = @TENDN)
	BEGIN
		RAISERROR(N'Loi: Ten dang nhap %s da ton tai', 16, 1, @TENDN);
		RETURN;
	END

	-- Insert vao bang NHANVIEN
	INSERT INTO NHANVIEN(MANV, HOTEN, EMAIL, LUONG, TENDN, MATKHAU, PUBKEY)
    VALUES (@MANV, @HOTEN, @EMAIL, @LUONGCB, @TENDN, @MK, @PUB);

	PRINT N'Nhan vien ' + @MANV + ' da duoc them thanh cong.'
END 
GO

CREATE OR ALTER PROCEDURE SP_UPDATE_PUBLIC_ENCRYPT_NHANVIEN
	@MANV VARCHAR(20),
    @HOTEN NVARCHAR(100),
    @EMAIL VARCHAR(100),
    @TENDN NVARCHAR(100) = NULL,
	@MK VARBINARY(MAX)
AS
BEGIN
	
	DECLARE @MK_HIENTAI VARBINARY(MAX), @MANV_HIENTAI VARCHAR(20);
	SELECT @MANV_HIENTAI = MANV, @MK_HIENTAI = MATKHAU FROM NHANVIEN WHERE TENDN = @TENDN

	IF @MANV != @MANV_HIENTAI
	BEGIN
		RAISERROR(N'Bạn không thể chỉnh thông tin nhân viên khác!', 16, 1000);
		RETURN;
	END

	-- Insert vao bang NHANVIEN
	UPDATE NHANVIEN SET HOTEN = @HOTEN, EMAIL = @EMAIL, TENDN = @TENDN, MATKHAU = @MK WHERE MANV = @MANV
	
	PRINT N'Nhân viên ' + @MANV + ' đã được chỉnh sửa thành công.'
END 
GO

/*ii) Stored procedure dùng để truy vấn dữ liệu nhân viên (NHANVIEN)*/
CREATE OR ALTER PROCEDURE SP_SEL_PUBLIC_ENCRYPT_NHANVIEN
    @TENDN NVARCHAR(100),
    @MK VARBINARY(MAX)
AS
BEGIN

	IF NOT EXISTS(SELECT * FROM NHANVIEN WHERE TENDN = @TENDN)
	BEGIN
		RAISERROR(N'Ten dang nhap %s khong ton tai', 16, 1, @TENDN);
		RETURN;
	END

	DECLARE @MANV VARCHAR(20), @HOTEN NVARCHAR(100), @EMAIL VARCHAR(100), 
	@ELUONG VARBINARY(MAX), @PUBKEY VARCHAR(20), @STORED_HASHED_MK VARBINARY(MAX);

	SELECT TOP 1 @MANV = MANV, @HOTEN = HOTEN, @EMAIL = EMAIL, 
	@ELUONG = LUONG, @STORED_HASHED_MK = MATKHAU, @PUBKEY = PUBKEY FROM NHANVIEN WHERE TENDN = @TENDN;

	IF @MANV = NULL OR @MK != @STORED_HASHED_MK
	BEGIN
		RAISERROR('Ten dang nhap hoac mat khau khong dung.', 16, 1);
		RETURN;
	END

	SELECT MANV, HOTEN, EMAIL, LUONG FROM NHANVIEN WHERE MANV = @MANV

END 
GO

exec SP_SEL_PUBLIC_ENCRYPT_NHANVIEN @TENDN=N'21120263',@MK=0x5BAA61E4C9B93F3F0682250B6CF8331B7EE68FD8
GO

CREATE OR ALTER PROCEDURE SP_SEL_PUBLIC_ALL_NHANVIEN
AS
BEGIN
	SELECT MANV, HOTEN, EMAIL, TENDN FROM NHANVIEN
END
GO


CREATE OR ALTER PROCEDURE SP_LOGIN_ENCRYPTED
    @TENDN NVARCHAR(100),
    @MK NVARCHAR(MAX),
	@MANV VARCHAR(20) = NULL OUT,
	@HOTEN NVARCHAR(100) = NULL OUT,
	@PUB VARCHAR(MAX) = NULL OUT
AS
BEGIN
	

	IF NOT EXISTS(SELECT * FROM NHANVIEN WHERE TENDN = @TENDN)
	BEGIN
		RAISERROR(N'Loi: Ten dang nhap %s khong ton tai', 16, 1, @TENDN);
		SET @MANV = NULL; SET @HOTEN = NULL;
		RETURN;
	END

	DECLARE @EMAIL VARCHAR(100), 
	@ELUONG VARBINARY(MAX), @MKBAM VARBINARY(MAX);

	SELECT TOP 1 @MANV = MANV, @HOTEN = HOTEN, @EMAIL = EMAIL, 
	@ELUONG = LUONG, @MKBAM = MATKHAU, @PUB = PUBKEY FROM NHANVIEN WHERE TENDN = @TENDN;

	IF @MK != @MKBAM
	BEGIN
		RAISERROR(N'Lỗi: Sai mật khẩu đăng nhập.', 16, 10000);
		SET @MANV = NULL; SET @HOTEN = NULL;
		RETURN;
	END

END 
GO

CREATE OR ALTER PROCEDURE SP_INS_LOP
@MALOP VARCHAR(20), @TENLOP NVARCHAR(100), @TENDN NVARCHAR(100)
AS
BEGIN
	DECLARE @MASONV VARCHAR(20)
	SELECT @MASONV = MANV FROM NHANVIEN WHERE TENDN = @TENDN
	
	IF @MASONV IS NULL
	BEGIN
		PRINT 'Loi: Ten dang nhap hoac mat khau khong dung.';
		RETURN;
	END

	BEGIN TRY
		INSERT INTO LOP(MALOP, TENLOP, MANV) VALUES (@MALOP, @TENLOP, @MASONV)
		PRINT N'Lop voi ma lop ' + @MALOP + N' da duoc them thanh cong.'
	END TRY
	BEGIN CATCH
		PRINT N'Co loi xay ra khi them lop.'
	END CATCH
END
GO

CREATE OR ALTER PROCEDURE SP_UPDATE_LOP
@MALOP VARCHAR(20), @TENLOP NVARCHAR(100), @TENDN NVARCHAR(100)
AS
BEGIN
	DECLARE @MASONV VARCHAR(20)
	SELECT @MASONV = MANV FROM NHANVIEN WHERE TENDN = @TENDN
	
	IF @MASONV IS NULL
	BEGIN
		RAISERROR(N'Tên đăng nhập không đúng.', 16, 1);
		RETURN;
	END

	BEGIN TRY
		UPDATE LOP SET TENLOP = @TENLOP WHERE MANV = @MASONV AND MALOP = @MALOP
		PRINT N'Lop voi ma lop ' + @MALOP + N' da duoc sua thanh cong.';
	END TRY
	BEGIN CATCH
		RAISERROR(N'Co loi xay ra khi them lop: %s', 16, 1);
		RETURN
	END CATCH
END
GO

CREATE OR ALTER PROCEDURE SP_DEL_LOP
@MALOP VARCHAR(20), @TENDN NVARCHAR(100)
AS
BEGIN
	DECLARE @MASONV VARCHAR(20)
	SELECT @MASONV = MANV FROM NHANVIEN WHERE TENDN = @TENDN
	
	IF @MASONV IS NULL
	BEGIN
		RAISERROR(N'Tên đăng nhập không đúng.', 16, 1);
		RETURN;
	END

	BEGIN TRY
		DELETE FROM LOP WHERE MANV = @MASONV AND MALOP = @MALOP
		PRINT N'Lớp với mã lớp ' + @MALOP + N' đã được xóa thành công.';
	END TRY
	BEGIN CATCH
		RAISERROR(N'Không thể xóa lớp do có sinh viên trong lớp.', 16, 1);
		RETURN
	END CATCH
END
GO


CREATE OR ALTER PROCEDURE SP_SELECT_LOP
@TENDN NVARCHAR(100)
AS
BEGIN
	DECLARE @MANV VARCHAR(20);
	SELECT @MANV = MANV FROM NHANVIEN WHERE TENDN = @TENDN;

	IF @MANV IS NULL
	BEGIN
		PRINT N'Nhan vien voi ten dang nhap ' + @TENDN + ' khong ton tai.'
	END
	SELECT MALOP, TENLOP, MANV, IIF(MANV = @MANV, 1, 0) AS DANGQL FROM LOP
END
GO


CREATE OR ALTER PROCEDURE SP_INS_SINHVIEN_LOP_ENCRYPTED
    @TENDN NVARCHAR(100),
    @MALOP VARCHAR(20),
    @MASV VARCHAR(20),
    @HOTEN NVARCHAR(100) = NULL,
    @NGAYSINH DATETIME = NULL,
    @DIACHI NVARCHAR(200) = NULL,
	@TENDNSV NVARCHAR(100),
	@MK VARBINARY(MAX),
	@KETQUA NVARCHAR(200) = NULL OUT
AS
BEGIN
	-- Xác thực nhân viên
    DECLARE @MANV VARCHAR(20);
    SELECT TOP 1 @MANV = MANV FROM NHANVIEN WHERE TENDN = @TENDN

    -- Kiểm tra thông tin đăng nhập
    IF @MANV IS NULL
    BEGIN
        PRINT N'Lỗi: Tên đăng nhập hoặc mật khẩu không đúng.';
		SET @KETQUA = N'Lỗi: Tên đăng nhập hoặc mật khẩu không đúng.';
        RETURN;
    END

    -- Kiểm tra xem nhân viên có quản lý lớp này không
    IF NOT EXISTS (SELECT 1 FROM LOP WHERE MALOP = @MALOP AND MANV = @MANV)
    BEGIN
        PRINT N'Lỗi: Bạn không có quyền sửa thông tin sinh viên của lớp này.';
		SET @KETQUA = N'Lỗi: Bạn không có quyền sửa thông tin sinh viên của lớp này.';
        RETURN;
    END

	BEGIN TRY
		INSERT INTO SINHVIEN(MASV, HOTEN, DIACHI, NGAYSINH, MALOP, TENDN, MATKHAU)
		VALUES (@MASV, @HOTEN, @DIACHI, @NGAYSINH, @MALOP, @TENDNSV, @MK )
		SET @KETQUA = N'Thêm sinh viên với mã sinh viên ' + @MASV + ' thành công.'
	END TRY
	BEGIN CATCH
		SET @KETQUA = N'Lỗi: Có lỗi xảy ra khi thêm sinh viên. Vui lòng kiểm tra lại mã lớp, ngày sinh và mã sinh viên.'
	END CATCH
END
GO

-- Stored procedure để sửa thông tin sinh viên của một lớp mà nhân viên quản lý
CREATE OR ALTER PROCEDURE SP_UPDATE_SINHVIEN_LOP
    @TENDN NVARCHAR(100),
    @MALOP VARCHAR(20),
    @MASV VARCHAR(20),
    @HOTEN NVARCHAR(100) = NULL,
    @NGAYSINH DATETIME = NULL,
    @DIACHI NVARCHAR(200) = NULL
AS
BEGIN
    -- Xác thực nhân viên
    DECLARE @MANV VARCHAR(20);
    SELECT TOP 1 @MANV = MANV FROM NHANVIEN WHERE TENDN = @TENDN

    -- Kiểm tra thông tin đăng nhập
    IF @MANV IS NULL
    BEGIN
        RAISERROR(N'Lỗi: Tên đăng nhập hoặc mật khẩu không đúng.', 16, 1);
        RETURN;
    END;

    -- Kiểm tra xem nhân viên có quản lý lớp này không
    IF NOT EXISTS (SELECT 1 FROM LOP WHERE MALOP = @MALOP AND MANV = @MANV)
    BEGIN
        RAISERROR(N'Lỗi: Bạn không có quyền sửa thông tin sinh viên của lớp này.',16, 1);
        RETURN;
    END

    -- Kiểm tra xem sinh viên có thuộc lớp này không
    IF NOT EXISTS (SELECT 1 FROM SINHVIEN WHERE MASV = @MASV AND MALOP = @MALOP)
    BEGIN
        RAISERROR(N'Lỗi: Sinh viên này không thuộc lớp này.', 16, 1);
        RETURN;
    END

    -- Sửa thông tin sinh viên
    UPDATE SINHVIEN
    SET HOTEN = ISNULL(@HOTEN, HOTEN),
        NGAYSINH = ISNULL(@NGAYSINH, NGAYSINH),
        DIACHI = ISNULL(@DIACHI, DIACHI)
    WHERE MASV = @MASV AND MALOP = @MALOP;
    PRINT N'Sửa thông tin sinh viên thành công.';
END
GO

-- Stored procedure để xem thông tin sinh viên của một lớp mà nhân viên quản lý
CREATE OR ALTER PROCEDURE SP_SEL_SINHVIEN_LOP
    @TENDN NVARCHAR(100),
    @MALOP VARCHAR(20)
AS
BEGIN
    -- Xác thực nhân viên
    DECLARE @MANV VARCHAR(20);
    SELECT @MANV = MANV FROM NHANVIEN WHERE TENDN = @TENDN

    -- Kiểm tra thông tin đăng nhập
    IF @MANV IS NULL
    BEGIN
        PRINT N'Lỗi: Tên đăng nhập hoặc mật khẩu không đúng.';
        RETURN;
    END

    ---- Kiểm tra xem nhân viên có quản lý lớp này không
    --IF NOT EXISTS (SELECT 1 FROM LOP WHERE MALOP = @MALOP AND MANV = @MANV)
    --BEGIN
    --    PRINT N'Lỗi: Bạn không có quyền xem thông tin sinh viên của lớp này.';
    --    RETURN;
    --END

    -- Lấy thông tin sinh viên của lớp
    SELECT MASV, HOTEN, NGAYSINH, DIACHI, MALOP, TENDN FROM SINHVIEN WHERE MALOP = @MALOP;
END
GO


-- Stored procedure nhập bảng điểm của sinh viên (điểm thi được mã hóa)
CREATE OR ALTER PROCEDURE SP_INS_SCORE_ENCRYPTED
    @TENDN NVARCHAR(100),
    @MASV VARCHAR(20),
    @MAHP VARCHAR(20),
    @DIEMTHI VARBINARY(MAX)
AS
BEGIN
    -- Xác thực nhân viên
    DECLARE @MANV VARCHAR(20);
    SELECT @MANV = MANV FROM NHANVIEN WHERE TENDN = @TENDN

    -- Kiểm tra thông tin đăng nhập
    IF @MANV IS NULL
    BEGIN
        RAISERROR(N'Lỗi: Tên đăng nhập không đúng.', 16, 1);
        RETURN;
    END

	-- Kiểm tra nhân viên với tên đăng nhập TENDN có quản lý lớp hay không
	IF NOT EXISTS(SELECT * FROM (NHANVIEN JOIN LOP ON NHANVIEN.MANV = LOP.MANV) JOIN SINHVIEN ON LOP.MALOP = SINHVIEN.MALOP
	WHERE SINHVIEN.MASV = @MASV AND NHANVIEN.TENDN = @TENDN)
	BEGIN
		RAISERROR(N'Bạn không có quyền sửa điểm cho sinh viên với mã sinh viên %s do sinh viên này thuộc lớp bạn không quản lý.', 16, 1, @MASV);
		RETURN;
	END

    -- Kiểm tra xem sinh viên và học phần có tồn tại không
    IF NOT EXISTS (SELECT 1 FROM SINHVIEN WHERE MASV = @MASV)
    BEGIN
        RAISERROR(N'Lỗi: Sinh viên không tồn tại.', 16, 1);
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM HOCPHAN WHERE MAHP = @MAHP)
    BEGIN
        RAISERROR(N'Lỗi: Học phần không tồn tại.', 16, 1);
        RETURN;
    END

	DECLARE @PUBKEY VARCHAR(20);
	SELECT @PUBKEY = PUBKEY FROM NHANVIEN WHERE MANV = @MANV;

	IF EXISTS(SELECT * FROM BANGDIEM WHERE MASV = @MASV AND MAHP = @MAHP)
	BEGIN
		UPDATE BANGDIEM SET DIEMTHI = @DIEMTHI WHERE MASV = @MASV AND MAHP = @MAHP
	END
	ELSE
	BEGIN
		-- Nhập điểm vào bảng điểm
		INSERT INTO BANGDIEM (MASV, MAHP, DIEMTHI)
		VALUES (@MASV, @MAHP, @DIEMTHI);
	END

    PRINT N'Nhập điểm thành công.';
END
GO

CREATE OR ALTER PROCEDURE SP_DEL_SCORE_ENCRYPTED
	@TENDN NVARCHAR(100),
    @MASV VARCHAR(20),
    @MAHP VARCHAR(20)
AS
BEGIN
	-- Xác thực nhân viên
    DECLARE @MANV VARCHAR(20);
    SELECT @MANV = MANV FROM NHANVIEN WHERE TENDN = @TENDN

    -- Kiểm tra thông tin đăng nhập
    IF @MANV IS NULL
    BEGIN
        RAISERROR(N'Tên đăng nhập không đúng.', 16, 1);
        RETURN;
    END

	-- Kiểm tra nhân viên với tên đăng nhập TENDN có quản lý lớp hay không
	IF NOT EXISTS(SELECT * FROM (NHANVIEN JOIN LOP ON NHANVIEN.MANV = LOP.MANV) JOIN SINHVIEN ON LOP.MALOP = SINHVIEN.MALOP
	WHERE SINHVIEN.MASV = @MASV AND NHANVIEN.TENDN = @TENDN)
	BEGIN
		RAISERROR(N'Bạn không có quyền xóa điểm cho sinh viên với mã sinh viên %s do sinh viên này thuộc lớp bạn không quản lý.', 16, 1, @MASV);
		RETURN;
	END

	-- Kiểm tra xem sinh viên và học phần có tồn tại không
    IF NOT EXISTS (SELECT 1 FROM SINHVIEN WHERE MASV = @MASV)
    BEGIN
        RAISERROR(N'Sinh viên không tồn tại.', 16, 1);
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM HOCPHAN WHERE MAHP = @MAHP)
    BEGIN
        RAISERROR(N'Học phần không tồn tại.', 16, 1);
        RETURN;
    END

	DELETE FROM BANGDIEM WHERE MASV = @MASV AND MAHP = @MAHP
END
GO

CREATE OR ALTER PROCEDURE SP_SEL_SCORE_ENCRYPTED
    @TENDN NVARCHAR(100),
    @MAHP VARCHAR(20) = NULL
AS
BEGIN
	DECLARE @MASONV VARCHAR(20);
	SELECT @MASONV = MANV FROM NHANVIEN WHERE TENDN = @TENDN

	IF @MASONV IS NULL
	BEGIN
		PRINT N'Ten dang nhap hoac mat khau khong dung.'
		RETURN;
	END

	DECLARE @PUBKEY VARCHAR(20);
	
	-- Lấy public key của nhân viên đang đăng nhập
	SELECT TOP 1 @PUBKEY = PUBKEY FROM NHANVIEN WHERE MANV = @MASONV
	IF @MAHP IS NULL
	BEGIN
		SELECT DISTINCT MAHP, SINHVIEN.MASV, SINHVIEN.MALOP, HOTEN AS TENSV, DIEMTHI
		FROM (BANGDIEM LEFT JOIN SINHVIEN ON BANGDIEM.MASV = SINHVIEN.MASV), LOP 
		WHERE SINHVIEN.MALOP IN (SELECT MALOP FROM LOP WHERE MANV = @MASONV)
	END
	ELSE
	BEGIN
		SELECT DISTINCT MAHP, SINHVIEN.MASV, SINHVIEN.MALOP, HOTEN AS TENSV, DIEMTHI
		FROM (BANGDIEM LEFT JOIN SINHVIEN ON BANGDIEM.MASV = SINHVIEN.MASV), LOP 
		WHERE SINHVIEN.MALOP IN (SELECT MALOP FROM LOP WHERE MANV = @MASONV) AND MAHP = @MAHP
	END
END
GO

-- Stored procedure để xóa lớp học
CREATE OR ALTER PROCEDURE SP_DEL_LOP
    @MALOP VARCHAR(20)
AS
BEGIN
    -- Kiểm tra xem lớp học có tồn tại không
    IF NOT EXISTS (SELECT 1 FROM LOP WHERE MALOP = @MALOP)
    BEGIN
        PRINT N'Lỗi: Lớp học không tồn tại.';
        RETURN;
    END

    -- Xóa lớp học
    DELETE FROM LOP WHERE MALOP = @MALOP;
    PRINT N'Xóa lớp học thành công.';
END
GO

-- Stored procedure để xem học phần
CREATE OR ALTER PROCEDURE SP_SEL_HOCPHAN
AS
BEGIN
    -- Lấy tất cả các học phần
    SELECT MAHP, TENHP, SOTC FROM HOCPHAN;
END
GO

-- Stored procedure để xóa học phần
CREATE OR ALTER PROCEDURE SP_DEL_HOCPHAN
    @MAHP VARCHAR(20)
AS
BEGIN
    -- Kiểm tra xem học phần có tồn tại không
    IF NOT EXISTS (SELECT 1 FROM HOCPHAN WHERE MAHP = @MAHP)
    BEGIN
        PRINT N'Lỗi: Học phần không tồn tại.';
        RETURN;
    END

    -- Xóa học phần
    DELETE FROM HOCPHAN WHERE MAHP = @MAHP;
    PRINT N'Xóa học phần thành công.';
END
GO

-- Stored procedure để tạo học phần
CREATE OR ALTER PROCEDURE SP_INS_HOCPHAN
    @MAHP VARCHAR(20),
    @TENHP NVARCHAR(100),
    @SOTC INT
AS
BEGIN
    -- Kiểm tra xem học phần đã tồn tại chưa
    IF EXISTS (SELECT 1 FROM HOCPHAN WHERE MAHP = @MAHP)
    BEGIN
        PRINT N'Lỗi: Học phần đã tồn tại.';
        RETURN;
    END

    -- Tạo học phần mới
    INSERT INTO HOCPHAN (MAHP, TENHP, SOTC) VALUES (@MAHP, @TENHP, @SOTC);
    PRINT N'Tạo học phần thành công.';
END
GO

-- Stored procedure để sửa học phần
CREATE OR ALTER PROCEDURE SP_UPDATE_HOCPHAN
    @MAHP VARCHAR(20),
    @TENHP NVARCHAR(100) = NULL,
    @SOTC INT = NULL
AS
BEGIN
    -- Kiểm tra xem học phần có tồn tại không
    IF NOT EXISTS (SELECT 1 FROM HOCPHAN WHERE MAHP = @MAHP)
    BEGIN
        RAISERROR(N'Lỗi: Học phần không tồn tại.', 16, 1);
        RETURN;
    END

    -- Sửa thông tin học phần
    UPDATE HOCPHAN
    SET TENHP = ISNULL(@TENHP, TENHP),
        SOTC = ISNULL(@SOTC, SOTC)
    WHERE MAHP = @MAHP;
    PRINT N'Sửa học phần thành công.';
END
GO


