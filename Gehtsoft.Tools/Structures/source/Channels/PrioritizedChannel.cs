using System;

namespace Gehtsoft.Tools.Structures.Channels
{
    public class PrioritizedChannel<T> : ChannelBase<T>
        where T : IComparable<T>
    {
        public PrioritizedChannel() : base(new FibonacciHeapChannelContainer<T>())
        {

        }
        public PrioritizedChannel(int capacity) : base(capacity, new FibonacciHeapChannelContainer<T>())
        {

        }
    }
}