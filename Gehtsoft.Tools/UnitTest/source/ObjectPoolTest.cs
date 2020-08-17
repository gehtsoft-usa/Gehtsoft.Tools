using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Gehtsoft.Tools.Structures;
using NUnit;
using NUnit.Framework;

namespace Gehtsoft.Tools.UnitTest
{
    
    [TestFixture]
    public class PoolTest
    {
        public const int WORKERS = 10;
        public const int REPEAT = 5;
        public const int DELAY = 100;

        public class pooledClass
        {
            private StringBuilder mLog = new StringBuilder();
            private bool mDisposed;
            private DateTime mLastUse;
            private DateTime mDisposedAt;

            public bool Disposed {
                get { return mDisposed; }
                set
                {
                    mDisposed = value;
                    mDisposedAt = DateTime.Now;

                }
            }

            public long BetweenUseAndDispose => mDisposedAt.Ticks - mLastUse.Ticks;

            public void Log(char c)
            {
                mLastUse = DateTime.Now;
                mLog.Append(c);
            }

            public override string ToString()
            {
                return mLog.ToString();
            }
        }

        public class pooledClassFactory : IObjectFactory<pooledClass>
        {
            internal List<pooledClass> mPooled = new List<pooledClass>();

            public pooledClass Create()
            {
                pooledClass c =  new pooledClass();
                mPooled.Add(c);
                return c;
            }

            public void Dispose(pooledClass objectT)
            {
                if (objectT.Disposed)
                    throw new Exception("Object is being disposed twice");
                objectT.Disposed = true;
            }
        }

        class worker
        {
            private ObjectPoolWithLifeSpan<pooledClass> mPool;
            private char mID;
            private Thread mThread;
            public long MaxWaitTime;

            public bool IsAlive => mThread.IsAlive;

            public worker(ObjectPoolWithLifeSpan<pooledClass> pool, char id)
            {
                mPool = pool;
                mID = id;
                mThread = id == 0x30 ? new Thread(run1) : new Thread(run);
                mThread.Start();
            }

            private void run()
            {
                for (int i = 0; i < REPEAT; i++)
                {
                    using (Borrowed<pooledClass> c = mPool.Borrow())
                    {
                        c.Object.Log(mID);
                        Thread.Sleep(DELAY);   //simulate long-running activity
                    }
                }
            }
            private void run1()
            {
                for (int i = 0; i < REPEAT; i++)
                {
                    while (true)
                    {
                        DateTime s = DateTime.Now;
                        Borrowed<pooledClass> c = mPool.Borrow(DELAY * 2);
                        if (c != null)
                        {
                            c.Object.Log(mID);
                            Thread.Sleep(DELAY); //simulate long-running activity
                            c.Dispose();
                            break;
                        }
                        MaxWaitTime = DateTime.Now.Ticks - s.Ticks;
                    }
                }
            }
        }

        [Test]
        public static void TestObjectPool()
        {
            pooledClassFactory factory = new pooledClassFactory();
            using (ObjectPoolWithLifeSpan<pooledClass> pool = new ObjectPoolWithLifeSpan<pooledClass>(factory, 4, TimeSpan.FromMilliseconds(750)))
            {
                worker[] workers = new worker[WORKERS];

                for (int i = 0; i < WORKERS; i++)
                    workers[i] = new worker(pool, (char) (0x30 + i));

                while (true)
                {
                    bool alive = false;

                    for (int i = 0; i < WORKERS; i++)
                        alive |= workers[i].IsAlive;

                    if (!alive)
                        break;

                    Thread.Sleep(50);
                    Assert.IsTrue(pool.InUseObjects <= 4, "Too many objects");
                    Assert.IsTrue(pool.TotalObjects <= 4, "Too many objects");
                }

                List<pooledClass> pooled = factory.mPooled;
                Assert.IsTrue(pooled.Count == 4, "Pool size");

                long limit = TimeSpan.FromMilliseconds(DELAY * 2 + 50).Ticks;
                Assert.IsTrue(workers[0].MaxWaitTime < limit, "Wait time");

                int[] count = new int[WORKERS];
                for (int i = 0; i < WORKERS; i++)
                    count[i] = 0;

                for (int i = 0; i < pooled.Count; i++)
                {
                    string cc = pooled[i].ToString();
                    foreach (char c in cc)
                    {
                        int idx = c - 0x30;
                        count[idx]++;
                    }
                }

                for (int i = 0; i < WORKERS; i++)
                    Assert.IsTrue(count[i] == REPEAT, $"count of {i}");

                Thread.Sleep(1500);
                limit = TimeSpan.FromMilliseconds(750 + 550).Ticks;
                for (int i = 0; i < pooled.Count; i++)
                {
                    Assert.IsTrue(pooled[i].Disposed, "No disposed yet");
                    Assert.IsTrue(pooled[i].BetweenUseAndDispose < limit, "Too long to dispose");
                }
            }
        }
    }
}
