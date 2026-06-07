-- =============================================================================
-- 0006_SeedPage.sql
-- Seeds the Page table with the default page for the MyMonitorHub account.
-- =============================================================================

IF NOT EXISTS (SELECT 1 FROM [Page] WHERE [AccountId] = 1001)
BEGIN
    INSERT INTO [Page] ([ParentId], [Description], [ChildIndex], [AccountId])
    VALUES (NULL, N'My Monitor Page', 0, 1001);
END
