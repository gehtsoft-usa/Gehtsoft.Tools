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
        public TimeSpan WriteTimeout { get; set; } = TimeSpan.FromSeconds(10);
        private RollingFileWriter mFileWriter;

        public LogLevel Level
        {
            get => mLevel;
            set => mLevel = value;
        }

        public bool IsAlive => mWriterThread.IsAlive;

        public MessageQueueWriter(LogLevel level, string path, string prefix, string extension, RollingPeriod period, MessageQueue queue)
        {
            mLevel = level;
            mQueue = queue;
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
                    Thread.Sleep(WriteTimeout);
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
        }
    }
}
