namespace Gehtsoft.Tools.Structures.Channels
{
    public interface IChannelContainer<T>
    {
        int Count { get; }
        void Enqueue(T message);
        T Peek();
        T Dequeue();
    }
}