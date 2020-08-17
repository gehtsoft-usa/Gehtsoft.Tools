using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Gehtsoft.Tools.TypeUtils
{
    public class MutexSlim : IDisposable
    {
        private SemaphoreSlim mSemaphore = new SemaphoreSlim(1, 1);
        private int mConsequentAquiring = 0;
        private ulong mAcquiringThreadId = 0;

        private static ulong CurrentId
        {
            get
            {
                int? taskId = Task.CurrentId;

                const ulong t1 = 0x100000000;
                const ulong t2 = 0x200000000;
                ulong m, r;

                if (taskId != null)
                {
                    m = t1;
                    r = ((ulong)taskId) & 0xffffffff;
                }
                else
                {
                    m = t2;
                    r = ((ulong)Thread.CurrentThread.ManagedThreadId) & 0xffffffff;
                }

                return m | r;
            }
        }

        public WaitHandle WaitHandle => mSemaphore.AvailableWaitHandle;

        public MutexSlim()
        {
        }

        public MutexSlim(bool owned)
        {
            if (owned)
                Wait();
        }

        ~MutexSlim()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            mSemaphore.Dispose();
        }

        private class Unlocker : IDisposable
        {
            private readonly MutexSlim mMutex;

            public Unlocker(MutexSlim mutex)
            {
                mMutex = mutex;
            }

            public void Dispose()
            {
                mMutex.Release();
            }
        }

        private Unlocker mUnlocker = null;

        private Unlocker SingleUnlocker => mUnlocker ?? (mUnlocker = new Unlocker(this));

        public IDisposable Lock()
        {
            Wait();
            return SingleUnlocker;
        }

        public async Task<IDisposable> LockAsync()
        {
            await WaitAsync();
            return SingleUnlocker;
        }

        private static TimeSpan MaxTimeout = TimeSpan.FromMilliseconds(Int32.MaxValue);

        public void Wait() => Wait(MaxTimeout, CancellationToken.None);

        public bool Wait(int timeout) => Wait(TimeSpan.FromMilliseconds(timeout), CancellationToken.None);

        public bool Wait(TimeSpan timeout) => Wait(timeout, CancellationToken.None);

        public void Wait(CancellationToken token) => Wait(MaxTimeout, token);

        public bool Wait(TimeSpan timeout, CancellationToken token)
        {
            ulong id = CurrentId;
            if (mSemaphore.CurrentCount == 0 && mAcquiringThreadId == id)
            {
                mConsequentAquiring++;
                return true;
            }
            bool rc = mSemaphore.Wait(timeout, token);
            if (rc)
                mAcquiringThreadId = id;
            return rc;
        }

        public Task WaitAsync() => WaitAsync(MaxTimeout, CancellationToken.None);

        public Task<bool> WaitAsync(int timeout) => WaitAsync(TimeSpan.FromMilliseconds(timeout), CancellationToken.None);

        public Task<bool> WaitAsync(TimeSpan timeout) => WaitAsync(timeout, CancellationToken.None);

        public Task WaitAsync(CancellationToken token) => WaitAsync(MaxTimeout, token);

        public async Task<bool> WaitAsync(TimeSpan timeout, CancellationToken token)
        {
            ulong id = CurrentId;
            if (mSemaphore.CurrentCount == 0 && mAcquiringThreadId == id)
            {
                mConsequentAquiring++;
                return true;
            }

            bool rc = await mSemaphore.WaitAsync(timeout, token);
            if (rc)
                this.mAcquiringThreadId = CurrentId;
            return rc;
        }

        public bool IsLocked => mSemaphore.CurrentCount == 0;

        public bool IsLockedByMe => IsLocked && CurrentId == mAcquiringThreadId;

        public void Release()
        {
            if (mConsequentAquiring > 0)
                mConsequentAquiring--;
            else
            {
                mAcquiringThreadId = 0;
                mSemaphore.Release(1);
            }
        }
    }
}