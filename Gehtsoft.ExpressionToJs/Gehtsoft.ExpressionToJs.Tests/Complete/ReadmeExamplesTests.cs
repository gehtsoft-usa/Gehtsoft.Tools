using System;
using System.Linq.Expressions;
using Xunit;

namespace Gehtsoft.ExpressionToJs.Tests.Complete
{
    /// <summary>Pins the exact emitted JS for the examples shown in the package README / repo README.</summary>
    public class ReadmeExamplesTests
    {
        [Fact]
        public void QuickStart_RangeRule()
        {
            Expression<Func<int, bool>> rule = x => x > 0 && x < 100;
            Assert.Equal("((jsv_greater(x, 0)) && (jsv_less(x, 100)))", new ExpressionCompiler(rule).JavaScriptExpression);
        }

        public class ChangePassword { public string Password { get; set; } public string Confirm { get; set; } }

        [Fact]
        public void ModelRule_PasswordMatch_WithReference()
        {
            Expression<Func<ChangePassword, bool>> rule = m => m.Password == m.Confirm;
            var compiler = new ExpressionCompiler(rule);
            compiler.Parameters.MapReference(_ => true);
            Assert.Equal("jsv_equal(reference('Password'), reference('Confirm'))", compiler.JavaScriptExpression);
        }
    }
}
