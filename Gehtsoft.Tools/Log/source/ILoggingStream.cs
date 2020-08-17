using System;

namespace Gehtsoft.Tools.Log
{
    public interface ILoggingStream
    {
        LogLevel Level { get; }

        void Log(string message);
        void Log(string format, params object[] parameters);
        void Log(Exception e, string message);
    }
}