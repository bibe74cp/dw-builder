-- ============================================================================
-- TEST SOURCE DATA — INSERT SCRIPT
-- ============================================================================
-- Object: Test data for TestSourceDB.dbo.Customers and dbo.Orders
-- Date: 2026-05-16
-- Author: db-developer
-- Description: Inserts realistic test data (20 customers, 30 orders)
--              Data is recognizable for validation purposes
-- ============================================================================

USE [TestSourceDB];
GO

SET NOCOUNT ON;

-- ============================================================================
-- 1. INSERT CUSTOMERS (20 records)
-- ============================================================================

DELETE FROM dbo.Orders;
DELETE FROM dbo.Customers;
DBCC CHECKIDENT ('dbo.Customers', RESEED, 0);
DBCC CHECKIDENT ('dbo.Orders', RESEED, 0);
GO

INSERT INTO dbo.Customers (CustomerCode, CustomerName, Email, City, IsActive)
VALUES
    ('CUST001', 'Test Customer 001', 'customer001@testmail.com', 'Milan', 1),
    ('CUST002', 'Test Customer 002', 'customer002@testmail.com', 'Rome', 1),
    ('CUST003', 'Test Customer 003', 'customer003@testmail.com', 'Turin', 1),
    ('CUST004', 'Test Customer 004', 'customer004@testmail.com', 'Florence', 1),
    ('CUST005', 'Test Customer 005', 'customer005@testmail.com', 'Naples', 1),
    ('CUST006', 'Test Customer 006', 'customer006@testmail.com', 'Bologna', 1),
    ('CUST007', 'Test Customer 007', 'customer007@testmail.com', 'Genoa', 1),
    ('CUST008', 'Test Customer 008', 'customer008@testmail.com', 'Venice', 1),
    ('CUST009', 'Test Customer 009', 'customer009@testmail.com', 'Palermo', 1),
    ('CUST010', 'Test Customer 010', 'customer010@testmail.com', 'Bari', 1),
    ('CUST011', 'Test Customer 011', 'customer011@testmail.com', 'Verona', 1),
    ('CUST012', 'Test Customer 012', 'customer012@testmail.com', 'Padua', 1),
    ('CUST013', 'Test Customer 013', 'customer013@testmail.com', 'Trieste', 1),
    ('CUST014', 'Test Customer 014', 'customer014@testmail.com', 'Brescia', 1),
    ('CUST015', 'Test Customer 015', 'customer015@testmail.com', 'Parma', 1),
    ('CUST016', 'Test Customer 016', 'customer016@testmail.com', 'Modena', 1),
    ('CUST017', 'Test Customer 017', 'customer017@testmail.com', 'Reggio Emilia', 1),
    ('CUST018', 'Test Customer 018', 'customer018@testmail.com', 'Perugia', 1),
    ('CUST019', 'Test Customer 019', 'customer019@testmail.com', 'Livorno', 1),
    ('CUST020', 'Test Customer 020', 'customer020@testmail.com', 'Catania', 1);
GO

PRINT 'Inserted 20 customers.';
GO

-- ============================================================================
-- 2. INSERT ORDERS (30 records)
-- ============================================================================

INSERT INTO dbo.Orders (CustomerId, OrderDate, TotalAmount, Status)
VALUES
    (1,  '2026-01-10', 1500.00, 'Completed'),
    (1,  '2026-02-15', 2300.00, 'Completed'),
    (2,  '2026-01-20', 450.00,  'Completed'),
    (3,  '2026-02-05', 780.00,  'Pending'),
    (3,  '2026-03-12', 1200.00, 'Completed'),
    (4,  '2026-01-25', 3400.00, 'Completed'),
    (5,  '2026-02-28', 890.00,  'Shipped'),
    (5,  '2026-03-18', 1100.00, 'Pending'),
    (6,  '2026-01-15', 2100.00, 'Completed'),
    (7,  '2026-02-22', 670.00,  'Completed'),
    (8,  '2026-03-05', 4500.00, 'Shipped'),
    (8,  '2026-03-20', 920.00,  'Pending'),
    (9,  '2026-01-30', 1850.00, 'Completed'),
    (10, '2026-02-10', 3200.00, 'Completed'),
    (11, '2026-03-01', 540.00,  'Pending'),
    (12, '2026-01-18', 2900.00, 'Completed'),
    (12, '2026-02-25', 1600.00, 'Shipped'),
    (13, '2026-03-08', 780.00,  'Pending'),
    (14, '2026-01-22', 4100.00, 'Completed'),
    (15, '2026-02-14', 1250.00, 'Completed'),
    (16, '2026-03-16', 890.00,  'Pending'),
    (17, '2026-01-12', 3300.00, 'Completed'),
    (18, '2026-02-20', 1900.00, 'Shipped'),
    (18, '2026-03-22', 720.00,  'Pending'),
    (19, '2026-01-28', 2600.00, 'Completed'),
    (20, '2026-02-18', 1480.00, 'Completed'),
    (1,  '2026-03-25', 980.00,  'Pending'),
    (2,  '2026-03-10', 1750.00, 'Shipped'),
    (5,  '2026-01-05', 2200.00, 'Completed'),
    (10, '2026-03-15', 3800.00, 'Pending');
GO

PRINT 'Inserted 30 orders.';
GO

-- ============================================================================
-- 3. VALIDATION QUERY
-- ============================================================================

SELECT 
    'Customers' AS TableName,
    COUNT(*) AS RecordCount
FROM dbo.Customers

UNION ALL

SELECT 
    'Orders' AS TableName,
    COUNT(*) AS RecordCount
FROM dbo.Orders;
GO

-- ============================================================================
-- SCRIPT COMPLETED
-- ============================================================================

PRINT 'Test data inserted successfully.';
PRINT 'Next step: Execute 03_ConfigureTestSourceInDwBuilder.sql';
GO
