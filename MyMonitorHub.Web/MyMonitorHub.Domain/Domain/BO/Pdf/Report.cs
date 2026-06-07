using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Haley.RotateText;
using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using PageSize = iText.Kernel.Geom.PageSize;
using PdfBorder = iText.Layout.Borders.Border;
using PdfHAlign = iText.Layout.Properties.HorizontalAlignment;
using PdfImage = iText.Layout.Element.Image;
using MyMonitorHub.Domain.BO.Transfer;
using MyMonitorHub.Domain.Entity;
using MyMonitorHub.Domain.Interface;
using MyMonitorHub.Domain.Service;
using MyMonitorHub.Domain.Util;

namespace MyMonitorHub.Domain.BO.Pdf
{
    public class Report
    {
        private readonly IDbContextScopeFactory _contextScopeFactory;

        public Report(IDbContextScopeFactory contextScopeFactory)
        {
            _contextScopeFactory = contextScopeFactory;
        }

        private static readonly PdfFont TimesRoman = PdfFontFactory.CreateFont(StandardFonts.TIMES_ROMAN);
        private static readonly PdfFont TimesBold = PdfFontFactory.CreateFont(StandardFonts.TIMES_BOLD);

        private static readonly DeviceRgb HeaderBg = new DeviceRgb(214, 220, 254);
        private static readonly DeviceRgb BorderGray = new DeviceRgb(206, 206, 206);
        private static readonly DeviceRgb PromptBg = new DeviceRgb(245, 245, 245);
        private static readonly DeviceRgb CellBorderGray = new DeviceRgb(196, 196, 196);

        private bool _isDemo;
        public string PDFPath { get; set; }

