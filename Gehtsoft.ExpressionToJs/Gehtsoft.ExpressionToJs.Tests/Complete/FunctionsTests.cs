using System;
using Xunit;

namespace Gehtsoft.ExpressionToJs.Tests.Complete
{
    /// <summary>Pure C#-side unit tests for the <see cref="Functions"/> helper class.</summary>
    public class FunctionsTests
    {
        [Theory]
        [InlineData(1.234, 0.234)]
        [InlineData(-1.234, 0.234)]
        [InlineData(5.0, 0.0)]
        [InlineData(-5.0, 0.0)]
        public void Fractional(double v, double expected) => Assert.Equal(expected, Functions.Fractional(v), 6);

        [Fact]
        public void DaysSince()
        {
            Assert.Equal(30, Functions.DaysSince(new DateTime(2020, 1, 31), new DateTime(2020, 1, 1)), 6);
            Assert.Equal(-30, Functions.DaysSince(new DateTime(2020, 1, 1), new DateTime(2020, 1, 31)), 6);
            Assert.Equal(0, Functions.DaysSince(new DateTime(2020, 1, 1), new DateTime(2020, 1, 1)), 6);
        }

        [Fact]
        public void MonthsSince_WholeMonths()
            => Assert.Equal(72, Functions.MonthsSince(new DateTime(2008, 1, 31), new DateTime(2002, 1, 31)), 6);

        [Fact]
        public void YearsSince_WholeYears()
            => Assert.Equal(6, Functions.YearsSince(new DateTime(2008, 1, 31), new DateTime(2002, 1, 31)), 6);

        [Theory]
        [InlineData("4444333322221111", true)]
        [InlineData("4444 3333 2222 1111", true)]
        [InlineData("4444-3333-2222-1111", true)]
        [InlineData("4444 3333 2222 1112", false)]
        [InlineData("378282246310005", true)]
        [InlineData("abcd", false)]
        [InlineData(null, false)]
        public void IsCreditCardNumberCorrect(string value, bool expected)
            => Assert.Equal(expected, Functions.IsCreditCardNumberCorrect(value));

        [Theory]
        [InlineData("true", true)]
        [InlineData("YES", true)]
        [InlineData("1", true)]
        [InlineData("on", true)]
        [InlineData("false", false)]
        [InlineData("no", false)]
        [InlineData("0", false)]
        [InlineData("OFF", false)]
        public void ToBool_Strings(string value, bool expected) => Assert.Equal(expected, Functions.ToBool(value));

        [Fact]
        public void ToBool_NullIsFalse() => Assert.False(Functions.ToBool(null));

        [Fact]
        public void ToBool_BoolPassesThrough() => Assert.True(Functions.ToBool(true));

        [Fact]
        public void ToBool_InvalidThrows() => Assert.Throws<ArgumentException>(() => Functions.ToBool("maybe"));

        [Theory]
        [InlineData("50", 50)]
        [InlineData("-7", -7)]
        [InlineData("0", 0)]
        public void ToInt_Strings(string value, int expected) => Assert.Equal(expected, Functions.ToInt(value));

        [Fact]
        public void ToInt_NullIsZero() => Assert.Equal(0, Functions.ToInt(null));

        [Fact]
        public void ToInt_IntPassesThrough() => Assert.Equal(42, Functions.ToInt(42));

        [Fact]
        public void ToInt_InvalidThrows() => Assert.Throws<FormatException>(() => Functions.ToInt("nope"));

        [Fact]
        public void IsNull()
        {
            Assert.True(((object)null).IsNull());
            Assert.False("x".IsNull());
        }

        [Fact]
        public void IsNotNull()
        {
            Assert.False(((object)null).IsNotNull());
            Assert.True("x".IsNotNull());
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData("", true)]
        [InlineData("x", false)]
        public void IsNullOrEmpty(string value, bool expected) => Assert.Equal(expected, value.IsNullOrEmpty());

        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("x", true)]
        public void IsNotNullOrEmpty(string value, bool expected) => Assert.Equal(expected, value.IsNotNullOrEmpty());
    }
}
