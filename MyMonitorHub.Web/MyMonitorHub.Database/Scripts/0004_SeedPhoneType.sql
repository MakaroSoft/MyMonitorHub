-- =============================================================================
-- 0004_SeedPhoneType.sql
-- Seeds the PhoneType table with standard phone type options.
-- =============================================================================

IF NOT EXISTS (SELECT 1 FROM [PhoneType] WHERE [PhoneTypeId] = 1)
BEGIN
    SET IDENTITY_INSERT [PhoneType] ON;

    INSERT INTO [PhoneType] ([PhoneTypeId], [Name]) VALUES (1, 'Mobile');
    INSERT INTO [PhoneType] ([PhoneTypeId], [Name]) VALUES (2, 'Home');
    INSERT INTO [PhoneType] ([PhoneTypeId], [Name]) VALUES (3, 'Work');

    SET IDENTITY_INSERT [PhoneType] OFF;
END
