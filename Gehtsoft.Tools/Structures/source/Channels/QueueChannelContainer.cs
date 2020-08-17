using System.Collections.Generic;

namespace Gehtsoft.Tools.Structures.Channels
{
    class QueueChannelContainer<T> : IChannelContainer<T>
    {
        Queue<T> mQueue = new Queue<T>();

        public int Count => mQueue.Count;
        public void Enqueue(T message) => mQueue.Enqueue(message);

        public T Peek() => mQueue.Peek();
        public T Dequeue() => mQueue.Dequeue();
    }
}