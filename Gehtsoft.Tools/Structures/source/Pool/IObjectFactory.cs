using System;
using System.Collections.Generic;
using System.Text;

namespace Gehtsoft.Tools.Structures
{
    public interface IObjectFactory<T>
    {
        T Create();
        void Dispose(T objectT);
    }
}
