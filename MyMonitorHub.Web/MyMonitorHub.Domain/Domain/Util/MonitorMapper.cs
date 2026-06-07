using MyMonitorHub.Domain.Dto;
using MyMonitorHub.Domain.Entity;
using MyMonitorHub.Domain.Models;
using MyMonitorHub.Domain.Service;
using Riok.Mapperly.Abstractions;

namespace MyMonitorHub.Domain.Util;

[Mapper]
public static partial class MonitorMapper
{
    public static partial IQueryable<RoleModel> ProjectToRoleModel(this IQueryable<Role> q);

    public static partial IQueryable<ContactDetailModel> ProjectToContactDetailModel(this IQueryable<Contact> q);
    public static partial IQueryable<ContactModel> ProjectToContactModel(this IQueryable<Contact> q);

    public static partial IQueryable<DeviceGroupService.DeviceGroupColumns> ProjectToDeviceGroupColumns(this IQueryable<DeviceGroup> q);
    public static partial IQueryable<DeviceGroupDetailModel> ProjectToDeviceGroupDetailModel(this IQueryable<DeviceGroup> q);
    public static partial IQueryable<DeviceGroupModel> ProjectToDeviceGroupModel(this IQueryable<DeviceGroup> q);

    public static partial IQueryable<DeviceModel> ProjectToDeviceModel(this IQueryable<Device> q);
    public static partial IQueryable<DeviceDetailModel> ProjectToDeviceDetailModel(this IQueryable<Device> q);

    public static partial IQueryable<EmailNotificationService.EmailNotificationModel> ProjectToEmailNotificationModel(this IQueryable<EmailNotification> q);
    public static partial IQueryable<AlertMaintenanceEmailModel> ProjectToAlertMaintenanceEmailModel(this IQueryable<EmailNotification> q);

    public static partial IQueryable<MemberService.MemberModel> ProjectToMemberModel(this IQueryable<Member> q);
    public static partial IQueryable<OrganizationService.OrganizationModel> ProjectToOrganizationModel(this IQueryable<Member> q);

    public static partial IQueryable<PageModel> ProjectToPageModel(this IQueryable<Page> q);
    public static partial IQueryable<PageDetailModel> ProjectToPageDetailModel(this IQueryable<Page> q);

    public static partial IQueryable<UtilService.UserSummary> ProjectToUserSummary(this IQueryable<User> q);
}
