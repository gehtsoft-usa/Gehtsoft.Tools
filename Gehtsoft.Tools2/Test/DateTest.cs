using System;
using FluentAssertions;
using Gehtsoft.Tools2.Extensions;
using Xunit;

namespace Gehtsoft.Tools2.UnitTest
{
    public class DateTest
    {
        [Fact]
        public void TruncateToSeconds()
        {
            var d = DateTime.Now;
            var d1 = d.TruncateToSeconds();

            d1.Year.Should().Be(d.Year);
            d1.Month.Should().Be(d.Month);
            d1.Day.Should().Be(d.Day);
            d1.Hour.Should().Be(d.Hour);
            d1.Minute.Should().Be(d.Minute);
            d1.Second.Should().Be(d.Second);
            d1.Millisecond.Should().Be(0);
        }

        [Fact]
        public void TruncateToMinutes()
        {
            var d = DateTime.Now;
            var d1 = d.TruncateToMinutes();

            d1.Year.Should().Be(d.Year);
            d1.Month.Should().Be(d.Month);
            d1.Day.Should().Be(d.Day);
            d1.Hour.Should().Be(d.Hour);
            d1.Minute.Should().Be(d.Minute);
            d1.Second.Should().Be(0);
            d1.Millisecond.Should().Be(0);
        }

        [Fact]
        public void TruncateToHours()
        {
            var d = DateTime.Now;
            var d1 = d.TruncateToHours();

            d1.Year.Should().Be(d.Year);
            d1.Month.Should().Be(d.Month);
            d1.Day.Should().Be(d.Day);
            d1.Hour.Should().Be(d.Hour);
            d1.Minute.Should().Be(0);
            d1.Second.Should().Be(0);
            d1.Millisecond.Should().Be(0);
        }

        [Fact]
        public void TruncateTime()
        {
            var d = DateTime.Now;
            var d1 = d.TruncateTime();

            d1.Year.Should().Be(d.Year);
            d1.Month.Should().Be(d.Month);
            d1.Day.Should().Be(d.Day);
            d1.Hour.Should().Be(0);
            d1.Minute.Should().Be(0);
            d1.Second.Should().Be(0);
            d1.Millisecond.Should().Be(0);
        }
    }
}
