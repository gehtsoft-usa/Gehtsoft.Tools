using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Gehtsoft.Tools.Log.RollingFile
{
    public class RollingFileWriter : IDisposable
    {
        private readonly string mPath;
        private readonly string mPrefix;
        private readonly string mExtension;
        private readonly RollingPeriod mRollingPeriod;
        public Func<DateTime> DateTimeProvider { get; set; }
        private DateTime mCurrentNameStamp = new DateTime(0);
        private string mCurrentName = null;
        private Mutex mMutex = null;
        private MemoryStream mBuffer = new MemoryStream();       
        private FileStream mFileStream;


        public RollingFileWriter(string path, string prefix, string extension, RollingPeriod period)
        {
            mPath = path;
            mPrefix = prefix;
            mExtension = extension;
            mRollingPeriod = period;
            DateTimeProvider = NowProvider;
        }

        public bool Write(string text)
        {
            if (mFileStream == null)
                return false;
            try
            {
                if (!Lock())
                    return false;
                try
                {
                    byte[] bytext = Encoding.UTF8.GetBytes(text + Environment.NewLine);
                    mBuffer.Position = mBuffer.Length;
                    mBuffer.Write(bytext, 0, bytext.Length);
                    return true;
                }
                finally
                {
                    Unlock();
                }
            }
            catch (Exception )
            {
                return false;
            }
        }

        private TimeSpan DEFAULTMUTEXTIMEOUT = TimeSpan.FromMilliseconds(100);

        private bool Lock()
        {
            try
            {
                return mMutex.WaitOne(DEFAULTMUTEXTIMEOUT);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void Unlock()
        {
            mMutex.ReleaseMutex();
        }

        public void WriteFinished()
        {
            while (true)
                if (Lock())
                    break;
            try
            {
                if (mBuffer.Length > 0)
                {
                    byte[] buffer = mBuffer.ToArray();
                    mFileStream.Position = mFileStream.Length;
                    mFileStream.Write(buffer, 0, buffer.Length);
                    mBuffer.Position = 0;
                    mBuffer.SetLength(0);
                }
                mFileStream.Flush();
            }
            finally
            {
                Unlock();
            }
        }

        public void Dispose()
        {
            CloseAll();
        }

        private void CloseAll()
        {
            if (mMutex != null)
            {
                mMutex.Dispose();
                mMutex = null;
            }

            if (mFileStream != null)
            {
                mFileStream.Flush();
                mFileStream.Dispose();
            }

            mFileStream = null;
        }

        private static DateTime NowProvider() => DateTime.Now;

        public bool SetupWriter()
        {
            bool isChanged = false;
            DateTime now = DateTimeProvider();
            if (IsNameChanged(now))
            {
                CloseAll();

                string nameBase = ConstructFileNameBase(now);
                int sequence = 0;
                
                while (true)
                {
                    string nameCandidate = ConstructFileName(nameBase, sequence);
                    try
                    {
                        FileStream fs = new FileStream(nameCandidate, FileMode.Append, FileAccess.Write, FileShare.ReadWrite, 32768);
                        mFileStream = fs;
                        mCurrentName = nameCandidate;
                        mCurrentNameStamp = now;

                        MD5 md5 = MD5.Create();
                        string hash = Convert.ToBase64String(md5.ComputeHash(Encoding.UTF8.GetBytes(mCurrentName)));
                        mMutex = new Mutex(false, ".gsroflog" + hash);
                        isChanged = true;
                        break;
                    }
                    catch (Exception e)
                    {
                        sequence++;
                        if (sequence > 100)
                            throw new InvalidOperationException("Too many attempts to find unlocked file", e);
                    }
                }
            }

            if (mBuffer.Length > 0)
                WriteFinished();

            mBuffer.Position = 0;
            mBuffer.SetLength(0);
            return isChanged;
        }

        private string ConstructFileName(string nameBase, int occurence)
        {
            if (occurence == 0)
                return $"{nameBase}{mExtension}";
            else
                return $"{nameBase}-{occurence}{mExtension}";
        }

        public bool IsNameChanged(DateTime time)
        {
            if (mCurrentName == null)
                return true;

            switch (mRollingPeriod)
            {
                case RollingPeriod.None:
                    return false;
                case RollingPeriod.Hour:
                    return (time.Hour != mCurrentNameStamp.Hour || time.DayOfYear != mCurrentNameStamp.DayOfYear);
                case RollingPeriod.Day:
                    return (time.DayOfYear != mCurrentNameStamp.DayOfYear || time.Year != mCurrentNameStamp.Year);
                case RollingPeriod.Month:
                    return (time.Month != mCurrentNameStamp.Month || time.Year != mCurrentNameStamp.Year);
                case RollingPeriod.Week:
                    return (time.DayOfWeek == DayOfWeek.Sunday) && (time.DayOfYear != mCurrentNameStamp.DayOfYear || time.Year != mCurrentNameStamp.Year);
                default:
                    throw new ArgumentException("Unknown rolling time period", nameof(mRollingPeriod));
            }
        }

        private string ConstructFileNameBase(DateTime time)
        {
            switch (mRollingPeriod)
            {
                case RollingPeriod.None:
                    return $"{mPath}{mPrefix}";
                case RollingPeriod.Hour:
                    return $"{mPath}{mPrefix}-{time.Year:D4}{time.Month:D2}{time.Day:D2}-{time.Hour:D2}";
                case RollingPeriod.Day:
                    return $"{mPath}{mPrefix}-{time.Year:D4}{time.Month:D2}{time.Day:D2}";
                case RollingPeriod.Month:
                    return $"{mPath}{mPrefix}-{time.Year:D4}{time.Month:D2}";
                case RollingPeriod.Week:
                    return $"{mPath}{mPrefix}-{time.Year:D4}{time.Month:D2}{time.Day:D2}";
                default:
                    throw new ArgumentException("Unknown rolling time period", nameof(mRollingPeriod));
            }
        }
    }
}
