-- =============================================
-- PayOS Enhanced Multi-Gateway Integration
-- Phase 1: Database Changes - v3 (Fixed Batch Compilation)
-- Date: October 5, 2025
-- Description: Creates PayOSTransactions table, updates WalletTransactions,
--              and creates PaymentGatewayConfig table
-- FIX: Uses dynamic SQL to avoid batch compilation errors
-- =============================================

USE [MathBridgeDB]
GO

SET NOCOUNT ON;
GO

PRINT '========================================='
PRINT 'Starting Phase 1 Database Migration v3'
PRINT 'Date: ' + CONVERT(VARCHAR, GETDATE(), 120)
PRINT '========================================='
PRINT ''
GO

-- =============================================
-- SECTION 1: Create PaymentGatewayConfig Table
-- =============================================
PRINT 'SECTION 1: PaymentGatewayConfig Table'
PRINT '-------------------------------------'
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PaymentGatewayConfig]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[PaymentGatewayConfig] (
        [gateway_id] INT PRIMARY KEY IDENTITY(1,1),
        [gateway_name] NVARCHAR(50) NOT NULL,
        [is_enabled] BIT NOT NULL DEFAULT 1,
        [display_name] NVARCHAR(100) NOT NULL,
        [display_order] INT NOT NULL DEFAULT 0,
        [min_amount] DECIMAL(18, 2) NOT NULL DEFAULT 10000,
        [max_amount] DECIMAL(18, 2) NOT NULL DEFAULT 50000000,
        [description] NVARCHAR(500) NULL,
        [icon_url] NVARCHAR(MAX) NULL,
        [created_date] DATETIME NOT NULL DEFAULT GETDATE(),
        [updated_date] DATETIME NULL,
        CONSTRAINT [UQ_PaymentGatewayConfig_GatewayName] UNIQUE ([gateway_name])
    );
    PRINT '✓ PaymentGatewayConfig table created successfully.'
END
ELSE
BEGIN
    PRINT '⚠ PaymentGatewayConfig table already exists. Skipping creation.'
END
GO

IF NOT EXISTS (SELECT * FROM [dbo].[PaymentGatewayConfig] WHERE [gateway_name] = 'SePay')
BEGIN
    INSERT INTO [dbo].[PaymentGatewayConfig] 
        ([gateway_name], [is_enabled], [display_name], [display_order], [min_amount], [max_amount], [description])
    VALUES 
        ('SePay', 1, N'SePay - Chuyển khoản ngân hàng', 1, 10000, 50000000, N'Thanh toán qua chuyển khoản ngân hàng với mã QR');
    PRINT '✓ SePay gateway configuration inserted.'
END
ELSE
BEGIN
    PRINT '⚠ SePay gateway configuration already exists.'
END
GO

IF NOT EXISTS (SELECT * FROM [dbo].[PaymentGatewayConfig] WHERE [gateway_name] = 'PayOS')
BEGIN
    INSERT INTO [dbo].[PaymentGatewayConfig] 
        ([gateway_name], [is_enabled], [display_name], [display_order], [min_amount], [max_amount], [description])
    VALUES 
        ('PayOS', 1, N'PayOS - Cổng thanh toán trực tuyến', 2, 10000, 50000000, N'Thanh toán trực tuyến qua thẻ, ví điện tử');
    PRINT '✓ PayOS gateway configuration inserted.'
END
ELSE
BEGIN
    PRINT '⚠ PayOS gateway configuration already exists.'
END
GO

PRINT ''
GO

-- =============================================
-- SECTION 2: Update WalletTransactions Table
-- Step 1: Add Column
-- =============================================
PRINT 'SECTION 2: WalletTransactions Table Update'
PRINT '-------------------------------------------'
GO

IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[WalletTransactions]') 
    AND name = 'payment_gateway'
)
BEGIN
    PRINT 'Adding payment_gateway column to WalletTransactions...'
    
    ALTER TABLE [dbo].[WalletTransactions] 
    ADD [payment_gateway] NVARCHAR(50) NULL;
    
    PRINT '✓ Column payment_gateway added successfully.'
END
ELSE
BEGIN
    PRINT '⚠ Column payment_gateway already exists in WalletTransactions.'
END
GO

-- =============================================
-- SECTION 2: Update WalletTransactions Table
-- Step 2: Update Existing Records (Dynamic SQL)
-- =============================================

-- Update existing records using dynamic SQL to avoid batch compilation errors
DECLARE @UpdateSQL NVARCHAR(MAX);
DECLARE @RowCount INT;

