using FluentAssertions;
using Gehtsoft.Tools2.Structure;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Gehtsoft.Tools2.UnitTest
{
    public class FastScroll
    {
        const int bucketSize = (2 << 10);

        private readonly ITestOutputHelper mTestOutputHelper;

        public FastScroll(ITestOutputHelper testOutputHelper)
        {
            mTestOutputHelper = testOutputHelper;
        }

        private static FastScrollList<int> CreateThreeBucketsList()
        {
            
            var r = new FastScrollList<int>();
            for (int i = 0; i < bucketSize * 3; i++)
                r.AddToEnd(i + 1);

            return r;
        }

        [Fact]
        public void ElementAtBeforeScroll()
        {
            var buffer = CreateThreeBucketsList();
            buffer.Count().Should().Be(bucketSize * 3);
            for (int i = 0; i < bucketSize * 3; i++)
                buffer[i].Should().Be(i + 1);
        }

        [Fact]
        public void RemoveFromTop_One()
        {
            var buffer = CreateThreeBucketsList();

            buffer.RemoveFromTop(1);

            buffer.Count().Should().Be(bucketSize * 3 - 1);
            for (int i = 0; i < buffer.Count; i++)
                buffer[i].Should().Be(i + 2);
        }

        [Fact]
        public void RemoveFromTop_HalfBucket()
        {
            var buffer = CreateThreeBucketsList();

            buffer.RemoveFromTop(512);

            buffer.Count().Should().Be(bucketSize * 3 - 512);
            for (int i = 0; i < buffer.Count; i++)
                buffer[i].Should().Be(i + 513);
        }

        [Fact]
        public void RemoveFromTop_MoreThanBucket()
        {
            var buffer = CreateThreeBucketsList();

            buffer.RemoveFromTop(1025);

            buffer.Count.Should().Be(bucketSize * 3 - 1025);
            for (int i = 0; i < buffer.Count; i++)
                buffer[i].Should().Be(i + 1026);
        }       

        [Fact]
        public void RemoveFromTop_And_AddContent()
        {
            var buffer = CreateThreeBucketsList();

            buffer.RemoveFromTop(512);

            for (int i = 0; i < 10; i++)
                buffer.AddToEnd(buffer[buffer.Count - 1] + 1);

            buffer.Count.Should().Be(bucketSize * 3 - 502);
            for (int i = 0; i < buffer.Count; i++)
                buffer[i].Should().Be(i + 513);
        }

        [Fact]
        public void RemoveFromTop_And_InsertContent()
        {
            var buffer = CreateThreeBucketsList();

            buffer.RemoveFromTop(512);

            for (int i = 0; i < 10; i++)
                buffer.InsertAtTop(buffer[0] - 1);

            buffer.Count.Should().Be(bucketSize * 3 - 502);
            for (int i = 0; i < buffer.Count; i++)
                buffer[i].Should().Be(i + 503);
        }

        [Fact]
        public void InsertContent()
        {
            var buffer = CreateThreeBucketsList();

            for (int i = 0; i < 10; i++)
                buffer.InsertAtTop(buffer[0] - 1);

            buffer.Count.Should().Be(bucketSize * 3 + 10);
            for (int i = 0; i < buffer.Count; i++)
                buffer[i].Should().Be(-9 + i, $"element {i}");
        }

        [Fact]
        public void Clear()
        {
            var buffer = new FastScrollList<int>();
            buffer.AddToEnd(1);
            buffer.AddToEnd(2);
            buffer.AddToEnd(3);
            buffer.Clear();
            buffer.Count.Should().Be(0);
            buffer.Should().BeEmpty();
        }

        [Fact]
        public void SetAt()
        {
            var buffer = new FastScrollList<int>();
            buffer.AddToEnd(1);
            buffer.AddToEnd(2);
            buffer.AddToEnd(3);
            buffer[0] = 4;
            buffer[1] = 5;
            buffer[2] = 6;
            buffer.Count.Should().Be(3);
            buffer.Should().BeEquivalentTo(new int[] { 4, 5, 6 });
        }

        [Fact]
        public void FasterThanList()
        {
            var sw = new Stopwatch(); 
             
            sw.Reset();
            sw.Start();
            var l = new List<int>();
            for (int i = 0; i < 1024; i++)
                l.Add(i);
            for (int i = 0; i < 100000; i++)
            {
                l.RemoveAt(0);
                l.Add(i);
            }
            sw.Stop();
            var lt = sw.Elapsed;

            sw.Reset();
            sw.Start();
            var f = new FastScrollList<int>();
            for (int i = 0; i < 1024; i++)
                f.AddToEnd(i);
            for (int i = 0; i < 100000; i++)
            {
                f.RemoveFromTop(0);
                f.AddToEnd(i);
            }
            sw.Stop();
            var ft = sw.Elapsed;

            mTestOutputHelper.WriteLine("list {0} vs fastscroll {1}", lt, ft);
            ft.Should().BeLessThan(lt);

        }
    }
}
