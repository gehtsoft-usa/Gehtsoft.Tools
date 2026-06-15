using System;
using System.Globalization;
using System.Linq.Expressions;
using Jint;
using Xunit;

namespace Gehtsoft.ExpressionToJs.Tests.Complete
{
    /// <summary>The configurable Local/UTC DateTimeMode applied to constants, member reads and diffs.</summary>
    public class DateTimeModeTests
    {
        private static string Js<TA, TR>(Expression<Func<TA, TR>> expr, DateTimeMode mode)
            => new ExpressionCompiler(expr, mode).JavaScriptExpression;

        // ---------- member reads: Local (default) vs Utc API family ----------

        [Fact]
        public void Local_MemberReads_UseLocalApi()
        {
            Assert.Equal("jsv_greater(d.getFullYear(), 2000)", Js<DateTime, bool>(d => d.Year > 2000, DateTimeMode.Local));
        }

        [Theory]
        [InlineData("Year", "d.getUTCFullYear()")]
        [InlineData("Day", "d.getUTCDate()")]
        public void Utc_MemberReads_UseUtcApi(string member, string expected)
        {
            Expression<Func<DateTime, int>> expr = member == "Year" ? (d => d.Year) : (d => d.Day);
            Assert.Equal(expected, Js(expr, DateTimeMode.Utc));
        }

        [Fact]
        public void Utc_Month_And_DayOfWeek()
        {
            Assert.Equal("(d.getUTCMonth() + 1)", Js<DateTime, int>(d => d.Month, DateTimeMode.Utc));
            // DayOfWeek is an enum -> the comparison wraps it in a Convert, so match on the API.
            Assert.Contains("getUTCDay()", Js<DateTime, bool>(d => d.DayOfWeek == DayOfWeek.Thursday, DateTimeMode.Utc));
        }

        // ---------- calendar diffs carry the mode flag ----------

        [Fact]
        public void MonthsSince_CarriesModeFlag()
        {
            Expression<Func<DateTime, double>> expr = d => Functions.MonthsSince(d, new DateTime(2020, 1, 1));
            Assert.EndsWith(", false)", Js(expr, DateTimeMode.Local));
            Assert.EndsWith(", true)", Js(expr, DateTimeMode.Utc));
            Assert.StartsWith("jsv_monthssince(", Js(expr, DateTimeMode.Utc));
        }

        // ---------- end-to-end UTC reads in Jint (fixed epoch instant, tz-independent) ----------

        private static object EvalMember(Expression<Func<DateTime, int>> expr, DateTime utcInstant, DateTimeMode mode)
        {
            var engine = new Engine();
            engine.Execute(ExpressionToJsStubAccessor.GetJsIncludesAsString());
            double epochMs = (utcInstant.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
            engine.Execute("var d = new Date(" + epochMs.ToString(CultureInfo.InvariantCulture) + ");");
            return engine.Evaluate(new ExpressionCompiler(expr, mode).JavaScriptExpression).ToObject();
        }

        [Fact]
        public void Utc_RoundTrip_ReadsUtcComponents()
        {
            // 02:00 UTC on the 15th. getUTCDate is 15 regardless of the engine's local timezone.
            var dt = new DateTime(2020, 3, 15, 2, 0, 0, DateTimeKind.Utc);
            Assert.Equal(2020, Convert.ToInt32(EvalMember(d => d.Year, dt, DateTimeMode.Utc)));
            Assert.Equal(15, Convert.ToInt32(EvalMember(d => d.Day, dt, DateTimeMode.Utc)));
        }

        // ---------- MonthsSince / YearsSince UTC parity (C# vs jsv with utc=true) ----------

        public static TheoryData<DateTime, DateTime> UtcPairs => new TheoryData<DateTime, DateTime>
        {
            { new DateTime(2021, 3, 15, 0, 0, 0, DateTimeKind.Utc), new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            { new DateTime(2023, 7, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2019, 4, 1, 0, 0, 0, DateTimeKind.Utc) },
        };

        [Theory]
        [MemberData(nameof(UtcPairs))]
        public void MonthsSince_UtcParity(DateTime a, DateTime b)
        {
            var engine = new Engine();
            engine.Execute(ExpressionToJsStubAccessor.GetJsIncludesAsString());
            engine.SetValue("a", a);
            engine.SetValue("b", b);
            double js = Convert.ToDouble(engine.Evaluate("jsv_monthssince(a, b, true)").ToObject());
            Assert.Equal(Functions.MonthsSince(a, b), js, 0);
        }

        [Theory]
        [MemberData(nameof(UtcPairs))]
        public void YearsSince_UtcParity(DateTime a, DateTime b)
        {
            var engine = new Engine();
            engine.Execute(ExpressionToJsStubAccessor.GetJsIncludesAsString());
            engine.SetValue("a", a);
            engine.SetValue("b", b);
            double js = Convert.ToDouble(engine.Evaluate("jsv_yearssince(a, b, true)").ToObject());
            Assert.Equal(Functions.YearsSince(a, b), js, 0);
        }
    }
}
