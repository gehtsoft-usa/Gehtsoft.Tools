using System;

namespace Gehtsoft.Tools.Structures.Channels
{
    class FibonacciHeapChannelContainer<T> : IChannelContainer<T> 
        where T : IComparable<T>
    {
        private readonly FibonacciHeap<T, T> mQueue = new FibonacciHeap<T, T>();
        public int Count => mQueue.Count;
        public void Enqueue(T message) => mQueue.Add(message, message);

        public T Peek()
        {
            if (mQueue.Count == 0 || mQueue.Minimum == null)
                return default(T);
            return mQueue.Minimum.Value;
        }

        public T Dequeue()
        {
            return mQueue.ExtractMin().Value;
        }
    }
}