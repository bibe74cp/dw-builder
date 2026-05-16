-- ============================================================================
-- ESEMPIO DI OUTPUT GENERATO DAL DDL GENERATOR SERVICE
-- ============================================================================
-- Scenario: Source "ERP" con tabella "SalesOrder"
-- Business keys: DataAreaId, OrderId
-- Non-key fields: CustomerCode, OrderDate, TotalAmount, Status
-- ============================================================================

-- ============================================================================
-- 1. LANDING TABLE CREATE SCRIPT
-- ============================================================================

-- CREATE TABLE script for landing table [ERP].[SalesOrder]
-- Generated: 2026-05-16 14:30:00 UTC
-- Source: ERP System | Table: dbo.SalesOrder

CREATE TABLE [ERP].[SalesOrder] (

    -- Business Key Columns
    [DataAreaId] NVARCHAR(4) NOT NULL,
    [OrderId] NVARCHAR(20) NOT NULL,

    -- Technical Columns
    [ChangeHashKey]   CHAR(64)       NOT NULL,
    [InsertDatetime]  DATETIME2      NOT NULL,
    [UpdateDatetime]  DATETIME2      NOT NULL,
    [IsDeleted]       BIT            NOT NULL DEFAULT 0,

    -- Non-Key Columns
    [CustomerCode] NVARCHAR(20) NULL,
    [OrderDate] DATE NOT NULL,
    [TotalAmount] DECIMAL(18,2) NULL,
    [Status] NVARCHAR(10) NULL,

    CONSTRAINT [PK_ERP_SalesOrder] PRIMARY KEY CLUSTERED ([DataAreaId], [OrderId])
);

-- ============================================================================
-- 2. STAGING TABLE CREATE SCRIPT
-- ============================================================================

-- CREATE TABLE script for staging table [ERP].[stg_SalesOrder]
-- Generated: 2026-05-16 14:30:00 UTC
-- Source: ERP System | Table: dbo.SalesOrder

CREATE TABLE [ERP].[stg_SalesOrder] (

    -- Business Key Columns
    [DataAreaId] NVARCHAR(4) NOT NULL,
    [OrderId] NVARCHAR(20) NOT NULL,

    -- Technical Columns
    [ChangeHashKey]   CHAR(64)       NOT NULL,
    [InsertDatetime]  DATETIME2      NOT NULL,
    [UpdateDatetime]  DATETIME2      NOT NULL,
    [IsDeleted]       BIT            NOT NULL DEFAULT 0,

    -- Non-Key Columns
    [CustomerCode] NVARCHAR(20) NULL,
    [OrderDate] DATE NOT NULL,
    [TotalAmount] DECIMAL(18,2) NULL,
    [Status] NVARCHAR(10) NULL
);

-- Note: Staging table has no PRIMARY KEY constraint.
-- Used for TRUNCATE + BULK INSERT + MERGE pattern.

-- ============================================================================
-- 3. ALTER TABLE SCRIPT (scenario: adding new field "ShipDate")
-- ============================================================================

-- ALTER TABLE script for [ERP].[SalesOrder]
-- Generated: 2026-05-16 14:35:00 UTC

ALTER TABLE [ERP].[SalesOrder]
    ADD [ShipDate] DATE NULL;

-- ============================================================================
-- 4. ALTER TABLE SCRIPT with warnings (scenario: removed field in config)
-- ============================================================================

-- ALTER TABLE script for [ERP].[SalesOrder]
-- Generated: 2026-05-16 14:40:00 UTC

-- WARNING: The following columns exist in the database but are not configured:
-- Column [OLDFIELD1] exists in DB but not in configuration
-- Column [OLDFIELD2] exists in DB but not in configuration
-- Manual intervention required if these columns should be removed.

ALTER TABLE [ERP].[SalesOrder]
    ADD [NewField1] NVARCHAR(50) NULL,
    ADD [NewField2] INT NULL;
