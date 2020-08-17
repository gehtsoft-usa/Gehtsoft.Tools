using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gehtsoft.Tools.Structures
{
    public class FixedCircularBuffer<T> : IList<T> 
    {
        protected T[] mBuffer;
        protected int mCapacity;
        protected int mHead = 0;
        protected int mSize = 0;

        public FixedCircularBuffer(int capacity = 1024)
        {
            mCapacity = capacity;
            mSize = 0;
            mHead = 0;
            mBuffer = new T[capacity];
        }

        protected int ConvertIndex(int index)
        {
            if (index < 0 || index >= mSize)
                throw new ArgumentOutOfRangeException(nameof(index));
            return (mHead + index) % mCapacity;
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < mSize; i++)
                yield return mBuffer[ConvertIndex(i)];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private const string OVERFLOW = "The ring buffer capacity is exceeded";

        public virtual void Add(T item)
        {
            if (mSize == mCapacity)
                throw new InvalidOperationException(OVERFLOW);
            mSize++;
            mBuffer[ConvertIndex(mSize - 1)] = item;
        }

        public void Clear()
        {
            mSize = 0;
        }

        public bool Contains(T item) => IndexOf(item) > 0;

        public void CopyTo(T[] array, int arrayIndex)
        {
            for (int i = 0; i < mSize; i++)
                array[arrayIndex + i] = mBuffer[ConvertIndex(arrayIndex)];
        }

        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index < 0)
                return false;
            RemoveAt(index);
            return true;
        }

        public int Count => mSize;

        public bool IsReadOnly => false;
        
        public int IndexOf(T item)
        {
            for (int i = 0; i < mSize; i++)
                if (object.Equals(mBuffer[ConvertIndex(i)], item))
                    return i;
            return -1;
        }

        public virtual void Insert(int index, T item)
        {
            if (index < 0 || index >= mSize)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (mSize == mCapacity)
                throw new InvalidOperationException(OVERFLOW);
            
            mSize++;

            if (index == 0)
            {
                mHead--;
                if (mHead < 0)
                    mHead = mCapacity - 1;
                mBuffer[mHead] = item;
            }
            else
            {
                for (int i = mSize - 1; i > index; i--)
                    mBuffer[ConvertIndex(i)] = mBuffer[ConvertIndex(i - 1)];
                mBuffer[ConvertIndex(index)] = item;
            }
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= mSize)
                throw new ArgumentOutOfRangeException(nameof(index));
            
            if (index == 0)
            {
                mHead = (mHead + 1) % mCapacity;               
            }
            else if (index != mSize - 1)
            {
                for (int i = index; i < mSize - 1; i++)
                    mBuffer[ConvertIndex(i)] = mBuffer[ConvertIndex(i + 1)];
            }
            mSize--;
        }

        public T this[int index]
        {
            get => mBuffer[ConvertIndex(index)];
            set => mBuffer[ConvertIndex(index)] = value;
        }

        public T First
        {
            get
            {
                if (mSize == 0)
                    return default(T);
                else
                    return mBuffer[mHead];
            }
        }

        public T Dequeue()
        {
            T v = First;
            RemoveAt(0);
            return v;
        }

        public void Enqueue(T value)
        {
            Add(value);
        }
    }

    public class AutoExpandCircularBuffer<T> : FixedCircularBuffer<T>
    {
        private int mGrowFactor;

        public AutoExpandCircularBuffer(int initialCapacity = 1024, int growFactor = 20) : base(initialCapacity)
        {
            mGrowFactor = growFactor;
        }

        private void Expand()
        {
            int newCapacity = mCapacity + mCapacity * mGrowFactor / 100;
            T[] newBuffer = new T[newCapacity];
            for (int i = 0; i < mSize; i++)
                newBuffer[i] = mBuffer[ConvertIndex(i)];
            mBuffer = newBuffer;
            mCapacity = newCapacity;
            mHead = 0;
        }

        public override void Add(T item)
        {
            if (mSize == mCapacity)
                Expand();

            base.Add(item);
        }

        public override void Insert(int index, T item)
        {
            if (mSize == mCapacity)
                Expand();

            base.Insert(index, item);
        }
    }
}
