using System;
using System.Collections.Generic;
using System.Reflection;
using MyMonitorHub.Domain.BO;
using MyMonitorHub.Domain.Service;
using Xunit;

namespace MyMonitorHub.Domain.Tests
{
    public class RuleParserTests
    {
        private static AccountService.Descriptions MakeDescriptions(
            string page = "",
            string group = "",
            string device = "",
            string category = "")
        {
            return new AccountService.Descriptions
            {
                Page = page,
                DeviceGroup = group,
                Device = device,
                Catagory = category,
            };
        }

        private static List<Times> GetTimesList(RuleParser parser)
        {
            var field = typeof(RuleParser).GetField("_timesList", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);
            return (List<Times>)field!.GetValue(parser)!;
        }

        private static RuleParser ParseRule(string rule)
        {
            var parser = new RuleParser(rule);
            parser.Parse();
            return parser;
        }

        // ---------- Parse: success ----------

        [Fact]
        public void Parse_MinimalRule_SetsUser()
        {
            var parser = ParseRule("user troy gets alerts");

            Assert.Equal("troy", parser.GetUser());
        }

        [Fact]
        public void Parse_EmailUser_SetsFullEmail()
        {
            var parser = ParseRule("user user@example.com gets alerts");

            Assert.Equal("user@example.com", parser.GetUser());
        }

        [Fact]
        public void Parse_ForPage_StoresPage()
        {
            var parser = ParseRule("user troy gets alerts for page \"Server Health\"");

            Assert.True(parser.Match(MakeDescriptions(page: "Server Health")));
            Assert.False(parser.Match(MakeDescriptions(page: "Other")));
        }

        [Fact]
        public void Parse_ForDeviceQuoted_StoresDevice()
        {
            var parser = ParseRule("user troy gets alerts for device \"router-1\"");

            Assert.True(parser.Match(MakeDescriptions(device: "router-1")));
            Assert.False(parser.Match(MakeDescriptions(device: "switch-2")));
        }

        [Fact]
        public void Parse_ForDeviceGroup_StoresGroup()
        {
            var parser = ParseRule("user troy gets alerts for device group \"US-East\"");

            Assert.True(parser.Match(MakeDescriptions(group: "US-East")));
            Assert.False(parser.Match(MakeDescriptions(group: "US-West")));
        }

        [Fact]
        public void Parse_ForDeviceType_StoresType()
        {
            var parser = ParseRule("user troy gets alerts for device type \"Router\"");

            Assert.True(parser.Match(MakeDescriptions(category: "Router")));
            Assert.False(parser.Match(MakeDescriptions(category: "Switch")));
        }

        [Fact]
        public void Parse_MultipleForClauses_AllStored()
        {
            var parser = ParseRule(
                "user troy gets alerts for page \"P1\" for page \"P2\" for device \"D1\"");

            Assert.True(parser.Match(MakeDescriptions(page: "P1", device: "D1")));
            Assert.True(parser.Match(MakeDescriptions(page: "P2", device: "D1")));
            Assert.False(parser.Match(MakeDescriptions(page: "P3", device: "D1")));
            Assert.False(parser.Match(MakeDescriptions(page: "P1", device: "D2")));
        }

        [Fact]
        public void Parse_BetweenTwelveHourTimes_NormalizesToTwentyFourHour()
        {
            var parser = ParseRule("user troy gets alerts between 9am and 5pm");

            var times = GetTimesList(parser);
            Assert.Single(times);
            Assert.Equal("09:00", times[0].From);
            Assert.Equal("17:00", times[0].To);
        }

        [Fact]
        public void Parse_BetweenTwentyFourHourTimes()
        {
            var parser = ParseRule("user troy gets alerts between 09:00 and 17:00");

            var times = GetTimesList(parser);
            Assert.Single(times);
            Assert.Equal("09:00", times[0].From);
            Assert.Equal("17:00", times[0].To);
        }

        [Fact]
        public void Parse_BetweenWithMinutes()
        {
            var parser = ParseRule("user troy gets alerts between 8:30am and 5:45pm");

            var times = GetTimesList(parser);
            Assert.Single(times);
            Assert.Equal("08:30", times[0].From);
            Assert.Equal("17:45", times[0].To);
        }

        [Fact]
        public void Parse_OvernightSplit_ProducesTwoRanges()
        {
            var parser = ParseRule("user troy gets alerts between 10pm and 2am");

            var times = GetTimesList(parser);
            Assert.Equal(2, times.Count);
            Assert.Equal("22:00", times[0].From);
            Assert.Equal("23:59", times[0].To);
            Assert.Equal("00:00", times[1].From);
            Assert.Equal("02:00", times[1].To);
        }

