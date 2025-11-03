-- Notification System SQL Scripts
-- Execute these in SQL Server Management Studio or your database tool

-- ============================================
-- 1. CREATE Notifications TABLE
-- ============================================
CREATE TABLE [dbo].[Notifications]
(
    [NotificationId] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [UserId] UNIQUEIDENTIFIER NOT NULL,
    [ContractId] UNIQUEIDENTIFIER NULL,
    [BookingId] UNIQUEIDENTIFIER NULL,
    [Title] NVARCHAR(255) NOT NULL,
    [Message] NVARCHAR(MAX) NOT NULL,
    [NotificationType] NVARCHAR(50) NOT NULL,
    [Status] NVARCHAR(50) NOT NULL DEFAULT 'Pending',
    [CreatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [SentDate] DATETIME2 NULL,
    [ReadDate] DATETIME2 NULL,
    CONSTRAINT [FK_Notifications_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([UserId]),
    CONSTRAINT [FK_Notifications_Contracts] FOREIGN KEY ([ContractId]) REFERENCES [dbo].[Contracts]([ContractId]),
    CONSTRAINT [FK_Notifications_Bookings] FOREIGN KEY ([BookingId]) REFERENCES [dbo].[Bookings]([BookingId])
);

CREATE INDEX [IX_Notifications_UserId] ON [dbo].[Notifications]([UserId]);
CREATE INDEX [IX_Notifications_Status] ON [dbo].[Notifications]([Status]);
CREATE INDEX [IX_Notifications_CreatedDate] ON [dbo].[Notifications]([CreatedDate]);

GO

-- ============================================
-- 2. CREATE NotificationLogs TABLE
-- ============================================
CREATE TABLE [dbo].[NotificationLogs]
(
    [LogId] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [NotificationId] UNIQUEIDENTIFIER NOT NULL,
    [ContractId] UNIQUEIDENTIFIER NULL,
    [SessionId] UNIQUEIDENTIFIER NULL,
    [Channel] NVARCHAR(50) NOT NULL,
    [Status] NVARCHAR(50) NOT NULL,
    [ErrorMessage] NVARCHAR(MAX) NULL,
    [CreatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [FK_NotificationLogs_Notifications] FOREIGN KEY ([NotificationId]) REFERENCES [dbo].[Notifications]([NotificationId]) ON DELETE CASCADE,
    CONSTRAINT [FK_NotificationLogs_Contracts] FOREIGN KEY ([ContractId]) REFERENCES [dbo].[Contracts]([ContractId]),
    CONSTRAINT [FK_NotificationLogs_Sessions] FOREIGN KEY ([SessionId]) REFERENCES [dbo].[Sessions]([SessionId])
);

CREATE INDEX [IX_NotificationLogs_NotificationId] ON [dbo].[NotificationLogs]([NotificationId]);
CREATE INDEX [IX_NotificationLogs_CreatedDate] ON [dbo].[NotificationLogs]([CreatedDate]);

GO

-- ============================================
-- 3. CREATE NotificationTemplates TABLE
-- ============================================
CREATE TABLE [dbo].[NotificationTemplates]
(
    [TemplateId] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [Name] NVARCHAR(255) NOT NULL,
    [Subject] NVARCHAR(255) NOT NULL,
    [Body] NVARCHAR(MAX) NOT NULL,
    [NotificationType] NVARCHAR(50) NOT NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [CreatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedDate] DATETIME2 NULL
);

CREATE INDEX [IX_NotificationTemplates_NotificationType] ON [dbo].[NotificationTemplates]([NotificationType]);

GO

-- ============================================
-- 4. CREATE NotificationPreferences TABLE
-- ============================================
CREATE TABLE [dbo].[NotificationPreferences]
(
    [PreferenceId] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [UserId] UNIQUEIDENTIFIER NOT NULL UNIQUE,
    [ReceiveEmailNotifications] BIT NOT NULL DEFAULT 1,
    [ReceiveSMSNotifications] BIT NOT NULL DEFAULT 0,
    [ReceiveWebNotifications] BIT NOT NULL DEFAULT 1,
    [ReceiveSessionReminders] BIT NOT NULL DEFAULT 1,
    [ReceiveContractUpdates] BIT NOT NULL DEFAULT 1,
    [ReceivePaymentNotifications] BIT NOT NULL DEFAULT 1,
    [CreatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedDate] DATETIME2 NULL,
    CONSTRAINT [FK_NotificationPreferences_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([UserId]) ON DELETE CASCADE
);

CREATE INDEX [IX_NotificationPreferences_UserId] ON [dbo].[NotificationPreferences]([UserId]);

GO

-- ============================================
-- 5. ADD Navigation Properties (Update Users Table)
-- ============================================
-- Note: If these columns don't already exist, uncomment below
-- ALTER TABLE [dbo].[Users] ADD NotificationPreferenceId UNIQUEIDENTIFIER NULL;
-- ALTER TABLE [dbo].[Users] ADD CONSTRAINT [FK_Users_NotificationPreferences] FOREIGN KEY (NotificationPreferenceId) REFERENCES [dbo].[NotificationPreferences]([PreferenceId]);

GO

-- ============================================
-- 6. VERIFICATION SCRIPT (Run to verify tables)
-- ============================================
SELECT 
    'Notifications' AS TableName, COUNT(*) AS RowCount FROM [dbo].[Notifications]
UNION ALL
SELECT 
    'NotificationLogs' AS TableName, COUNT(*) AS RowCount FROM [dbo].[NotificationLogs]
UNION ALL
SELECT 
    'NotificationTemplates' AS TableName, COUNT(*) AS RowCount FROM [dbo].[NotificationTemplates]
UNION ALL
SELECT 
    'NotificationPreferences' AS TableName, COUNT(*) AS RowCount FROM [dbo].[NotificationPreferences];

GO

-- ============================================
-- 7. SAMPLE DATA (Optional - for testing)
-- ============================================
-- INSERT INTO [dbo].[NotificationTemplates] 
-- ([TemplateId], [Name], [Subject], [Body], [NotificationType], [IsActive])
-- VALUES
-- (NEWID(), 'Session Reminder 24h', 'Upcoming Session Reminder', 'Your session is scheduled for tomorrow', 'SESSION_REMINDER_24H', 1),
-- (NEWID(), 'Session Reminder 1h', 'Session Starting Soon', 'Your session starts in 1 hour', 'SESSION_REMINDER_1H', 1),
-- (NEWID(), 'Payment Confirmation', 'Payment Received', 'Your payment has been processed successfully', 'PAYMENT_CONFIRMATION', 1),
-- (NEWID(), 'Booking Confirmed', 'Booking Confirmation', 'Your booking has been confirmed', 'BOOKING_CONFIRMATION', 1);

GO

-- ============================================
-- 8. DROP TABLES SCRIPT (If needed for cleanup)
-- ============================================
-- WARNING: Uncomment only if you need to delete these tables
-- DROP TABLE [dbo].[NotificationLogs];
-- DROP TABLE [dbo].[Notifications];
-- DROP TABLE [dbo].[NotificationTemplates];
-- DROP TABLE [dbo].[NotificationPreferences];

GO
