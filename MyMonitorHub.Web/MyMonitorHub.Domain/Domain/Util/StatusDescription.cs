using System;
using System.Text.RegularExpressions;
using iText.Kernel.Colors;
using PdfColor = iText.Kernel.Colors.Color;
using iText.Kernel.Font;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using PdfHAlign = iText.Layout.Properties.HorizontalAlignment;

namespace MyMonitorHub.Domain.Util
{
    public class StatusDescription
    {
        public static Table FormatPDF(string statusDescription, PdfFont font, float fontSize = 10f)
        {
            var diskPattern = new Regex(@"^Free: \d*\.?\d+(GB|MB)\(.+\%\), min: \d*\.?\d+(GB|MB)$");
            if (diskPattern.IsMatch(statusDescription))
            {
                var splitPattern = new Regex(@"(\d*\.?\d+|GB|MB|%)");
                var collection = splitPattern.Matches(statusDescription);

                var free = double.Parse(collection[0].Value);
                if (collection[1].Value == "GB") free = Math.Round(free * 1024, MidpointRounding.AwayFromZero);

                var percentFree = double.Parse(collection[2].Value);

                var fail = double.Parse(collection[4].Value);
                if (collection[5].Value == "GB") fail = Math.Round(fail * 1024, MidpointRounding.AwayFromZero);

                var total = Math.Round(free / percentFree * 100, MidpointRounding.AwayFromZero);

                return FormatDiskPDF(total, free, fail, 0, font, fontSize);
            }
            if (statusDescription.StartsWith("_@DISK@_|"))
            {
                var parms = statusDescription.Split(new[] { "|" }, StringSplitOptions.None);
                var total = double.Parse(parms[1]);
                var used = double.Parse(parms[2]);
                var fail = double.Parse(parms[6]);
                var warn = double.Parse(parms[7]);
                var free = Math.Round(total - used, MidpointRounding.AwayFromZero);
                return FormatDiskPDF(total, free, fail, warn, font, fontSize);
            }
            if (statusDescription.StartsWith("_@URL@_|"))
            {
                var parms = statusDescription.Split(new[] { "|" }, StringSplitOptions.None);
                var avg = int.Parse(parms[4]);
                return MakeSimpleTable("Response time is " + avg + "ms", font, fontSize);
            }
            if (statusDescription.StartsWith("_@ERROR@_|"))
            {
                var parms = statusDescription.Split(new[] { "|" }, StringSplitOptions.None);
                var current = int.Parse(parms[1]);
                var recent = int.Parse(parms[2]);
                return MakeSimpleTable("Current error count:  " + current + ", errors within last cycle: " + recent, font, fontSize);
            }
            else
            {
                if (statusDescription.StartsWith("_@URL2@_|"))
                {
                    var parms = statusDescription.Split(new[] { "|" }, StringSplitOptions.None);
                    statusDescription = parms[5];
                }
                return MakeSimpleTable(statusDescription, font, fontSize);
            }
        }

        private static Table MakeSimpleTable(string text, PdfFont font, float fontSize)
        {
            var table = new Table(1).SetHorizontalAlignment(PdfHAlign.LEFT);
            var cell = new Cell()
                .SetBorder(Border.NO_BORDER)
                .SetVerticalAlignment(VerticalAlignment.TOP)
                .SetPaddingTop(0f)
                .Add(new Paragraph(text).SetFont(font).SetFontSize(fontSize));
            table.AddCell(cell);
            return table;
        }

        private static Table FormatDiskPDF(double total, double free, double fail, double warn, PdfFont font, float fontSize)
        {
            var totalGB = Math.Round(total / 1024, MidpointRounding.AwayFromZero);
            var freeGB = Math.Round(free / 1024, 1, MidpointRounding.AwayFromZero);
            var freePercent = Math.Round(free / total * 100, 1, MidpointRounding.AwayFromZero);
            var freePercentInt = (int)Math.Round(freePercent, MidpointRounding.AwayFromZero);
            var usedPercentInt = 100 - freePercentInt;

            PdfColor color = ColorConstants.GREEN;
            if (free < fail) color = ColorConstants.RED;
            else if (warn != 0 && free < warn) color = ColorConstants.YELLOW;

            var message = "Total = " + totalGB + "GB. Free = " + freeGB + "GB (" + freePercent + "%)";
            return GetTableAndDesc(usedPercentInt, freePercentInt, color, message, font, fontSize);
        }

