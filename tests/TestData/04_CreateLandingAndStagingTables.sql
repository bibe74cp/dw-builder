-- ============================================================================
-- LANDING AND STAGING TABLES — DDL SCRIPT
-- ============================================================================
-- Object: [test].[Customers] (landing) and [test].[stg_Customers] (staging)
-- Date: 2026-05-16
-- Author: db-developer
-- Description: Creates landing and staging tables for TestSourceDB.dbo.Customers
--              Structure follows DW-Builder standard pattern
-- ============================================================================

USE [DwBuilderDW];
GO

-- ============================================================================
-- 1. CREATE SCHEMA [test]
-- ============================================================================

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'test')
BEGIN
    EXEC('CREATE SCHEMA [test]');
    PRINT 'Schema [test] created successfully.';
END
ELSE
BEGIN
    PRINT 'Schema [test] already exists.';
END
GO

-- ============================================================================
-- 2. CREATE LANDING TABLE [test].[Customers]
-- ============================================================================

IF OBJECT_ID(N'test.Customers', 'U') IS NOT NULL
BEGIN
    DROP TABLE [test].[Customers];
    PRINT 'Existing table [test].[Customers] dropped.';
END
GO

CREATE TABLE [test].[Customers] (

    -- Business Key Columns
    [CustomerId]        INT             NOT NULL,

    -- Technical Columns
    [ChangeHashKey]     CHAR(64)        NOT NULL,
    [InsertDatetime]    DATETIME2       NOT NULL,
    [UpdateDatetime]    DATETIME2       NOT NULL,
    [IsDeleted]         BIT             NOT NULL DEFAULT 0,

    -- Non-Key Columns
    [CustomerCode]      NVARCHAR(20)    NOT NULL,
    [CustomerName]      NVARCHAR(100)   NOT NULL,
    [Email]             NVARCHAR(100)   NULL,
    [City]              NVARCHAR(50)    NULL,
    [IsActive]          BIT             NOT NULL,

    CONSTRAINT [PK_test_Customers] PRIMARY KEY CLUSTERED ([CustomerId])
);
GO

PRINT 'Landing table [test].[Customers] created successfully.';
GO

-- ============================================================================
-- 3. CREATE STAGING TABLE [test].[stg_Customers]
-- ============================================================================

IF OBJECT_ID(N'test.stg_Customers', 'U') IS NOT NULL
BEGIN
    DROP TABLE [test].[stg_Customers];
    PRINT 'Existing table [test].[stg_Customers] dropped.';
END
GO

CREATE TABLE [test].[stg_Customers] (

    -- Business Key Columns
    [CustomerId]        INT             NOT NULL,

    -- Technical Columns
    [ChangeHashKey]     CHAR(64)        NOT NULL,
    [InsertDatetime]    DATETIME2       NOT NULL,
    [UpdateDatetime]    DATETIME2       NOT NULL,
    [IsDeleted]         BIT             NOT NULL DEFAULT 0,

    -- Non-Key Columns
    [CustomerCode]      NVARCHAR(20)    NOT NULL,
    [CustomerName]      NVARCHAR(100)   NOT NULL,
    [Email]             NVARCHAR(100)   NULL,
    [City]              NVARCHAR(50)    NULL,
    [IsActive]          BIT             NOT NULL
);
GO

PRINT 'Staging table [test].[stg_Customers] created successfully.';
GO

-- Note: Staging table has no PRIMARY KEY constraint.
-- Used for TRUNCATE + BULK INSERT + MERGE pattern.

-- ============================================================================
-- SCRIPT COMPLETED
-- ============================================================================

PRINT 'Landing and staging tables setup completed successfully.';
PRINT 'Next step: Generate BIML file via DW-Builder API and compile with BimlExpress.';
GO