        public void SRClosed(int accountId, int serviceRequestId)
        {
            if (accountId < 0)
            {
                accountId = Math.Abs(accountId);
                _isDemo = true;
            }

            string userName, deviceDesc, srId, timestamp, notes, pageDesc, groupDesc, status;
            DeviceGroup group;
            PageTransfer pageTransfer = null;
            List<SRDetailHistory> history;

            using (var scope = _contextScopeFactory.Create())
            {
                var serviceRequest =
                    scope.Get<ServiceRequest>().Where(
                        x => x.AccountId == accountId && x.ServiceRequestId == serviceRequestId)
                        .Select(x => new
                        {
                            x.ServiceRequestId,
                            deviceId = x.DeviceId,
                            deviceDesc = x.Device.Description,
                            assignedToId = x.AssignedToId,
                            x.Device,
                            userName = x.User.Email,
                            notes = x.Notes,
                            timeStamp = x.TimeStamp,
                            status = x.Status
                        }).FirstOrDefault();

                if (serviceRequest == null)
                    throw new Exception("Service request is invalid or does not belong to your account.");

                history = scope.Get<Event>()
                    .Where(x => x.ServiceRequestId == serviceRequest.ServiceRequestId)
                    .AsEnumerable()
                    .GroupBy(ev => ev.Category + "> " + ev.SubCategory)
                    .Select(g => new SRDetailHistory { Description = g.Key, Events = g })
                    .ToList();

                group = serviceRequest.Device.DeviceGroup;
                userName = serviceRequest.userName;
                deviceDesc = serviceRequest.deviceDesc;
                srId = serviceRequest.ServiceRequestId.ToString(CultureInfo.InvariantCulture);
                timestamp = serviceRequest.timeStamp.ToString(CultureInfo.InvariantCulture);
                notes = serviceRequest.notes;

                if (group != null)
                {
                    pageDesc = group.Page.Description;
                    groupDesc = _isDemo ? "<Company Name Goes Here>" : group.Description;
                }
                else
                {
                    pageDesc = "Device has been soft deleted and does not belong to a page";
                    groupDesc = "Device has been soft deleted and does not belong to a device group";
                }

                status = (serviceRequest.assignedToId == null || serviceRequest.status == "O") ? "Open" : "Closed";

                if (group != null)
                    pageTransfer = new PageService(_contextScopeFactory).GetPageInformation(group.Page.PageId, serviceRequest.deviceId, true);
            }

            var path = Path.Combine(ThreadStaticHelper.RootPath,
                "Repository", group.DeviceGroupId.ToString(), "closedSRs");
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            path = _isDemo
                ? Path.Combine(ThreadStaticHelper.RootPath, "Repository", "DemoSr" + serviceRequestId + ".pdf")
                : Path.Combine(ThreadStaticHelper.RootPath, "repository", group.DeviceGroupId.ToString(), "closedSRs", "sr" + serviceRequestId + ".pdf");
            PDFPath = path;

            using var writer = new PdfWriter(new FileStream(path, FileMode.Create));
            using var pdfDoc = new PdfDocument(writer);
            using var doc = new Document(pdfDoc, PageSize.LETTER);
            doc.SetMargins(50f, 25f, 25f, 25f);

            const string introText = "This is an automated email notifying you that the following alert was captured by the monitoring system and has now been resolved.";
            doc.Add(new Paragraph(introText).SetFont(TimesRoman).SetFontSize(10f).SetMarginBottom(10f));

            // Summary table
            var summaryTable = new Table(UnitValue.CreatePercentArray(new float[] { 30f, 70f })).UseAllAvailableWidth().SetMarginBottom(10f);
            summaryTable.AddCell(GetHeaderP1(2).Add(Para("ServiceRequest", TimesBold)));
            AddPromptValueRow(summaryTable, "Id", srId);
            AddPromptValueRow(summaryTable, "Status", status);
            AddPromptValueRow(summaryTable, "Time", timestamp);
            AddPromptValueRow(summaryTable, "Page", pageDesc);
            AddPromptValueRow(summaryTable, "Device Group", groupDesc);
            AddPromptValueRow(summaryTable, "Device", deviceDesc);
            AddPromptValueRow(summaryTable, "Assigned To", userName);
            AddPromptValueRow(summaryTable, "Notes", notes);
            doc.Add(summaryTable);

            // Detail header
            var detailHeaderTable = new Table(1).UseAllAvailableWidth().SetMarginBottom(10f);
            detailHeaderTable.AddCell(GetHeaderP1().Add(Para("Service Request Detail", TimesBold)));
            doc.Add(detailHeaderTable);

            foreach (var detail in history)
            {
                var detailTable = new Table(UnitValue.CreatePercentArray(new float[] { 300f, 200f, 450f, 50f }))
                    .SetWidth(UnitValue.CreatePercentValue(98f))
                    .SetHorizontalAlignment(PdfHAlign.RIGHT);

                detailTable.AddHeaderCell(GetHeaderP1().Add(Para(detail.Description)));
                detailTable.AddHeaderCell(GetHeaderP1().Add(Para("Time")));
                detailTable.AddHeaderCell(GetHeaderP1().Add(Para("Status")));
                detailTable.AddHeaderCell(GetHeaderP1().Add(Para("")));

                foreach (var evt in detail.Events)
                {
                    detailTable.AddCell(GetCellColumnValue().Add(Para(evt.ItemName)));
                    detailTable.AddCell(GetCellColumnValue().Add(Para(evt.ServerReceivedTimeStamp.ToString(CultureInfo.InvariantCulture))));
                    detailTable.AddCell(GetCellColumnValue().SetPadding(0f)
                        .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                        .Add(StatusDescription.FormatPDF(evt.StatusDescription, TimesRoman)));
                    detailTable.AddCell(GetCellColumnValue().SetPadding(0f)
                        .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                        .Add(GetImageTable(evt.Status)));
                }
                doc.Add(detailTable);
            }

            // Current status header
            var currentStatusHeaderTable = new Table(1).UseAllAvailableWidth().SetMarginTop(10f).SetMarginBottom(10f);
            currentStatusHeaderTable.AddCell(GetHeaderP1().Add(Para("Current Device Status", TimesBold)));
            doc.Add(currentStatusHeaderTable);

            if (pageTransfer == null) return;

            var columns = pageTransfer.ColumnHeaders.Length;
            var colWidths = new float[columns + 2];
            colWidths[0] = 300f;
            for (var i = 1; i <= columns; i++) colWidths[i] = 30f;
            colWidths[colWidths.Length - 1] = 1000 - ((columns * 30f) + 300f);

            // Column headers table (with rotated text images)
            var colHeaderTable = new Table(UnitValue.CreatePercentArray(colWidths))
                .SetWidth(UnitValue.CreatePercentValue(98f))
                .SetHorizontalAlignment(PdfHAlign.RIGHT);

            // Measure actual icon column width so header images align exactly with the icons below.
            var sampleIconPath = Path.Combine(ThreadStaticHelper.RootPath, "Content", "ms", "images", "icons16", "dot.gif");
            var sampleIcon = new PdfImage(ImageDataFactory.Create(sampleIconPath));
            var iconColWidth = sampleIcon.GetImageWidth() * 0.75f;

            colHeaderTable.AddCell(new Cell().SetBorder(PdfBorder.NO_BORDER));
            foreach (var col in pageTransfer.ColumnHeaders)
            {
                var defn = new Haley.RotateText.Definition.RotateTextDefn
                {
                    Text = col.Description,
                    Angle = -90,
                    Font = new System.Drawing.Font("Verdana", 8, System.Drawing.GraphicsUnit.Point)
                };
                var bytes = ImageGenerator.GenerateImageBytes(defn);
                var img = new PdfImage(ImageDataFactory.Create(bytes));
                var scaleFactor = iconColWidth / img.GetImageWidth();
                img.Scale(scaleFactor, scaleFactor);
                colHeaderTable.AddCell(new Cell().SetBorder(PdfBorder.NO_BORDER).SetPadding(0f)
                    .SetVerticalAlignment(VerticalAlignment.BOTTOM).Add(img));
            }
            colHeaderTable.AddCell(new Cell().SetBorder(PdfBorder.NO_BORDER));
            doc.Add(colHeaderTable);

            // Data rows
            var dataTable = new Table(UnitValue.CreatePercentArray(colWidths))
                .SetWidth(UnitValue.CreatePercentValue(98f))
                .SetHorizontalAlignment(PdfHAlign.RIGHT);

            foreach (var data in pageTransfer.DeviceGroups[0].Devices)
            {
                dataTable.AddCell(GetCellColumnPrompt().Add(Para(data.Description)));
                foreach (var ct in data.Columns)
                {
                    dataTable.AddCell(GetCellColumnPrompt().SetPadding(0f)
                        .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                        .Add(GetImageTable(ct.Code)));
                }
                dataTable.AddCell(GetCellColumnPrompt());
            }
            doc.Add(dataTable);
        }

