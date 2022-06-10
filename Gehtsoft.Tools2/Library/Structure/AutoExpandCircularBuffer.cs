namespace Gehtsoft.Tools2.Structure
{
    /// <summary>
    /// The circullar buffer with ability to autogrow
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AutoExpandCircularBuffer<T> : FixedCircularBuffer<T>
    {
        private readonly int mGrowFactor;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="initialCapacity"></param>
        /// <param name="growFactor"></param>
        public AutoExpandCircularBuffer(int initialCapacity = 1024, int growFactor = 20) : base(initialCapacity)
        {
            mGrowFactor = growFactor;
        }

        private void Expand()
        {
            int grow = mCapacity * mGrowFactor / 100;
            
            if (grow <= 0)
                grow = 16;

            int newCapacity = mCapacity + grow;
            T[] newBuffer = new T[newCapacity];
            for (int i = 0; i < mSize; i++)
                newBuffer[i] = mBuffer[ConvertIndex(i)];
            mBuffer = newBuffer;
            mCapacity = newCapacity;
            mHead = 0;
        }

        /// <summary>
        /// Adds an element 
        /// </summary>
        /// <param name="item"></param>
        public override void Add(T item)
        {
            if (mSize == mCapacity)
                Expand();

            base.Add(item);
        }

        /// <summary>
        /// Inserts and element
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        public override void Insert(int index, T item)
        {
            if (mSize == mCapacity)
                Expand();

            base.Insert(index, item);
        }
    }
}
