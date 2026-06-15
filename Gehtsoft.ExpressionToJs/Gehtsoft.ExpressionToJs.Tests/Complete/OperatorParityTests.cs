using System;
using System.Linq.Expressions;
using Xunit;

namespace Gehtsoft.ExpressionToJs.Tests.Complete
{
    /// <summary>Low-layer parity for arithmetic and comparison operators.</summary>
    public class OperatorParityTests : DifferentialTestBase
    {
        // ---------- arithmetic (expected GREEN) ----------

        [Theory]
        [InlineData(10, 5, 15)]
        [InlineData(-3, 8, 5)]
        public void Add(int a, int b, int expected) => AssertBinary<int, int, int>((x, y) => x + y, a, b, expected);

        [Theory]
        [InlineData(10, 5, 5)]
        [InlineData(3, 8, -5)]
        public void Subtract(int a, int b, int expected) => AssertBinary<int, int, int>((x, y) => x - y, a, b, expected);

        [Theory]
        [InlineData(6, 7, 42)]
        [InlineData(-6, 7, -42)]
        public void Multiply(int a, int b, int expected) => AssertBinary<int, int, int>((x, y) => x * y, a, b, expected);

        [Theory]
        [InlineData(10, 3, 1)]
        [InlineData(-10, 3, -1)]
        public void Modulo(int a, int b, int expected) => AssertBinary<int, int, int>((x, y) => x % y, a, b, expected);

        [Theory]
        [InlineData(7.5, 2.5, 3.0)]
        [InlineData(1.0, 4.0, 0.25)]
        public void DoubleDivision(double a, double b, double expected) => AssertBinary<double, double, double>((x, y) => x / y, a, b, expected);

        [Theory]
        [InlineData(5, -5)]
        [InlineData(-3, 3)]
        public void Negate(int a, int expected) => AssertUnary<int, int>(x => -x, a, expected);

        // ---------- §1: integer division must truncate toward zero (expected RED) ----------
        // C# `11 / 2` is 5; jsv_divide emits `a / b` so JS yields 5.5.
        [Theory]
        [InlineData(11, 2, 5)]
        [InlineData(7, 2, 3)]
        [InlineData(-7, 2, -3)]   // C# truncates toward zero, not toward -infinity
        [InlineData(9, 4, 2)]
        public void IntegerDivision_TruncatesTowardZero(int a, int b, int expected)
            => AssertBinary<int, int, int>((x, y) => x / y, a, b, expected);

        // ---------- §1: Power maps to jsv_power but the stub only defines jsv_powerof (expected RED) ----------
        // C# lambdas don't normally emit ExpressionType.Power, so build it explicitly.
        [Fact]
        public void Power_IsSupported()
        {
            var x = Expression.Parameter(typeof(double), "x");
            var expr = Expression.Lambda<Func<double, double>>(Expression.Power(x, Expression.Constant(2.0)), x);
            AssertUnary(expr, 3.0, 9.0);
        }

        // ---------- §1: decimal arithmetic must be exact (expected RED) ----------
        // In C# 0.1m + 0.2m == 0.3m is true; in binary-double JS 0.1 + 0.2 != 0.3.
        [Fact]
        public void DecimalArithmetic_IsExact()
            => AssertUnary<decimal, bool>(x => x + 0.2m == 0.3m, 0.1m, true);

        // ---------- comparison (expected GREEN) ----------

        [Theory]
        [InlineData(10, 10, true)]
        [InlineData(10, 11, false)]
        public void Equal(int a, int b, bool expected) => AssertBinary<int, int, bool>((x, y) => x == y, a, b, expected);

        [Theory]
        [InlineData(10, 11, true)]
        [InlineData(10, 10, false)]
        public void NotEqual(int a, int b, bool expected) => AssertBinary<int, int, bool>((x, y) => x != y, a, b, expected);

        [Theory]
        [InlineData(9, 10, true)]
        [InlineData(10, 10, false)]
        public void Less(int a, int b, bool expected) => AssertBinary<int, int, bool>((x, y) => x < y, a, b, expected);

        [Theory]
        [InlineData(11, 10, true)]
        [InlineData(10, 10, false)]
        public void Greater(int a, int b, bool expected) => AssertBinary<int, int, bool>((x, y) => x > y, a, b, expected);

        [Theory]
        [InlineData(true, false, false)]
        [InlineData(true, true, true)]
        public void AndAlso(bool a, bool b, bool expected) => AssertBinary<bool, bool, bool>((x, y) => x && y, a, b, expected);

        [Theory]
        [InlineData(true, false, true)]
        [InlineData(false, false, false)]
        public void OrElse(bool a, bool b, bool expected) => AssertBinary<bool, bool, bool>((x, y) => x || y, a, b, expected);
    }
}
