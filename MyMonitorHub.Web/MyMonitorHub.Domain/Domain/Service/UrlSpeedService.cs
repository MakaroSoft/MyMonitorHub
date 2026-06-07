using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using MyMonitorHub.Domain.Interface;
using UrlSpeed = MyMonitorHub.Domain.Entity.UrlSpeed;

namespace MyMonitorHub.Domain.Service
{
    public sealed class UrlSpeedService
    {
        private readonly IDbContextScopeFactory _dbContextScopeFactory;
        public UrlSpeedService(IDbContextScopeFactory dbContextScopeFactory)
        {
            _dbContextScopeFactory = dbContextScopeFactory;
        }

        public List<ResponseGraphService.UrlSpeedData> GetData(int itemId, DateTime startDate)
        {
            using (var scope = _dbContextScopeFactory.Create())
            {
                return (from us in
                            scope.Get<UrlSpeed>().Where(
                                us => us.Timestamp.Date == startDate && us.Item.ItemId == itemId)
                        orderby us.Timestamp
                        select new ResponseGraphService.UrlSpeedData
                        {
                            Timestamp = us.Timestamp,
                            AvgSpeedMS = us.AvgSpeedMS
                        }).ToList();
            }
        }
        public List<ResponseGraphService.UrlSpeedData> GetData(int itemId, DateTime startDate, DateTime endDate)
        {
            using (var scope = _dbContextScopeFactory.Create())
            {
                return (from us in
                            scope.Get<UrlSpeed>().Where(
                                us => us.Timestamp.Date >= startDate &&
                                      us.Timestamp.Date <= endDate && us.Item.ItemId == itemId)
                        orderby us.Timestamp
                        select new ResponseGraphService.UrlSpeedData
                        {
                            Timestamp = us.Timestamp,
                            AvgSpeedMS = us.AvgSpeedMS
                        }).ToList();
            }
        }
    }
}
