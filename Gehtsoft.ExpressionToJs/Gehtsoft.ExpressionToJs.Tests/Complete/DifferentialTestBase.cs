using System;
using System.Globalization;
using System.Linq.Expressions;
using Jint;
using Xunit;

namespace Gehtsoft.ExpressionToJs.Tests.Complete
{
    /// <summary>
    /// Base for the low-layer differential suite.
    /// <para>Every case is checked three ways:</para>
    /// <list type="number">
    /// <item>the compiled C# delegate (the expression value) must equal the expected value
    /// (proves the operation is correct);</item>
    /// <item>the emitted JavaScript, evaluated in Jint with the stub loaded, must equal the
    /// expected value;</item>
    /// <item>the expression value and the JavaScript value must agree directly - so a shared
    /// wrong answer on both sides is still caught against the expected anchor, and any
    /// C#/JS divergence is reported explicitly.</item>
    /// </list>
    /// Tests that assert the C#-faithful result for a known §1 discrepancy will be RED
    /// until the corresponding bug is fixed - that is the point: they catch the divergence.
    /// </summary>
    public abstract class DifferentialTestBase
    {
        protected static Engine NewEngine()
        {
            var engine = new Engine();
            engine.Execute(ExpressionToJsStubAccessor.GetJsIncludesAsString());
            return engine;
        }

        protected static string Compile(LambdaExpression expression)
            => new ExpressionCompiler(expression).JavaScriptExpression;

        /// <summary>Evaluate a raw JS snippet with the stub loaded and return the CLR value.</summary>
        protected static object EvalJs(string js, Action<Engine> bind = null)
        {
            var engine = NewEngine();
            bind?.Invoke(engine);
            return engine.Evaluate(js).ToObject();
        }

        private static object RunJs(string js, Action<Engine> bind, string context)
        {
            try
            {
                return EvalJs(js, bind);
            }
            catch (Exception ex)
            {
                Assert.Fail($"{context}: JavaScript `{js}` failed to evaluate - {ex.GetType().Name}: {ex.Message}");
                return null;
            }
        }

        protected void AssertUnary<TArg, TResult>(Expression<Func<TArg, TResult>> expression, TArg arg, TResult expected)
        {
            // (1) the C# side must produce the expected value
            TResult csharp;
            try { csharp = expression.Compile()(arg); }
            catch (Exception ex) { Assert.Fail($"C# evaluation threw {ex.GetType().Name}: {ex.Message}"); return; }
            AssertValue(expected, csharp, "C#");

            // (2) the emitted JS must produce the same value
            string js;
            try { js = Compile(expression); }
            catch (Exception ex) { Assert.Fail($"JS compilation threw {ex.GetType().Name}: {ex.Message}"); return; }
            string p = expression.Parameters[0].Name;
            object jsResult = RunJs(js, e => e.SetValue(p, arg), "JS");
            AssertValue(expected, jsResult, $"JS `{js}`");

            // (3) the expression value and the JavaScript value must agree directly
            AssertValue(csharp, jsResult, $"C# vs JS `{js}`");
        }

        protected void AssertBinary<TA, TB, TResult>(Expression<Func<TA, TB, TResult>> expression, TA a, TB b, TResult expected)
        {
            TResult csharp;
            try { csharp = expression.Compile()(a, b); }
            catch (Exception ex) { Assert.Fail($"C# evaluation threw {ex.GetType().Name}: {ex.Message}"); return; }
            AssertValue(expected, csharp, "C#");

            string js;
            try { js = Compile(expression); }
            catch (Exception ex) { Assert.Fail($"JS compilation threw {ex.GetType().Name}: {ex.Message}"); return; }
            string p0 = expression.Parameters[0].Name, p1 = expression.Parameters[1].Name;
            object jsResult = RunJs(js, e => { e.SetValue(p0, a); e.SetValue(p1, b); }, "JS");
            AssertValue(expected, jsResult, $"JS `{js}`");

            // (3) the expression value and the JavaScript value must agree directly
            AssertValue(csharp, jsResult, $"C# vs JS `{js}`");
        }

        protected static void AssertValue(object expected, object actual, string side)
        {
            if (expected is null)
            {
                Assert.True(actual is null, $"{side}: expected null but got '{actual}'");
                return;
            }

            if (IsNumeric(expected))
            {
                Assert.True(actual != null && IsNumeric(actual), $"{side}: expected numeric {expected} but got '{actual ?? "null"}'");
                double e = Convert.ToDouble(expected, CultureInfo.InvariantCulture);
                double a = Convert.ToDouble(actual, CultureInfo.InvariantCulture);
                double tol = 1e-9 * Math.Max(1.0, Math.Abs(e));
                Assert.True(Math.Abs(e - a) <= tol, $"{side}: expected {e} but got {a}");
                return;
            }

            if (expected is bool eb)
            {
                Assert.True(actual != null, $"{side}: expected {eb} but got null");
                Assert.Equal(eb, Convert.ToBoolean(actual));
                return;
            }

            Assert.Equal(expected.ToString(), actual?.ToString());
        }

        protected static bool IsNumeric(object o) =>
            o is sbyte || o is byte || o is short || o is ushort ||
            o is int || o is uint || o is long || o is ulong ||
            o is float || o is double || o is decimal;
    }
}
