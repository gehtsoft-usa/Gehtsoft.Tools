using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gehtsoft.Tools.Log.RollingFile
{
    public enum RollingPeriod
    {
        None,
        Hour,
        Day,
        Week,
        Month,
    }

    public class RollingFileLogService : ILogService
    {
        private LogLevel mLogLevel;
        private readonly MessageQueue mQueue;
        private readonly MessageQueueWriter mMessageQueueWriter;

        public LogLevel Level
        {
            get { return mLogLevel; }
            set
            {
                mLogLevel = value;
                UpdateConfig();
            }
        }

        public bool IsOn(LogLevel level) => ((int)level >= (int)Level) && level != LogLevel.Off;
        private QueuedLogStream mInfo, mDebug, mWarning, mError, mFatal;
        public ILoggingStream Debug => IsOn(LogLevel.Debug) ? mDebug : null;
        public ILoggingStream Info => IsOn(LogLevel.Info) ? mInfo : null;
        public ILoggingStream Warning => IsOn(LogLevel.Warning) ? mWarning : null;
        public ILoggingStream Error => IsOn(LogLevel.Error) ? mError : null;
        public ILoggingStream Fatal => IsOn(LogLevel.Fatal) ? mFatal : null;
        public TimeSpan WriteTimeout
        {
            get => mMessageQueueWriter.WriteTimeout;
            set => mMessageQueueWriter.WriteTimeout = value;
        }

        public RollingFileLogService(LogLevel level = LogLevel.Off, string path = "./log/", string prefix = "log", string extension=".txt", RollingPeriod period = RollingPeriod.Day)
        {
            mQueue = new MessageQueue();

            if (!path.EndsWith(@"\") && !path.EndsWith(@"/"))
                path += @"/";

            if (!extension.StartsWith("."))
                path = "." + path;

            mMessageQueueWriter = new MessageQueueWriter(level, path, prefix, extension, period, mQueue);
           
            mDebug = new QueuedLogStream(this, LogLevel.Debug);
            mInfo = new QueuedLogStream(this, LogLevel.Info);
            mWarning = new QueuedLogStream(this, LogLevel.Warning);
            mError = new QueuedLogStream(this, LogLevel.Error);
            mFatal = new QueuedLogStream(this, LogLevel.Fatal);
           
            UpdateConfig();
        }

        private void UpdateConfig()
        {
            mMessageQueueWriter.Flush();
            mMessageQueueWriter.Level = mLogLevel;
        }

        public void InternalLog(LogLevel level, string message)
        {
            mQueue.Enqueue(new LogMessage() {Text = message, IsFormat = false, Level = level});
            if (level == LogLevel.Fatal || level == LogLevel.Error)
                mMessageQueueWriter.Flush();

        }

        public void InternalLog(LogLevel level, string format, params object[] parameters)
        {
            try
            {
                if (parameters != null)
                {
                    foreach (object parameter in parameters)
                    {
                        if (parameter is IDisposable)
                        {
                            string rs = string.Format(format, parameters);
                            mQueue.Enqueue(new LogMessage() {Text = rs, IsFormat = false, Level = level});
                            return;
                        }
                    }
                }

                if (parameters == null)
                    mQueue.Enqueue(new LogMessage() {Text = format, IsFormat = false, Level = level});
                else
                    mQueue.Enqueue(new LogMessage() {Text = format, Args = parameters, IsFormat = true, Level = level});
            }
            finally
            {
                if (level == LogLevel.Fatal || level == LogLevel.Error)
                    mMessageQueueWriter.Flush();
            }
        }

        internal void InternalLog(LogLevel level, Exception e, string message)
        {
            mQueue.Enqueue(new LogMessage() {Text = message, Exception = e, IsFormat = false, Level = level});
            if (level == LogLevel.Fatal || level == LogLevel.Error)
                mMessageQueueWriter.Flush();
        }

        public void Log(LogLevel level, string message)
        {
            if (IsOn(level))
                InternalLog(level, message);
        }

        public void Log(LogLevel level, string format, params object[] parameters)
        {
            if (IsOn(level))
                InternalLog(level, format, parameters);
        }

        public void Log(LogLevel level, Exception e, string message)
        {
            if (IsOn(level))
                InternalLog(level, e, message);
        }

        public void Dispose()
        {
            mMessageQueueWriter.Dispose();
        }

        public void Flush()
        {
            mMessageQueueWriter.Flush();
        }
    }
}
