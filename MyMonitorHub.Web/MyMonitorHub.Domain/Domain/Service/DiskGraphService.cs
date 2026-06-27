using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security;
using MyMonitorHub.Domain.Interface;
using MyMonitorHub.Domain.Security;
using MyMonitorHub.Domain.Util;
using ZedGraph;
using DiskUsage = MyMonitorHub.Domain.Entity.DiskUsage;

namespace MyMonitorHub.Domain.Service
{
    public class DiskGraphService
    {
        private readonly IDbContextScopeFactory _dbContextScopeFactory;
        private string _date;



        public DiskGraphService(IDbContextScopeFactory dbContextScopeFactory)
        {
            _dbContextScopeFactory = dbContextScopeFactory;
        }

        private class DiskColumns
        {
            public DateTime Timestamp { get; set; }
            public long DiskSize { get; set; }
            public long CurUsedMB { get; set; }
        }

        public Stream RenderGraph(int itemId, string report)
        {
            return RenderGraph(itemId, report, "");
        }

        private IEnumerable<DiskColumns> GetData(int itemId, DateTime startDate)
        {
            using (var scope = _dbContextScopeFactory.Create())
            {
                return (from d in
                    scope.Get<DiskUsage>().Where(
                        us => us.Timestamp.Date == startDate && us.Item.ItemId == itemId)
                    orderby d.Timestamp
                    select new DiskColumns
                    {
                        Timestamp = d.Timestamp,
                        CurUsedMB = d.CurUsedMB,
                        DiskSize = d.DiskSize
                    }).ToList();

            }
        }

        private IEnumerable<DiskColumns> GetData(int itemId, DateTime startDate, DateTime endDate)
        {
            using (var scope = _dbContextScopeFactory.Create())
            {
                return (from d in
                            scope.Get<DiskUsage>().Where(
                                d => d.Timestamp.Date >= startDate &&
                                      d.Timestamp.Date <= endDate && d.Item.ItemId == itemId)
                        orderby d.Timestamp
                        select new DiskColumns
                        {
                            Timestamp = d.Timestamp,
                            CurUsedMB = d.CurUsedMB,
                            DiskSize = d.DiskSize
                        }).ToList();
            }
        }


        public Stream RenderGraph(int itemId, string report, string date)
        {
            _date = date;
            if (!Authorizer.IsAdministrator)
            {
                var securityInfo = new ItemService(_dbContextScopeFactory).GetSecurityInfo(itemId);

                if (securityInfo == null)
                {
                    throw new Exception("Invalid data.");
                }


                if (securityInfo.accountId != Helper.AccountId)
                {
                    throw new SecurityException("You only have permission to access your own account pages.");
                }

                // filter based on permissions
                if (securityInfo.deviceGroupId == 0)
                {
                    throw new SecurityException("Access denied.");
                }

            }

            if (report == null)
            {
                return null;
            }

            if (report == "day")
            {
                return DisplayDay(itemId, report);
            }
            if (report == "week")
            {
                return DisplayWeek(itemId, report);
            }
            if (report == "month")
            {
                return DisplayMonth(itemId, report);
            }



            // continue to support these ones
            if (report == "today" || report == "yesterday")
            {
                return DisplayDay(itemId, report);
            }
            if (report == "thisWeek" || report == "lastWeek" || report == "last7")
            {
                return DisplayWeek(itemId, report);
            }
            if (report == "thisMonth" || report == "lastMonth" || report == "last31")
            {
                return DisplayMonth(itemId, report);
            }
            return null;
        }

