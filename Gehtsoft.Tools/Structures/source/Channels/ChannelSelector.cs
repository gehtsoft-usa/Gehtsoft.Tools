using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Gehtsoft.Tools.Structures.Channels
{
    public class ChannelSelector
    {
        private readonly IChannel[] mChannels;

        public ChannelSelector(params IChannel[] channels)
        {
            mChannels = channels;
            
        }

        public object Select() => Select(CancellationToken.None);

        public object[] SelectAll() => SelectAll(CancellationToken.None);

        public object Select(CancellationToken cancellationToken)
        {
            WaitHandle[] handles = null;

            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                    throw new OperationCanceledException();

                for (int i = 0; i < mChannels.Length; i++)
                {
                    IChannel channel = mChannels[i];
                    using (channel.SyncRoot.Lock())
                    {
                        if (!channel.IsEmpty)
                            return channel.Receive();
                    }
                }

                handles = new WaitHandle[mChannels.Length + 1];
                for (int i = 0; i < mChannels.Length; i++)
                    handles[i] = mChannels[i].DataAvailableWaitHandle;
                handles[handles.Length - 1] = cancellationToken.WaitHandle;

                while (true)
                {
                    int i = WaitHandle.WaitAny(handles);

                    if (i == WaitHandle.WaitTimeout || i == handles.Length - 1)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            throw new OperationCanceledException();
                    }
                    else
                    {
                        IChannel channel = mChannels[i];
                        using (channel.SyncRoot.Lock())
                        {
                            if (!channel.IsEmpty)
                                return channel.Receive();
                        }
                    }
                    break;
                }
            }
        }

        public object[] SelectAll(CancellationToken cancellationToken)
        {
            List<object> rv = new List<object>();
            WaitHandle[] handles = null;

            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                    throw new OperationCanceledException();

                for (int i = 0; i < mChannels.Length; i++)
                {
                    IChannel channel = mChannels[i];
                    using (channel.SyncRoot.Lock())
                    {
                        while (!channel.IsEmpty)
                            rv.Add(channel.Receive());
                    }
                }

                if (rv.Count > 0)
                    return rv.ToArray();

                handles = new WaitHandle[mChannels.Length + 1];
                for (int i = 0; i < mChannels.Length; i++)
                    handles[i] = mChannels[i].DataAvailableWaitHandle;
                handles[handles.Length - 1] = cancellationToken.WaitHandle;


                while (true)
                {
                    int i = WaitHandle.WaitAny(handles);

                    if (i == WaitHandle.WaitTimeout || i == handles.Length - 1)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            throw new OperationCanceledException();
                    }
                    break;
                }
            }
        }


        public Task<object> SelectAsync() => SelectAsync(CancellationToken.None);

        public Task<object> SelectAsync(CancellationToken cancellationToken)
        {
            return Task.Run(() => Select(cancellationToken));
        }

        public Task<object[]> SelectAllAsync() => SelectAllAsync(CancellationToken.None);

        public Task<object[]> SelectAllAsync(CancellationToken cancellationToken)
        {
            return Task.Run(() => SelectAll(cancellationToken));
        }
    }
}