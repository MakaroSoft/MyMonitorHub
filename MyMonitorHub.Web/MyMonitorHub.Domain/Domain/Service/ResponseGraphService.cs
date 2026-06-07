using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Security;
using MyMonitorHub.Domain.Interface;
using MyMonitorHub.Domain.Security;
using MyMonitorHub.Domain.Util;
using ZedGraph;

namespace MyMonitorHub.Domain.Service
{
    public class ResponseGraphService
    {
        private readonly IDbContextScopeFactory _dbContextScopeFactory;
        private string _date;

        public ResponseGraphService(IDbContextScopeFactory dbContextScopeFactory)
        {
            _dbContextScopeFactory = dbContextScopeFactory;
        }


        public Stream RenderGraph(int itemId, string report)
        {
            return RenderGraph(itemId, report, "");
        }

        public class SecurityInfo
        {
            public int accountId { get; set; }
            public int deviceGroupId { get; set; }
            public int pageId { get; set; }
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
            if (report == "monthEnd")
            {
                return DisplayWeek(itemId, report);
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

        public class UrlSpeedData
        {
            public DateTime Timestamp { get; set; }
            public int AvgSpeedMS { get; set; }
        }

        private Stream DisplayDay(int itemId, string day)
        {
            // Get the GraphPane so we can work with it
            var myPane = new GraphPane();


            myPane.XAxis.Title.Text = "Time in half hour increments";
            myPane.YAxis.Title.Text = "Response time in ms";

            var list = new PointPairList();

            var startDate = DateTime.Now.Date;
            if (day == "today")
            {
                myPane.Title.Text = "Response time for Today " + startDate.ToString("MMMM dd, yyyy");
            }
            else if (day == "yesterday")
            {
                var oneDay = new TimeSpan(1, 0, 0, 0); // 1 day
                startDate = startDate - oneDay;

                myPane.Title.Text = "Response time for Yesterday " + startDate.ToString("MMMM dd, yyyy");
            }
            else if (day == "day")
            {
                if (!string.IsNullOrEmpty(_date))
                {
                    startDate = DateTime.Parse(_date);
                }
                myPane.Title.Text = "Response time for " + startDate.ToString("MMMM dd, yyyy");
            }
            else
            {
                return null;
            }
            var detail = new UrlSpeedService(_dbContextScopeFactory).GetData(itemId, startDate);



            var halfHours = new Averager[48];
            foreach (var urlSpeed in detail)
            {
                var ts2 = urlSpeed.Timestamp.TimeOfDay;
                var minutes = (int) ts2.TotalMinutes;
                var column = minutes/30;
                if (halfHours[column] == null) halfHours[column] = new Averager();
                halfHours[column].Add(urlSpeed.AvgSpeedMS);
            }

            double slowestSpeed = 0;

            var currColumn = 0;
            foreach (var columnData in halfHours)
            {
                var currentDate = startDate.AddMinutes(currColumn*30);
                currColumn++;
                double x = new XDate(currentDate);
                var y = columnData == null ? 0 : columnData.GetAverage();
                double z = 2; // blue
                if (y >= 2500) z = 0;
                list.Add(x, y, z);

                slowestSpeed = Math.Max(slowestSpeed, y);
            }
            myPane.YAxis.Scale.Max = slowestSpeed;
            myPane.YAxis.Scale.Max = 2500d;

            myPane.YAxis.MajorGrid.IsVisible = true;

            myPane.BarSettings.MinClusterGap = 0;
            var myCurve = myPane.AddBar("", list, Color.Blue);


            Color[] colors = {Color.Red, Color.Yellow, Color.Blue};
            myCurve.Bar.Fill = new Fill(colors) {Type = FillType.GradientByZ, RangeMin = 0, RangeMax = 2};


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


            myPane.XAxis.Title.Text = "Time in 30 minute increments";
            myPane.YAxis.Title.Text = "Response time in ms";

            var list = new PointPairList();

            var startDate = DateTime.Now.Date;
            if (day == "last7")
            {
                myPane.Title.Text = "Response time for the last 7 days";
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
                myPane.Title.Text = "Respone time for the week of " + startDate.ToString("MMMM dd, yyyy");
            }
            else if (day == "week")
            {
                if (!string.IsNullOrEmpty(_date))
                {
                    startDate = DateTime.Parse(_date);
                }
                myPane.Title.Text = "Response time for " + startDate.ToString("MMMM dd, yyyy");
            }
            else if (day == "monthEnd")
            {
                if (!string.IsNullOrEmpty(_date))
                {
                    startDate = DateTime.Parse(_date);
                }
                myPane.Title.Text = "Response time for " + startDate.ToString("MMMM");
            }
            else
            {
                return null;
            }

            var size = 48 * 7;
            var endDate = startDate + new TimeSpan(6, 0, 0, 0);
            if (day == "monthEnd")
            {
                var daysInMonth = DateTime.DaysInMonth(startDate.Year, startDate.Month);
                size = 48 * daysInMonth;
                endDate = startDate + new TimeSpan(daysInMonth - 1, 0, 0, 0);
            }

            var detail = new UrlSpeedService(_dbContextScopeFactory).GetData(itemId, startDate, endDate);

            var halfHours = new Averager[size];
            foreach (var urlSpeed in detail)
            {
                var ts2 = urlSpeed.Timestamp - startDate;
                var minutes = (int) ts2.TotalMinutes;
                var column = minutes/30;
                if (halfHours[column] == null) halfHours[column] = new Averager();
                halfHours[column].Add(urlSpeed.AvgSpeedMS);
            }

            double slowestSpeed = 0;

            var currColumn = 0;
            foreach (var columnData in halfHours)
            {
                var currentDate = startDate.AddMinutes(currColumn*30);
                currColumn++;
                double x = new XDate(currentDate);
                var y = columnData == null ? 0 : columnData.GetAverage();
                double z = 2; // blue
                if (y >= 2500) z = 0;
                list.Add(x, y, z);

                slowestSpeed = Math.Max(slowestSpeed, y);
            }


            myPane.YAxis.Scale.Max = 2500d; // slowestSpeed;
            myPane.YAxis.MajorGrid.IsVisible = true;

            myPane.BarSettings.MinClusterGap = 0;
            myPane.AddBar("", list, Color.Blue);

            myPane.XAxis.Type = AxisType.Date;

            myPane.XAxis.Scale.MajorUnit = DateUnit.Day;

            myPane.XAxis.Scale.BaseTic = new XDate(startDate);
            myPane.XAxis.Scale.Min = new XDate(startDate - new TimeSpan(0, 30, 0)); // backup 30 minutes
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
            var multiplier = day == "monthEnd"? 2: 1;
            myPane.GetImage(640 * multiplier, 480, 96, true)
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
            myPane.YAxis.Title.Text = "Response time in ms";

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
            myPane.Title.Text = "Respone time for " + startDate.ToString("MMMM yyyy");

            var daysInMonth = DateTime.DaysInMonth(startDate.Year, startDate.Month);
            var endDate = new DateTime(startDate.Year, startDate.Month, daysInMonth);
            if (day == "last31")
            {
                endDate = DateTime.Now.Date;
                myPane.Title.Text = "Response time for the last 31 days";
                daysInMonth = 31;
            }

            var detail = new UrlSpeedService(_dbContextScopeFactory).GetData(itemId, startDate, endDate);

            var days = new Averager[daysInMonth];
            foreach (var urlSpeed in detail)
            {
                var dt = urlSpeed.Timestamp - startDate;
                var column = dt.Days;
                if (days[column] == null) days[column] = new Averager();
                days[column].Add(urlSpeed.AvgSpeedMS);
            }

            double slowestSpeed = 0;

            var currColumn = 0;
            foreach (var columnData in days)
            {
                var currentDate = startDate.AddDays(currColumn);
                currColumn++;
                double x = new XDate(currentDate);
                var y = columnData == null ? 0 : columnData.GetAverage();
                list.Add(x, y);
                slowestSpeed = Math.Max(slowestSpeed, y);
            }
            myPane.YAxis.Scale.Max = 2500d; // slowestSpeed;
            myPane.YAxis.MajorGrid.IsVisible = true;

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

        private class Averager
        {
            private double _count;
            private double _total;

            public void Add(double total)
            {
                _total += total;
                _count++;
            }

            public double GetAverage()
            {
// ReSharper disable once CompareOfFloatsByEqualityOperator
                if (_count == 0)
                {
                    return 0;
                }
                return _total/_count;
            }
        }
    } // class
}