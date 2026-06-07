using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using MyMonitorHub.Domain.BO.Pdf;
using MyMonitorHub.Domain.BO.Transfer;
using MyMonitorHub.Domain.Entity;
using MyMonitorHub.Domain.Interface;
using MyMonitorHub.Domain.Util;

namespace MyMonitorHub.Domain.Service
{
    public class SrReportService
    {

        private readonly IDbContextScopeFactory _dbContextScopeFactory;
        private readonly ILogoService _logoService;

        public SrReportService(IDbContextScopeFactory dbContextScopeFactory, ILogoService logoService)
        {
            _dbContextScopeFactory = dbContextScopeFactory;
            _logoService = logoService;
        }

        public SrReportService(IDbContextScopeFactory dbContextScopeFactory)
        {
            _dbContextScopeFactory = dbContextScopeFactory;
        }

        public SrReportModel GetSrReportModel(int serviceRequestId, int accountId)
        {
            List<SrDetailHistory> history;

            SrReportModel myModel;
            using (var scope = _dbContextScopeFactory.Create())
            {
                myModel =
                    scope.Get<ServiceRequest>().Where(
                            x => x.ServiceRequestId == serviceRequestId
                              && x.Device.DeviceGroup.AccountId == accountId)
                        .Select(x => new SrReportModel
                        {
                            Id = x.ServiceRequestId,
                            DeviceGroup = x.Device.DeviceGroup.Description,
                            Device = x.Device.Description,
                            DeviceId = x.DeviceId,
                            PageId = x.Device.DeviceGroup.PageId,
                            AssignedTo = x.User.Email,
                            Notes = x.Notes,
                            Time = x.TimeStamp,
                            Status = x.Status
                        }).FirstOrDefault();

                if (myModel == null)
                {
                    throw new Exception("Service request not found or does not belong to your account.");
                }

                var stuff = scope.Get<Event>()
                    .Where(x => x.ServiceRequestId == myModel.Id)
                    .ToList();

                myModel.History = (from ev in stuff
                                   group ev by ev.Category + "> " + ev.SubCategory
                    into g
                                   select new SrDetailHistory
                                   {
                                       Description = g.Key,
                                       Events = g
                                   }).ToList();


                if (string.IsNullOrWhiteSpace(myModel.AssignedTo))
                {
                    myModel.Status = "Open";
                }
                else if (myModel.Status == "O")
                {
                    myModel.Status = "Open";
                }
                else
                {
                    myModel.Status = "Closed";
                }


            } // using




            myModel.Logo = _logoService.GetLogo();
            return myModel;
        }

        public class SrReportModel
        {
            public string CustomerName { get; set; }
            public string Logo { get; set; }
            public int Id { get; set; }
            public string Status { get; set; }
            public DateTime Time { get; set; }
            public string DeviceGroup { get; set; }
            public string Device { get; set; }
            public string AssignedTo { get; set; }
            public string Notes { get; set; }
            public IList<SrDetailHistory> History { get; set; }
            public int PageId { get; set; }
            public int DeviceId { get; set; }
        }

        public class SrDetailHistory
        {
            public string Description { get; set; }
            public IGrouping<string, Event> Events { get; set; }
        } // inner class
    }
}
