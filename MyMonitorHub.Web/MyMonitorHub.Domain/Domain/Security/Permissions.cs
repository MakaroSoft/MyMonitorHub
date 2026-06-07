using System.Collections.Generic;
using MyMonitorHub.Domain.Service;

namespace MyMonitorHub.Domain.Security
{
    public class Permission
    {
        public PermissionGroup Group { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string FailMessage { get; set; }
    }

    public class PermissionGroup
    {
        public string Description { get; set; }
        public string Name { get; set; }
        public List<Permission> Permissions = new List<Permission>();

        public PermissionGroup Add(Permission permission)
        {
            Permissions.Add(permission);
            return this;
        }
    }

    public class Permissions
    {
        private static Permissions _current;

        private readonly List<PermissionGroup> _groups = new List<PermissionGroup>();

        public List<PermissionGroup> Groups
        {
            get { return _groups; }
        }

        public static Permissions Current
        {
            get { return _current ?? (_current = new Permissions()); }
        }

        private Permissions()
        {
            _groups.Add(RoleMaint
                .Add(CanViewRoles)
                .Add(CanEditRoles)
                .Add(CanDeleteRoles)
                .Add(CanCreateNewRole));
            _groups.Add(Users
                .Add(CanViewAnyone)
                .Add(CanEditAnyone)
                .Add(CanEditAllDeviceGroups)
                .Add(CanAddNewUser)
                .Add(CanDeleteAnyone));
            _groups.Add(ConfigMaint
                .Add(CanViewConfiguration)
                .Add(CanEditConfiguration));
            _groups.Add(AccountMaint
                .Add(CanViewAcctInfo)
                .Add(CanEditAcctInfo));
            _groups.Add(PageMaint
                .Add(CanViewPageMaint)
                .Add(CanEditPageMaint));
            _groups.Add(DeviceGroupMaint
                .Add(CanViewDeviceGroupMaint)
                .Add(CanEditDeviceGroupMaint));
            _groups.Add(DeviceMaint
                .Add(CanViewDeviceMaint)
                .Add(CanEditDeviceMaint));
            _groups.Add(Notes
                .Add(CanViewNotes)
                .Add(CanEditNotes));
            _groups.Add(DisplayPage
                .Add(CanViewPage)
                .Add(CanEditPage)
                .Add(CanClearPage));
            _groups.Add(Downloads
                .Add(CanViewDownloads)
                .Add(CanEditDownloads));
            _groups.Add(Rules
                .Add(CanViewAlertRules)
                .Add(CanEditAlertRules));
            _groups.Add(ServReq
                .Add(CanViewServiceRequests)
                .Add(CanEditServiceRequests));
            _groups.Add(Reports
                .Add(CanViewReports));
            _groups.Add(InformationRequest
                .Add(CanSendCustomerInfoRequests));
            _groups.Add(General
                .Add(CanReceiveAlerts)
                );
            _groups.Add(DeviceDetail
                .Add(CanStopStartServices)
                );
        }

        // Device Detail
        public static readonly PermissionGroup DeviceDetail = new PermissionGroup
        {
            Name = "DeviceDetail",
            Description = "Device Detail Page"
        };

        public static readonly Permission CanStopStartServices = new Permission
        {
            Group = DeviceDetail,
            Name = "CanStopStartServices",
            Description = "Can stop and start services",
            FailMessage = "You do not have permission to stop or start services"
        };

        public static readonly Permission CanExecuteCommands = new Permission
        {
            Group = DeviceDetail,
            Name = "CanExecuteCommands",
            Description = "Can execute commands from the command line",
            FailMessage = "You do not have permission to execute commands"
        };
        public static readonly Permission CanUpload = new Permission
        {
            Group = DeviceDetail,
            Name = "CanUpload",
            Description = "Can upload files",
            FailMessage = "You do not have permission to upload files"
        };
        public static readonly Permission CanDownload = new Permission
        {
            Group = DeviceDetail,
            Name = "CanDownload",
            Description = "Can download files",
            FailMessage = "You do not have permission to download files"
        };

        // Remoting
        public static readonly PermissionGroup Remoting = new PermissionGroup
        {
            Name = "Remoting",
            Description = "Remote Connections to your server"
        };