IF EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[WalletTransactions]') 
    AND name = 'payment_gateway'
)
BEGIN
    -- Check if there are NULL values to update
    SET @UpdateSQL = N'SELECT @Count = COUNT(*) FROM [dbo].[WalletTransactions] WHERE [payment_gateway] IS NULL';
    EXEC sp_executesql @UpdateSQL, N'@Count INT OUTPUT', @Count = @RowCount OUTPUT;
    
    IF @RowCount > 0
    BEGIN
        PRINT 'Updating ' + CAST(@RowCount AS NVARCHAR(10)) + ' existing records with default gateway...'
        
        SET @UpdateSQL = N'UPDATE [dbo].[WalletTransactions] SET [payment_gateway] = ''SePay'' WHERE [payment_gateway] IS NULL';
        EXEC sp_executesql @UpdateSQL;
        
        PRINT '✓ Updated records with default gateway (SePay).'
    END
    ELSE
    BEGIN
        PRINT '✓ All records already have a payment gateway assigned.'
    END
END
GO

-- Create index on payment_gateway
IF NOT EXISTS (
    SELECT * FROM sys.indexes 
    WHERE name = 'IX_WalletTransactions_PaymentGateway' 
    AND object_id = OBJECT_ID('[dbo].[WalletTransactions]')
)
BEGIN
    CREATE INDEX [IX_WalletTransactions_PaymentGateway] 
    ON [dbo].[WalletTransactions]([payment_gateway]);
    PRINT '✓ Index IX_WalletTransactions_PaymentGateway created.'
END
ELSE
BEGIN
    PRINT '⚠ Index IX_WalletTransactions_PaymentGateway already exists.'
END
GO

PRINT ''
GO

-- =============================================
-- SECTION 3: Create PayOSTransactions Table
-- =============================================
PRINT 'SECTION 3: PayOSTransactions Table'
PRINT '-----------------------------------'
GO

IF NOT EXISTS (
    SELECT * FROM sys.objects 
    WHERE object_id = OBJECT_ID(N'[dbo].[PayOSTransactions]') 
    AND type in (N'U')
)
BEGIN
    CREATE TABLE [dbo].[PayOSTransactions] (
        [payos_transaction_id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        [wallet_transaction_id] UNIQUEIDENTIFIER NOT NULL,
        [order_code] BIGINT NOT NULL,
        [payment_link_id] NVARCHAR(255) NULL,
        [checkout_url] NVARCHAR(MAX) NULL,
        [payment_status] NVARCHAR(50) NOT NULL,
        [amount] DECIMAL(18, 2) NOT NULL,
        [description] NVARCHAR(500) NULL,
        [return_url] NVARCHAR(MAX) NULL,
        [cancel_url] NVARCHAR(MAX) NULL,
        [created_date] DATETIME NOT NULL DEFAULT GETDATE(),
        [updated_date] DATETIME NULL,
        [paid_at] DATETIME NULL,
        CONSTRAINT [FK_PayOSTransactions_WalletTransactions] 
            FOREIGN KEY ([wallet_transaction_id]) 
            REFERENCES [dbo].[WalletTransactions]([transaction_id])
            ON DELETE CASCADE
    );
    PRINT '✓ PayOSTransactions table created successfully.'
END
ELSE
BEGIN
    PRINT '⚠ PayOSTransactions table already exists. Skipping creation.'
END
GO

PRINT ''
GO

-- =============================================
-- SECTION 4: Create Indexes for PayOSTransactions
-- =============================================
PRINT 'SECTION 4: PayOSTransactions Indexes'
PRINT '-------------------------------------'
GO

IF NOT EXISTS (
    SELECT * FROM sys.indexes 
    WHERE name = 'IX_PayOSTransactions_OrderCode' 
    AND object_id = OBJECT_ID('[dbo].[PayOSTransactions]')
)
BEGIN
    CREATE UNIQUE INDEX [IX_PayOSTransactions_OrderCode] 
    ON [dbo].[PayOSTransactions]([order_code]);
    PRINT '✓ Index IX_PayOSTransactions_OrderCode created.'
END
ELSE
BEGIN
    PRINT '⚠ Index IX_PayOSTransactions_OrderCode already exists.'
END
GO

IF NOT EXISTS (
    SELECT * FROM sys.indexes 
    WHERE name = 'IX_PayOSTransactions_WalletTransactionId' 
    AND object_id = OBJECT_ID('[dbo].[PayOSTransactions]')
)
BEGIN
    CREATE INDEX [IX_PayOSTransactions_WalletTransactionId] 
    ON [dbo].[PayOSTransactions]([wallet_transaction_id]);
    PRINT '✓ Index IX_PayOSTransactions_WalletTransactionId created.'
END
ELSE
BEGIN
    PRINT '⚠ Index IX_PayOSTransactions_WalletTransactionId already exists.'
END
GO

IF NOT EXISTS (
    SELECT * FROM sys.indexes 
    WHERE name = 'IX_PayOSTransactions_PaymentStatus' 
    AND object_id = OBJECT_ID('[dbo].[PayOSTransactions]')
)
BEGIN
    CREATE INDEX [IX_PayOSTransactions_PaymentStatus] 
    ON [dbo].[PayOSTransactions]([payment_status]);
    PRINT '✓ Index IX_PayOSTransactions_PaymentStatus created.'
END
ELSE
BEGIN
    PRINT '⚠ Index IX_PayOSTransactions_PaymentStatus already exists.'
END
GO

IF NOT EXISTS (
    SELECT * FROM sys.indexes 
    WHERE name = 'IX_PayOSTransactions_CreatedDate' 
    AND object_id = OBJECT_ID('[dbo].[PayOSTransactions]')
)
BEGIN
    CREATE INDEX [IX_PayOSTransactions_CreatedDate] 
    ON [dbo].[PayOSTransactions]([created_date] DESC);
    PRINT '✓ Index IX_PayOSTransactions_CreatedDate created.'
END
ELSE
BEGIN
    PRINT '⚠ Index IX_PayOSTransactions_CreatedDate already exists.'
END
GO

PRINT ''
GO

-- =============================================
-- SECTION 5: Verification
-- =============================================
PRINT '========================================='
PRINT 'VERIFICATION RESULTS'
PRINT '========================================='
PRINT ''
GO

-- Check tables existence
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PayOSTransactions]') AND type in (N'U'))
    PRINT '✓ PayOSTransactions table exists'
