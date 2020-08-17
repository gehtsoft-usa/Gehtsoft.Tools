using System;
using System.Threading;
using System.Threading.Tasks;
using Gehtsoft.Tools.TypeUtils;

namespace Gehtsoft.Tools.Structures.Channels
{
    public class ChannelBase<T> : IChannel<T>, IDisposable
    {
        private ManualResetEventSlim mDataPickedUp = new ManualResetEventSlim(false);
        private ManualResetEventSlim mDataPut = new ManualResetEventSlim(false);
        private AutoResetEvent mDataAvailable = new AutoResetEvent(false);
        private readonly IChannelContainer<T> mQueue;
        private readonly int mCapacity = 0;
        private bool mClosed = false;
        private readonly MutexSlim mMutex = new MutexSlim();
        private CancellationTokenSource mCloseToken = new CancellationTokenSource();

        protected ChannelBase(IChannelContainer<T> channelContainer) : this(0, channelContainer)
        {
        }

        protected ChannelBase(int capacity, IChannelContainer<T> channelContainer)
        {
            mQueue = channelContainer;
            mCapacity = capacity;
        }

        ~ChannelBase() => Dispose(false);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            mDataAvailable?.Dispose();
            mDataAvailable = null;
            mDataPut?.Dispose();
            mDataPut = null;
            mDataPickedUp?.Dispose();
            mDataPickedUp = null;
            mMutex.Dispose();
        }

        public bool Post(T message)
        {
            if (mClosed)
                throw new ChannelIsClosedException();

            using (mMutex.Lock())
            {
                if (mCapacity > 0 && mQueue.Count >= mCapacity)
                    return false;
                mQueue.Enqueue(message);
                mDataPut.Set();
                mDataAvailable.Set();
            }
            return true;
        }

        public void Send(T message) => Send(message, CancellationToken.None);

        public void Send(T message, CancellationToken cancellationToken)
        {
            while (true)
            {
                using (mMutex.Lock())
                {
                    if (Post(message))
                        return;
                    if (mDataPickedUp.IsSet)
                        mDataPickedUp.Reset();
                }
                using (CancellationTokenSource compositeToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, mCloseToken.Token))
                    mDataPickedUp.Wait(compositeToken.Token);
            }
        }

        public Task SendAsync(T message) => SendAsync(message, CancellationToken.None);

        public Task SendAsync(T message, CancellationToken cancellationToken)
        {
            return Task.Run(() => Send(message, cancellationToken));
        }

        public int Count => mQueue.Count;

        public bool IsEmpty => Count == 0;

        object IChannel.Receive()
        {
            return Receive();
        }

        public MutexSlim SyncRoot => mMutex;

        public bool IsClosed => mClosed;

        public void Close()
        {
            mClosed = true;
            mCloseToken.Cancel();
        }

        public T Peek()
        {
            using (mMutex.Lock())
            {
                if (mQueue.Count > 0)
                    return mQueue.Peek();
                return default(T);
            }
        }

        public T Receive() => Receive(CancellationToken.None);

        public T Receive(CancellationToken cancellationToken)
        {
            while (true)
            {
                using (mMutex.Lock())
                {
                    if (Count > 0)
                    {
                        T t = mQueue.Dequeue();
                        mDataPickedUp.Set();
                        return t;
                    }

                    if (mClosed)
                        throw new ChannelIsClosedException();

                    if (mDataPut.IsSet)
                        mDataPut.Reset();
                }

                using (CancellationTokenSource compositeToken =
                    CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, mCloseToken.Token))
                {
                    try
                    {
                        mDataPut.Wait(compositeToken.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        if (!mClosed)
                            throw;
                    }
                }
            }
        }

        public Task<T> ReceiveAsync() => ReceiveAsync(CancellationToken.None);

        public Task<T> ReceiveAsync(CancellationToken cancellationToken)
        {
            return Task.Run(() => Receive(cancellationToken));
        }

        public WaitHandle DataAvailableWaitHandle => mDataAvailable;
    }
}