        private static Table GetTable(int usedPercent, int freePercent, PdfColor color)
        {
            var tbl = new Table(new float[] { usedPercent, freePercent })
                .SetWidth(50f).SetFixedLayout()
                .SetHorizontalAlignment(PdfHAlign.LEFT);

            tbl.AddCell(new Cell().SetPadding(3f).SetBackgroundColor(color).SetBorder(Border.NO_BORDER));
            tbl.AddCell(new Cell().SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetBorder(Border.NO_BORDER));
            return tbl;
        }

        private static Table GetTableAndDesc(int usedPercent, int freePercent, PdfColor color, string description, PdfFont font, float fontSize)
        {
            var table = new Table(2).SetWidth(100f).SetFixedLayout()
                .SetHorizontalAlignment(PdfHAlign.LEFT);

            var barCell = new Cell().SetBorder(Border.NO_BORDER).SetPadding(0f)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .Add(GetTable(usedPercent, freePercent, color));
            table.AddCell(barCell);

            var descCell = new Cell().SetBorder(Border.NO_BORDER)
                .SetVerticalAlignment(VerticalAlignment.TOP)
                .SetPaddingTop(0f)
                .Add(new Paragraph(description).SetFont(font).SetFontSize(fontSize));
            table.AddCell(descCell);
            return table;
        }

        public static string FormatHTML(string statusDescription)
        {
            if (statusDescription == "") return "";

            var diskPattern = new Regex(@"^Free: \d*\.?\d+(GB|MB)\(.+\%\), min: \d*\.?\d+(GB|MB)$");
            if (diskPattern.IsMatch(statusDescription))
            {
                var splitPattern = new Regex(@"(\d*\.?\d+|GB|MB|%)");
                var collection = splitPattern.Matches(statusDescription);

                var free = double.Parse(collection[0].Value);
                if (collection[1].Value == "GB") free = Math.Round(free * 1024, MidpointRounding.AwayFromZero);
                var percentFree = double.Parse(collection[2].Value);
                var fail = double.Parse(collection[4].Value);
                if (collection[5].Value == "GB") fail = Math.Round(fail * 1024, MidpointRounding.AwayFromZero);
                var total = Math.Round(free / percentFree * 100, MidpointRounding.AwayFromZero);
                return FormatDiskHTML(total, free, fail, 0);
            }
            if (statusDescription.StartsWith("_@DISK@_|"))
            {
                var parms = statusDescription.Split(new[] { "|" }, StringSplitOptions.None);
                var total = double.Parse(parms[1]);
                var used = double.Parse(parms[2]);
                var fail = double.Parse(parms[6]);
                var warn = double.Parse(parms[7]);
                var free = Math.Round(total - used, MidpointRounding.AwayFromZero);
                return FormatDiskHTML(total, free, fail, warn);
            }
            if (statusDescription.StartsWith("_@URL@_|"))
            {
                var parms = statusDescription.Split(new[] { "|" }, StringSplitOptions.None);
                return "Response time is " + int.Parse(parms[4]) + "ms";
            }
            if (statusDescription.StartsWith("_@URL2@_|"))
            {
                var parms = statusDescription.Split(new[] { "|" }, StringSplitOptions.None);
                return parms[5];
            }
            if (statusDescription.StartsWith("_@ERROR@_|"))
            {
                var parms = statusDescription.Split(new[] { "|" }, StringSplitOptions.None);
                return "Current error count:  " + int.Parse(parms[1]) + ", errors within last cycle: " + int.Parse(parms[2]);
            }
            return statusDescription;
        }

        private static string FormatDiskHTML(double total, double free, double fail, double warn)
        {
            var totalGB = Math.Round(total / 1024, MidpointRounding.AwayFromZero);
            var freeGB = Math.Round(free / 1024, 1, MidpointRounding.AwayFromZero);
            var freePercent = Math.Round(free / total * 100, 1, MidpointRounding.AwayFromZero);
            var freePercentInt = (int)Math.Round(freePercent, MidpointRounding.AwayFromZero);
            var usedPercentInt = 100 - freePercentInt;

            var statusClass = "diskGood";
            if (free < fail) statusClass = "diskFail";
            else if (warn != 0 && free < warn) statusClass = "diskWarn";

            var message = "Total = " + totalGB + "GB. Free = " + freeGB + "GB (" + freePercent + "%)";
            return "<table cellpadding=\"0\" cellspacing=\"0\"><tr><td>" +
                   "<table width=\"150px\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"height: 10px;width: 150px;\">" +
                   "<tr>" +
                   "<td class=\"" + statusClass + "\" style=\"width: " + usedPercentInt + "%; height: 10px;\"/>" +
                   "<td class=\"diskRest\" style=\"height: 10px; width: " + freePercentInt + "%;\" />" +
                   "</tr>" +
                   "</table></td><td>&nbsp;&nbsp;" + message + "</td></tr></table>";
        }
    }
}
