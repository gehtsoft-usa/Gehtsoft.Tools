using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Gehtsoft.Tools.Structures
{
    public class ObjectPool<T> : IDisposable
    {
        protected class ObjectHolder<T1> where T1 : T
        {
            public T1 Object { get; private set; }
            public DateTime LastAccess { get; set; }

            public bool InUse { get; set; }

            public ObjectHolder(T1 objectT)
            {
                Object = objectT;
                LastAccess = DateTime.Now;
                InUse = false;
            }
        }


        protected ObjectHolder<T>[] mPool;
        protected IObjectFactory<T> mFactory;
        protected object mMutex;
        protected AutoResetEvent mReleased;

        public ObjectPool(IObjectFactory<T> factory, int poolSize)
        {
            mPool = new ObjectHolder<T>[poolSize];
            mFactory = factory;
            mMutex = new object();
            mReleased = new AutoResetEvent(false);
        }

        ~ObjectPool()
        {
            Dispose(false);
        }


        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            lock (mMutex)
            {
                if (mPool != null)
                {
                    for (int i = 0; i < mPool.Length; i++)
                    {
                        if (mPool[i] != null)
                        {
                            if (mPool[i].InUse)
                                throw new InvalidOperationException("Pool is disposed while one or more objects are still in use");

                            mFactory.Dispose(mPool[i].Object);
                            mPool[i] = null;
                        }
                    }
                }
                mPool = null;
            }

            if (disposing)
                GC.SuppressFinalize(this);
        }

        public Borrowed<T> Borrow(int milliseconds)
        {
            return Borrow(TimeSpan.FromMilliseconds(milliseconds));
        }
        public Borrowed<T> Borrow()
        {
            return Borrow(TimeSpan.FromTicks(0));
        }

        public Borrowed<T> Borrow(TimeSpan _timeout)
        {
            T objectT = default(T);
            long now = DateTime.Now.Ticks;
            long timeout = _timeout.Ticks;

            while (true)
            {
                lock (mMutex)
                {
                    for (int i = 0; i < mPool.Length; i++)
                    {
                        if (mPool[i] == null)
                        {
                            objectT = mFactory.Create();
                            ObjectHolder<T> holder = new ObjectHolder<T>(objectT);
                            mPool[i] = holder;
                            mPool[i].InUse = true;
                            mPool[i].LastAccess = DateTime.Now;
                            Borrowed<T> borrowed = new Borrowed<T>(objectT);
                            borrowed.OnRelease += this.OnRelease;
                            return borrowed;
                        }
                        else if (!mPool[i].InUse)
                        {
                            mPool[i].InUse = true;
                            mPool[i].LastAccess = DateTime.Now;
                            Borrowed<T> borrowed = new Borrowed<T>(mPool[i].Object);
                            borrowed.OnRelease += this.OnRelease;
                            return borrowed;
                        }
                    }
                }
                mReleased.WaitOne(1);
                if (timeout > 0)
                {
                    if ((DateTime.Now.Ticks - now) > timeout)
                        return null;
                }
            }
        }

        private void OnRelease(T objectT)
        {
            lock (mMutex)
            {
                for (int i = 0; i < mPool.Length; i++)
                {
                    if (mPool[i] != null && object.ReferenceEquals(objectT, mPool[i].Object))
                    {
                        mPool[i].InUse = false;
                        mPool[i].LastAccess = DateTime.Now;
                        mReleased.Set();
                        return;
                    }
                }
            }
        }

        public int TotalObjects
        {
            get
            {
                lock (mMutex)
                {
                    int total = 0;
                    for (int i = 0; i < mPool.Length; i++)
                    {
                        if (mPool[i] != null)
                            total++;
                    }
                    return total;
                }
            }
        }

        public int InUseObjects
        {
            get
            {
                lock (mMutex)
                {
                    int total = 0;
                    for (int i = 0; i < mPool.Length; i++)
                    {
                        if (mPool[i] != null && mPool[i].InUse)
                            total++;
                    }
                    return total;
                }
            }
        }
    }
}

