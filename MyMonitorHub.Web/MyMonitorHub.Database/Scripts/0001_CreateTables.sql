-- =============================================================================
-- 0001_CreateTables.sql
-- Creates all application tables. Tables are ordered so that referenced tables
-- are always created before the tables that reference them.
-- =============================================================================

-- ----------------------------------------------------------------------------
-- User
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'User' AND type = 'U')
BEGIN
    CREATE TABLE [User] (
        [UserId]             INT            NOT NULL IDENTITY(1,1),
        [Password]           NVARCHAR(256)  NULL,
        [FirstName]          NVARCHAR(50)   NULL,
        [LastName]           NVARCHAR(50)   NULL,
        [HomePhone]          NVARCHAR(50)   NULL,
        [WorkPhone]          NVARCHAR(50)   NULL,
        [CellPhone]          NVARCHAR(50)   NULL,
        [Email]              NVARCHAR(50)   NULL,
        [HomePage]           NVARCHAR(200)  NULL,
        [TwoFactorSecret]    NVARCHAR(MAX)  NULL,
        [TwoFactorEnabled]   BIT            NULL,
        CONSTRAINT [PK_User] PRIMARY KEY ([UserId])
    );
END

-- ----------------------------------------------------------------------------
-- Account
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Account' AND type = 'U')
BEGIN
    CREATE TABLE [Account] (
        [AccountId]      INT            NOT NULL IDENTITY(1,1),
        [Identification] NVARCHAR(50)   NOT NULL,
        [Description]    NVARCHAR(50)   NOT NULL,
        [Rules]          NVARCHAR(MAX)  NULL,
        CONSTRAINT [PK_Account] PRIMARY KEY ([AccountId])
    );
END

-- ----------------------------------------------------------------------------
-- PhoneType
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PhoneType' AND type = 'U')
BEGIN
    CREATE TABLE [PhoneType] (
        [PhoneTypeId] INT          NOT NULL IDENTITY(1,1),
        [Name]        VARCHAR(10)  NOT NULL,
        CONSTRAINT [PK_PhoneType] PRIMARY KEY ([PhoneTypeId])
    );
END

-- ----------------------------------------------------------------------------
-- DeviceType
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'DeviceType' AND type = 'U')
BEGIN
    CREATE TABLE [DeviceType] (
        [DeviceTypeId] INT           NOT NULL IDENTITY(1,1),
        [Name]         NVARCHAR(50)  NOT NULL,
        CONSTRAINT [PK_DeviceType] PRIMARY KEY ([DeviceTypeId])
    );
END

-- ----------------------------------------------------------------------------
-- Field
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Field' AND type = 'U')
BEGIN
    CREATE TABLE [Field] (
        [FieldId]   INT           NOT NULL IDENTITY(1,1),
        [FieldName] NVARCHAR(50)  NOT NULL,
        CONSTRAINT [PK_Field] PRIMARY KEY ([FieldId])
    );
END

-- ----------------------------------------------------------------------------
-- Role  (depends on: Account)
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Role' AND type = 'U')
BEGIN
    CREATE TABLE [Role] (
        [RoleId]      INT           NOT NULL IDENTITY(1,1),
        [AccountId]   INT           NULL,
        [RoleCode]    NVARCHAR(50)  NOT NULL,
        [RoleXML]     NVARCHAR(MAX) NULL,
        [Description] NVARCHAR(50)  NOT NULL,
        CONSTRAINT [PK_Role] PRIMARY KEY ([RoleId]),
        CONSTRAINT [FK_Role_Account] FOREIGN KEY ([AccountId]) REFERENCES [Account] ([AccountId])
    );
END

