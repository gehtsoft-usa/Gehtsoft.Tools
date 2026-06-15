using System;
using Xunit;

namespace Gehtsoft.ExpressionToJs.Tests.Complete
{
    /// <summary>Low-layer parity for the Math.* surface.</summary>
    public class MathFunctionParityTests : DifferentialTestBase
    {
        // ---------- expected GREEN ----------

        [Theory]
        [InlineData(-5.0, 5.0)]
        [InlineData(5.0, 5.0)]
        public void Abs(double v, double e) => AssertUnary<double, double>(x => Math.Abs(x), v, e);

        [Theory]
        [InlineData(2.9, 2.0)]
        [InlineData(-2.1, -3.0)]
        public void Floor(double v, double e) => AssertUnary<double, double>(x => Math.Floor(x), v, e);

        [Theory]
        [InlineData(2.1, 3.0)]
        [InlineData(-2.9, -2.0)]
        public void Ceiling(double v, double e) => AssertUnary<double, double>(x => Math.Ceiling(x), v, e);

        [Theory]
        [InlineData(2.9, 2.0)]
        [InlineData(-2.9, -2.0)]   // truncation goes toward zero on both sides
        public void Truncate(double v, double e) => AssertUnary<double, double>(x => Math.Truncate(x), v, e);

        [Theory]
        [InlineData(-3.0, -1)]
        [InlineData(0.0, 0)]
        [InlineData(4.0, 1)]
        public void Sign(double v, int e) => AssertUnary<double, int>(x => Math.Sign(x), v, e);

        [Theory]
        [InlineData(16.0, 4.0)]
        public void Sqrt(double v, double e) => AssertUnary<double, double>(x => Math.Sqrt(x), v, e);

        // ---------- §1: Math.Round must use banker's rounding (expected RED) ----------
        // C# rounds half-to-even: Round(2.5)=2, Round(0.5)=0; JS Math.round rounds half-up.
        [Theory]
        [InlineData(2.5, 2.0)]
        [InlineData(3.5, 4.0)]
        [InlineData(0.5, 0.0)]
        [InlineData(1.5, 2.0)]
        [InlineData(2.4, 2.0)]
        [InlineData(2.6, 3.0)]
        public void Round_UsesBankersRounding(double v, double e) => AssertUnary<double, double>(x => Math.Round(x), v, e);
    }
}
