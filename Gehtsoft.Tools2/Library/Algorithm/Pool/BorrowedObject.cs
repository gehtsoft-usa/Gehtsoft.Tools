using System;
using System.Collections.Generic;
using System.Text;

namespace Gehtsoft.Tools2.Algorithm.Pool
{
    public class Borrowed<T> : IDisposable
    {
        public T Object { get; }

        public delegate void ReleaseDelegate(T objectT);
        public event ReleaseDelegate OnRelease;

        internal Borrowed(T objectT)
        {
            Object = objectT;
        }

        ~Borrowed()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (Object != null)
                OnRelease?.Invoke(Object);
            if (disposing)
                GC.SuppressFinalize(this);
        }
    }
}
