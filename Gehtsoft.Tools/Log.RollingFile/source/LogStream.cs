using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gehtsoft.Tools.Log;
using Gehtsoft.Tools.Log.RollingFile;

namespace Gehtsoft.Tools.Log.RollingFile
{
    public class QueuedLogStream : ILoggingStream
    {
        public LogLevel Level { get; private set; }
        private readonly RollingFileLogService mService;

        internal QueuedLogStream(RollingFileLogService service, LogLevel level)
        {
            mService = service;
            Level = level;
        }          

        public void Log(string message)
        {
            mService.InternalLog(Level, message);
        }

        public void Log(string format, params object[] parameters)
        {
            mService.InternalLog(Level, format, parameters);
        }

        public void Log(Exception e, string message)
        {
            mService.InternalLog(Level, e, message);
        }
    }

}