        [Fact]
        public void Parse_TwelveAm_MapsToMidnight()
        {
            var parser = ParseRule("user troy gets alerts between 12am and 1am");

            var times = GetTimesList(parser);
            Assert.Single(times);
            Assert.Equal("00:00", times[0].From);
            Assert.Equal("01:00", times[0].To);
        }

        [Fact]
        public void Parse_TwelvePm_MapsToNoon()
        {
            var parser = ParseRule("user troy gets alerts between 12pm and 1pm");

            var times = GetTimesList(parser);
            Assert.Single(times);
            Assert.Equal("12:00", times[0].From);
            Assert.Equal("13:00", times[0].To);
        }

        // ---------- Parse: failure ----------

        [Fact]
        public void Parse_MissingUserKeyword_Throws()
        {
            Assert.Throws<Exception>(() => ParseRule("foo bar gets alerts"));
        }

        [Fact]
        public void Parse_SpacedEmail_Throws()
        {
            Assert.Throws<Exception>(() => ParseRule("user example @ example.com gets alerts"));
        }

        [Fact]
        public void Parse_SpacedTime_Throws()
        {
            Assert.Throws<Exception>(() => ParseRule("user troy gets alerts between 9 : 00 am and 5pm"));
        }

        [Fact]
        public void Parse_BadKeywordAfterFor_Throws()
        {
            Assert.Throws<Exception>(() => ParseRule("user troy gets alerts for elephant \"big\""));
        }

        [Fact]
        public void Parse_DeviceWithoutQuotedName_Throws()
        {
            Assert.Throws<Exception>(() => ParseRule("user troy gets alerts for device foo"));
        }

        [Fact]
        public void Parse_InvalidAmHour_Throws()
        {
            Assert.Throws<Exception>(() => ParseRule("user troy gets alerts between 13am and 2pm"));
        }

        [Fact]
        public void Parse_InvalidMinutes_Throws()
        {
            Assert.Throws<Exception>(() => ParseRule("user troy gets alerts between 8:75am and 9am"));
        }

        [Fact]
        public void Parse_Invalid24HourTime_Throws()
        {
            Assert.Throws<Exception>(() => ParseRule("user troy gets alerts between 25 and 26"));
        }

        // ---------- Match: filter logic (no time clauses) ----------

        [Fact]
        public void Match_NoFilters_ReturnsTrue()
        {
            var parser = ParseRule("user troy gets alerts");

            Assert.True(parser.Match(MakeDescriptions(page: "anything", device: "x")));
        }

        [Fact]
        public void Match_PageMatches_CaseInsensitive_ReturnsTrue()
        {
            var parser = ParseRule("user troy gets alerts for page \"Server Health\"");

            Assert.True(parser.Match(MakeDescriptions(page: "server health")));
            Assert.True(parser.Match(MakeDescriptions(page: "SERVER HEALTH")));
        }

        [Fact]
        public void Match_PageDoesNotMatch_ReturnsFalse()
        {
            var parser = ParseRule("user troy gets alerts for page \"Server Health\"");

            Assert.False(parser.Match(MakeDescriptions(page: "Database")));
        }

        [Fact]
        public void Match_DeviceMatches_ReturnsTrue()
        {
            var parser = ParseRule("user troy gets alerts for device \"router-1\"");

            Assert.True(parser.Match(MakeDescriptions(device: "ROUTER-1")));
        }

        [Fact]
        public void Match_DeviceGroupMatches_ReturnsTrue()
        {
            var parser = ParseRule("user troy gets alerts for device group \"US-East\"");

            Assert.True(parser.Match(MakeDescriptions(group: "us-east")));
        }

        [Fact]
        public void Match_DeviceTypeMatches_ReturnsTrue()
        {
            var parser = ParseRule("user troy gets alerts for device type \"Router\"");

            Assert.True(parser.Match(MakeDescriptions(category: "ROUTER")));
        }

        [Fact]
        public void Match_OneFilterFails_OthersPass_ReturnsFalse()
        {
            var parser = ParseRule(
                "user troy gets alerts for page \"P1\" for device \"D1\" for device type \"T1\"");

            Assert.False(parser.Match(MakeDescriptions(page: "P1", device: "D1", category: "T2")));
            Assert.False(parser.Match(MakeDescriptions(page: "P1", device: "D2", category: "T1")));
            Assert.False(parser.Match(MakeDescriptions(page: "P2", device: "D1", category: "T1")));
            Assert.True(parser.Match(MakeDescriptions(page: "P1", device: "D1", category: "T1")));
        }
    }
}