-- ----------------------------------------------------------------------------
-- Page  (depends on: Account)
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Page' AND type = 'U')
BEGIN
    CREATE TABLE [Page] (
        [PageId]      INT           NOT NULL IDENTITY(1,1),
        [ParentId]    INT           NULL,
        [Description] NVARCHAR(50)  NOT NULL,
        [ChildIndex]  INT           NOT NULL,
        [AccountId]   INT           NOT NULL,
        CONSTRAINT [PK_Page] PRIMARY KEY ([PageId]),
        CONSTRAINT [FK_Page_Account] FOREIGN KEY ([AccountId]) REFERENCES [Account] ([AccountId])
    );
END

-- ----------------------------------------------------------------------------
-- DeviceGroup  (depends on: Page, Account)
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'DeviceGroup' AND type = 'U')
BEGIN
    CREATE TABLE [DeviceGroup] (
        [DeviceGroupId]      INT            NOT NULL IDENTITY(1,1),
        [PageId]             INT            NOT NULL,
        [ChildIndex]         INT            NOT NULL,
        [AccountId]          INT            NOT NULL,
        [Description]        NVARCHAR(50)   NOT NULL,
        [emailsForClosedSRs] NVARCHAR(250)  NULL,
        [Notes]              NVARCHAR(MAX)  NULL,
        [ContactInformation] NVARCHAR(MAX)  NULL,
        CONSTRAINT [PK_DeviceGroup] PRIMARY KEY ([DeviceGroupId]),
        CONSTRAINT [FK_DeviceGroup_Page] FOREIGN KEY ([PageId]) REFERENCES [Page] ([PageId])
    );
END

-- ----------------------------------------------------------------------------
-- Contact  (depends on: DeviceGroup, PhoneType)
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Contact' AND type = 'U')
BEGIN
    CREATE TABLE [Contact] (
        [ContactId]    INT           NOT NULL IDENTITY(1,1),
        [Name]         VARCHAR(50)   NOT NULL,
        [Phone1]       VARCHAR(20)   NOT NULL,
        [PhoneType1Id] INT           NOT NULL,
        [Phone2]       VARCHAR(20)   NULL,
        [PhoneType2Id] INT           NULL,
        [Phone3]       VARCHAR(20)   NULL,
        [PhoneType3Id] INT           NULL,
        [Notes]        VARCHAR(1000) NULL,
        [DeviceGroupId] INT          NOT NULL,
        CONSTRAINT [PK_Contact] PRIMARY KEY ([ContactId]),
        CONSTRAINT [FK_Contact_DeviceGroup]  FOREIGN KEY ([DeviceGroupId])  REFERENCES [DeviceGroup] ([DeviceGroupId]),
        CONSTRAINT [FK_Contact_PhoneType1]   FOREIGN KEY ([PhoneType1Id])   REFERENCES [PhoneType] ([PhoneTypeId]),
        CONSTRAINT [FK_Contact_PhoneType2]   FOREIGN KEY ([PhoneType2Id])   REFERENCES [PhoneType] ([PhoneTypeId]),
        CONSTRAINT [FK_Contact_PhoneType3]   FOREIGN KEY ([PhoneType3Id])   REFERENCES [PhoneType] ([PhoneTypeId])
    );
END

-- ----------------------------------------------------------------------------
-- Device  (depends on: DeviceGroup, DeviceType, Account)
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Device' AND type = 'U')
BEGIN
    CREATE TABLE [Device] (
        [DeviceId]           INT            NOT NULL IDENTITY(1,1),
        [ApiKey]             NVARCHAR(36)   NULL,
        [DeviceGroupId]      INT            NULL,
        [Description]        NVARCHAR(50)   NOT NULL,
        [ChildIndex]         INT            NOT NULL,
        [AccountId]          INT            NOT NULL,
        [DeviceTypeId]       INT            NULL,
        [Deleted]            BIT            NOT NULL,
        [WebUrl]             NVARCHAR(MAX)  NULL,
        [Notes]              NVARCHAR(MAX)  NULL,
        [ContactInformation] NVARCHAR(MAX)  NULL,
        [PauseUntilDateTime] DATETIME2      NULL,
        [ConfigXml]          NVARCHAR(MAX)  NULL,
        CONSTRAINT [PK_Device] PRIMARY KEY ([DeviceId]),
        CONSTRAINT [FK_Device_DeviceGroup] FOREIGN KEY ([DeviceGroupId]) REFERENCES [DeviceGroup] ([DeviceGroupId]),
        CONSTRAINT [FK_Device_DeviceType]  FOREIGN KEY ([DeviceTypeId])  REFERENCES [DeviceType] ([DeviceTypeId])
    );
