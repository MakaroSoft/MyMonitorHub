using System;
using System.Collections.Generic;
using System.Globalization;
using MyMonitorHub.Common.Util;
using MyMonitorHub.Domain.Service;

namespace MyMonitorHub.Domain.BO
{
    public class Times
    {
        public string From;
        public string To;
    }

    public class RuleParser
    {
        private readonly string _command;
        private List<string> _deviceGroupList;
        private List<string> _deviceList;
        private List<string> _deviceTypeList;
        private int _errorAt;
        private int _index;
        private List<string> _pageList;
        private List<Times> _timesList;
        private Token[] _tokens;

        private string _user;

        public RuleParser(string command)
        {
            _command = command;
        }

        public bool Match(AccountService.Descriptions descriptions)
        {
            var pageName = descriptions.Page;
            var deviceGroupName = descriptions.DeviceGroup;
            var deviceName = descriptions.Device;
            var deviceTypeName = descriptions.Catagory;

            if (!InTimeRange()) return false;
            if (!WatchingPage(pageName)) return false;
            if (!WatchingDeviceGroup(deviceGroupName)) return false;
            if (!WatchingDevice(deviceName)) return false;
            if (!WatchingDeviceType(deviceTypeName)) return false;

            return true;
        }

        private bool InTimeRange()
        {
            if (_timesList.Count == 0) return true;
            foreach (var t in _timesList)
            {
                var currentTime = DateTime.Now.ToString("HH:mm");
                if (String.Compare(currentTime, t.From, StringComparison.Ordinal) >= 0 &&
                    String.Compare(currentTime, t.To, StringComparison.Ordinal) <= 0) return true;
            }
            return false;
        }

        private bool WatchingPage(string pageName)
        {
            if (_pageList.Count == 0) return true;
            foreach (var pageBeingWatched in _pageList)
            {
                if (pageName.Equals(pageBeingWatched, StringComparison.CurrentCultureIgnoreCase)) return true;
            }
            return false;
        }

        private bool WatchingDeviceGroup(string deviceGroupName)
        {
            if (_deviceGroupList.Count == 0) return true;
            foreach (var deviceGroupBeingWatched in _deviceGroupList)
            {
                if (deviceGroupName.Equals(deviceGroupBeingWatched, StringComparison.CurrentCultureIgnoreCase))
                    return true;
            }
            return false;
        }

        private bool WatchingDevice(string deviceName)
        {
            if (_deviceList.Count == 0) return true;
            foreach (var deviceBeingWatched in _deviceList)
            {
                if (deviceName.Equals(deviceBeingWatched, StringComparison.CurrentCultureIgnoreCase)) return true;
            }
            return false;
        }

        private bool WatchingDeviceType(string deviceTypeName)
        {
            if (_deviceTypeList.Count == 0) return true;
            foreach (var deviceTypeBeingWatched in _deviceTypeList)
            {
                if (deviceTypeName.Equals(deviceTypeBeingWatched, StringComparison.CurrentCultureIgnoreCase))
                    return true;
            }
            return false;
        }

        public string GetUser()
        {
            return _user;
        }

        public void Parse()
        {
            try
            {
                StartParse();
            }
            catch (Exception e)
            {
                if (_errorAt != -1)
                {
                    var troy = "command: ";
                    for (var i = 0; i <= _errorAt; i++)
                    {
                        var word = _tokens[i].Value;
                        if (i == _errorAt)
                        {
                            word = "<span class='errorWord'>" + word + "</span>";
                        }
                        troy += word + " ";
                    }
                    var message = e.Message + "<br/>" + troy;
                    throw new Exception(message);
                }
                throw;
            }
        }

        private void StartParse()
        {
            _errorAt = -1;
            _index = -1;
            _timesList = new List<Times>();
            _pageList = new List<string>();
            _deviceGroupList = new List<string>();
            _deviceTypeList = new List<string>();
            _deviceList = new List<string>();

            var tok = new StringTokenizer(_command)
            {
                IgnoreWhiteSpace = true,
                IgnoreEOL = true,
            };

            _tokens = tok.GetTokensAsArray();

            if (Lookahead("user"))
            {
                UserCommand();
            }
            else
            {
                _errorAt = _index + 1;
                throw new Exception("Expecting 'user'");
            }
        }

        private void UserCommand()
        {
            _user = GetNextToken().Value;
            MatchNextWord("gets");
            MatchNextWord("alerts");
            while (IsMore())
            {
                if (Lookahead("for"))
                {
                    if (Lookahead("page"))
                    {
                        DoPage();
                    }
                    else if (Lookahead("device"))
                    {
                        if (Lookahead("group"))
                        {
                            DoDeviceGroup();
                        }
                        else if (Lookahead("type"))
                        {
                            DoDeviceType();
                        }
                        else if (LookaheadQuotedString())
                        {
                            DoDevice();
                        }
                        else
                        {
                            _errorAt = _index + 1;
                            throw new Exception("Expecting 'group', 'type', or quoted string.");
                        }
                    }
                    else
                    {
                        _errorAt = _index + 1;
                        throw new Exception("Expecting 'page' or 'device'");
                    }
                }
                else if (Lookahead("between"))
                {
                    DoBetween();
                }
                else
                {
                    _errorAt = _index + 1;
                    throw new Exception("Expecting 'for' or 'between'");
                }
            }
        }

