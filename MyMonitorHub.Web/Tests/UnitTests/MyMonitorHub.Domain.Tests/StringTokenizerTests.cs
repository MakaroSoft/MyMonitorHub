using System.Collections.Generic;
using System.Linq;
using MyMonitorHub.Common.Util;
using Xunit;

namespace MyMonitorHub.Domain.Tests
{
    public class StringTokenizerTests
    {
        private static List<Token> Tokenize(string input, bool ignoreWs = true, bool ignoreEol = true)
        {
            var tok = new StringTokenizer(input)
            {
                IgnoreWhiteSpace = ignoreWs,
                IgnoreEOL = ignoreEol,
            };
            return tok.GetTokensAsArray()
                .Where(t => t.Kind != TokenKind.EOF)
                .ToList();
        }

        [Fact]
        public void EmptyString_ReturnsOnlyEof()
        {
            var tok = new StringTokenizer("");
            var all = tok.GetTokensAsArray();

            Assert.Single(all);
            Assert.Equal(TokenKind.EOF, all[0].Kind);
        }

        [Fact]
        public void SingleWord_ReturnsOneWordToken()
        {
            var tokens = Tokenize("hello");

            Assert.Single(tokens);
            Assert.Equal(TokenKind.Word, tokens[0].Kind);
            Assert.Equal("hello", tokens[0].Value);
        }

        [Fact]
        public void MultipleWords_SplitOnSingleSpace()
        {
            var tokens = Tokenize("a b c");

            Assert.Equal(3, tokens.Count);
            Assert.All(tokens, t => Assert.Equal(TokenKind.Word, t.Kind));
            Assert.Equal(new[] { "a", "b", "c" }, tokens.Select(t => t.Value));
        }

        [Fact]
        public void MultipleWords_SplitOnMultipleSpacesAndTabs()
        {
            var tokens = Tokenize("a   b\tc");

            Assert.Equal(3, tokens.Count);
            Assert.Equal(new[] { "a", "b", "c" }, tokens.Select(t => t.Value));
        }

        [Fact]
        public void EmailAddress_StaysAsOneToken()
        {
            var tokens = Tokenize("user@example.com");

            Assert.Single(tokens);
            Assert.Equal(TokenKind.Word, tokens[0].Kind);
            Assert.Equal("user@example.com", tokens[0].Value);
        }

        [Fact]
        public void TimeWithColonAndAmSuffix_StaysAsOneToken()
        {
            var tokens = Tokenize("8:00am");

            Assert.Single(tokens);
            Assert.Equal(TokenKind.Word, tokens[0].Kind);
            Assert.Equal("8:00am", tokens[0].Value);
        }

        [Fact]
        public void PunctuationInsideAtom_StaysAsOneToken()
        {
            var tokens = Tokenize("a,b;c.d");

            Assert.Single(tokens);
            Assert.Equal(TokenKind.Word, tokens[0].Kind);
            Assert.Equal("a,b;c.d", tokens[0].Value);
        }

        [Fact]
        public void QuotedString_PreservesInternalSpaces()
        {
            var tokens = Tokenize("\"a quick brown fox\"");

            Assert.Single(tokens);
            Assert.Equal(TokenKind.QuotedString, tokens[0].Kind);
            Assert.Equal("\"a quick brown fox\"", tokens[0].Value);
        }

        [Fact]
        public void QuotedStringAdjacentToWord_RemainSeparateTokens()
        {
            var tokens = Tokenize("for page \"Server Health\"");

            Assert.Equal(3, tokens.Count);
            Assert.Equal(TokenKind.Word, tokens[0].Kind);
            Assert.Equal("for", tokens[0].Value);
            Assert.Equal(TokenKind.Word, tokens[1].Kind);
            Assert.Equal("page", tokens[1].Value);
            Assert.Equal(TokenKind.QuotedString, tokens[2].Kind);
            Assert.Equal("\"Server Health\"", tokens[2].Value);
        }

        [Fact]
        public void EmptyQuotedString_ReturnsOneToken()
        {
            var tokens = Tokenize("\"\"");

            Assert.Single(tokens);
            Assert.Equal(TokenKind.QuotedString, tokens[0].Kind);
            Assert.Equal("\"\"", tokens[0].Value);
        }

        [Fact]
        public void EscapedDoubleQuotesInString_StaysAsOneToken()
        {
            var tokens = Tokenize("\"he said \"\"hi\"\"\"");

            Assert.Single(tokens);
            Assert.Equal(TokenKind.QuotedString, tokens[0].Kind);
            Assert.Equal("\"he said \"\"hi\"\"\"", tokens[0].Value);
        }

        [Fact]
        public void IgnoreWhiteSpaceFalse_EmitsWhiteSpaceTokens()
        {
            var tokens = Tokenize("a b", ignoreWs: false);

            Assert.Equal(3, tokens.Count);
            Assert.Equal(TokenKind.Word, tokens[0].Kind);
            Assert.Equal(TokenKind.WhiteSpace, tokens[1].Kind);
            Assert.Equal(TokenKind.Word, tokens[2].Kind);
        }

        [Fact]
        public void IgnoreEolFalse_EmitsEolTokens()
        {
            var tokens = Tokenize("a\nb", ignoreEol: false);

            Assert.Equal(3, tokens.Count);
            Assert.Equal(TokenKind.Word, tokens[0].Kind);
            Assert.Equal(TokenKind.EOL, tokens[1].Kind);
            Assert.Equal(TokenKind.Word, tokens[2].Kind);
        }

        [Fact]
        public void WordsSeparatedByNewlines_SplitWhenEolIgnored()
        {
            var tokens = Tokenize("a\nb");

            Assert.Equal(2, tokens.Count);
            Assert.Equal("a", tokens[0].Value);
            Assert.Equal("b", tokens[1].Value);
        }

        [Fact]
        public void FullRuleExample_TokenizesAsExpected()
        {
            var tokens = Tokenize("user user@example.com gets alerts between 8:00am and 8:00pm");

            Assert.Equal(8, tokens.Count);
            Assert.All(tokens, t => Assert.Equal(TokenKind.Word, t.Kind));
            Assert.Equal(
                new[] { "user", "user@example.com", "gets", "alerts", "between", "8:00am", "and", "8:00pm" },
                tokens.Select(t => t.Value));
        }
    }
}
