using System;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gehtsoft.Tools.ConfigurationProfile
{
    public class ProfileFactory
    {
        private static ProfileFactory gProfileFactory;
        private Profile mProfile;
        public delegate void ProfileChangedDelegate();
        public event ProfileChangedDelegate ProfileChanged;
        private bool mAutoSave;
        private Thread mAutoSaveThread;

        /// <summary>
        /// value, key => encrypted
        /// </summary>
        public static Func<string, string, string> Encrypt { get; set; } = null;

        /// <summary>
        /// value, key, defaultValue => decrypted
        /// </summary>
        public static Func<string, string, string, string> Decrypt { get; set; } = null;

        private static string SimpleEncrypt(string value, string key)
        {
            byte[] bvalue = Encoding.UTF8.GetBytes(value);
            byte[] bkey = Encoding.UTF8.GetBytes(key);

            int l = bkey.Length;
            for (int i = 0, j = 0; i < bvalue.Length; i++, j = (j + 1) % l)
                bvalue[i] = (byte)(bvalue[i] ^ bkey[j]);
            return Convert.ToBase64String(bvalue);
        }

        private static string SimpleDecrypt(string value, string key, string defaultValue)
        {
            try
            {
                byte[] bvalue = Convert.FromBase64String(value);
                byte[] bkey = Encoding.UTF8.GetBytes(key);
                int l = bkey.Length;
                for (int i = 0, j = 0; i < bvalue.Length; i++, j = (j + 1) % l)
                    bvalue[i] = (byte)(bvalue[i] ^ bkey[j]);
                return Encoding.UTF8.GetString(bvalue);
            }
            catch (Exception )
            {
                return defaultValue;
            }
        }

        public static ProfileFactory Instance
        {
            get
            {
                if (gProfileFactory == null)
                    gProfileFactory = new ProfileFactory();
                return gProfileFactory;
            }
        }

        public ProfileFactory()
        {
            if (Encrypt == null)
                Encrypt = SimpleEncrypt;
            if (Decrypt == null)
                Decrypt = SimpleDecrypt;
        }

        ~ProfileFactory()
        {
            mSuspendWather = true;
            Save();
        }

        public Profile Profile
        {
            get
            {
                if (mProfile == null)
                    mProfile = new Profile();
                return mProfile;
            }
        }

        private string mLocation;
        private string mFileName;
        private FileSystemWatcher mWatcher;


        public void Configure(string fileName, bool watchFile, bool autoSave)
        {
            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName));

            FileInfo fi = new FileInfo(fileName);
            if (fi.Directory == null || !fi.Directory.Exists)
                throw new ArgumentException("Directory must exists", nameof(fileName));

            mFileName = fi.FullName;
            mLocation = fi.Directory.FullName;

            if (watchFile)
            {
                mWatcher = new FileSystemWatcher(fi.Directory.FullName)
                {
                    NotifyFilter = NotifyFilters.Attributes |
                                   NotifyFilters.CreationTime |
                                   NotifyFilters.FileName |
                                   NotifyFilters.LastAccess |
                                   NotifyFilters.LastWrite |
                                   NotifyFilters.Size
                };
                mWatcher.Deleted += File_Changed;
                mWatcher.Changed += File_Changed;
                mWatcher.IncludeSubdirectories = false;
                mWatcher.EnableRaisingEvents = true;
            }

            if (fi.Exists)
            {
                mProfile = ProfileLoader.LoadProfile(mFileName);
                mProfile.Changed = false;
                mProfile.Source = mFileName;
            }

            mAutoSave = autoSave;
            if (mAutoSave)
            {
                mAutoSaveThread = new Thread(AutoSaveProc);
                mAutoSaveThread.IsBackground = true;
                mAutoSaveThread.Start();
            }

            AppDomain.CurrentDomain.ProcessExit += OnProcessExited;
        }

        private bool mSuspendWather = false;

        public void Save()
        {
            if (mProfile != null && mFileName != null)
            {
                lock (mProfile.Mutex)
                {
                    if (mProfile.Changed)
                    {
                        try
                        {
                            mSuspendWather = true;
                            ProfileLoader.SaveProfile(mFileName, mProfile);
                        }
                        finally
                        {
                            mSuspendWather = false;
                        }
                        mProfile.Changed = false;
                    }
                }
            }
        }


        public void Close()
        {
            if (mProfile.Changed && mAutoSave)
                Save();
            mSuspendWather = true;
            mProfile = null;
        }

        private void OnProcessExited(object sender, EventArgs eventArgs)
        {
            mSuspendWather = true;
            Close();
        }

        private void File_Changed(object sender, FileSystemEventArgs e)
        {
            if (mSuspendWather)
                return;
            if (String.Compare(e.FullPath, mFileName, StringComparison.OrdinalIgnoreCase) == 0)
            {
                if (e.ChangeType == WatcherChangeTypes.Changed)
                {
                    lock (mProfile.Mutex)
                    {
                        mProfile = ProfileLoader.LoadProfile(mFileName);
                        mProfile.Changed = false;
                        mProfile.Source = mFileName;
                        ProfileChanged?.Invoke();
                    }
                }
                else if (e.ChangeType == WatcherChangeTypes.Deleted)
                {
                    mProfile = new Profile();
                    ProfileChanged?.Invoke();
                }
            }
        }

        private void AutoSaveProc()
        {
            while (true)
            {
                Thread.Sleep(1000);
                if (mProfile == null)
                    continue;
                lock (mProfile.Mutex)
                {
                    if (mProfile.Changed)
                    {
                        Save();
                    }
                }
            }
        }
    }
}

