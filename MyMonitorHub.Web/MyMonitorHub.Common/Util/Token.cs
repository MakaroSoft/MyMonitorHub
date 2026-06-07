namespace MyMonitorHub.Common.Util
{
    public enum TokenKind
    {
        Unknown,
        Word,
        Number,
        QuotedString,
        WhiteSpace,
        Symbol,
        EOL,
        EOF
    }

    public class Token
    {
        private readonly int _column;
        private readonly TokenKind _kind;
        private readonly int _line;
        private readonly string _value;

        public Token(TokenKind kind, string value, int line, int column)
        {
            _kind = kind;
            _value = value;
            _line = line;
            _column = column;
        }

        public int Column
        {
            get { return _column; }
        }

        public TokenKind Kind
        {
            get { return _kind; }
        }

        public int Line
        {
            get { return _line; }
        }

        public string Value
        {
            get { return _value; }
        }
    }
}