        private void AddPromptValueRow(Table table, string prompt, string value)
        {
            table.AddCell(GetCellColumnPrompt().Add(Para(prompt)));
            table.AddCell(GetCellColumnValue().Add(Para(value)));
        }

        private static Paragraph Para(string text, PdfFont font = null) =>
            new Paragraph(text ?? "").SetFont(font ?? TimesRoman).SetFontSize(10f);

        public Cell GetHeaderP1(int colspan = 1) => new Cell(1, colspan)
            .SetBackgroundColor(HeaderBg)
            .SetBorderLeft(PdfBorder.NO_BORDER).SetBorderRight(PdfBorder.NO_BORDER)
            .SetBorderTop(new SolidBorder(BorderGray, 1f))
            .SetBorderBottom(new SolidBorder(BorderGray, 1f))
            .SetVerticalAlignment(VerticalAlignment.TOP)
            .SetPaddingTop(0f);

        public Cell GetCellColumnPrompt() => new Cell()
            .SetBackgroundColor(PromptBg)
            .SetBorderLeft(PdfBorder.NO_BORDER).SetBorderRight(PdfBorder.NO_BORDER).SetBorderTop(PdfBorder.NO_BORDER)
            .SetBorderBottom(new SolidBorder(CellBorderGray, 1f))
            .SetPaddingLeft(10f)
            .SetVerticalAlignment(VerticalAlignment.TOP)
            .SetPaddingTop(0f);

