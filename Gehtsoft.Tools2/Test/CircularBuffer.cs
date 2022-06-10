using FluentAssertions;
using Gehtsoft.Tools2.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Gehtsoft.Tools2.UnitTest
{
    public class CircularBufferTest
    {
        [Fact]
        public void InitiallyEmpty()
        {
            var buffer = new FixedCircularBuffer<int>();
            buffer.Count.Should().Be(0);
            buffer.Should().BeEmpty();
        }

        [Fact]
        public void Add()
        {
            var buffer = new FixedCircularBuffer<int>();
            buffer.Add(1);
            buffer[0].Should().Be(1);
            buffer.Count.Should().Be(1);
            buffer.Should().BeEquivalentTo(new int[] { 1 });
        }

        [Fact]
        public void AddMany()
        {
            var buffer = new FixedCircularBuffer<int>();
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            buffer[0].Should().Be(1);
            buffer[1].Should().Be(2);
            buffer[2].Should().Be(3);
            buffer.Count.Should().Be(3);
            buffer.Should().BeEquivalentTo(new int[] { 1, 2, 3 });
        }

        [Fact]
        public void InsertFirst()
        {
            var buffer = new FixedCircularBuffer<int>();
            buffer.Add(2);
            buffer.Add(3);
            buffer.Insert(0, 1);
            buffer.Count.Should().Be(3);
            buffer.Should().BeEquivalentTo(new int[] { 1, 2, 3 });
        }

        [Fact]
        public void InsertLast()
        {
            var buffer = new FixedCircularBuffer<int>();
            buffer.Add(1);
            buffer.Add(2);
            buffer.Insert(2, 3);
            buffer.Count.Should().Be(3);
            buffer.Should().BeEquivalentTo(new int[] { 1, 2, 3 });
        }

        [Fact]
        public void InsertInTheMiddle()
        {
            var buffer = new FixedCircularBuffer<int>();
            buffer.Add(1);
            buffer.Add(3);
            buffer.Insert(1, 2);

            buffer.Count.Should().Be(3);
            buffer.Should().BeEquivalentTo(new int[] { 1, 2, 3 });

        }

        [Fact]
        public void InsertFirst_WhenShifted()
        {
            var buffer = new FixedCircularBuffer<int>(3);

            buffer.Add(10);
            buffer.Add(2);
            buffer.RemoveAt(0); //remove 10/shift buffer
            buffer.Add(3);
            buffer.Insert(0, 1);

            buffer.Count.Should().Be(3);
            buffer.Should().BeEquivalentTo(new int[] { 1, 2, 3 });
        }

        [Fact]
        public void InsertLast_WhenShifted()
        {
            var buffer = new FixedCircularBuffer<int>(3);

            buffer.Add(10);
            buffer.Add(1);
            buffer.Add(2);
            buffer.RemoveAt(0); //remove 10/shift buffer
            buffer.Insert(2, 3);

            buffer.Count.Should().Be(3);
            buffer.Should().BeEquivalentTo(new int[] { 1, 2, 3 });
        }

        [Fact]
        public void InsertInTheMiddle_WhenShifted()
        {
            var buffer = new FixedCircularBuffer<int>(3);

            buffer.Add(10);
            buffer.Add(1);
            buffer.Add(3);
            buffer.RemoveAt(0); //remove 10/shift buffer
            buffer.Insert(1, 2);
            buffer.Count.Should().Be(3);
            buffer.Should().BeEquivalentTo(new int[] { 1, 2, 3 });
        }


        [Fact]
        public void RemoveFirst()
        {
            var buffer = new FixedCircularBuffer<int>();
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            buffer.RemoveAt(0);
            buffer.Count.Should().Be(2);
            buffer.Should().BeEquivalentTo(new int[] { 2, 3 });
        }

        [Fact]
        public void RemoveLast()
        {
            var buffer = new FixedCircularBuffer<int>();
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            buffer.RemoveAt(2);
            buffer.Count.Should().Be(2);
            buffer.Should().BeEquivalentTo(new int[] { 1, 2 });
        }

        [Fact]
        public void RemoveInTheMiddle()
        {
            var buffer = new FixedCircularBuffer<int>();
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            buffer.RemoveAt(1);
            buffer.Count.Should().Be(2);
            buffer.Should().BeEquivalentTo(new int[] { 1, 3 });
        }

        [Fact]
        public void RemoveFirst_WhenShifted()
        {
            var buffer = new FixedCircularBuffer<int>(3);

            buffer.Add(10);
            buffer.Add(1);
            buffer.Add(2);
            buffer.RemoveAt(0); //remove 10/shift buffer
            buffer.Add(3);

            buffer.RemoveAt(0);
            buffer.Count.Should().Be(2);
            buffer.Should().BeEquivalentTo(new int[] { 2, 3 });
        }

        [Fact]
        public void RemoveLast_WhenShifted()
        {
            var buffer = new FixedCircularBuffer<int>(3);

            buffer.Add(10);
            buffer.Add(1);
            buffer.Add(2);
            buffer.RemoveAt(0); //remove 10/shift buffer
            buffer.Add(3);

            buffer.RemoveAt(2);
            buffer.Count.Should().Be(2);
            buffer.Should().BeEquivalentTo(new int[] { 1, 2 });
        }

        [Fact]
        public void RemoveInTheMiddle_WhenShifted()
        {
            var buffer = new FixedCircularBuffer<int>(3);

            buffer.Add(10);
            buffer.Add(1);
            buffer.Add(2);
            buffer.RemoveAt(0); //remove 10/shift buffer
            buffer.Add(3);

            buffer.RemoveAt(1);
            buffer.Count.Should().Be(2);
            buffer.Should().BeEquivalentTo(new int[] { 1, 3 });
        }

        [Fact]
        public void AddOverCapacity()
        {
            var buffer = new FixedCircularBuffer<int>(3);
            buffer.Add(0);
            buffer.Add(0);
            buffer.Add(0);
            ((Action)(() => buffer.Add(0))).Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void InsertOverCapacity()
        {
            var buffer = new FixedCircularBuffer<int>(3);
            buffer.Add(0);
            buffer.Add(0);
            buffer.Add(0);
            ((Action)(() => buffer.Insert(0, 1))).Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void GetIndex_OutOfRange()
        {
            var buffer = new FixedCircularBuffer<int>(3);
            buffer.Add(0);
            buffer.Add(0);
            ((Action)(() => _ = buffer[2])).Should().Throw<ArgumentOutOfRangeException>();
            ((Action)(() => _ = buffer[-1])).Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Insert_OutOfRange()
        {
            var buffer = new FixedCircularBuffer<int>(3);
            buffer.Add(0);
            buffer.Add(0);
            ((Action)(() => buffer.Insert(3, 1))).Should().Throw<ArgumentOutOfRangeException>();
            ((Action)(() => buffer.Insert(-1, 1))).Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void RemoveAt_OutOfRange()
        {
            var buffer = new FixedCircularBuffer<int>(3);
            buffer.Add(0);
            buffer.Add(0);
            ((Action)(() => buffer.RemoveAt(2))).Should().Throw<ArgumentOutOfRangeException>();
            ((Action)(() => buffer.RemoveAt(-1))).Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void IndexOf()
        {
            var buffer = new FixedCircularBuffer<int>();
            buffer.Add(1);
            buffer.Add(2);
            buffer.IndexOf(1).Should().Be(0);
            buffer.IndexOf(2).Should().Be(1);
            buffer.IndexOf(3).Should().BeLessThan(0);
        }

        [Fact]
        public void Contains()
        {
            var buffer = new FixedCircularBuffer<int>();
            buffer.Add(1);
            buffer.Add(2);
            buffer.Contains(1).Should().BeTrue();
            buffer.Contains(2).Should().BeTrue();
            buffer.Contains(3).Should().BeFalse();
        }

        [Fact]
        public void RemoveByValue()
        {
            var buffer = new FixedCircularBuffer<int>();
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);

            buffer.Remove(2);
            buffer.Should().BeEquivalentTo(new int[] { 1, 3 });
        }

        [Fact]
        public void ToArray()
        {
            var buffer = new FixedCircularBuffer<int>();
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);

            int[] arr = new int[buffer.Count];
            buffer.CopyTo(arr, 0);

            arr.Should().BeEquivalentTo(new int[] { 1, 2, 3 });
        }

        [Fact]
        public void Clear()
        {
            var buffer = new FixedCircularBuffer<int>();
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);

            buffer.Clear();
            buffer.Count().Should().Be(0);
        }

        [Fact]
        public void Enqueue()
        {
            var buffer = new FixedCircularBuffer<int>();
            buffer.Enqueue(1);
            buffer.Enqueue(2);
            buffer.Enqueue(3);

            buffer.Count.Should().Be(3);
            buffer.Should().BeEquivalentTo(new int[] { 1, 2, 3 });
        }

        [Fact]
        public void Dequeue()
        {
            var buffer = new FixedCircularBuffer<int>();
            buffer.Enqueue(1);
            buffer.Enqueue(2);
            buffer.Enqueue(3);

            buffer.Dequeue().Should().Be(1);
            buffer.Count.Should().Be(2);
            buffer.Should().BeEquivalentTo(new int[] { 2, 3 });

            buffer.Dequeue().Should().Be(2);
            buffer.Count.Should().Be(1);
            buffer.Should().BeEquivalentTo(new int[] { 3 });

            buffer.Dequeue().Should().Be(3);
            buffer.Should().BeEmpty();
        }

        [Fact]
        public void Endurance()
        {
            var buffer = new FixedCircularBuffer<int>(1024);
            for (int i = 0; i < 1024; i++)
                buffer.Add(i);

            for (int i = 1024; i < 3000; i++)
            {
                buffer.RemoveAt(0);
                buffer.Add(i);

                buffer.Count.Should().Be(1024);
                for (int j = 0; j < 1024; j++)
                    buffer[j].Should().Be(j + i - 1023);
            }
        }

        [Fact]
        public void ExpandByAdd()
        {
            var buffer = new AutoExpandCircularBuffer<int>(3);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            buffer.Add(4);

            buffer.Count().Should().Be(4);
            buffer.Should().BeEquivalentTo(new int[] { 1, 2, 3, 4 });
        }

        [Fact]
        public void ExpandByInsert()
        {
            var buffer = new AutoExpandCircularBuffer<int>(3);
            buffer.Add(2);
            buffer.Add(3);
            buffer.Add(4);
            buffer.Insert(0, 1);

            buffer.Count.Should().Be(4);
            buffer.Should().BeEquivalentTo(new int[] { 1, 2, 3, 4 });
        }

        [Fact]
        public void SetValue()
        {
            var buffer = new FixedCircularBuffer<int>(3);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            
            buffer[0] = 4;
            buffer[1] = 5;
            buffer[2] = 6;

            buffer.Count.Should().Be(3);
            buffer.Should().BeEquivalentTo(new int[] { 4, 5, 6 });
        }
    }
}