        public static readonly Permission CanRemote = new Permission
        {
            Group = Remoting,
            Name = "canRemote",
            Description = "Can remote to your server",
            FailMessage = "You do not have permission to use the remoting feature"
        };

        public static readonly Permission CanPrefillPassword = new Permission
        {
            Group = Remoting,
            Name = "prefillPassword",
            Description = "Prefill the password field",
            FailMessage = "You do not have permission to use the defined password"
        };
        public static readonly Permission CanPrefillServer = new Permission
        {
            Group = Remoting,
            Name = "prefillServer",
            Description = "Prefill the server field",
            FailMessage = "You do not have permission to use the defined server name"
        };
        public static readonly Permission CanPrefillDomain = new Permission
        {
            Group = Remoting,
            Name = "prefillDomain",
            Description = "Prefill the domain field",
            FailMessage = "You do not have permission to use the defined domain name"
        };
        public static readonly Permission CanPrefillUsername = new Permission
        {
            Group = Remoting,
            Name = "prefillUsername",
            Description = "Prefill the username field",
            FailMessage = "You do not have permission to use the defined username"
        };

        // ILO Remoting
        public static readonly PermissionGroup IloRemoting = new PermissionGroup
        {
            Name = "ILO Remoting",
            Description = "Remote Connections to your ILO"
        };
        public static readonly Permission CanRemoteToIlo = new Permission
        {
            Group = IloRemoting,
            Name = "canRemoteToIlo",
            Description = "Can remote to your ILO",
            FailMessage = "You do not have permission to use the remoting feature to access your ILO"
        };
        public static readonly Permission CanPrefillIloPassword = new Permission
        {
            Group = IloRemoting,
            Name = "prefillIloPassword",
            Description = "Prefill the ILO password field",
            FailMessage = "You do not have permission to use the defined ILO password"
        };

        public static readonly PermissionGroup RoleMaint = new PermissionGroup
        {
            Name = "RoleMaint",
            Description = "Role Maintenance"
        };

        public static readonly PermissionGroup Users = new PermissionGroup
        {
            Name = "Users",
            Description = "User Maintenance"
        };

        public static readonly PermissionGroup ConfigMaint = new PermissionGroup
        {
            Name = "ConfigMaint",
            Description = "Configuration"
        };

        public static readonly PermissionGroup AccountMaint = new PermissionGroup
        {
            Name = "AccountMaint",
            Description = "Account Maintenance"
        };

        public static readonly PermissionGroup PageMaint = new PermissionGroup
        {
            Name = "PageMaint",
            Description = "Page Maintenance"
        };
        public static readonly PermissionGroup DeviceGroupMaint = new PermissionGroup
        {
            Name = "DeviceGroupMaint",
            Description = "Device Group Maintenance"
        };
        public static readonly PermissionGroup DeviceMaint = new PermissionGroup
        {
            Name = "DeviceMaint",
            Description = "Device Group Maintenance"
        };

        public static readonly PermissionGroup Notes = new PermissionGroup {Name = "Notes", Description = "Notes"};

        public static readonly PermissionGroup DisplayPage = new PermissionGroup
        {
            Name = "DisplayPage",
            Description = "Display Page"
        };

        public static readonly PermissionGroup Downloads = new PermissionGroup
        {
            Name = "Downloads",
            Description = "Downloads"
        };

        public static readonly PermissionGroup Rules = new PermissionGroup {Name = "Rules", Description = "Alert Rules"};

        public static readonly PermissionGroup ServReq = new PermissionGroup
        {
            Name = "ServReq",
            Description = "Service Requests"
        };

        public static readonly PermissionGroup Reports = new PermissionGroup
        {
            Name = "Reports",
            Description = "Reporting"
        };

        public static readonly PermissionGroup InformationRequest = new PermissionGroup
        {
            Name = "InformationRequest",
            Description = "Information Request"
        };

        public static readonly PermissionGroup General = new PermissionGroup
        {
            Name = "General",
            Description = "General Permissions"
        };