        private Stream DisplayDay(int itemId, string day)
        {
            // Get the GraphPane so we can work with it
            var myPane = new GraphPane();


            myPane.XAxis.Title.Text = "Time in half hour increments";
            myPane.YAxis.Title.Text = "Disk space in GB";

            var list = new PointPairList();

            var startDate = DateTime.Now.Date;
            if (day == "today")
            {
                myPane.Title.Text = "Disk usage for Today " + startDate.ToString("MMMM dd, yyyy");
            }
            else if (day == "yesterday")
            {
                var oneDay = new TimeSpan(1, 0, 0, 0); // 1 day
                startDate = startDate - oneDay;

                myPane.Title.Text = "Disk usage for Yesterday " + startDate.ToString("MMMM dd, yyyy");
            }
            else if (day == "day")
            {
                if (!string.IsNullOrEmpty(_date))
                {
                    startDate = DateTime.Parse(_date);
                }
                myPane.Title.Text = "Disk usage for " + startDate.ToString("MMMM dd, yyyy");
            }
            else
            {
                return null;
            }
            var detail = GetData(itemId, startDate);

            long diskSize = 0;

            var halfHours = new double[48];
            foreach (var diskUsage in detail)
            {
                diskSize = Math.Max(diskSize, diskUsage.DiskSize);
                var ts2 = diskUsage.Timestamp.TimeOfDay;
                var minutes = (int) ts2.TotalMinutes;
                var column = minutes/30;
                halfHours[column] = diskUsage.CurUsedMB/1024d; // TODO averaging required
            }

            myPane.YAxis.Scale.Max = diskSize/1024d;

            var currColumn = 0;
            foreach (var columnData in halfHours)
            {
                var currentDate = startDate.AddMinutes(currColumn*30);
                currColumn++;
                double x = new XDate(currentDate);
                var y = columnData;
                list.Add(x, y);
            }

            myPane.BarSettings.MinClusterGap = 0;
            myPane.AddBar("", list, Color.Blue);

            var endDate = startDate;


            myPane.XAxis.Type = AxisType.Date;

            myPane.XAxis.Scale.MajorUnit = DateUnit.Day;

            myPane.XAxis.Scale.BaseTic = new XDate(startDate);
            myPane.XAxis.Scale.Min = new XDate(startDate - new TimeSpan(0, 30, 0)); // back up 30 minutes
            myPane.XAxis.Scale.Max = new XDate(endDate + new TimeSpan(1, 0, 0, 0)); // add one day

            myPane.XAxis.Scale.MajorStep = 2;
            //myPane.XAxis.Scale.MinorStep = 1;

            //myPane.XAxis.Scale.Format = "ddd-dd";

            // force an axischange to plot all data and recalculate all axis
            // this is normally done by the control, but this is not possible in mvc3
            var bm = new Bitmap(1, 1);
            using (var g = Graphics.FromImage(bm))
                myPane.AxisChange(g);

            // create a stream to store a PNG-format image
            var outStream = new MemoryStream();

            // ouput graph to stream
            myPane.GetImage(640, 480, 96, true)
                .Save(outStream, ImageFormat.Png);

            // set streamposition to 0
            outStream.Position = 0;

            // return stream as file result
            return outStream;
        }

        private Stream DisplayWeek(int itemId, string day)
        {
            // Get the GraphPane so we can work with it
            var myPane = new GraphPane();


            myPane.XAxis.Title.Text = "Time in 6 hour increments";
            myPane.YAxis.Title.Text = "Disk space in GB";

            var list = new PointPairList();

            var startDate = DateTime.Now.Date;
            if (day == "last7")
            {
                myPane.Title.Text = "Disk usage for the last 7 days";
                startDate = startDate - new TimeSpan(6, 0, 0, 0); // subtract 6 days
            }
            else if (day == "thisWeek" || day == "lastWeek")
            {
                var offset = (day == "thisWeek" ? 0 : 7);
                var days = new TimeSpan();
                var dow = DateTime.Now.DayOfWeek;
                if (dow == DayOfWeek.Monday)
                {
                    days = new TimeSpan(0 + offset, 0, 0, 0);
                }
                else if (dow == DayOfWeek.Tuesday)
                {
                    days = new TimeSpan(1 + offset, 0, 0, 0);
                }
                else if (dow == DayOfWeek.Wednesday)
                {
                    days = new TimeSpan(2 + offset, 0, 0, 0);
                }
                else if (dow == DayOfWeek.Thursday)
                {
                    days = new TimeSpan(3 + offset, 0, 0, 0);
                }
                else if (dow == DayOfWeek.Friday)
                {
                    days = new TimeSpan(4 + offset, 0, 0, 0);
                }
                else if (dow == DayOfWeek.Saturday)
                {
                    days = new TimeSpan(5 + offset, 0, 0, 0);
                }
                else if (dow == DayOfWeek.Sunday)
                {
                    days = new TimeSpan(6 + offset, 0, 0, 0);
                }

                startDate = startDate - days;
                myPane.Title.Text = "Disk usage for the week of " + startDate.ToString("MMMM dd, yyyy");
            }
            else if (day == "week")
            {
                if (!string.IsNullOrEmpty(_date))
                {
                    startDate = DateTime.Parse(_date);
                }
                myPane.Title.Text = "Disk usage for " + startDate.ToString("MMMM dd, yyyy");
            }
            else
            {
                return null;
            }
            var endDate = startDate + new TimeSpan(6, 0, 0, 0);

            var detail = GetData(itemId, startDate, endDate);

            long diskSize = 0;

            var sixHours = new double[28];
            foreach (var diskUsage in detail)
            {
                diskSize = Math.Max(diskSize, diskUsage.DiskSize);
                var ts2 = diskUsage.Timestamp - startDate;
                var minutes = (int) ts2.TotalMinutes;
                var column = minutes/360;
                sixHours[column] = diskUsage.CurUsedMB/1024d; // TODO averaging required
            }

            myPane.YAxis.Scale.Max = diskSize/1024d;

            var currColumn = 0;
            foreach (var columnData in sixHours)
            {
                var currentDate = startDate.AddHours(currColumn*6);
                currColumn++;
                double x = new XDate(currentDate);
                var y = columnData;
                list.Add(x, y);
            }

            myPane.BarSettings.MinClusterGap = 0;
            myPane.AddBar("", list, Color.Blue);

            myPane.XAxis.Type = AxisType.Date;

            myPane.XAxis.Scale.MajorUnit = DateUnit.Day;

            myPane.XAxis.Scale.BaseTic = new XDate(startDate);
            myPane.XAxis.Scale.Min = new XDate(startDate - new TimeSpan(6, 0, 0)); // backup 6 hours
            myPane.XAxis.Scale.Max = new XDate(endDate + new TimeSpan(1, 0, 0, 0)); // add one day

            myPane.XAxis.Scale.MajorStep = 1;
            myPane.XAxis.Scale.MinorStep = 1;

            myPane.XAxis.Scale.Format = "ddd-dd";

            // force an axischange to plot all data and recalculate all axis
            // this is normally done by the control, but this is not possible in mvc3
            var bm = new Bitmap(1, 1);
            using (var g = Graphics.FromImage(bm))
                myPane.AxisChange(g);

            // create a stream to store a PNG-format image
            var outStream = new MemoryStream();

            // ouput graph to stream
            myPane.GetImage(640, 480, 96, true)
                .Save(outStream, ImageFormat.Png);

            // set streamposition to 0
            outStream.Position = 0;

            // return stream as file result
            return outStream;
        }

