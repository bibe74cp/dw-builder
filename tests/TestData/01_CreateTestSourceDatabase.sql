-- ============================================================================
-- TEST SOURCE DATABASE — CREATE SCRIPT
-- ============================================================================
-- Object: TestSourceDB (test source database)
-- Date: 2026-05-16
-- Author: db-developer
-- Description: Creates test source database with sample tables (Customers, Orders)
--              Used to validate BIML generation and SSIS package execution
-- ============================================================================

USE master;
GO

-- ============================================================================
-- 1. CREATE DATABASE
-- ============================================================================

IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = N'TestSourceDB')
BEGIN
    CREATE DATABASE [TestSourceDB];
    PRINT 'Database TestSourceDB created successfully.';
END
ELSE
BEGIN
    PRINT 'Database TestSourceDB already exists.';
END
GO

USE [TestSourceDB];
GO

-- ============================================================================
-- 2. CREATE TABLE: Customers
-- ============================================================================

IF OBJECT_ID(N'dbo.Customers', 'U') IS NOT NULL
    DROP TABLE dbo.Customers;
GO

CREATE TABLE dbo.Customers (
    CustomerId      INT             NOT NULL IDENTITY(1,1),
    CustomerCode    NVARCHAR(20)    NOT NULL,
    CustomerName    NVARCHAR(100)   NOT NULL,
    Email           NVARCHAR(100)   NULL,
    City            NVARCHAR(50)    NULL,
    IsActive        BIT             NOT NULL DEFAULT 1,

    CONSTRAINT PK_Customers PRIMARY KEY CLUSTERED (CustomerId),
    CONSTRAINT UQ_Customers_CustomerCode UNIQUE (CustomerCode)
);
GO

PRINT 'Table dbo.Customers created successfully.';
GO

-- ============================================================================
-- 3. CREATE TABLE: Orders
-- ============================================================================

IF OBJECT_ID(N'dbo.Orders', 'U') IS NOT NULL
    DROP TABLE dbo.Orders;
GO

CREATE TABLE dbo.Orders (
    OrderId         INT             NOT NULL IDENTITY(1,1),
    CustomerId      INT             NOT NULL,
    OrderDate       DATE            NOT NULL,
    TotalAmount     DECIMAL(18,2)   NOT NULL,
    Status          NVARCHAR(20)    NOT NULL,

    CONSTRAINT PK_Orders PRIMARY KEY CLUSTERED (OrderId),
    CONSTRAINT FK_Orders_Customers FOREIGN KEY (CustomerId)
        REFERENCES dbo.Customers (CustomerId)
);
GO

PRINT 'Table dbo.Orders created successfully.';
GO

-- ============================================================================
-- SCRIPT COMPLETED
-- ============================================================================

PRINT 'Test source database setup completed successfully.';
PRINT 'Next step: Execute 02_InsertTestData.sql';
GO