END

-- ----------------------------------------------------------------------------
-- Category  (depends on: Account)
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Category' AND type = 'U')
BEGIN
    CREATE TABLE [Category] (
        [CategoryId]  INT           NOT NULL IDENTITY(1,1),
        [AccountId]   INT           NOT NULL,
        [Description] NVARCHAR(50)  NOT NULL,
        CONSTRAINT [PK_Category] PRIMARY KEY ([CategoryId]),
        CONSTRAINT [FK_Category_Account] FOREIGN KEY ([AccountId]) REFERENCES [Account] ([AccountId])
    );
END

-- ----------------------------------------------------------------------------
-- NoteGroupTemplate  (depends on: Account)
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'NoteGroupTemplate' AND type = 'U')
BEGIN
    CREATE TABLE [NoteGroupTemplate] (
        [NoteGroupTemplateId] INT           NOT NULL IDENTITY(1,1),
        [Description]         NVARCHAR(50)  NOT NULL,
        [Inherits]            INT           NULL,
        [AccountId]           INT           NOT NULL,
        CONSTRAINT [PK_NoteGroupTemplate] PRIMARY KEY ([NoteGroupTemplateId]),
        CONSTRAINT [FK_NoteGroupTemplate_Account] FOREIGN KEY ([AccountId]) REFERENCES [Account] ([AccountId])
    );
END

-- ----------------------------------------------------------------------------
-- Member  (depends on: Account, Role, User)
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Member' AND type = 'U')
BEGIN
    CREATE TABLE [Member] (
        [MemberId]            INT           NOT NULL IDENTITY(1,1),
        [OrganizationId]      INT           NOT NULL,
        [UserId]              INT           NOT NULL,
        [RoleId]              INT           NOT NULL,
        [SendAlertsTo]        NVARCHAR(50)  NULL,
        [DefaultOrganization] BIT           NOT NULL,
        CONSTRAINT [PK_Member] PRIMARY KEY ([MemberId]),
        CONSTRAINT [FK_Member_Account] FOREIGN KEY ([OrganizationId]) REFERENCES [Account] ([AccountId]),
        CONSTRAINT [FK_Member_Role]    FOREIGN KEY ([RoleId])          REFERENCES [Role] ([RoleId]),
        CONSTRAINT [FK_Member_User]    FOREIGN KEY ([UserId])          REFERENCES [User] ([UserId])
    );
END

-- ----------------------------------------------------------------------------
-- Configuration  (depends on: Account)
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Configuration' AND type = 'U')
BEGIN
    CREATE TABLE [Configuration] (
        [configurationId]              INT  NOT NULL IDENTITY(1,1),
        [accountId]                    INT  NOT NULL,
        [healthResponseMinutes]        INT  NOT NULL,
        [healthResponseAlertMinutes]   INT  NULL,
        [srNotAcceptedAlertMinutes]    INT  NULL,
        CONSTRAINT [PK_Configuration] PRIMARY KEY ([configurationId]),
        CONSTRAINT [FK_Configuration_Account] FOREIGN KEY ([accountId]) REFERENCES [Account] ([AccountId])
    );
END

