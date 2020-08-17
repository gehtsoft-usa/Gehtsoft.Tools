using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gehtsoft.Tools.TypeUtils
{
    public class EnumerateAs<TD, TB> : IEnumerable<TB> where TD : TB
    {
        class EnumeratorHelper : IEnumerator<TB>
        {
            private IEnumerator<TD> mBaseEnum;

            public EnumeratorHelper(IEnumerator<TD> baseEnum)
            {
                mBaseEnum = baseEnum;
            }

            public void Dispose()
            {
                mBaseEnum?.Dispose();
                mBaseEnum = null;
            }

            public bool MoveNext()
            {
                return mBaseEnum?.MoveNext() ?? false;
            }

            public void Reset()
            {
                mBaseEnum.Reset();
            }

            public TB Current => mBaseEnum.Current;

            object IEnumerator.Current => Current;
        }

        private readonly IEnumerable<TD> mSource;

        public EnumerateAs(IEnumerable<TD> source)
        {
            mSource = source;
        }

        public IEnumerator<TB> GetEnumerator()
        {
            return new EnumeratorHelper(mSource.GetEnumerator());
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new EnumeratorHelper(mSource.GetEnumerator());
        }
    }
}
