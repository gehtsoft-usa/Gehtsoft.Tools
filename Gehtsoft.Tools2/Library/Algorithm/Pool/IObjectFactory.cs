using System;
using System.Collections.Generic;
using System.Text;

namespace Gehtsoft.Tools2.Algorithm.Pool
{
    public interface IObjectFactory<T>
    {
        T Create();
        void Dispose(T objectT);
    }
}