-- ----------------------------------------------------------------------------
-- EmailNotification  (depends on: User)
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'EmailNotification' AND type = 'U')
BEGIN
    CREATE TABLE [EmailNotification] (
        [EmailNotificationId] INT           NOT NULL IDENTITY(1,1),
        [UserId]              INT           NOT NULL,
        [Email]               NVARCHAR(MAX) NULL,
        [Disabled]            BIT           NULL,
        CONSTRAINT [PK_EmailNotification] PRIMARY KEY ([EmailNotificationId]),
        CONSTRAINT [FK_EmailNotification_User] FOREIGN KEY ([UserId]) REFERENCES [User] ([UserId])
    );
END

-- ----------------------------------------------------------------------------
-- ServiceRequest  (depends on: Device, Account, User)
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ServiceRequest' AND type = 'U')
BEGIN
    CREATE TABLE [ServiceRequest] (
        [ServiceRequestId] INT           NOT NULL IDENTITY(1,1),
        [AccountId]        INT           NOT NULL,
        [DeviceId]         INT           NOT NULL,
        [Notes]            NVARCHAR(MAX) NOT NULL,
        [AssignedToId]     INT           NULL,
        [TimeStamp]        DATETIME2     NOT NULL,
        [Status]           NVARCHAR(1)   NOT NULL,
        [LastAlertTime]    DATETIME2     NULL,
        CONSTRAINT [PK_ServiceRequest] PRIMARY KEY ([ServiceRequestId]),
        CONSTRAINT [FK_ServiceRequest_Device] FOREIGN KEY ([DeviceId])     REFERENCES [Device] ([DeviceId]),
        CONSTRAINT [FK_ServiceRequest_User]   FOREIGN KEY ([AssignedToId]) REFERENCES [User] ([UserId])
    );
END

-- ----------------------------------------------------------------------------
-- Item  (depends on: Category, Device, ServiceRequest)
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Item' AND type = 'U')
BEGIN
    CREATE TABLE [Item] (
        [ItemId]                INT           NOT NULL IDENTITY(1,1),
        [CategoryId]            INT           NOT NULL,
        [SubCategoryName]       NVARCHAR(50)  NOT NULL,
        [Description]           NVARCHAR(50)  NOT NULL,
        [EventId]               INT           NULL,
        [Status]                INT           NOT NULL,
        [StatusDescription]     NVARCHAR(50)  NOT NULL,
        [TimeStamp]             DATETIME2     NOT NULL,
        [DeviceId]              INT           NOT NULL,
        [LastServiceRequestId]  INT           NULL,
        [AccountId]             INT           NOT NULL,
        [ConstantlyReportsInYN] BIT           NOT NULL,
        CONSTRAINT [PK_Item] PRIMARY KEY ([ItemId]),
        CONSTRAINT [FK_Item_Category]       FOREIGN KEY ([CategoryId])           REFERENCES [Category] ([CategoryId]),
        CONSTRAINT [FK_Item_Device]         FOREIGN KEY ([DeviceId])             REFERENCES [Device] ([DeviceId]),
        CONSTRAINT [FK_Item_ServiceRequest] FOREIGN KEY ([LastServiceRequestId]) REFERENCES [ServiceRequest] ([ServiceRequestId])
    );
END

-- ----------------------------------------------------------------------------
-- Event  (depends on: ServiceRequest)
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Event' AND type = 'U')
BEGIN
    CREATE TABLE [Event] (
        [EventId]                   INT            NOT NULL IDENTITY(1,1),
        [AccountId]                 INT            NOT NULL,
        [DeviceId]                  INT            NOT NULL,
        [Category]                  NVARCHAR(20)   NOT NULL,
        [SubCategory]               NVARCHAR(20)   NULL,
        [ItemName]                  NVARCHAR(50)   NULL,
        [Status]                    INT            NULL,
        [StatusDescription]         NVARCHAR(MAX)  NOT NULL,
        [ClientReceivedTimeStamp]   DATETIME2      NOT NULL,
        [ServerReceivedTimeStamp]   DATETIME2      NOT NULL,
        [ServiceRequestId]          INT            NULL,
        CONSTRAINT [PK_Event] PRIMARY KEY ([EventId]),
        CONSTRAINT [FK_Event_ServiceRequest] FOREIGN KEY ([ServiceRequestId]) REFERENCES [ServiceRequest] ([ServiceRequestId])
    );
