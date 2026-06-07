using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MyMonitorHub.Domain.Entity;
using MyMonitorHub.Domain.Interface;
using MyMonitorHub.Domain.Util;
using Account = MyMonitorHub.Domain.Entity.Account;

namespace MyMonitorHub.Domain.Service
{
    public sealed class AccountService
    {
        private readonly IDbContextScopeFactory _dbContextScopeFactory;
        public AccountService(IDbContextScopeFactory dbContextScopeFactory)
        {
            _dbContextScopeFactory = dbContextScopeFactory;
        }

        public Descriptions GetDescriptions(int deviceId)
        {
            using (var scope = _dbContextScopeFactory.Create())
            {
                var d = scope.Get<Device>().Where(x => x.DeviceId == deviceId)
                    .Select(device => new
                    {
                        deviceDesc = device.Description,
                        deviceGroupDesc = device.DeviceGroup.Description,
                        pageDesc = device.DeviceGroup.Page.Description,
                        devTypeDesc = device.DeviceType.Name
                    }).FirstOrDefault();

                if (d == null) return null;

                return new Descriptions
                {
                    Page = d.pageDesc,
                    DeviceGroup = d.deviceGroupDesc,
                    Device = d.deviceDesc,
                    Catagory = d.devTypeDesc
                };
            }
        }

        public List<TextValuePair> GetKeyValuePairs()
        {
            using (var scope = _dbContextScopeFactory.Create())
            {
                return scope.Get<Account>()
                    .Select(x => new {Text = x.Description, Value = x.AccountId})
                    .AsEnumerable()
                    .Select(x => new TextValuePair {Text = x.Text, Value = x.Value.ToString(CultureInfo.InvariantCulture)})
                    .ToList();
            }
        }

        public class Descriptions
        {
            public string Catagory;
            public string Device;
            public string DeviceGroup;
            public string Page;
        }
    }
}