        public Cell GetDeviceClass() => new Cell()
            .SetBackgroundColor(HeaderBg)
            .SetBorderLeft(PdfBorder.NO_BORDER).SetBorderRight(PdfBorder.NO_BORDER)
            .SetBorderTop(new SolidBorder(BorderGray, 1f))
            .SetBorderBottom(new SolidBorder(BorderGray, 1f))
            .SetVerticalAlignment(VerticalAlignment.TOP)
            .SetPaddingTop(0f);

        public Cell GetCategoryClass() => new Cell()
            .SetBorderLeft(PdfBorder.NO_BORDER).SetBorderRight(PdfBorder.NO_BORDER).SetBorderTop(PdfBorder.NO_BORDER)
            .SetBorderBottom(new SolidBorder(CellBorderGray, 0.5f))
            .SetVerticalAlignment(VerticalAlignment.TOP)
            .SetPaddingTop(0f);

        public Cell GetSubCategoryClass() => new Cell()
            .SetBorder(PdfBorder.NO_BORDER)
            .SetVerticalAlignment(VerticalAlignment.TOP)
            .SetPaddingTop(0f);

        public Cell GetItemClass() => new Cell()
            .SetBorder(PdfBorder.NO_BORDER)
            .SetVerticalAlignment(VerticalAlignment.TOP)
            .SetPaddingTop(0f);

        public Cell GetCellColumnValue() => new Cell()
            .SetBorderLeft(PdfBorder.NO_BORDER).SetBorderRight(PdfBorder.NO_BORDER).SetBorderTop(PdfBorder.NO_BORDER)
            .SetBorderBottom(new SolidBorder(CellBorderGray, 1f))
            .SetVerticalAlignment(VerticalAlignment.TOP)
            .SetPaddingTop(0f);

        public Table GetImageTable(int? status)
        {
            var name = status == null ? "dot" : status.ToString();
            return GetImageTable(name!);
        }

        public Table GetImageTable(string name)
        {
            var path = Path.Combine(ThreadStaticHelper.RootPath, "Content", "ms", "images", "icons16", name + ".gif");
            if (!File.Exists(path))
                path = Path.Combine(ThreadStaticHelper.RootPath, "Content", "ms", "images", "icons16", "dot.gif");
            var img = new PdfImage(ImageDataFactory.Create(path));
            img.Scale(0.75f, 0.75f);
            return new Table(1).SetWidth(img.GetImageWidth() * 0.75f).SetFixedLayout()
                .AddCell(new Cell().SetBorder(PdfBorder.NO_BORDER).SetPadding(0f).Add(img)
                    .SetVerticalAlignment(VerticalAlignment.MIDDLE));
        }

        public Table GetLogo()
        {
            var path = Path.Combine(ThreadStaticHelper.RootPath, "Content", "ms", "images", "Logo.png");
            var img = new PdfImage(ImageDataFactory.Create(path));
            img.Scale(0.35f, 0.35f);
            return new Table(1).SetWidth(img.GetImageWidth() * 0.35f).SetFixedLayout()
                .SetHorizontalAlignment(PdfHAlign.LEFT)
                .AddCell(new Cell().SetBorder(PdfBorder.NO_BORDER).SetPadding(0f).Add(img)
                    .SetVerticalAlignment(VerticalAlignment.MIDDLE));
        }

        public class SRDetailHistory
        {
            public string Description { get; set; }
            public IGrouping<string, Event> Events { get; set; }
        }
    }
}
