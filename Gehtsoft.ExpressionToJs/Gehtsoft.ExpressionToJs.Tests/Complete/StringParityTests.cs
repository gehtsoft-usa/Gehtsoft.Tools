using System;
using System.Text.RegularExpressions;
using Xunit;

namespace Gehtsoft.ExpressionToJs.Tests.Complete
{
    /// <summary>Low-layer parity for string operations and string/regex constant emission.</summary>
    public class StringParityTests : DifferentialTestBase
    {
        // ---------- expected GREEN ----------

        [Theory]
        [InlineData("Hello", "HELLO")]
        public void ToUpper(string s, string e) => AssertUnary<string, string>(x => x.ToUpper(), s, e);

        [Theory]
        [InlineData("Hello", "hello")]
        public void ToLower(string s, string e) => AssertUnary<string, string>(x => x.ToLower(), s, e);

        [Theory]
        [InlineData("  hi  ", "hi")]
        public void Trim(string s, string e) => AssertUnary<string, string>(x => x.Trim(), s, e);

        [Theory]
        [InlineData("abcdef", 6)]
        public void Length(string s, int e) => AssertUnary<string, int>(x => x.Length, s, e);

        [Theory]
        [InlineData("abcdef", "bc", true)]
        [InlineData("abcdef", "xy", false)]
        public void Contains(string s, string needle, bool e) => AssertUnary<string, bool>(x => x.Contains(needle), s, e);

        [Theory]
        [InlineData("abcdef", "abc", true)]
        [InlineData("abcdef", "xyz", false)]
        public void StartsWith(string s, string prefix, bool e) => AssertUnary<string, bool>(x => x.StartsWith(prefix), s, e);

        [Theory]
        [InlineData("abcdef", "cd", 2)]
        [InlineData("abcdef", "xy", -1)]
        public void IndexOf(string s, string needle, int e) => AssertUnary<string, int>(x => x.IndexOf(needle), s, e);

        [Theory]
        [InlineData("abcdef", "bcdef")]
        public void Substring(string s, string e) => AssertUnary<string, string>(x => x.Substring(1), s, e);

        // ---------- §1: string constants must be escaped (expected RED) ----------
        // An apostrophe or newline in a constant breaks the emitted single-quoted JS literal.
        [Theory]
        [InlineData("it's a test")]
        [InlineData("new\nline")]
        public void StringConstant_SpecialChars_ProduceValidJs(string value)
            => AssertUnary<string, bool>(x => x == value, value, true);

        // A backslash is interpreted as a JS escape and silently corrupts the value.
        [Fact]
        public void StringConstant_Backslash_RoundTrips()
            => AssertUnary<string, bool>(x => x == "a\\b", "a\\b", true);

        // ---------- §1: regex source must be escaped (expected RED) ----------
        // A '/' inside the pattern terminates the emitted /.../ literal.
        [Theory]
        [InlineData("a/b", "a/b")]
        public void RegexConstant_WithSlash_ProducesValidJs(string pattern, string input)
            => AssertUnary<string, bool>(x => Regex.IsMatch(x, pattern), input, true);

        // ---------- defect probe: string concatenation via '+' (expected RED) ----------
        // `string + string` compiles to String.Concat, which AddCall does not handle.
        [Theory]
        [InlineData("hi", "hi!")]
        public void StringConcatenation_IsSupported(string s, string expected)
            => AssertUnary<string, string>(x => x + "!", s, expected);
    }
}