END

-- ----------------------------------------------------------------------------
-- NoteGroup  (depends on: DeviceGroup, NoteGroupTemplate)
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'NoteGroup' AND type = 'U')
BEGIN
    CREATE TABLE [NoteGroup] (
        [NoteGroupId]         INT           NOT NULL IDENTITY(1,1),
        [DeviceGroupId]       INT           NOT NULL,
        [NoteGroupTemplateId] INT           NOT NULL,
        [Description]         NVARCHAR(50)  NOT NULL,
        CONSTRAINT [PK_NoteGroup] PRIMARY KEY ([NoteGroupId]),
        CONSTRAINT [FK_NoteGroup_DeviceGroup]       FOREIGN KEY ([DeviceGroupId])       REFERENCES [DeviceGroup] ([DeviceGroupId]),
        CONSTRAINT [FK_NoteGroup_NoteGroupTemplate] FOREIGN KEY ([NoteGroupTemplateId]) REFERENCES [NoteGroupTemplate] ([NoteGroupTemplateId])
    );
END

-- ----------------------------------------------------------------------------
-- FieldsForNoteGroupTemplate  (depends on: Field, NoteGroupTemplate)
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'FieldsForNoteGroupTemplate' AND type = 'U')
BEGIN
    CREATE TABLE [FieldsForNoteGroupTemplate] (
        [FieldsForNoteGroupId]  INT  NOT NULL IDENTITY(1,1),
        [NoteGroupTemplateId]   INT  NOT NULL,
        [FieldId]               INT  NOT NULL,
        CONSTRAINT [PK_FieldsForNoteGroupTemplate] PRIMARY KEY ([FieldsForNoteGroupId]),
        CONSTRAINT [FK_FieldsForNoteGroupTemplate_NoteGroupTemplate] FOREIGN KEY ([NoteGroupTemplateId]) REFERENCES [NoteGroupTemplate] ([NoteGroupTemplateId]),
        CONSTRAINT [FK_FieldsForNoteGroupTemplate_Field]             FOREIGN KEY ([FieldId])             REFERENCES [Field] ([FieldId])
    );
END

-- ----------------------------------------------------------------------------
-- ValuesForNoteGroup  (depends on: NoteGroup, Field)
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ValuesForNoteGroup' AND type = 'U')
BEGIN
    CREATE TABLE [ValuesForNoteGroup] (
        [ValuesForNoteGroupId] INT            NOT NULL IDENTITY(1,1),
        [NoteGroupId]          INT            NOT NULL,
        [Value]                NVARCHAR(200)  NOT NULL,
        [FieldId]              INT            NOT NULL,
        CONSTRAINT [PK_ValuesForNoteGroup] PRIMARY KEY ([ValuesForNoteGroupId]),
        CONSTRAINT [FK_ValuesForNoteGroup_NoteGroup] FOREIGN KEY ([NoteGroupId]) REFERENCES [NoteGroup] ([NoteGroupId]),
        CONSTRAINT [FK_ValuesForNoteGroup_Field]     FOREIGN KEY ([FieldId])     REFERENCES [Field] ([FieldId])
    );
END

-- ----------------------------------------------------------------------------
-- CpuUsage  (depends on: Item)
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CpuUsage' AND type = 'U')
BEGIN
    CREATE TABLE [CpuUsage] (
        [CpuUsageId] INT            NOT NULL IDENTITY(1,1),
        [ItemId]     INT            NOT NULL,
        [Timestamp]  DATETIME2      NOT NULL,
        [JsonData]   NVARCHAR(MAX)  NULL,
        CONSTRAINT [PK_CpuUsage] PRIMARY KEY ([CpuUsageId]),
        CONSTRAINT [FK_CpuUsage_Item] FOREIGN KEY ([ItemId]) REFERENCES [Item] ([ItemId])
    );