        private void DoBetween()
        {
            var fromTime = GetTime();
            MatchNextWord("and");
            var toTime = GetTime();

            var t = new Times();
            if (String.Compare(fromTime, toTime, StringComparison.Ordinal) > 0)
            {
                t.From = fromTime;
                t.To = "23:59";
                _timesList.Add(t);
                fromTime = "00:00";
                t = new Times();
            }
            t.From = fromTime;
            t.To = toTime;
            _timesList.Add(t);
        }

        private void DoPage()
        {
            var page = GetNextToken();
            if (page.Kind != TokenKind.QuotedString)
            {
                _errorAt = _index;
                throw new Exception("Expected Quoted String");
            }
            var pageName = page.Value.Substring(1, page.Value.Length - 2);
            _pageList.Add(pageName);
        }

        private void DoDevice()
        {
            var device = GetNextToken();
            if (device.Kind != TokenKind.QuotedString)
            {
                _errorAt = _index;
                throw new Exception("Expected Quoted String");
            }
            var deviceName = device.Value.Substring(1, device.Value.Length - 2);
            _deviceList.Add(deviceName);
        }

        private void DoDeviceType()
        {
            var deviceType = GetNextToken();
            if (deviceType.Kind != TokenKind.QuotedString)
            {
                _errorAt = _index;
                throw new Exception("Expected Quoted String");
            }
            var deviceTypeName = deviceType.Value.Substring(1, deviceType.Value.Length - 2);
            _deviceTypeList.Add(deviceTypeName);
        }

        private void DoDeviceGroup()
        {
            var deviceGroup = GetNextToken();
            if (deviceGroup.Kind != TokenKind.QuotedString)
            {
                _errorAt = _index;
                throw new Exception("Expected Quoted String");
            }
            var deviceGroupName = deviceGroup.Value.Substring(1, deviceGroup.Value.Length - 2);
            _deviceGroupList.Add(deviceGroupName);
        }

        private string GetTime()
        {
            var token = GetNextToken();
            var raw = token.Value;

            var isAm = false;
            var isPm = false;
            if (raw.Length >= 2)
            {
                var suffix = raw.Substring(raw.Length - 2);
                if (string.Equals(suffix, "am", StringComparison.OrdinalIgnoreCase))
                {
                    isAm = true;
                    raw = raw.Substring(0, raw.Length - 2);
                }
                else if (string.Equals(suffix, "pm", StringComparison.OrdinalIgnoreCase))
                {
                    isPm = true;
                    raw = raw.Substring(0, raw.Length - 2);
                }
            }

            var parts = raw.Split(':');
            if (parts.Length > 2)
            {
                _errorAt = _index;
                throw new Exception("Invalid time.");
            }

            int hours;
            var minutes = 0;
            if (!int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out hours))
            {
                _errorAt = _index;
                throw new Exception("Invalid Number.");
            }
            if (parts.Length == 2)
            {
                if (!int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out minutes))
                {
                    _errorAt = _index;
                    throw new Exception("Invalid Number.");
                }
                if (minutes < 0 || minutes > 59)
                {
                    _errorAt = _index;
                    throw new Exception("Invalid Minutes.");
                }
            }

            if (isAm)
            {
                if (hours < 1 || hours > 12)
                {
                    _errorAt = _index;
                    throw new Exception("Invalid hours.");
                }
                if (hours == 12) hours = 0;
            }
            else if (isPm)
            {
                if (hours < 1 || hours > 12)
                {
                    _errorAt = _index;
                    throw new Exception("Invalid hours.");
                }
                if (hours != 12)
                {
                    hours += 12;
                }
            }
            else
            {
                if (hours < 0 || hours > 23)
                {
                    _errorAt = _index;
                    throw new Exception("Invalid 24 hour time.");
                }
            }
            return hours.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0') + ":" + minutes.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0');
        }

        private bool IsMore()
        {
            if (_index + 1 == _tokens.Length || _tokens[_index + 1].Kind == TokenKind.EOF)
            {
                return false;
            }
            return true;
        }

        private Token GetNextToken()
        {
            _index++;
            if (_index >= _tokens.Length)
            {
                _errorAt = -1;
                throw new Exception("No More Tokens");
            }
            return _tokens[_index];
        }

        private bool LookaheadQuotedString()
        {
            var t = GetNextToken();
            _index--; // always back up because I need to read the quoted string later
            if (t.Kind == TokenKind.QuotedString)
            {
                return true;
            }
            return false;
        }

        private bool Lookahead(string match)
        {
            try
            {
                MatchNextWord(match);
                return true;
            }
            catch (Exception)
            {
                _index--;
                return false;
            }
        }

        private void MatchNextWord(string match)
        {
            var word = GetNextToken().Value;
            if (word != match)
            {
                _errorAt = _index;
                throw new Exception("Expecting '" + match + "'");
            }
        }
    } // class
} // namespace