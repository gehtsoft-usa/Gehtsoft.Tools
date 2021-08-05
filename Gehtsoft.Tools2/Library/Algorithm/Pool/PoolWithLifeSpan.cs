using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Gehtsoft.Tools2.Algorithm.Pool
{
    public class ObjectPoolWithLifeSpan<T> : ObjectPool<T>
    {
        private long mLifespan;
        private Thread mThread;

        public ObjectPoolWithLifeSpan(IObjectFactory<T> factory, int poolSize, TimeSpan lifespan) : base(factory, poolSize)
        {
            mLifespan = lifespan.Ticks;
            mThread = new Thread(ThreadProc);
            mThread.IsBackground = true;
            mThread.Start();
        }

        private void ThreadProc()
        {
            while (true)
            {
                Thread.Sleep(250);
                long now = DateTime.Now.Ticks;
                if (mPool == null)
                    continue;
                lock (mMutex)
                {
                    for (int i = 0; i < mPool.Length; i++)
                    {
                        if (mPool[i] != null && !mPool[i].InUse)
                        {
                            if ((now - mPool[i].LastAccess.Ticks) > mLifespan)
                            {
                                mFactory.Dispose(mPool[i].Object);
                                mPool[i] = null;
                            }
                        }
                    }
                }
            }
        }
    }
}
