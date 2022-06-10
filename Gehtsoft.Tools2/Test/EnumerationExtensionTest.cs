using FluentAssertions;
using Gehtsoft.Tools2.Extensions;
using System.Collections.Generic;
using Xunit;

namespace Gehtsoft.Tools2.UnitTest
{
    public class EnumerationExtensionTest
    {
        [Fact]
        public void ForAll()
        {
            List<int> a = new List<int>(new int[] { 1, 2, 3});
            IEnumerable<int> ia = a;
            
            int s = 0;
            ia.ForAll(x => s += x);
            s.Should().Be(6);
        }

        [Fact]
        public void IndexOf()
        {
            List<int> a = new List<int>(new int[] { 1, 2, 3 });
            IEnumerable<int> ia = a;

            ia.IndexOf(1).Should().Be(0);
            ia.IndexOf(2).Should().Be(1);
            ia.IndexOf(3).Should().Be(2);
            ia.IndexOf(4).Should().BeLessThan(0);
        }

        [Fact]
        public void IndexOfNull()
        {
            List<string> a = new List<string>(new string[] { "a", "b" });
            IEnumerable<string> ia = a;

            ia.IndexOf("b").Should().Be(1);
            ia.IndexOf((string)null).Should().BeLessThan(0);
            a.Add(null);
            ia.IndexOf((string)null).Should().Be(2);
        }
    }
}
