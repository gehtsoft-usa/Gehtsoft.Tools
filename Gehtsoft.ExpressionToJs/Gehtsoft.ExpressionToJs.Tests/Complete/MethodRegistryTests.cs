using System;
using System.Linq.Expressions;
using Jint;
using Xunit;

namespace Gehtsoft.ExpressionToJs.Tests.Complete
{
    /// <summary>Tests for the public method-translation extension surface (IJsMethodRegistry).</summary>
    public class MethodRegistryTests
    {
        public static class CustomFuncs
        {
            public static double Square(double x) => x * x;
        }

        private sealed class FakeAbsTranslator : IMethodCallTranslator
        {
            public bool TryTranslate(MethodCallExpression call, IExpressionEmitContext ctx, out string js)
            {
                if (call.Method.DeclaringType == typeof(Math) && call.Method.Name == nameof(Math.Abs))
                {
                    js = $"CUSTOM_ABS({ctx.Emit(call.Arguments[0])})";
                    return true;
                }
                js = null;
                return false;
            }
        }

        private static double Eval(string js, double x)
        {
            var engine = new Engine();
            engine.Execute(ExpressionToJsStubAccessor.GetJsIncludesAsString());
            engine.SetValue("x", x);
            return Convert.ToDouble(engine.Evaluate(js).ToObject());
        }

        [Fact]
        public void Builtins_AreUnaffected_WithoutRegistration()
        {
            Expression<Func<double, double>> expr = x => Math.Abs(x);
            Assert.Equal("Math.abs(x)", new ExpressionCompiler(expr).JavaScriptExpression);
        }

        [Fact]
        public void MapMethod_ByTypeAndName_RegistersCustomFunction()
        {
            Expression<Func<double, double>> expr = x => CustomFuncs.Square(x);
            var compiler = new ExpressionCompiler(expr);
            compiler.Methods.MapMethod(typeof(CustomFuncs), nameof(CustomFuncs.Square), "Math.pow($0, 2)");
            Assert.Equal(9.0, Eval(compiler.JavaScriptExpression, 3.0), 9);
        }

        [Fact]
        public void MapMethod_ByMethodInfo_RegistersCustomFunction()
        {
            var method = typeof(CustomFuncs).GetMethod(nameof(CustomFuncs.Square));
            Expression<Func<double, double>> expr = x => CustomFuncs.Square(x);
            var compiler = new ExpressionCompiler(expr);
            compiler.Methods.MapMethod(method, "($0 * $0)");
            Assert.Equal(16.0, Eval(compiler.JavaScriptExpression, 4.0), 9);
        }

        [Fact]
        public void AddTranslator_ShadowsBuiltin()
        {
            Expression<Func<double, double>> expr = x => Math.Abs(x);
            var compiler = new ExpressionCompiler(expr);
            compiler.Methods.AddTranslator(new FakeAbsTranslator());
            Assert.Equal("CUSTOM_ABS(x)", compiler.JavaScriptExpression);
        }

        [Fact]
        public void Registration_IsPerInstance()
        {
            Expression<Func<double, double>> expr = x => Math.Abs(x);
            var customized = new ExpressionCompiler(expr);
            customized.Methods.AddTranslator(new FakeAbsTranslator());
            _ = customized.JavaScriptExpression;

            // A fresh compiler must not see the other instance's registration.
            Assert.Equal("Math.abs(x)", new ExpressionCompiler(expr).JavaScriptExpression);
        }
    }
}
