using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gehtsoft.Tools2.Algorithm
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
        private T[][] mChunks;

        /// <summary>
        /// Maximum number of chunks
        /// </summary>
        private int mChunksLimit;

        /// <summary>
        /// Maximum number of elements
        /// </summary>
        private int mLimit;

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

        private void indexToChunk(int index, out int chunk, out int offset)
        {
            if (index < mSecondChunkFirstIndex)
            {
                chunk = 0;
                offset = mFirstInFirst + index;
            }
            else
            {
                index -= mSecondChunkFirstIndex;
                chunk = ((index >> CHUNK_BITS) + 1);
                offset = (index & CHUNK_INDEX_MASK);
            }
        }

        public FastScrollList(int limit = 1_048_576)
        {
            mFirstInFirst = 0;
            mSecondChunkFirstIndex = CHUNK_SIZE;
            mSize = 0;
            mLimit = limit;
            mChunksLimit = limit / CHUNK_SIZE + 2;
            mChunks = new T[mChunksLimit][];
        }

        public int Count => mSize;
        public int Limit => mLimit;
        public bool IsFull => mSize >= mLimit;

        public T this[int index]
        {
            get
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
                        chunk = ((index >> CHUNK_BITS) + 1);
                        offset = (index & CHUNK_INDEX_MASK);
                    }

                    return mChunks[chunk][offset];
                }
                throw new IndexOutOfRangeException();
            }

            set => SetAt(index, value);
        }

        public void AddToEnd(T value)
        {
            if (mSize == mLimit)
                throw new InvalidOperationException("The list is full");

            int chunk, offset;
            T[] pchunk;
            indexToChunk(mSize, out chunk, out offset);

            if (mChunks[chunk] == null)
                mChunks[chunk] = (pchunk = new T[CHUNK_SIZE]);
            else
                pchunk = mChunks[chunk];

            pchunk[offset] = value;
            mSize++;
        }

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
                int chunk, offset;
                indexToChunk(mSize - 1, out chunk, out offset);
                if (chunk == mChunksLimit - 1)
                    throw new InvalidOperationException("The list is full");

                for (int i = chunk + 1; i < mChunks.Length; i++)
                    mChunks[i] = mChunks[i - 1];
                mChunks[0] = new T[CHUNK_SIZE];
                mFirstInFirst = CHUNK_SIZE - 1;
                mSecondChunkFirstIndex = 1;
            }

            mChunks[0][mFirstInFirst] = value;
            mSize++;
        }

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
                    chunk = ((index >> CHUNK_BITS) + 1);
                    offset = (index & CHUNK_INDEX_MASK);
                }

                return mChunks[chunk][offset];
            }
            throw new IndexOutOfRangeException();
        }

        public void SetAt(int index, T value)
        {
            if (index < mSize)
            {
                int chunk, offset;
                indexToChunk(index, out chunk, out offset);
                mChunks[chunk][offset] = value;
                return;
            }
            throw new IndexOutOfRangeException();
        }

        public void RemoveFromTop(int count = 1)
        {
            if (count > mSize)
                count = mSize;

            int chunk, offset;
            indexToChunk(count, out chunk, out offset);
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

        public void Clear()
        {
            for (int i = 0; i < mChunksLimit; i++)
                mChunks[i] = null;
            mSize = mFirstInFirst = mSecondChunkFirstIndex = 0;
        }

        public IEnumerator<T> GetEnumerator() => new Enumerator(this);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


        class Enumerator : IEnumerator<T>
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
