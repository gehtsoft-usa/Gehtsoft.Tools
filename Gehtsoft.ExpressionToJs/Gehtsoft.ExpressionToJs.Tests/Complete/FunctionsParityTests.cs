using System;
using System.Collections.Generic;
using System.Globalization;
using Jint;
using Xunit;

namespace Gehtsoft.ExpressionToJs.Tests.Complete
{
    /// <summary>
    /// Differential parity between the server-side <see cref="Functions"/> methods and their
    /// client-side jsv_* counterparts in stub.js, over a shared data set.
    /// </summary>
    public class FunctionsParityTests : DifferentialTestBase
    {
        private static double Jsv(string fn, DateTime a, DateTime b)
            => Convert.ToDouble(EvalJs($"{fn}(a, b)", e => { e.SetValue("a", a); e.SetValue("b", b); }));

        private static string N(double v) => v.ToString(CultureInfo.InvariantCulture);

        // ---------- expected GREEN ----------

        [Theory]
        [InlineData(1.234, 0.234)]
        [InlineData(-1.234, 0.234)]
        [InlineData(5.0, 0.0)]
        public void Fractional(double v, double expected)
        {
            Assert.Equal(expected, Functions.Fractional(v), 6);
            Assert.Equal(expected, Convert.ToDouble(EvalJs($"jsv_fractional({N(v)})")), 6);
        }

        [Theory]
        [InlineData("4444333322221111", true)]
        [InlineData("4444 3333 2222 1111", true)]
        [InlineData("4444 3333 2222 1112", false)]
        [InlineData("378282246310005", true)]
        public void CreditCard(string value, bool expected)
        {
            Assert.Equal(expected, Functions.IsCreditCardNumberCorrect(value));
            Assert.Equal(expected, Convert.ToBoolean(EvalJs($"jsv_ccn_valid('{value}')")));
        }

        [Theory]
        [InlineData("on", true)]
        [InlineData("off", false)]
        [InlineData("YES", true)]
        [InlineData("0", false)]
        public void ToBool(string value, bool expected)
        {
            Assert.Equal(expected, Functions.ToBool(value));
            Assert.Equal(expected, Convert.ToBoolean(EvalJs($"jsv_string2bool('{value}')")));
        }

        public static IEnumerable<object[]> WholePeriods => new[]
        {
            new object[] { new DateTime(2008, 1, 31), new DateTime(2002, 1, 31) },
            new object[] { new DateTime(2010, 6, 15), new DateTime(2005, 6, 15) },
        };

        [Theory]
        [MemberData(nameof(WholePeriods))]
        public void DaysSince_Matches(DateTime a, DateTime b)
            => Assert.Equal(Functions.DaysSince(a, b), Jsv("jsv_dayssince", a, b), 0);

        // ---------- §1: MonthsSince / YearsSince use different algorithms (expected RED for partial periods) ----------
        public static IEnumerable<object[]> PartialPeriods => new[]
        {
            new object[] { new DateTime(2021, 3, 15), new DateTime(2020, 1, 1) },
            new object[] { new DateTime(2020, 11, 20), new DateTime(2020, 2, 10) },
            new object[] { new DateTime(2023, 7, 1), new DateTime(2019, 4, 1) },
        };

        [Theory]
        [MemberData(nameof(PartialPeriods))]
        public void MonthsSince_Matches(DateTime a, DateTime b)
            => Assert.Equal(Functions.MonthsSince(a, b), Jsv("jsv_monthssince", a, b), 0);

        [Theory]
        [MemberData(nameof(PartialPeriods))]
        public void YearsSince_Matches(DateTime a, DateTime b)
            => Assert.Equal(Functions.YearsSince(a, b), Jsv("jsv_yearssince", a, b), 0);
    }
}
