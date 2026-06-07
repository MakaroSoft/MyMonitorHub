using System;
using System.Collections.Generic;
using System.IO;

namespace MyMonitorHub.Common.Util
{
    /// <summary>
    ///     StringTokenizer splits input into tokens using only whitespace as a separator.
    ///     Any contiguous run of non-whitespace characters becomes a single <see cref="TokenKind.Word"/>,
    ///     except that text inside double quotes is captured as a <see cref="TokenKind.QuotedString"/>
    ///     and preserves internal whitespace.
    /// </summary>
    public class StringTokenizer
    {
        private const char EOF = (char) 0;

        private readonly string _data;
        private int _column;

        private bool _ignoreEol;
        private bool _ignoreWhiteSpace;
        private int _line;
        private int _pos; // position within data
        private int _saveCol;
        private int _saveLine;
        private int _savePos;

        public StringTokenizer(TextReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException("reader");

            _data = reader.ReadToEnd();

            Reset();
        }

        public StringTokenizer(string data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            _data = data;

            Reset();
        }

        /// <summary>
        ///     if set to true, white space characters will be ignored,
        ///     but EOL and whitespace inside of string will still be tokenized
        /// </summary>
        public bool IgnoreWhiteSpace
        {
            get { return _ignoreWhiteSpace; }
            set { _ignoreWhiteSpace = value; }
        }

        /// <summary>
        ///     if set to true, EOL characters will be ignored,
        ///     but EOL and whitespace inside of string will still be tokenized
        /// </summary>
        public bool IgnoreEOL
        {
            get { return _ignoreEol; }
            set { _ignoreEol = value; }
        }

        private void Reset()
        {
            _ignoreWhiteSpace = false;
            _ignoreEol = false;

            _line = 1;
            _column = 1;
            _pos = 0;
        }

        protected char La(int count)
        {
            if (_pos + count >= _data.Length)
                return EOF;
            return _data[_pos + count];
        }

        protected char Consume()
        {
            var ret = _data[_pos];
            _pos++;
            _column++;

            return ret;
        }

        protected Token CreateToken(TokenKind kind, string value)
        {
            return new Token(kind, value, _line, _column);
        }

        protected Token CreateToken(TokenKind kind)
        {
            var tokenData = _data.Substring(_savePos, _pos - _savePos);
            return new Token(kind, tokenData, _saveLine, _saveCol);
        }

        public Token Next()
        {
            ReadToken:

            var ch = La(0);
            switch (ch)
            {
                case EOF:
                    return CreateToken(TokenKind.EOF, "EOF");

                case ' ':
                case '\t':
                {
                    if (_ignoreWhiteSpace)
                    {
                        Consume();
                        goto ReadToken;
                    }
                    return ReadWhitespace();
                }

                case '\r':
                {
                    if (_ignoreEol)
                    {
                        Consume();
                        goto ReadToken;
                    }
                    StartRead();
                    Consume();
                    if (La(0) == '\n')
                        Consume(); // on DOS/Windows we have \r\n for new line

                    _line++;
                    _column = 1;

                    return CreateToken(TokenKind.EOL);
                }
                case '\n':
                {
                    if (_ignoreEol)
                    {
                        Consume();
                        goto ReadToken;
                    }
                    StartRead();
                    Consume();
                    _line++;
                    _column = 1;

                    return CreateToken(TokenKind.EOL);
                }

                case '"':
                {
                    return ReadString();
                }

                default:
                {
                    return ReadAtom();
                }
            }
        }

        /// <summary>
        ///     save read point positions so that CreateToken can use those
        /// </summary>
        private void StartRead()
        {
            _saveLine = _line;
            _saveCol = _column;
            _savePos = _pos;
        }

        /// <summary>
        ///     reads all whitespace characters (does not include newline)
        /// </summary>
        protected Token ReadWhitespace()
        {
            StartRead();

            Consume(); // consume the looked-ahead whitespace char

            while (true)
            {
                var ch = La(0);
                if (ch == '\t' || ch == ' ')
                    Consume();
                else
                    break;
            }

            return CreateToken(TokenKind.WhiteSpace);
        }

        /// <summary>
        ///     reads an atom: a contiguous run of non-whitespace, non-EOF characters.
        ///     Used for everything that isn't whitespace, EOL, or a quoted string.
        /// </summary>
        protected Token ReadAtom()
        {
            StartRead();

            Consume(); // consume first character of the atom

            while (true)
            {
                var ch = La(0);
                if (ch == EOF || ch == ' ' || ch == '\t' || ch == '\r' || ch == '\n')
                    break;
                Consume();
            }

            return CreateToken(TokenKind.Word);
        }

        /// <summary>
        ///     reads all characters until next " is found.
        ///     If "" (2 quotes) are found, then they are consumed as
        ///     part of the string
        /// </summary>
        protected Token ReadString()
        {
            StartRead();

            Consume(); // read "

            while (true)
            {
                var ch = La(0);
                if (ch == EOF)
                    break;
                if (ch == '\r') // handle CR in strings
                {
                    Consume();
                    if (La(0) == '\n') // for DOS & windows
                        Consume();

                    _line++;
                    _column = 1;
                }
                else if (ch == '\n') // new line in quoted string
                {
                    Consume();

                    _line++;
                    _column = 1;
                }
                else if (ch == '"')
                {
                    Consume();
                    if (La(0) != '"')
                        break; // done reading, and this quotes does not have escape character
                    Consume(); // consume second ", because first was just an escape
                }
                else
                    Consume();
            }

            return CreateToken(TokenKind.QuotedString);
        }

        public string[] GetTokensAsStringArray()
        {
            var list = new List<string>();
            Token token;
            do
            {
                token = Next();
                list.Add(token.Value);
            } while (token.Kind != TokenKind.EOF);
            return list.ToArray();
        }

        public Token[] GetTokensAsArray()
        {
            var list = new List<Token>();
            Token token;
            do
            {
                token = Next();
                list.Add(token);
            } while (token.Kind != TokenKind.EOF);
            return list.ToArray();
        }
    }
}
