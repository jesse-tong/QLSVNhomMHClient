USE master;
GO
IF DB_ID('QLSVNhom') IS NOT NULL
BEGIN
    DROP DATABASE QLSVNhom;
END
GO

-- Tạo Database mới
CREATE DATABASE QLSVNhom;
GO

-- Sử dụng Database vừa tạo
USE QLSVNhom;
GO