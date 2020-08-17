using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gehtsoft.Tools.TypeUtils
{
    public class MutexSlimDictionary<T> : IDisposable
    {
        private MutexSlim mDictionaryMutex = new MutexSlim();
        private Dictionary<T, MutexSlim> mNamedMutex = new Dictionary<T, MutexSlim>();

        ~MutexSlimDictionary()
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
            using (mDictionaryMutex.Lock())
            {
                foreach (var el in mNamedMutex)
                    el.Value.Dispose();
            }
            mDictionaryMutex.Dispose();
        }

        public MutexSlim Get(T name)
        {
            using (mDictionaryMutex.Lock())
            {
                if (!mNamedMutex.TryGetValue(name, out MutexSlim mutex))
                {
                    mutex = new MutexSlim();
                    mNamedMutex[name] = mutex;
                }
                return mutex;
            }
        }

        public async Task<MutexSlim> GetAsync(T name)
        {
            using (await mDictionaryMutex.LockAsync())
            {
                if (!mNamedMutex.TryGetValue(name, out MutexSlim mutex))
                {
                    mutex = new MutexSlim();
                    mNamedMutex[name] = mutex;
                }
                return mutex;
            }
        }

        public IDisposable Lock(T name)
        {
            using (mDictionaryMutex.Lock())
            {
                if (!mNamedMutex.TryGetValue(name, out MutexSlim mutex))
                {
                    mutex = new MutexSlim();
                    mNamedMutex[name] = mutex;
                }
                return mutex.Lock();
            }
        }

        public async Task<IDisposable> LockAsync(T name)
        {
            using (await mDictionaryMutex.LockAsync())
            {
                if (!mNamedMutex.TryGetValue(name, out MutexSlim mutex))
                {
                    mutex = new MutexSlim();
                    mNamedMutex[name] = mutex;
                }
                return await mutex.LockAsync();
            }
        }
    }
}