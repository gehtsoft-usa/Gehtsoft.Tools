using System;

namespace Gehtsoft.Tools.Log
{
    public enum LogLevel
    {
        Off = 100,
        Debug = 1,
        Info = 2,
        Warning = 3,
        Error = 4,
        Fatal = 5,
    }

    public interface ILogService : IDisposable
    {
        LogLevel Level { get; set; }

        ILoggingStream Debug { get; }
        ILoggingStream Info { get; }
        ILoggingStream Warning { get; }
        ILoggingStream Error { get; }
        ILoggingStream Fatal { get; }

        void Log(LogLevel level, string message);
        void Log(LogLevel level, string format, params object[] parameters);
        void Log(LogLevel level, Exception e, string message);
    }
}
