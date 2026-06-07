-- =============================================================================
-- 0002_SeedAccount.sql
-- Seeds the Account table with the default MyMonitorHub account.
-- =============================================================================

IF NOT EXISTS (SELECT 1 FROM [Account] WHERE [AccountId] = 1001)
BEGIN
    SET IDENTITY_INSERT [Account] ON;

    INSERT INTO [Account] ([AccountId], [Identification], [Description], [Rules])
    VALUES (1001, N'MyMonitorHub', N'My Monitor Hub', N'user change@me.com gets alerts');

    SET IDENTITY_INSERT [Account] OFF;
END
