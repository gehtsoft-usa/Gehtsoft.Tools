using System;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Gehtsoft.ExpressionToJs.Tests.Complete
{
    /// <summary>
    /// Locks the constant-folding behavior of GetExpressionValue after replacing the
    /// compile-every-node approach with a free-parameter check.
    /// </summary>
    public class ConstantFoldingTests
    {
        private static string Compile<TA, TR>(Expression<Func<TA, TR>> expr)
            => new ExpressionCompiler(expr).JavaScriptExpression;

        [Fact]
        public void ClosedExpression_FoldsToConstant()
        {
            // No free parameter -> evaluated server-side and emitted as a literal.
            Assert.Equal("true", Compile<int, bool>(x => 2 + 2 == 4));
        }

        [Fact]
        public void CapturedValue_FoldsToConstant()
        {
            const int five = 5;
            Assert.Equal("25", Compile<int, int>(x => five * 5));
        }

        [Fact]
        public void LambdaBoundParameter_OverConstantCollection_StillFolds()
        {
            // 'n' is bound by the predicate lambda and the source is a captured constant, so the
            // whole call has no FREE parameter and must fold to a literal - it must not be mistaken
            // for a parameter reference (which would emit jsv_any over an unsupported constant).
            int[] nums = { 1, 2, 3 };
            Assert.Equal("true", Compile<int, bool>(x => nums.Any(n => n > 0)));
        }

        [Fact]
        public void FreeParameter_IsNotFolded()
        {
            // References the parameter -> translated structurally rather than evaluated.
            Assert.Equal("jsv_greater(jsv_length(x), 0)", Compile<string, bool>(x => x.Length > 0));
        }
    }
}
