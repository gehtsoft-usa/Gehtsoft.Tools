namespace Gehtsoft.Tools.Structures.Channels
{
    public class Channel<T> : ChannelBase<T>
    {
        public Channel() : base(new QueueChannelContainer<T>())
        {

        }
        public Channel(int capacity) : base(capacity, new QueueChannelContainer<T>())
        {

        }
    }
}