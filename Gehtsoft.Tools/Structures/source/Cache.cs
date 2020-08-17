using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Gehtsoft.Tools.TypeUtils;

namespace Gehtsoft.Tools.Structures
{
    public class Cache<T> : IDisposable
        where T : class
    {
        private readonly TimeSpan mLifeSpan;

        public Cache(TimeSpan lifeSpan)
        {
            mLifeSpan = lifeSpan;
        }

        ~Cache()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private ManualResetEvent mCancelCleanup = null;

        protected virtual void Dispose(bool disposing)
        {
            if (mCleanupThread != null && mCleanupThread.IsAlive)
                mCancelCleanup.Set();
            SyncRoot.Dispose();
        }

        private class CacheEqualityComparer : IEqualityComparer<object[]>
        {
            public bool Equals(object[] x, object[] y)
            {
                if (x == null && y == null)
                    return true;
                if (x == null || y == null)
                    return false;
                if (x.Length != y.Length)
                    return false;

                for (int i = 0; i < x.Length; i++)
                {
                    if (!Equals(x[i], y[i]))
                        return false;
                }

                return true;
            }

            public int GetHashCode(object[] obj)
            {
                if (obj == null || obj.Length == 0)
                    return 0.GetHashCode();

                int hash = 0;
                for (int i = 0; i < obj.Length; i++)
                {
                    int c = obj[i] == null ? 0.GetHashCode() : obj[i].GetHashCode();
                    hash = (hash * 397) ^ c;
                }

                return hash;
            }
        }

        protected class CacheElement
        {
            public WeakReference<T> Reference { get; set; }
            public DateTime LastAccess { get; set; }

            public CacheElement(WeakReference<T> reference, DateTime lastAccess)
            {
                Reference = reference;
                LastAccess = lastAccess;
            }
        }

        protected ConcurrentDictionary<object[], CacheElement> mCache = new ConcurrentDictionary<object[], CacheElement>(new CacheEqualityComparer());

        public void Reset()
        {
            using (SyncRoot.Lock())
                mCache.Clear();
        }

        public void ResetFor(params object[] args)
        {
            using (SyncRoot.Lock())
                mCache.TryRemove(args, out CacheElement v);
        }

        protected virtual bool TryGetUnderLock(out T value, params object[] args)
        {
            CacheElement value1;

            if (mCache.TryGetValue(args, out value1))
            {
                if ((DateTime.Now - value1.LastAccess) < mLifeSpan)
                {
                    if (value1.Reference.TryGetTarget(out value))
                    {
                        value1.LastAccess = DateTime.Now;
                        return true;
                    }
                }
            }

            value = default(T);
            return false;
        }

        public bool TryGet(out T value, params object[] args)
        {
            using (SyncRoot.Lock())
                return TryGetUnderLock(out value, args);
        }

        public virtual T Get(params object[] args)
        {
            TryGet(out T value, args);
            return value;
        }

        protected virtual void SetUnderLock(T x, params object[] args)
        {
            CacheElement value = new CacheElement(new WeakReference<T>(x), DateTime.Now);
            mCache[args] = value;
        }

        public void Set(T x, params object[] args)
        {
            using (SyncRoot.Lock())
            {
                SetUnderLock(x, args);
            }
        }

        public T this[params object[] args]
        {
            get => Get(args);
            set => Set(value, args);
        }

        public MutexSlim SyncRoot => new MutexSlim();

        public T GetEvenIfExpired(params object[] args)
        {
            using (SyncRoot.Lock())
            {
                CacheElement value;

                if (mCache.TryGetValue(args, out value))
                {
                    T rc;
                    if (value.Reference.TryGetTarget(out rc))
                        return rc;
                }

                return default(T);
            }
        }

        private Thread mCleanupThread;
        private TimeSpan mCleanupInterval;

        public void EnableAutoCleanup(TimeSpan? cleanupInterval = null)
        {
            mCleanupInterval = cleanupInterval ?? new TimeSpan(1, 0, 0);
            if (mCleanupThread == null || !mCleanupThread.IsAlive)
            {
                mCancelCleanup = new ManualResetEvent(false);
                mCleanupThread = new Thread(CleanupProcedure);
                mCleanupThread.IsBackground = true;
                mCleanupThread.Start();
            }
        }

        public bool IsCleanupAlive => mCleanupThread != null && mCleanupThread.IsAlive;

        private void CleanupProcedure() => CleanupHandler();

        protected virtual void CleanupHandler()
        {
            while (true)
            {
                try
                {
                    if (mCancelCleanup.WaitOne(mCleanupInterval))
                        break;

                    List<object[]> keys = new List<object[]>();
                    KeyValuePair<object[], CacheElement>[] contentCopy = mCache.ToArray();
                    foreach (var v in contentCopy)
                    {
                        if ((DateTime.Now - v.Value.LastAccess) >= mLifeSpan)
                            keys.Add(v.Key);
                    }
                    if (keys.Count > 0)
                    {
                        using (SyncRoot.Lock())
                        {
                            foreach (var key in keys)
                            {
                                mCache.TryRemove(key, out CacheElement value);
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    ;
                }
            }
        }
    }

    public abstract class CacheWithFactory<T> : Cache<T>
        where T : class
    {
        public CacheWithFactory(TimeSpan lifeSpan) : base(lifeSpan)
        {
        }

        protected override bool TryGetUnderLock(out T value, params object[] args)
        {
            if (!base.TryGetUnderLock(out value, args))
            {
                if (Factory(args, out value))
                {
                    SetUnderLock(value, args);
                    return true;
                }
            }

            return true;
        }

        protected abstract bool Factory(object[] key, out T result);
    }

    public class CacheWithActionFactory<T> : CacheWithFactory<T>
        where T : class
    {
        private readonly Func<object[], T> mAction;

        public CacheWithActionFactory(Func<object[], T> action, TimeSpan lifeSpan) : base(lifeSpan)
        {
            mAction = action;
        }

        protected override bool Factory(object[] key, out T result)
        {
            try
            {
                result = mAction.Invoke(key);
                return true;
            }
            catch (Exception)
            {
                result = default(T);
                return false;
            }
        }
    }

    public class AsyncCacheWithActionFactory<T> : CacheWithFactory<T>
        where T : class
    {
        private readonly Func<object[], CancellationToken, Task<T>> mAction;

        public AsyncCacheWithActionFactory(Func<object[], CancellationToken, Task<T>> action, TimeSpan lifeSpan) : base(lifeSpan)
        {
            mAction = action;
        }

        public Task<T> GetAsync(params object[] key) => GetAsync(CancellationToken.None, key);

        public async Task<T> GetAsync(CancellationToken token, params object[] key)
        {
            using (await SyncRoot.LockAsync())
            {
                if (TryGetUnderLock(out T result, key))
                    return result;
                T t = await mAction(key, token);
                SetUnderLock(t);
                return t;
            }
        }

        protected override bool Factory(object[] key, out T result)
        {
            try
            {
                result = mAction.Invoke(key, CancellationToken.None).WaitAndReturn();
                return true;
            }
            catch (Exception)
            {
                result = default(T);
                return false;
            }
        }
    }
}