        // role maintenance group
        public static readonly Permission CanReceiveAlerts = new Permission
        {
            Group = RoleMaint,
            Name = "alerts",
            Description = "Receive alerts",
            FailMessage = "You do not have permission to receive alerts"
        };

        // role maintenance group
        public static readonly Permission CanViewRoles = new Permission
        {
            Group = RoleMaint,
            Name = "view",
            Description = "View roles",
            FailMessage = "You do not have permission to view roles"
        };

        public static readonly Permission CanEditRoles = new Permission
        {
            Group = RoleMaint,
            Name = "edit",
            Description = "Edit roles",
            FailMessage = "You do not have permision to edit roles"
        };

        public static readonly Permission CanDeleteRoles = new Permission
        {
            Group = RoleMaint,
            Name = "delete",
            Description = "Delete roles",
            FailMessage = "You do not have permission to delete roles"
        };

        public static readonly Permission CanCreateNewRole = new Permission
        {
            Group = RoleMaint,
            Name = "new",
            Description = "Create new role",
            FailMessage = "You do not have permission to create roles"
        };

        // users group
        public static readonly Permission CanViewAnyone = new Permission
        {
            Group = Users,
            Name = "viewAnyone",
            Description = "View any user in the account",
            FailMessage = "You do not have permission to view the user"
        };

        public static readonly Permission CanEditAnyone = new Permission
        {
            Group = Users,
            Name = "editAnyone",
            Description = "Edit any user in the account",
            FailMessage = "You do not have permission to edit the user"
        };

        public static readonly Permission CanEditAllDeviceGroups = new Permission
        {
            Group = Users,
            Name = "editPermissionsAnyone",
            Description = "Edit any user's device groups in the account",
            FailMessage = "You do not have permission to edit the users device group"
        };

        public static readonly Permission CanAddNewUser = new Permission
        {
            Group = Users,
            Name = "new",
            Description = "Add a new user",
            FailMessage = "You do not have permission to create a new user"
        };

        public static readonly Permission CanDeleteAnyone = new Permission
        {
            Group = Users,
            Name = "deleteAnyone",
            Description = "Delete any user in the account",
            FailMessage = "You do not have permission to delete the user"
        };

        // configuration
        public static readonly Permission CanViewConfiguration = new Permission
        {
            Group = ConfigMaint,
            Name = "view",
            Description = "View configuration",
            FailMessage = "You do not have permission to view the configuration"
        };

        public static readonly Permission CanEditConfiguration = new Permission
        {
            Group = ConfigMaint,
            Name = "edit",
            Description = "Edit configuration",
            FailMessage = "You do not have permission to edit the configuration"
        };

        // Account Maintenance
        public static readonly Permission CanViewAcctInfo = new Permission
        {
            Group = AccountMaint,
            Name = "view",
            Description = "View account information",
            FailMessage = "You do not have permission to view the account information"
        };

        public static readonly Permission CanEditAcctInfo = new Permission
        {
            Group = AccountMaint,
            Name = "edit",
            Description = "Edit account information",
            FailMessage = "You do not have permission to edit the account information"
        };

        // Page Maintenance
        public static readonly Permission CanViewPageMaint = new Permission
        {
            Group = PageMaint,
            Name = "view",
            Description = "View page maintenance",
            FailMessage = "You do not have permission to view page maintenance"
        };

        public static readonly Permission CanEditPageMaint = new Permission
        {
            Group = PageMaint,
            Name = "edit",
            Description = "Edit page maintenance",
            FailMessage = "You do not have permission to edit page maintenance"
        };
        public static readonly Permission CanEditDeviceGroupMaint = new Permission
        {
            Group = DeviceGroupMaint,
            Name = "edit",
            Description = "Edit device group maintenance",
            FailMessage = "You do not have permission to edit device group maintenance"
        };
        public static readonly Permission CanViewDeviceGroupMaint = new Permission
        {
            Group = DeviceGroupMaint,
            Name = "view",
            Description = "View device group maintenance",
            FailMessage = "You do not have permission to view device group maintenance"
        };
        public static readonly Permission CanEditDeviceMaint = new Permission
        {
            Group = DeviceMaint,
            Name = "edit",
            Description = "Edit device maintenance",
            FailMessage = "You do not have permission to edit device maintenance"
        };
        public static readonly Permission CanViewDeviceMaint = new Permission
        {
            Group = DeviceMaint,
            Name = "view",
            Description = "View device maintenance",
            FailMessage = "You do not have permission to view device maintenance"
        };