END

-- ----------------------------------------------------------------------------
-- DiskUsage  (depends on: Item)
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'DiskUsage' AND type = 'U')
BEGIN
    CREATE TABLE [DiskUsage] (
        [DiskUsageId] INT        NOT NULL IDENTITY(1,1),
        [ItemId]      INT        NOT NULL,
        [Timestamp]   DATETIME2  NOT NULL,
        [DiskSize]    BIGINT     NULL,
        [CurUsedMB]   BIGINT     NULL,
        [MinUsedMB]   BIGINT     NULL,
        [MaxUsedMB]   BIGINT     NULL,
        [AvgUsedMB]   BIGINT     NULL,
        CONSTRAINT [PK_DiskUsage] PRIMARY KEY ([DiskUsageId]),
        CONSTRAINT [FK_DiskUsage_Item] FOREIGN KEY ([ItemId]) REFERENCES [Item] ([ItemId])
    );
END

-- ----------------------------------------------------------------------------
-- UrlSpeed  (depends on: Item)
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'UrlSpeed' AND type = 'U')
BEGIN
    CREATE TABLE [UrlSpeed] (
        [UrlSpeedId]  INT        NOT NULL IDENTITY(1,1),
        [ItemId]      INT        NOT NULL,
        [Timestamp]   DATETIME2  NOT NULL,
        [CurSpeedMS]  INT        NULL,
        [MinSpeedMS]  INT        NULL,
        [MaxSpeedMS]  INT        NULL,
        [AvgSpeedMS]  INT        NULL,
        CONSTRAINT [PK_UrlSpeed] PRIMARY KEY ([UrlSpeedId]),
        CONSTRAINT [FK_UrlSpeed_Item] FOREIGN KEY ([ItemId]) REFERENCES [Item] ([ItemId])
    );
END

-- ----------------------------------------------------------------------------
-- Error  (depends on: Item)
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Error' AND type = 'U')
BEGIN
    CREATE TABLE [Error] (
        [ErrorId]       INT        NOT NULL IDENTITY(1,1),
        [ItemId]        INT        NOT NULL,
        [Timestamp]     DATETIME2  NOT NULL,
        [NewErrorCount] INT        NULL,
        CONSTRAINT [PK_Error] PRIMARY KEY ([ErrorId]),
        CONSTRAINT [FK_Error_Item] FOREIGN KEY ([ItemId]) REFERENCES [Item] ([ItemId])
    );
END

-- ----------------------------------------------------------------------------
-- DeviceRefreshTokens  (depends on: Device)
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'DeviceRefreshTokens' AND type = 'U')
BEGIN
    CREATE TABLE [DeviceRefreshTokens] (
        [Id]        INT           NOT NULL IDENTITY(1,1),
        [DeviceId]  INT           NOT NULL,
        [AccountId] INT           NOT NULL,
        [TokenHash] NVARCHAR(64)  NOT NULL,
        [FamilyId]  UNIQUEIDENTIFIER NOT NULL,
        [IssuedAt]  DATETIME2     NOT NULL,
        [ExpiresAt] DATETIME2     NOT NULL,
        [Revoked]   BIT           NOT NULL,
        CONSTRAINT [PK_DeviceRefreshTokens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DeviceRefreshTokens_Device] FOREIGN KEY ([DeviceId]) REFERENCES [Device] ([DeviceId])
    );
    CREATE INDEX [IX_DeviceRefreshTokens_TokenHash] ON [DeviceRefreshTokens] ([TokenHash]);
    CREATE INDEX [IX_DeviceRefreshTokens_DeviceId]  ON [DeviceRefreshTokens] ([DeviceId]);
END

