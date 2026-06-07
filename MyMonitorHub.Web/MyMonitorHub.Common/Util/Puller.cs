using System;

namespace MyMonitorHub.Common.Util
{
    public class Puller
    {
        private readonly string _text;
        private int _lastPosition = -1;
        public Puller(string text)
        {
            _text = text;
        }

        public string Pull()
        {
            if (_lastPosition + 1 == _text.Length)
            {
                return "";
            }
            var index = _text.IndexOf("|", _lastPosition + 1, StringComparison.Ordinal);
            if (index == -1)
            {
                var result = _text.Substring(_lastPosition + 1);
                _lastPosition = _text.Length - 1;
                return result;
            }
            var result2 = _text.Substring(_lastPosition + 1, (index - _lastPosition) - 1);
            _lastPosition = index;
            return result2;
        }

        public string Tail
        {
            get
            {
                if (_lastPosition + 1 == _text.Length)
                {
                    return "";
                }
                return _text.Substring(_lastPosition + 1);
            }
        }
    }
}
