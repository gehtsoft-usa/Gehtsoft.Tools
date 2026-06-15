using System;
using Jint;
using Xunit;

namespace Gehtsoft.ExpressionToJs.Tests.Complete
{
    /// <summary>
    /// Deterministic, engine-independent checks on how <see cref="ExpressionCompiler.AddConstant"/>
    /// renders literals. These do not depend on the host timezone.
    /// </summary>
    public class ConstantEmissionTests
    {
        // ---------- expected GREEN ----------

        [Theory]
        [InlineData("plain")]
        [InlineData("with spaces")]
        public void StringConstant_Plain_RoundTrips(string value)
        {
            object back = new Engine().Evaluate(ExpressionCompiler.AddConstant(value)).ToObject();
            Assert.Equal(value, back);
        }

        // ---------- §1: string constants must be JS-escaped (expected RED) ----------
        [Theory]
        [InlineData("it's")]
        [InlineData("a\\b")]
        [InlineData("new\nline")]
        [InlineData("carriage\rreturn")]
        public void StringConstant_SpecialChars_RoundTrip(string value)
        {
            string js = ExpressionCompiler.AddConstant(value);
            object back;
            try { back = new Engine().Evaluate(js).ToObject(); }
            catch (Exception ex) { Assert.Fail($"emitted `{js}` is not valid JS - {ex.GetType().Name}: {ex.Message}"); return; }
            Assert.Equal(value, back);
        }

        // ---------- date constants honor the configured DateTimeMode ----------
        // A Utc-mode compiler emits a timezone-stable Date.UTC literal; the default (Local) compiler
        // emits a local new Date(...). The host must bind JS-side dates in the same frame.
        [Fact]
        public void DateConstant_IsTimezoneStable_InUtcMode()
        {
            System.Linq.Expressions.Expression<Func<DateTime, bool>> expr = d => d == new DateTime(2020, 1, 2, 3, 4, 5);
            var compiler = new ExpressionCompiler(expr, DateTimeMode.Utc);
            Assert.Contains("Date.UTC(2020,0,2,3,4,5)", compiler.JavaScriptExpression);
        }

        [Fact]
        public void DateConstant_IsLocal_ByDefault()
        {
            System.Linq.Expressions.Expression<Func<DateTime, bool>> expr = d => d == new DateTime(2020, 1, 2, 3, 4, 5);
            var compiler = new ExpressionCompiler(expr);
            string js = compiler.JavaScriptExpression;
            Assert.Contains("new Date(2020,0,2,3,4,5)", js);
            Assert.DoesNotContain("Date.UTC", js);
        }
    }
}
