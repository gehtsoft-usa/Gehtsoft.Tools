using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Gehtsoft.Tools.FileUtils;
using Gehtsoft.Tools.Log;
using Gehtsoft.Tools.Log.Serilog;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace Gehtsoft.Tools.UnitTest
{
    [TestFixture]
    public class TestSerologLog
    {
        [Test]
        public void SerilogTest()
        {
            string path = TypePathUtil.TypeFolder(typeof(TestSerologLog));
            DateTime now = DateTime.Now;
            string logfile = Path.Combine(path, $"log-{now.Year:D4}{now.Month:D2}{now.Day:D2}.txt");
            if (File.Exists(logfile))
                File.Delete(logfile);

            ILogService logger = new SerilogLogService(LogLevel.Debug, path);
            Assert.IsNotNull(logger.Debug);
            Assert.IsNotNull(logger.Info);
            Assert.IsNotNull(logger.Warning);
            Assert.IsNotNull(logger.Error);
            Assert.IsNotNull(logger.Fatal);

            Assert.AreEqual(LogLevel.Debug, logger.Debug.Level);
            Assert.AreEqual(LogLevel.Info, logger.Info.Level);
            Assert.AreEqual(LogLevel.Warning, logger.Warning.Level);
            Assert.AreEqual(LogLevel.Error, logger.Error.Level);
            Assert.AreEqual(LogLevel.Fatal, logger.Fatal.Level);

            logger.Debug?.Log("testdebug1");
            logger.Debug?.Log("testdebug1formatted {0:D5}", 123);
            logger.Info?.Log("testinfo1");
            logger.Warning?.Log("testwarning1");
            logger.Error?.Log("testerror1");
            logger.Fatal?.Log("testfatal1");

            try
            {
                throw new InvalidOperationException("exceptionmessage");
            }
            catch (Exception e)
            {
                logger.Error?.Log(e, "testexception1");
            }

            logger.Log(LogLevel.Debug, "testdebug2");
            logger.Log(LogLevel.Info, "testinfo2");
            logger.Log(LogLevel.Warning, "testwarning2");
            logger.Log(LogLevel.Error, "testerror2");
            logger.Log(LogLevel.Fatal, "testfatal2");

            logger.Level = LogLevel.Error;
            
            Assert.IsNull(logger.Debug);
            Assert.IsNull(logger.Info);
            Assert.IsNull(logger.Warning);
            Assert.IsNotNull(logger.Error);
            Assert.IsNotNull(logger.Fatal);
            logger.Error?.Log("testerror3");
            logger.Fatal?.Log("testfatal3");

            logger.Log(LogLevel.Debug, "testdebug4");
            logger.Log(LogLevel.Info, "testinfo4");
            logger.Log(LogLevel.Warning, "testwarning4");
            logger.Log(LogLevel.Error, "testerror4");
            logger.Log(LogLevel.Fatal, "testfatal4");
            logger.Log(LogLevel.Off, "off");

            logger.Level = LogLevel.Off;

            logger.Log(LogLevel.Debug, "testdebug5");
            logger.Log(LogLevel.Info, "testinfo5");
            logger.Log(LogLevel.Warning, "testwarning5");
            logger.Log(LogLevel.Error, "testerror5");
            logger.Log(LogLevel.Fatal, "testfatal5");

            logger.Dispose();

            List<string> lines = ReadFile(logfile);
            Assert.IsTrue(Contains(lines, LogLevel.Debug, "debug1"));
            Assert.IsTrue(Contains(lines, LogLevel.Debug, "testdebug1formatted 00123"));
            Assert.IsTrue(Contains(lines, LogLevel.Info, "info1"));
            Assert.IsTrue(Contains(lines, LogLevel.Warning, "warning1"));
            Assert.IsTrue(Contains(lines, LogLevel.Error, "error1"));
            Assert.IsTrue(Contains(lines, LogLevel.Error, "testexception1"));
            Assert.IsTrue(Contains(lines, LogLevel.Off, "System.InvalidOperationException: exceptionmessage"));
            Assert.IsTrue(Contains(lines, LogLevel.Fatal, "fatal1"));

            Assert.IsTrue(Contains(lines, LogLevel.Debug, "debug2"));
            Assert.IsTrue(Contains(lines, LogLevel.Info, "info2"));
            Assert.IsTrue(Contains(lines, LogLevel.Warning, "warning2"));
            Assert.IsTrue(Contains(lines, LogLevel.Error, "error2"));
            Assert.IsTrue(Contains(lines, LogLevel.Fatal, "fatal2"));

            Assert.IsTrue(Contains(lines, LogLevel.Error, "error3"));
            Assert.IsTrue(Contains(lines, LogLevel.Fatal, "fatal3"));

            Assert.IsFalse(Contains(lines, LogLevel.Debug, "debug4"));
            Assert.IsFalse(Contains(lines, LogLevel.Info, "info4"));
            Assert.IsFalse(Contains(lines, LogLevel.Warning, "warning4"));
            Assert.IsTrue(Contains(lines, LogLevel.Error, "error4"));
            Assert.IsTrue(Contains(lines, LogLevel.Fatal, "fatal4"));

            Assert.IsFalse(Contains(lines, LogLevel.Debug, "debug5"));
            Assert.IsFalse(Contains(lines, LogLevel.Info, "info5"));
            Assert.IsFalse(Contains(lines, LogLevel.Warning, "warning5"));
            Assert.IsFalse(Contains(lines, LogLevel.Error, "error5"));
            Assert.IsFalse(Contains(lines, LogLevel.Fatal, "fatal5"));
            Assert.IsFalse(Contains(lines, LogLevel.Off, "off"));
        }

        private List<string> ReadFile(string logfile)
        {
            List<string> lines = new List<string>();
            using (FileStream fs = new FileStream(logfile, FileMode.Open, FileAccess.Read))
            {
                using (StreamReader r = new StreamReader(fs))
                {
                    string line;
                    while ((line = r.ReadLine()) != null)
                        lines.Add(line);
                }
            }

            return lines;
        }

        private bool Contains(List<string>list, LogLevel level, string message)
        {
            foreach (string line in list)
            {
                bool rc1 = false;
                switch (level)
                {
                case LogLevel.Debug:
                    rc1 = line.Contains("[Debug]");
                    break;
                    case LogLevel.Info:
                        rc1 = line.Contains("[Information]");
                        break;
                    case LogLevel.Warning:
                        rc1 = line.Contains("[Warning]");
                        break;
                    case LogLevel.Error:
                        rc1 = line.Contains("[Error]");
                        break;
                    case LogLevel.Fatal:
                        rc1 = line.Contains("[Fatal]");
                        break;
                    case LogLevel.Off:
                        rc1 = true;
                        break;
                }

                bool rc2 = line.Contains(message);
                if (rc1 && rc2)
                    return true;
            }

            return false;
        }
    }
}
