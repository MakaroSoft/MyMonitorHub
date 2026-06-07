-- =============================================================================
-- 0003_SeedConfiguration.sql
-- Seeds the Configuration table with the default account configuration.
-- =============================================================================

IF NOT EXISTS (SELECT 1 FROM [Configuration] WHERE [accountId] = 1001)
BEGIN
    INSERT INTO [Configuration] ([accountId], [healthResponseMinutes], [healthResponseAlertMinutes], [srNotAcceptedAlertMinutes])
    VALUES (1001, 10, NULL, 10);
END
