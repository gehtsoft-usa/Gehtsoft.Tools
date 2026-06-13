using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Gehtsoft.Tools.Log.RollingFile
{
    public class MessageQueueWriter : IDisposable
    {
        private LogLevel mLevel;
        private readonly MessageQueue mQueue;
        private readonly Thread mWriterThread;
        //Signaled when WriteTimeout changes or on dispose, so the writer thread re-reads
        //the interval (or exits) instead of staying stuck in a sleep for the old duration.
        private readonly AutoResetEvent mWake = new AutoResetEvent(false);
        private TimeSpan mWriteTimeout = TimeSpan.FromSeconds(10);
        public TimeSpan WriteTimeout
        {
            get => mWriteTimeout;
            set
            {
                mWriteTimeout = value;
                mWake.Set();
            }
        }
        private RollingFileWriter mFileWriter;

        public LogLevel Level
        {
            get => mLevel;
            set => mLevel = value;
        }

        public bool IsAlive => mWriterThread.IsAlive;

        public MessageQueueWriter(LogLevel level, string path, string prefix, string extension, RollingPeriod period, MessageQueue queue, TimeSpan writeTimeout)
        {
            mLevel = level;
            mQueue = queue;
            //Set the interval on the field (not via the property) before the thread starts,
            //so the very first wait already uses the requested value - no signal needed yet.
            mWriteTimeout = writeTimeout;
            mFileWriter = new RollingFileWriter(path, prefix, extension, period);
            mWriterThread = new Thread(WriterThreadProc);
            mWriterThread.IsBackground = true;
            mWriterThread.Start();
        }

        private void WriterThreadProc()
        {
            while (true)
            {
                if (mFileWriter == null)
                    return;

                try
                {
                    WriteQueue();
                    //Block until the interval elapses, or until WriteTimeout changes / we are
                    //disposed (mWake is signaled), then loop and re-read the current interval.
                    mWake.WaitOne(WriteTimeout);
                }
                catch (Exception )
                {
                    //prevent thread from stopping due to unexpected exception
                }
            }
        }

        public void Flush(int maxAttempt, TimeSpan timeOut)
        {
            int count = 0;
            while (count < maxAttempt && !WriteQueue())
            {
                Thread.Sleep(timeOut);
                count++;
            }
        }

        public void Flush() => Flush(5, TimeSpan.FromMilliseconds(100));

        private bool WriteQueue()
        {
            lock (mQueue.Mutex)
            {
                if (mQueue.IsEmpty)
                    return true;

                mFileWriter.SetupWriter();

                while (!mQueue.IsEmpty)
                {
                    string message = mQueue.Top.ToString();
                    if (!mFileWriter.Write(message))
                        return false;
                    mQueue.Dequeue();
                }

                mFileWriter.WriteFinished();
                return true;
            }
        }


        public void Dispose()
        {
            Flush();
            mFileWriter?.Dispose();
            mFileWriter = null;
            //Wake the writer thread so it observes mFileWriter == null and exits promptly,
            //rather than lingering in a (possibly long) wait. The handle itself is intentionally
            //not disposed: the worker may still be about to WaitOne on it, and the SafeWaitHandle
            //finalizer reclaims it. This also keeps Dispose idempotent.
            mWake.Set();
        }
    }
}
