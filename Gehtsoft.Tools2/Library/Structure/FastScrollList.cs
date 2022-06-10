using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Gehtsoft.Tools2.Structure
{
    /// <summary>
    /// Fast scroll list if specific class for storing the data that needs:
    /// close to O(1) to add or remove the value from/to top or from/to the bottom of the list
    /// close to O(1) for indexed access
    ///
    /// A good example of such data is limited history of some dynamic data where:
    /// - a new data adds to the end
    /// - an older history may be uploaded and added to the beginning
    /// - when the limit is reached, the older history is removed from the top
    /// - yet the handling process requires random access to any element by the index
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FastScrollList<T> : IReadOnlyCollection<T>
    {
        private const int CHUNK_BITS = 10;
        private const int CHUNK_SIZE = 1 << CHUNK_BITS;
        private const int CHUNK_INDEX_MASK = ~(0x7fff_ffff << CHUNK_BITS);
        private const int LAST_CHUNK = CHUNK_SIZE - 1;

        /// <summary>
        /// Buffer of chunks
        /// </summary>
        private readonly T[][] mChunks;

        /// <summary>
        /// Maximum number of chunks
        /// </summary>
        private readonly int mChunksLimit;

        /// <summary>
        /// Maximum number of elements
        /// </summary>
        private readonly int mLimit;

        /// <summary>
        /// Current size
        /// </summary>

        private int mSize;

        /// <summary>
        /// The first used slot in a first chunk
        /// </summary>
        private int mFirstInFirst;

        /// <summary>
        /// The index of the first item in the second chunk
        /// </summary>
        private int mSecondChunkFirstIndex;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void IndexToChunk(int index, out int chunk, out int offset)
        {
            if (index < mSecondChunkFirstIndex)
            {
                chunk = 0;
                offset = mFirstInFirst + index;
            }
            else
            {
                index -= mSecondChunkFirstIndex;
                chunk = (index >> CHUNK_BITS) + 1;
                offset = index & CHUNK_INDEX_MASK;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="limit"></param>
        public FastScrollList(int limit = 1_048_576)
        {
            mFirstInFirst = 0;
            mSecondChunkFirstIndex = CHUNK_SIZE;
            mSize = 0;
            mLimit = limit;
            mChunksLimit = limit / CHUNK_SIZE + 2;
            mChunks = new T[mChunksLimit][];
        }

        /// <summary>
        /// Size of the list
        /// </summary>
        public int Count => mSize;

        /// <summary>
        /// The maximum number of items in the list
        /// </summary>
        public int Limit => mLimit;

        /// <summary>
        /// 
        /// </summary>
        public bool IsFull => mSize >= mLimit;

        /// <summary>
        /// Gets or sets an item by the index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetAt(index);
            set => SetAt(index, value);
        }

        /// <summary>
        /// Adds an element to the end of the list
        /// </summary>
        /// <param name="value"></param>
        /// <exception cref="InvalidOperationException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddToEnd(T value)
        {
            if (mSize == mLimit)
                throw new InvalidOperationException("The list is full");

            int chunk, offset;
            T[] pchunk;
            IndexToChunk(mSize, out chunk, out offset);

            if (mChunks[chunk] == null)
                mChunks[chunk] = pchunk = new T[CHUNK_SIZE];
            else
                pchunk = mChunks[chunk];

            pchunk[offset] = value;
            mSize++;
        }

        /// <summary>
        /// Insert the element to the begininning of the list.
        /// </summary>
        /// <param name="value"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public void InsertAtTop(T value)
        {
            if (mSize == mLimit)
                throw new InvalidOperationException("The list is full");

            if (mSize == 0)
            {
                AddToEnd(value);
                return;
            }
            if (mFirstInFirst > 0)
            {
                mFirstInFirst--;
                mSecondChunkFirstIndex++;
            }
            else
            {
                int chunk;
                IndexToChunk(mSize - 1, out chunk, out _);
                if (chunk == mChunksLimit - 1)
                    throw new InvalidOperationException("The list is full");

                for (int i = chunk + 1; i > 0; i--)
                    mChunks[i] = mChunks[i - 1];

                mChunks[0] = new T[CHUNK_SIZE];
                mFirstInFirst = CHUNK_SIZE - 1;
                mSecondChunkFirstIndex = 1;
            }

            mChunks[0][mFirstInFirst] = value;
            mSize++;
        }

        /// <summary>
        /// Gets element at the index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetAt(int index)
        {
            if (index < mSize)
            {
                int chunk, offset;
                if (index < mSecondChunkFirstIndex)
                {
                    chunk = 0;
                    offset = mFirstInFirst + index;
                }
                else
                {
                    index -= mSecondChunkFirstIndex;
                    chunk = (index >> CHUNK_BITS) + 1;
                    offset = index & CHUNK_INDEX_MASK;
                }

                return mChunks[chunk][offset];
            }
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        /// <summary>
        /// Sets an element at the index
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public void SetAt(int index, T value)
        {
            if (index < mSize)
            {
                int chunk, offset;
                IndexToChunk(index, out chunk, out offset);
                mChunks[chunk][offset] = value;
                return;
            }
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        /// <summary>
        /// Removes element(s) from the beginning of the list
        /// </summary>
        /// <param name="count"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveFromTop(int count = 1)
        {
            if (count > mSize)
                count = mSize;

            int chunk, offset;
            IndexToChunk(count, out chunk, out offset);
            while (chunk > 0)
            {
                for (int i = 0; i < mChunksLimit; i++)
                    mChunks[i] = i == mChunksLimit - 1 ? null : mChunks[i + 1];
                chunk--;
            }
            mSize -= count;
            mFirstInFirst = offset;
            mSecondChunkFirstIndex = CHUNK_SIZE - mFirstInFirst;
        }

        /// <summary>
        /// Clears the list
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < mChunksLimit; i++)
                mChunks[i] = null;
            mSize = mFirstInFirst = mSecondChunkFirstIndex = 0;
        }

        /// <summary>
        /// Gets enumerator
        /// </summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator() => new Enumerator(this);

        /// <summary>
        /// Gets enumerator
        /// </summary>
        /// <returns></returns>

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// The enumerator class
        /// </summary>
        sealed class Enumerator : IEnumerator<T>
        {
            private int mRest;
            private int mChunk;
            private int mChunkIndex;
            private T[] mCurrentChunk;
            private readonly FastScrollList<T> mList;

            internal Enumerator(FastScrollList<T> list)
            {
                mList = list;
                Reset();
            }

            /// <summary>
            /// The current element
            /// </summary>
            public T Current
            {
                get
                {
                    return mCurrentChunk[mChunkIndex];
                }
            }

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                //nothing to dispose
            }

            public bool MoveNext()
            {
                mChunkIndex++;
                if (mChunkIndex >= CHUNK_SIZE)
                {
                    mChunk++;
                    if (mChunk < mList.mChunks.Length)
                        mCurrentChunk = mList.mChunks[mChunk];
                    mChunkIndex = 0;
                }
                mRest--;
                return mRest >= 0;
            }

            public void Reset()
            {
                mChunk = 0;
                mChunkIndex = mList.mFirstInFirst - 1;
                mCurrentChunk = mList.mChunks[0];
                mRest = mList.Count;
            }
        }
    }
}
