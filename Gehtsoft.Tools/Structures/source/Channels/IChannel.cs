using System;
using System.Threading;
using System.Threading.Tasks;
using Gehtsoft.Tools.TypeUtils;

namespace Gehtsoft.Tools.Structures.Channels
{
    public interface IChannel : IDisposable
    {
        bool IsClosed { get; }

        void Close();

        int Count { get; }
        bool IsEmpty { get; }

        object Receive();

        MutexSlim SyncRoot { get; }
        WaitHandle DataAvailableWaitHandle { get; }
    }

    public interface IChannel<T> : IChannel
    {
        T Peek();

        new T Receive();

        T Receive(CancellationToken cancellationToken);

        Task<T> ReceiveAsync();

        Task<T> ReceiveAsync(CancellationToken cancellationToken);

        bool Post(T message);

        void Send(T message);

        void Send(T message, CancellationToken cancellationToken);

        Task SendAsync(T message);

        Task SendAsync(T message, CancellationToken cancellationToken);
    }
}