using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Gehtsoft.Tools.Log.Serilog
{
    public class SerilogLogService : ILogService
    {
        class SerilogLogStream : ILoggingStream
        {
            private ILogger mLogger;
            private LogEventLevel mLevel;

            internal SerilogLogStream(ILogger logger, LogEventLevel level, LogLevel level1)
            {
                mLogger = logger;
                mLevel = level;
                Level = level1;
            }

            public LogLevel Level { get; private set; }
            
            public void Log(string message)
            {
                mLogger.Write(mLevel, message);
            }

            public void Log(string format, params object[] parameters)
            {
                mLogger.Write(mLevel, string.Format(format, parameters));
            }

            public void Log(Exception e, string message)
            {
                mLogger.Write(mLevel, e, message);
            }
        }

        private Logger mLogger;
        private LogLevel mLogLevel  = LogLevel.Off;

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

        private SerilogLogStream mInfo, mDebug, mWarning, mError, mFatal;

        public ILoggingStream Debug => IsOn(LogLevel.Debug) ? mDebug : null;
        public ILoggingStream Info => IsOn(LogLevel.Info) ? mInfo : null;
        public ILoggingStream Warning => IsOn(LogLevel.Warning) ? mWarning : null;
        public ILoggingStream Error => IsOn(LogLevel.Error) ? mError : null;
        public ILoggingStream Fatal => IsOn(LogLevel.Fatal) ? mFatal : null;
        
        private string mLogPath;
        private string mLogPrefix;
        private string mLogExtension;

        public SerilogLogService(LogLevel level = LogLevel.Off, string path = "./log/", string prefix = "log", string extension=".txt")
        {
            mLogLevel = level;
            mLogPath = path;
            mLogPrefix = prefix;
            mLogExtension = extension;
            
            if (!mLogPath.EndsWith(@"\") && !mLogPath.EndsWith(@"/"))
                mLogPath += @"/";
            
            if (!extension.StartsWith("."))
                extension = "." + extension;

            UpdateConfig();

        }

        private void UpdateConfig()
        {
            if (mLogger != null)
            {
                mLogger.Dispose();
                mDebug = mInfo = mWarning = mError = mFatal = null;
            }


            LoggerConfiguration logconfig = new LoggerConfiguration();
            logconfig.WriteTo.File(mLogPath + mLogPrefix + mLogExtension, rollingInterval: RollingInterval.Day);

            switch (mLogLevel)
            {
                case LogLevel.Off:
                    return;
                case LogLevel.Debug:
                    logconfig.MinimumLevel.Debug();
                    break;
                case LogLevel.Info:
                    logconfig.MinimumLevel.Information();
                    break;
                case LogLevel.Warning:
                    logconfig.MinimumLevel.Warning();
                    break;
                case LogLevel.Error:
                    logconfig.MinimumLevel.Error();
                    break;
                case LogLevel.Fatal:
                    logconfig.MinimumLevel.Fatal();
                    break;
            }

            mLogger = logconfig.CreateLogger();
            mDebug = new SerilogLogStream(mLogger, LogEventLevel.Debug, LogLevel.Debug);
            mInfo = new SerilogLogStream(mLogger, LogEventLevel.Information, LogLevel.Info);
            mWarning = new SerilogLogStream(mLogger, LogEventLevel.Warning, LogLevel.Warning);
            mError = new SerilogLogStream(mLogger, LogEventLevel.Error, LogLevel.Error);
            mFatal = new SerilogLogStream(mLogger, LogEventLevel.Fatal, LogLevel.Fatal);
        }

        private LogEventLevel ToSerilogLevel(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Debug:
                    return LogEventLevel.Debug;
                case LogLevel.Info:
                    return LogEventLevel.Information;
                case LogLevel.Warning:
                    return LogEventLevel.Warning;
                case LogLevel.Error:
                    return LogEventLevel.Error;
                case LogLevel.Fatal:
                    return LogEventLevel.Fatal;
                default:
                    throw new ArgumentException($"Unsupported level {level}", nameof(level));
            }

        }

        public void Log(LogLevel level, string message)
        {
            if (IsOn(level))
                mLogger.Write(ToSerilogLevel(level), message);
        }

        public void Log(LogLevel level, string format, params object[] parameters)
        {
            if (IsOn(level))
                mLogger.Write(ToSerilogLevel(level), string.Format(format, parameters));
        }

        public void Log(LogLevel level, Exception e, string message)
        {
            if (IsOn(level))
                mLogger.Write(ToSerilogLevel(level), e, message);
        }

        public void Dispose()
        {
            mLogger?.Dispose();
            mLogger = null;
        }
    }
}