ELSE
    PRINT '✗ PayOSTransactions table MISSING'
GO

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[WalletTransactions]') AND name = 'payment_gateway')
    PRINT '✓ WalletTransactions.payment_gateway column exists'
ELSE
    PRINT '✗ WalletTransactions.payment_gateway column MISSING'
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PaymentGatewayConfig]') AND type in (N'U'))
    PRINT '✓ PaymentGatewayConfig table exists'
ELSE
    PRINT '✗ PaymentGatewayConfig table MISSING'
GO

PRINT ''
GO

-- Count records
DECLARE @GatewayCount INT = 0;
DECLARE @PayOSCount INT = 0;
DECLARE @WalletCount INT = 0;

SELECT @GatewayCount = COUNT(*) FROM [dbo].[PaymentGatewayConfig];
SELECT @PayOSCount = COUNT(*) FROM [dbo].[PayOSTransactions];

-- Use dynamic SQL for WalletTransactions count to avoid compilation error
DECLARE @CountSQL NVARCHAR(500);
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[WalletTransactions]') AND name = 'payment_gateway')
BEGIN
    SET @CountSQL = N'SELECT @Count = COUNT(*) FROM [dbo].[WalletTransactions] WHERE payment_gateway IS NOT NULL';
    EXEC sp_executesql @CountSQL, N'@Count INT OUTPUT', @Count = @WalletCount OUTPUT;
END

PRINT 'Record Counts:'
PRINT '  - Payment Gateways Configured: ' + CAST(@GatewayCount AS NVARCHAR(10));
PRINT '  - PayOS Transactions: ' + CAST(@PayOSCount AS NVARCHAR(10));
PRINT '  - Wallet Transactions with Gateway: ' + CAST(@WalletCount AS NVARCHAR(10));
PRINT ''
GO

-- List configured gateways
PRINT 'Configured Payment Gateways:'
PRINT '----------------------------'
SELECT 
    [gateway_name] AS [Gateway],
    [display_name] AS [Display Name],
    CASE [is_enabled] WHEN 1 THEN 'Yes' ELSE 'No' END AS [Enabled],
    [min_amount] AS [Min Amount],
    [max_amount] AS [Max Amount],
    [display_order] AS [Order]
FROM [dbo].[PaymentGatewayConfig]
ORDER BY [display_order];
GO

PRINT ''
PRINT '========================================='
PRINT '✓✓✓ Phase 1 Migration Completed! ✓✓✓'
PRINT '========================================='
PRINT 'Next Step: Proceed to Phase 2 (Domain Layer)'
PRINT ''
GO