        private Stream DisplayMonth(int itemId, string day)
        {
            // Get the GraphPane so we can work with it
            var myPane = new GraphPane();


            myPane.XAxis.Title.Text = "Days";
            myPane.YAxis.Title.Text = "Disk space in GB";

            var list = new PointPairList();

            var startDate = DateTime.Now.Date;
            if (day == "thisMonth")
            {
                if (startDate.Day != 1)
                {
                    startDate = startDate - new TimeSpan(startDate.Day - 1, 0, 0, 0); // get the first day of the month
                }
            }
            else if (day == "lastMonth")
            {
                if (startDate.Day != 1)
                {
                    startDate = startDate - new TimeSpan(startDate.Day - 1, 0, 0, 0); // get the first day of the month
                }
                startDate = startDate - new TimeSpan(10, 0, 0, 0); // subtract 10 days
                if (startDate.Day != 1)
                {
                    startDate = startDate - new TimeSpan(startDate.Day - 1, 0, 0, 0); // get the first day of the month
                }
            }
            else if (day == "last31")
            {
                startDate = startDate - new TimeSpan(30, 0, 0, 0); // 30 days
            }
            else if (day == "month")
            {
                if (!string.IsNullOrEmpty(_date))
                {
                    startDate = DateTime.Parse(_date);
                }
                startDate = new DateTime(startDate.Year, startDate.Month, 1);
            }
            else
            {
                return null;
            }
            myPane.Title.Text = "Disk usage for " + startDate.ToString("MMMM yyyy");

            var daysInMonth = DateTime.DaysInMonth(startDate.Year, startDate.Month);
            var endDate = new DateTime(startDate.Year, startDate.Month, daysInMonth);
            if (day == "last31")
            {
                endDate = DateTime.Now.Date;
                myPane.Title.Text = "Disk usage for the last 31 days";
                daysInMonth = 31;
            }

            var detail = GetData(itemId, startDate, endDate);

            long diskSize = 0;

            var days = new double[daysInMonth];
            foreach (var diskUsage in detail)
            {
                diskSize = Math.Max(diskSize, diskUsage.DiskSize);

                var dt = diskUsage.Timestamp - startDate;
                var column = dt.Days;

                days[column] = diskUsage.CurUsedMB/1024d; // TODO averaging required
            }

            myPane.YAxis.Scale.Max = diskSize/1024d;

            var currColumn = 0;
            foreach (var columnData in days)
            {
                var currentDate = startDate.AddDays(currColumn);
                currColumn++;
                double x = new XDate(currentDate);
                var y = columnData;
                list.Add(x, y);
            }

            myPane.BarSettings.MinClusterGap = 0;
            myPane.AddBar("", list, Color.Blue);

            myPane.XAxis.Type = AxisType.Date;

            myPane.XAxis.Scale.MajorUnit = DateUnit.Day;

            myPane.XAxis.Scale.BaseTic = new XDate(startDate);
            myPane.XAxis.Scale.Min = new XDate(startDate - new TimeSpan(1, 0, 0, 0)); // back up 1 day
            myPane.XAxis.Scale.Max = new XDate(endDate + new TimeSpan(1, 0, 0, 0)); // add one day

            myPane.XAxis.Scale.MajorStep = 7;
            myPane.XAxis.Scale.MinorStep = 1;

            myPane.XAxis.Scale.Format = "dd-MMM";

            // force an axischange to plot all data and recalculate all axis
            // this is normally done by the control, but this is not possible in mvc3
            var bm = new Bitmap(1, 1);
            using (var g = Graphics.FromImage(bm))
                myPane.AxisChange(g);

            // create a stream to store a PNG-format image
            var outStream = new MemoryStream();

            // ouput graph to stream
            myPane.GetImage(640, 480, 96, true)
                .Save(outStream, ImageFormat.Png);

            // set streamposition to 0
            outStream.Position = 0;

            // return stream as file result
            return outStream;
        }
    } // class
}