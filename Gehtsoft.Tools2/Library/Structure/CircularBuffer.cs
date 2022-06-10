using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gehtsoft.Tools2.Structure
{

    /// <summary>
    /// The circullar buffer
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FixedCircularBuffer<T> : IList<T>
    {
        /// <summary>
        /// The buffer content
        /// </summary>
        protected T[] mBuffer;

        /// <summary>
        /// The buffer capacity
        /// </summary>
        protected int mCapacity;

        /// <summary>
        /// The position of the buffer head
        /// </summary>
        protected int mHead = 0;

        /// <summary>
        /// The size of the buffer
        /// </summary>
        protected int mSize = 0;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="capacity"></param>
        public FixedCircularBuffer(int capacity = 1024)
        {
            mCapacity = capacity;
            mSize = 0;
            mHead = 0;
            mBuffer = new T[capacity];
        }

        /// <summary>
        /// Convert circular list index into internal buffer index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        protected int ConvertIndex(int index)
        {
            if (index < 0 || index >= mSize)
                throw new ArgumentOutOfRangeException(nameof(index));
            return (mHead + index) % mCapacity;
        }

        /// <summary>
        /// Gets enumerator
        /// </summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < mSize; i++)
                yield return mBuffer[ConvertIndex(i)];
        }

        /// <summary>
        /// Gets enumerator
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private const string OVERFLOW = "The ring buffer capacity is exceeded";

        /// <summary>
        /// Adds element to the buffer
        /// </summary>
        /// <param name="item"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public virtual void Add(T item)
        {
            if (mSize == mCapacity)
                throw new InvalidOperationException(OVERFLOW);
            mSize++;
            mBuffer[ConvertIndex(mSize - 1)] = item;
        }

        /// <summary>
        /// Clears the buffer
        /// </summary>
        public void Clear()
        {
            mSize = 0;
        }

        /// <summary>
        /// Checks whether the buffer contains an item
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(T item) => IndexOf(item) >= 0;

        /// <summary>
        /// Copys the buffer to an array
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            for (int i = 0; i < mSize; i++)
                array[arrayIndex + i] = mBuffer[ConvertIndex(i)];
        }

        /// <summary>
        /// Removes item from the buffer
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index < 0)
                return false;
            RemoveAt(index);
            return true;
        }

        /// <summary>
        /// Gets number of items in the buffer
        /// </summary>
        public int Count => mSize;

        /// <summary>
        /// Returns the flag indicating whether the buffer is readonly
        /// </summary>
        public bool IsReadOnly => false;


        /// <summary>
        /// Gets the index of the item
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public int IndexOf(T item)
        {
            for (int i = 0; i < mSize; i++)
                if (Equals(mBuffer[ConvertIndex(i)], item))
                    return i;
            return -1;
        }

        /// <summary>
        /// Inserts the item at the specified position 
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public virtual void Insert(int index, T item)
        {
            if (index == mSize)
            {
                Add(item);
                return;
            }

            if (index < 0 || index > mSize)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (mSize >= mCapacity)
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


        /// <summary>
        /// Remove the item at the specified position
        /// </summary>
        /// <param name="index"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
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

        /// <summary>
        /// Returns the item by the index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public T this[int index]
        {
            get => mBuffer[ConvertIndex(index)];
            set => mBuffer[ConvertIndex(index)] = value;
        }

        /// <summary>
        /// Gets the first element
        /// </summary>
        public T First
        {
            get
            {
                if (mSize == 0)
                    return default;
                else
                    return mBuffer[mHead];
            }
        }

        /// <summary>
        /// Dequeues element
        /// </summary>
        /// <returns></returns>
        public T Dequeue()
        {
            T v = First;
            RemoveAt(0);
            return v;
        }

        /// <summary>
        /// Enqueues element
        /// </summary>
        /// <param name="value"></param>
        public void Enqueue(T value)
        {
            Add(value);
        }
    }
}