        // Notes
        public static readonly Permission CanViewNotes = new Permission
        {
            Group = Notes,
            Name = "view",
            Description = "View notes",
            FailMessage = "You do not have permission to view notes"
        };

        public static readonly Permission CanEditNotes = new Permission
        {
            Group = Notes,
            Name = "edit",
            Description = "Edit notes",
            FailMessage = "You do not have permission to edit notes"
        };

        // Display Page
        public static readonly Permission CanViewPage = new Permission
        {
            Group = DisplayPage,
            Name = "view",
            Description = "View the page",
            FailMessage = "You do not have permission to view pages"
        };

        public static readonly Permission CanEditPage = new Permission
        {
            Group = DisplayPage,
            Name = "edit",
            Description = "Edit the page",
            FailMessage = "You do not have permission to edit pages"
        };

        public static readonly Permission CanClearPage = new Permission
        {
            Group = DisplayPage,
            Name = "clear",
            Description = "Clear statuses on the page",
            FailMessage = "You do not have permission to clear page status"
        };

        // Downloads
        public static readonly Permission CanViewDownloads = new Permission
        {
            Group = Downloads,
            Name = "view",
            Description = "View downloads",
            FailMessage = "You do not have permission to view downloads"
        };

        public static readonly Permission CanEditDownloads = new Permission
        {
            Group = Downloads,
            Name = "download",
            Description = "Allow downloads",
            FailMessage = "You do not have permission to download"
        };

        // Alert Rules
        public static readonly Permission CanViewAlertRules = new Permission
        {
            Group = Rules,
            Name = "view",
            Description = "View alert rules",
            FailMessage = "You do not have permission to view alert rules"
        };

        public static readonly Permission CanEditAlertRules = new Permission
        {
            Group = Rules,
            Name = "edit",
            Description = "Edit alert rules",
            FailMessage = "You do not have permission to edit alert rules"
        };

        // Service Requests
        public static readonly Permission CanViewServiceRequests = new Permission
        {
            Group = ServReq,
            Name = "view",
            Description = "View service requests",
            FailMessage = "You do not have permission to view service requests"
        };

        public static readonly Permission CanEditServiceRequests = new Permission
        {
            Group = ServReq,
            Name = "edit",
            Description = "Accept/Save/Close service requests",
            FailMessage = "You do not have permission to edit service requests"
        };

        // Reporting
        public static readonly Permission CanViewReports = new Permission
        {
            Group = Reports,
            Name = "view",
            Description = "View reports",
            FailMessage = "You do not have permission to view reports"
        };

        // Information Request
        public static readonly Permission CanSendCustomerInfoRequests = new Permission
        {
            Group = InformationRequest,
            Name = "view",
            Description = "send customer information requests",
            FailMessage = "You do not have permission to send customer information requests"
        };

        public static List<PageAccess> GetLayout()
        {
            var pages = new List<PageAccess>();

            var p = Current;
            foreach (var group in p.Groups)
            {
                var page = new PageAccess
                {
                    Code = group.Name,
                    Description = group.Description,
                    AccessCodes = new AccessCode[@group.Permissions.Count]
                };


                var index = 0;
                foreach (var permission in group.Permissions)
                {
                    var ac = new AccessCode
                    {
                        Code = permission.Name,
                        Description = permission.Description
                    };
                    page.AccessCodes[index] = ac;

                    index++;
                }

                pages.Add(page);

            }

            return pages;
        }

        public static List<RoleService.PermissionData> GetEmptyData()
        {
            var permissions = new List<RoleService.PermissionData>();

            var p = Current;
            foreach (var group in p.Groups)
            {
                foreach (var permission in group.Permissions)
                {
                    var perm = new RoleService.PermissionData
                    {
                        Check = false,
                        GroupCode = group.Name,
                        PermissionCode = permission.Name
                    };
                    permissions.Add(perm);
                }
            }
            return permissions;
        }
    }
}