-- ----------------------------------------------------------------------------
-- UserRecoveryCodes  (depends on: User)
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'UserRecoveryCodes' AND type = 'U')
BEGIN
    CREATE TABLE [UserRecoveryCodes] (
        [UserRecoveryCodeId] INT           NOT NULL IDENTITY(1,1),
        [UserId]             INT           NOT NULL,
        [CodeHash]           NVARCHAR(128) NOT NULL,
        [UsedAt]             DATETIME2     NULL,
        CONSTRAINT [PK_UserRecoveryCodes] PRIMARY KEY ([UserRecoveryCodeId]),
        CONSTRAINT [FK_UserRecoveryCodes_User] FOREIGN KEY ([UserId]) REFERENCES [User] ([UserId])
    );
    CREATE INDEX [IX_UserRecoveryCodes_UserId]   ON [UserRecoveryCodes] ([UserId]);
    CREATE UNIQUE INDEX [IX_UserRecoveryCodes_CodeHash] ON [UserRecoveryCodes] ([CodeHash]);
END

-- ----------------------------------------------------------------------------
-- MonthlyReport  (depends on: Account, DeviceGroup)
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'MonthlyReport' AND type = 'U')
BEGIN
    CREATE TABLE [MonthlyReport] (
        [MonthlyReportId] INT  NOT NULL IDENTITY(1,1),
        [AccountId]       INT  NOT NULL,
        [DeviceGroupId]   INT  NOT NULL,
        [Year]            INT  NOT NULL,
        [Month]           INT  NOT NULL,
        CONSTRAINT [PK_MonthlyReport] PRIMARY KEY ([MonthlyReportId]),
        CONSTRAINT [FK_MonthlyReport_Account]     FOREIGN KEY ([AccountId])     REFERENCES [Account] ([AccountId]),
        CONSTRAINT [FK_MonthlyReport_DeviceGroup] FOREIGN KEY ([DeviceGroupId]) REFERENCES [DeviceGroup] ([DeviceGroupId])
    );
END

-- ----------------------------------------------------------------------------
-- PendingRegistration  (no FK dependencies)
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PendingRegistration' AND type = 'U')
BEGIN
    CREATE TABLE [PendingRegistration] (
        [PendingRegistrationId] INT            NOT NULL IDENTITY(1,1),
        [Email]                 NVARCHAR(256)  NOT NULL,
        [Password]              NVARCHAR(256)  NOT NULL,
        [FirstName]             NVARCHAR(50)   NOT NULL,
        [LastName]              NVARCHAR(50)   NOT NULL,
        [Token]                 NVARCHAR(128)  NOT NULL,
        [CreatedAt]             DATETIME2      NOT NULL,
        [ExpiresAt]             DATETIME2      NOT NULL,
        CONSTRAINT [PK_PendingRegistration] PRIMARY KEY ([PendingRegistrationId])
    );
    CREATE UNIQUE INDEX [IX_PendingRegistration_Token] ON [PendingRegistration] ([Token]);
    CREATE INDEX [IX_PendingRegistration_Email] ON [PendingRegistration] ([Email]);
END

-- ----------------------------------------------------------------------------
-- PendingPasswordReset  (no FK dependencies)
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PendingPasswordReset' AND type = 'U')
BEGIN
    CREATE TABLE [PendingPasswordReset] (
        [PendingPasswordResetId] INT            NOT NULL IDENTITY(1,1),
        [Email]                  NVARCHAR(256)  NOT NULL,
        [Token]                  NVARCHAR(128)  NOT NULL,
        [CreatedAt]              DATETIME2      NOT NULL,
        [ExpiresAt]              DATETIME2      NOT NULL,
        CONSTRAINT [PK_PendingPasswordReset] PRIMARY KEY ([PendingPasswordResetId])
    );
    CREATE UNIQUE INDEX [IX_PendingPasswordReset_Token] ON [PendingPasswordReset] ([Token]);
    CREATE INDEX [IX_PendingPasswordReset_Email] ON [PendingPasswordReset] ([Email]);
END
