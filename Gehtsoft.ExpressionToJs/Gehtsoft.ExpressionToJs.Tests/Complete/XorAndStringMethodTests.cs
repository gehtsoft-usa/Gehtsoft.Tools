using System;
using Xunit;

namespace Gehtsoft.ExpressionToJs.Tests.Complete
{
    /// <summary>Logical/bitwise XOR (^) and the additional string methods.</summary>
    public class XorAndStringMethodTests : DifferentialTestBase
    {
        // ---- ^ : logical (bool) and bitwise (int) ----

        [Theory]
        [InlineData(true, false, true)]
        [InlineData(true, true, false)]
        [InlineData(false, false, false)]
        public void BoolXor(bool a, bool b, bool expected) => AssertBinary<bool, bool, bool>((x, y) => x ^ y, a, b, expected);

        [Fact]
        public void BoolXor_ResultUsableAsBoolean() => AssertBinary<bool, bool, bool>((x, y) => (x ^ y) == true, true, false, true);

        [Theory]
        [InlineData(5, 3, 6)]
        [InlineData(12, 10, 6)]
        [InlineData(0, 0, 0)]
        public void IntXor(int a, int b, int expected) => AssertBinary<int, int, int>((x, y) => x ^ y, a, b, expected);

        // ---- string methods ----

        [Theory]
        [InlineData("abcdef", "def", true)]
        [InlineData("abcdef", "abc", false)]
        public void EndsWith(string s, string suffix, bool e) => AssertUnary<string, bool>(x => x.EndsWith(suffix), s, e);

        [Theory]
        [InlineData("abcDEF", "def", true)]
        [InlineData("abcDEF", "xyz", false)]
        public void EndsWith_IgnoreCase(string s, string suffix, bool e)
            => AssertUnary<string, bool>(x => x.EndsWith(suffix, StringComparison.OrdinalIgnoreCase), s, e);

        [Theory]
        [InlineData("abcabc", "b", "X", "aXcaXc")]
        [InlineData("hello", "l", "L", "heLLo")]
        public void Replace(string s, string oldV, string newV, string e) => AssertUnary<string, string>(x => x.Replace(oldV, newV), s, e);

        [Fact]
        public void Replace_Char() => AssertUnary<string, string>(x => x.Replace('b', 'X'), "abcabc", "aXcaXc");

        [Fact]
        public void PadLeft_Space() => AssertUnary<string, string>(x => x.PadLeft(5), "ab", "   ab");

        [Fact]
        public void PadLeft_Char() => AssertUnary<string, string>(x => x.PadLeft(5, '0'), "ab", "000ab");

        [Fact]
        public void PadLeft_NoChange() => AssertUnary<string, string>(x => x.PadLeft(2), "abcd", "abcd");

        [Fact]
        public void PadRight_Char() => AssertUnary<string, string>(x => x.PadRight(5, '.'), "ab", "ab...");

        [Fact]
        public void TrimStart() => AssertUnary<string, string>(x => x.TrimStart(), "  ab ", "ab ");

        [Fact]
        public void TrimEnd() => AssertUnary<string, string>(x => x.TrimEnd(), " ab  ", " ab");

        [Fact]
        public void ToUpperInvariant() => AssertUnary<string, string>(x => x.ToUpperInvariant(), "abc", "ABC");

        [Fact]
        public void ToLowerInvariant() => AssertUnary<string, string>(x => x.ToLowerInvariant(), "ABC", "abc");

        [Theory]
        [InlineData("abcabc", "b", 4)]
        [InlineData("abcabc", "z", -1)]
        public void LastIndexOf(string s, string needle, int e) => AssertUnary<string, int>(x => x.LastIndexOf(needle), s, e);

        [Fact]
        public void Contains_Char() => AssertUnary<string, bool>(x => x.Contains('c'), "abc", true);
    }
}
