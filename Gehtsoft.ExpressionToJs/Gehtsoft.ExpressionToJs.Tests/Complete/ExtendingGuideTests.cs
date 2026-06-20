using System;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Xunit;

namespace Gehtsoft.ExpressionToJs.Tests.Complete
{
    /// <summary>
    /// Pins the exact emitted JS for every example in the "Extending the translator for your own
    /// types" guide (one order-form domain: a domain method, a computed DateTime property, an enum
    /// constant rendered by name).
    /// </summary>
    public class ExtendingGuideTests
    {
        public static class Coupon
        {
            // server-side check; the client ships a behaviourally identical jsv_coupon_ok
            public static bool IsValid(string code) => code != null && Regex.IsMatch(code, "^[A-Z]{4}-\\d{4}$");
            public static bool IsValid(string code, bool strict) => IsValid(code) && (!strict || code.StartsWith("VIP"));
        }

        // A translator (not a template): the emitted function depends on the constant 'strict' flag.
        private sealed class CouponTranslator : IMethodCallTranslator
        {
            public bool TryTranslate(MethodCallExpression call, IExpressionEmitContext ctx, out string js)
            {
                if (call.Method.DeclaringType == typeof(Coupon) && call.Method.Name == nameof(Coupon.IsValid) && call.Arguments.Count == 2)
                {
                    string code = ctx.Emit(call.Arguments[0]);
                    bool strict = (bool)((ConstantExpression)call.Arguments[1]).Value;
                    js = strict ? $"jsv_coupon_strict({code})" : $"jsv_coupon_ok({code})";
                    return true;
                }
                js = null;
                return false;
            }
        }

        public enum OrderStatus { Pending = 0, Shipped = 1, Delivered = 2 }

        public static class OrderRules
        {
            public static bool StatusIs(OrderStatus actual, OrderStatus expected) => actual == expected;
        }

        public class Order
        {
            public string CouponCode { get; set; }
            public DateTime PlacedOn { get; set; }
            public OrderStatus Status { get; set; }
        }

        [Fact]
        public void Method_DomainHelper_MapsToJsFunction()
        {
            Expression<Func<Order, bool>> rule = o => Coupon.IsValid(o.CouponCode);
            var compiler = new ExpressionCompiler(rule);
            compiler.Methods.MapMethod(typeof(Coupon), nameof(Coupon.IsValid), "jsv_coupon_ok($0)");
            Assert.Equal("jsv_coupon_ok(o.CouponCode)", compiler.JavaScriptExpression);
        }

        [Fact]
        public void Property_DefaultEmitsVerbatim_WhichIsWrongForAJsDate()
        {
            // without a mapping, DayOfYear falls through to verbatim member access -> o.PlacedOn.DayOfYear,
            // which a JavaScript Date does not have. This is exactly why a mapping is needed.
            Expression<Func<Order, bool>> rule = o => o.PlacedOn.DayOfYear <= 31;
            Assert.Equal("jsv_lessorequal(o.PlacedOn.DayOfYear, 31)", new ExpressionCompiler(rule).JavaScriptExpression);
        }

        [Fact]
        public void Property_ComputedOnClient_MapsToHelper()
        {
            Expression<Func<Order, bool>> rule = o => o.PlacedOn.DayOfYear <= 31;
            var compiler = new ExpressionCompiler(rule);
            compiler.Members.MapMember(typeof(DateTime), nameof(DateTime.DayOfYear), "jsv_dayofyear($obj)");
            Assert.Equal("jsv_lessorequal(jsv_dayofyear(o.PlacedOn), 31)", compiler.JavaScriptExpression);
        }

        [Fact]
        public void Constant_EnumDefault_EmitsNumericValue()
        {
            // an enum constant kept enum-typed (here a method argument) defaults to its number
            Expression<Func<Order, bool>> rule = o => OrderRules.StatusIs(o.Status, OrderStatus.Shipped);
            var compiler = new ExpressionCompiler(rule);
            compiler.Methods.MapMethod(typeof(OrderRules), nameof(OrderRules.StatusIs), "jsv_status_is($0, $1)");
            Assert.Equal("jsv_status_is(o.Status, 1)", compiler.JavaScriptExpression);
        }

        [Fact]
        public void Method_Translator_BranchesOnArgumentValue()
        {
            Expression<Func<Order, bool>> rule = o => Coupon.IsValid(o.CouponCode, true);
            var compiler = new ExpressionCompiler(rule);
            compiler.Methods.AddTranslator(new CouponTranslator());
            Assert.Equal("jsv_coupon_strict(o.CouponCode)", compiler.JavaScriptExpression);
        }

        [Fact]
        public void Constant_EnumByName_ViaMapConstant()
        {
            Expression<Func<Order, bool>> rule = o => OrderRules.StatusIs(o.Status, OrderStatus.Shipped);
            var compiler = new ExpressionCompiler(rule);
            compiler.Methods.MapMethod(typeof(OrderRules), nameof(OrderRules.StatusIs), "jsv_status_is($0, $1)");
            compiler.Constants.MapConstant<OrderStatus>(s => "'" + s + "'");
            Assert.Equal("jsv_status_is(o.Status, 'Shipped')", compiler.JavaScriptExpression);
        }
    }
}
