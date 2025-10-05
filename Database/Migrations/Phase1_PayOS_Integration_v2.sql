-- =============================================
-- PayOS Enhanced Multi-Gateway Integration
-- Phase 1: Database Changes - REVISED VERSION
-- Date: October 5, 2025
-- Description: Creates PayOSTransactions table, updates WalletTransactions,
--              and creates PaymentGatewayConfig table
-- NOTES: Execute each section separately if needed
-- =============================================

-- Change to your database name if different
USE [MathBridgeDB]
GO

SET NOCOUNT ON;
GO

PRINT '========================================='
PRINT 'Starting Phase 1 Database Migration'
PRINT 'Date: ' + CONVERT(VARCHAR, GETDATE(), 120)
PRINT '========================================='
PRINT ''
GO

-- =============================================
-- SECTION 1: Create PaymentGatewayConfig Table
-- (Creating this first as it has no dependencies)
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

-- Insert initial gateway configurations
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
-- =============================================
PRINT 'SECTION 2: WalletTransactions Table Update'
PRINT '-------------------------------------------'
GO

-- Check if column exists first
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
    
    -- Wait a moment for schema update
    WAITFOR DELAY '00:00:01';
    
    -- Now update existing records
    PRINT 'Updating existing records with default gateway...'
    
    UPDATE [dbo].[WalletTransactions] 
    SET [payment_gateway] = 'SePay' 
    WHERE [payment_gateway] IS NULL;
    
    DECLARE @UpdatedRows INT = @@ROWCOUNT;
    PRINT '✓ Updated ' + CAST(@UpdatedRows AS NVARCHAR(10)) + ' existing records with default gateway (SePay).'
END
ELSE
BEGIN
    PRINT '⚠ Column payment_gateway already exists in WalletTransactions.'
    
    -- Still update NULL values if any exist
    IF EXISTS (SELECT * FROM [dbo].[WalletTransactions] WHERE [payment_gateway] IS NULL)
    BEGIN
        UPDATE [dbo].[WalletTransactions] 
        SET [payment_gateway] = 'SePay' 
        WHERE [payment_gateway] IS NULL;
        
        DECLARE @UpdatedRows2 INT = @@ROWCOUNT;
        PRINT '✓ Updated ' + CAST(@UpdatedRows2 AS NVARCHAR(10)) + ' NULL records with default gateway.'
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

-- Unique index on OrderCode
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

-- Index on WalletTransactionId
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

-- Index on PaymentStatus
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

-- Index on CreatedDate (descending for recent queries)
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

-- Verify tables exist
DECLARE @VerificationResults TABLE (
    CheckItem NVARCHAR(100),
    Status NVARCHAR(10)
);

INSERT INTO @VerificationResults (CheckItem, Status)
SELECT 'PayOSTransactions table', 
       CASE WHEN EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PayOSTransactions]') AND type in (N'U'))
            THEN '✓ EXISTS' ELSE '✗ MISSING' END;

INSERT INTO @VerificationResults (CheckItem, Status)
SELECT 'WalletTransactions.payment_gateway column', 
       CASE WHEN EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[WalletTransactions]') AND name = 'payment_gateway')
            THEN '✓ EXISTS' ELSE '✗ MISSING' END;

INSERT INTO @VerificationResults (CheckItem, Status)
SELECT 'PaymentGatewayConfig table', 
       CASE WHEN EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PaymentGatewayConfig]') AND type in (N'U'))
            THEN '✓ EXISTS' ELSE '✗ MISSING' END;

-- Display verification results
SELECT CheckItem AS [Check Item], Status FROM @VerificationResults;
PRINT ''
GO

-- Count records
DECLARE @GatewayCount INT, @PayOSCount INT, @WalletCount INT;

SELECT @GatewayCount = COUNT(*) FROM [dbo].[PaymentGatewayConfig];
SELECT @PayOSCount = COUNT(*) FROM [dbo].[PayOSTransactions];
SELECT @WalletCount = COUNT(*) FROM [dbo].[WalletTransactions] WHERE payment_gateway IS NOT NULL;

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
PRINT '✓ Phase 1 Database Migration Completed!'
PRINT '========================================='
PRINT 'Next Step: Proceed to Phase 2 (Domain Layer)'
GO