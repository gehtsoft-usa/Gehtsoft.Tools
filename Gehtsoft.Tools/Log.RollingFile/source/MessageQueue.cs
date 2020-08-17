using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gehtsoft.Tools.Log.RollingFile
{
    public class LogMessage
    {
        public LogLevel Level { get; set; } = LogLevel.Off;
        public DateTime Time { get; set; } = DateTime.Now;
        public bool IsFormat { get; set; } = false;
        public string Text { get; set; } = null;
        public object[] Args { get; set; } = null;
        public Exception Exception { get; set; } = null;

        public override string ToString()
        {
            if (IsFormat)
            {
                try
                {
                    return $"{Time:s} [{Level}] {string.Format(Text, Args)}";
                }
                catch (Exception e)
                {
                    return $"{Time:s} [{Level}] Error formatting of {Text} => {e.Message}";
                }
            }
            else if (Exception == null)
            {
                return $"{Time:s} [{Level}] {Text}";
            }
            else
            {
                StringBuilder b = new StringBuilder();
                b.AppendLine($"{Time:s} [{Level}] {Text}");
                b.AppendLine("----- [Exception] ------------------------------------------");
                b.AppendLine(Exception.ToString());
                b.Append("----- [End Of Exception] -----------------------------------");
                return b.ToString();
            }
        }
    }


    public class MessageQueue
    {
        private object mMutex = new object();
        public object Mutex => mMutex;
        private Queue<LogMessage> mMessages = new Queue<LogMessage>();

        public MessageQueue()
        {

        }

        public void Enqueue(LogMessage message)
        {
            lock (mMutex)
                mMessages.Enqueue(message);
        }

        public bool IsEmpty => mMessages.Count == 0;

        public LogMessage Dequeue()
        {
            if (IsEmpty)
                return null;
             lock (mMutex)
                  return mMessages.Dequeue();
        }

        public LogMessage Top
        {
            get
            {
                if (IsEmpty)
                    return null;
                return mMessages.Peek();
            }
        }
    }
}
