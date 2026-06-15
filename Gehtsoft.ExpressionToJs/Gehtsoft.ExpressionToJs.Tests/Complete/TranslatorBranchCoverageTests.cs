using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Xunit;

namespace Gehtsoft.ExpressionToJs.Tests.Complete
{
    /// <summary>
    /// Differential coverage for translator branches the broader suite did not yet reach
    /// (coverage plan areas 1-5): <c>.ToString()</c>, the LINQ predicate/selector/default overloads,
    /// case-insensitive string comparisons plus the Regex options/instance forms, the
    /// string-conversion path of <c>AddConvert</c>, and collection <c>Contains</c> (static array and
    /// instance <c>List&lt;T&gt;</c> forms - Jint exposes a JS <c>.length</c> on a bound CLR list, so
    /// both run). Each case runs the C# delegate and the emitted JS in Jint and asserts both agree.
    /// </summary>
    public class TranslatorBranchCoverageTests : DifferentialTestBase
    {
        private static readonly int[] Nums = { 5, 3, 8, 1, 4 };

        // ---------------------------------------------------------- 1. ToString()

        [Fact]
        public void ToString_Int_Matches()
            => AssertUnary<int, bool>(x => x.ToString() == "42", 42, true);

        [Fact]
        public void ToString_Int_Mismatch()
            => AssertUnary<int, bool>(x => x.ToString() == "42", 7, false);

        [Fact]
        public void ToString_String_IsPassthrough()
            => AssertUnary<string, bool>(x => x.ToString() == "abc", "abc", true);

        // ---------------------------------------------------------- 2. LINQ predicate / selector / default overloads

        [Fact]
        public void First_WithPredicate()
            => AssertUnary<int[], int>(a => a.First(x => x > 3), Nums, 5);

        [Fact]
        public void Last_WithPredicate()
            => AssertUnary<int[], int>(a => a.Last(x => x > 3), Nums, 4);

        [Fact]
        public void Min_WithSelector()
            => AssertUnary<int[], int>(a => a.Min(x => -x), Nums, -8);

        [Fact]
        public void FirstOrDefault_WithFallback_NoMatch()
            => AssertUnary<int[], int>(a => a.FirstOrDefault(x => x > 100, -1), Nums, -1);

        [Fact]
        public void FirstOrDefault_WithFallback_Match()
            => AssertUnary<int[], int>(a => a.FirstOrDefault(x => x > 3, -1), Nums, 5);

        [Fact]
        public void LastOrDefault_WithFallback_NoMatch()
            => AssertUnary<int[], int>(a => a.LastOrDefault(x => x > 100, -1), Nums, -1);

        [Fact]
        public void LastOrDefault_WithFallback_Match()
            => AssertUnary<int[], int>(a => a.LastOrDefault(x => x > 3, -1), Nums, 4);

        // ---------------------------------------------------------- 3. case-insensitive string ops + Regex options/instance

        [Theory]
        [InlineData("abcdef", true)]
        [InlineData("ABCDEF", true)]
        [InlineData("xyz", false)]
        public void StartsWith_OrdinalIgnoreCase(string s, bool expected)
            => AssertUnary<string, bool>(x => x.StartsWith("AB", StringComparison.OrdinalIgnoreCase), s, expected);

        [Theory]
        [InlineData("ABcdef", true)]
        [InlineData("abcdef", false)]
        public void StartsWith_Ordinal_IsCaseSensitive(string s, bool expected)
            => AssertUnary<string, bool>(x => x.StartsWith("AB", StringComparison.Ordinal), s, expected);

        [Theory]
        [InlineData("abcdEF", true)]
        [InlineData("abcdef", false)]
        public void EndsWith_Ordinal_IsCaseSensitive(string s, bool expected)
            => AssertUnary<string, bool>(x => x.EndsWith("EF", StringComparison.Ordinal), s, expected);

        [Fact]
        public void IndexOf_OrdinalIgnoreCase()
            => AssertUnary<string, int>(x => x.IndexOf("CD", StringComparison.OrdinalIgnoreCase), "abcdef", 2);

        [Theory]
        [InlineData("ABxx", true)]
        [InlineData("zzzz", false)]
        public void Regex_IsMatch_IgnoreCaseOption(string s, bool expected)
            => AssertUnary<string, bool>(x => Regex.IsMatch(x, "ab", RegexOptions.IgnoreCase), s, expected);

        private static readonly Regex sAcRegex = new Regex("^a.c$", RegexOptions.IgnoreCase);

        [Theory]
        [InlineData("ABC", true)]
        [InlineData("a-c", true)]
        [InlineData("xyz", false)]
        public void Regex_IsMatch_InstanceForm(string s, bool expected)
            => AssertUnary<string, bool>(x => sAcRegex.IsMatch(x), s, expected);

        // ---------------------------------------------------------- 4. AddConvert string-conversion path

        [Fact]
        public void Cast_ObjectToString_EmitsToString()
            => AssertUnary<object, bool>(o => (string)o == "x", (object)"x", true);

        // ---------------------------------------------------------- 5. collection Contains

        [Fact]
        public void ArrayContains_Present()
            => AssertUnary<int[], bool>(a => a.Contains(3), new[] { 1, 2, 3 }, true);

        [Fact]
        public void ArrayContains_Absent()
            => AssertUnary<int[], bool>(a => a.Contains(9), new[] { 1, 2, 3 }, false);

        // Instance List<T>.Contains takes the e.Object != null branch and emits jsv_contains; a CLR
        // List bound into Jint exposes both .length and indexing, so it runs and is checked the same
        // way as the array form. (The emitted call shape is also pinned below.)
        [Fact]
        public void ListContains_InstanceForm_Present()
            => AssertUnary<List<int>, bool>(a => a.Contains(3), new List<int> { 1, 2, 3 }, true);

        [Fact]
        public void ListContains_InstanceForm_Absent()
            => AssertUnary<List<int>, bool>(a => a.Contains(9), new List<int> { 1, 2, 3 }, false);

        [Fact]
        public void ListContains_InstanceForm_EmitsJsvContains()
        {
            Expression<Func<List<int>, bool>> rule = a => a.Contains(3);
            Assert.Equal("jsv_contains(a, 3)", Compile(rule));
        }

        // An IEnumerable<T>-typed source forces the static Enumerable.Contains(source, value) form
        // (e.Object == null, two arguments), which routes through UnwrapCollection - a different
        // branch from the instance List/array forms above.
        [Fact]
        public void EnumerableContains_StaticForm_Present()
            => AssertUnary<IEnumerable<int>, bool>(a => a.Contains(3), new[] { 1, 2, 3 }, true);

        [Fact]
        public void EnumerableContains_StaticForm_Absent()
            => AssertUnary<IEnumerable<int>, bool>(a => a.Contains(9), new[] { 1, 2, 3 }, false);
    }
}
