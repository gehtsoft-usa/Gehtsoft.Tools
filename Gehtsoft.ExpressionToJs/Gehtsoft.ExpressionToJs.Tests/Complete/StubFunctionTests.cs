using System;
using Xunit;

namespace Gehtsoft.ExpressionToJs.Tests.Complete
{
    /// <summary>
    /// Direct tests of the jsv_* runtime helpers for edge inputs that a typed C# lambda
    /// cannot express (cross-type comparison, whitespace shapes, lenient parsing).
    /// </summary>
    public class StubFunctionTests : DifferentialTestBase
    {
        private static bool Bool(string js) => Convert.ToBoolean(EvalJs(js));

        // ---------- expected GREEN ----------
        [Theory]
        [InlineData("jsv_equal(10, 10)", true)]
        [InlineData("jsv_equal('a', 'a')", true)]
        [InlineData("jsv_equal(1, 2)", false)]
        public void Equality_SameType(string js, bool expected) => Assert.Equal(expected, Bool(js));

        // ---------- §1: equality must be strict to mirror C#'s typed == (expected RED) ----------
        // jsv_equal uses '==', so string/number and null/undefined coerce; C# never does.
        [Theory]
        [InlineData("jsv_equal('10', 10)", false)]
        [InlineData("jsv_equal(0, '')", false)]
        [InlineData("jsv_equal(null, undefined)", false)]
        [InlineData("jsv_equal(false, 0)", false)]
        public void Equality_IsStrict(string js, bool expected) => Assert.Equal(expected, Bool(js));

        [Theory]
        [InlineData("jsv_notequal('10', 10)", true)]
        [InlineData("jsv_notequal(null, undefined)", true)]
        public void Inequality_IsStrict(string js, bool expected) => Assert.Equal(expected, Bool(js));

        // ---------- §1: whitespace detection must mirror string.IsNullOrWhiteSpace (expected RED) ----------
        // jsv_isemptyorwhitespace tests /^ *$/ - only spaces, so tabs/newlines look non-empty.
        [Theory]
        [InlineData("jsv_isemptyorwhitespace('')", true)]
        [InlineData("jsv_isemptyorwhitespace('   ')", true)]
        [InlineData("jsv_isemptyorwhitespace('x')", false)]
        public void WhitespaceDetection_Spaces(string js, bool expected) => Assert.Equal(expected, Bool(js));

        [Theory]
        [InlineData("jsv_isemptyorwhitespace('\\t')", true)]
        [InlineData("jsv_isemptyorwhitespace('\\r\\n')", true)]
        [InlineData("jsv_isemptyorwhitespace('\\u00a0')", true)]
        public void WhitespaceDetection_NonSpaceWhitespace(string js, bool expected) => Assert.Equal(expected, Bool(js));

        // ---------- §1: integer parsing must reject trailing garbage (expected RED) ----------
        // C# Int32.Parse("12abc") throws; jsv_string2int (parseInt) silently returns 12.
        [Fact]
        public void String2Int_RejectsTrailingGarbage()
        {
            object r = EvalJs("jsv_string2int('12abc')");
            Assert.True(double.IsNaN(Convert.ToDouble(r)), $"expected NaN for '12abc' but got '{r}'");
        }

        [Theory]
        [InlineData("jsv_string2int('50')", 50)]
        [InlineData("jsv_string2int('-7')", -7)]
        public void String2Int_ParsesIntegers(string js, int expected) => Assert.Equal(expected, Convert.ToInt32(EvalJs(js)));
    }
}
