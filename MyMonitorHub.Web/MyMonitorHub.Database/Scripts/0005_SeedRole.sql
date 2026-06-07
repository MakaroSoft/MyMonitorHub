-- =============================================================================
-- 0005_SeedRole.sql
-- Seeds the Role table with system-level roles (AccountId = NULL = system-wide).
-- =============================================================================

IF NOT EXISTS (SELECT 1 FROM [Role] WHERE [RoleId] = 1000)
BEGIN
    SET IDENTITY_INSERT [Role] ON;

    INSERT INTO [Role] ([RoleId], [AccountId], [RoleCode], [RoleXML], [Description])
    VALUES (1000, NULL, N'Monitor',
        N'<?xml version="1.0"?><pages><page code="RoleMaint"><access code="view" /></page><page code="Users"><access code="viewAnyone" /></page><page code="ConfigMaint"><access code="view" /></page><page code="PageMaint"><access code="view" /></page><page code="DeviceGroupMaint"><access code="view" /></page><page code="DeviceMaint"><access code="view" /></page><page code="DisplayPage"><access code="view" /><access code="clear" /></page><page code="Downloads"><access code="view" /></page><page code="Rules"><access code="view" /></page><page code="ServReq"><access code="view" /><access code="edit" /></page><page code="General"><access code="alerts" /></page><page code="DeviceDetail"><access code="CanStopStartServices" /></page></pages>',
        N'Can accept and work on requests');

    INSERT INTO [Role] ([RoleId], [AccountId], [RoleCode], [RoleXML], [Description])
    VALUES (1001, NULL, N'Guest',
        N'<?xml version=''1.0''?><pages><page code="DisplayPage"><access code="view" /></page><page code="ServReq"><access code="view" /></page></pages>',
        N'Can look at account but not change anything');

    INSERT INTO [Role] ([RoleId], [AccountId], [RoleCode], [RoleXML], [Description])
    VALUES (1002, NULL, N'Administrator', NULL, N'God mode');

    INSERT INTO [Role] ([RoleId], [AccountId], [RoleCode], [RoleXML], [Description])
    VALUES (1003, NULL, N'Owner', NULL, N'You can do anything in your account');

    INSERT INTO [Role] ([RoleId], [AccountId], [RoleCode], [RoleXML], [Description])
    VALUES (1004, NULL, N'NoAccess',
        N'<?xml version=''1.0''?><pages></pages>',
        N'No Access Yet');

    SET IDENTITY_INSERT [Role] OFF;
END
