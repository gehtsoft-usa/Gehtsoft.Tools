using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Gehtsoft.UploadController.FileCollection
{
    public class UploadedFileContainer : IEnumerable<UploadedFile>
    {
        private DirectoryInfo mDirectoryInfo;
        public object SyncRoot { get; set; } = new object();

        private List<UploadedFile> mFiles = null;
        public string ContainerType { get; private set; }
        public string ContainerID { get; private set; }
       
        public string FullName => mDirectoryInfo.FullName;
        public bool Exists => Directory.Exists(mDirectoryInfo.FullName);

        internal UploadedFileContainer(DirectoryInfo directoryInfo, string type, string id)
        {
            mDirectoryInfo = directoryInfo;
            ContainerType = type;
            ContainerID = id;
        }

        public void Refresh()
        {
            lock (SyncRoot)
            {
                if (Exists)
                {
                    mFiles = new List<UploadedFile>();
                    string[] files = Directory.GetFiles(mDirectoryInfo.FullName);
                    foreach (string file in files)
                        mFiles.Add(new UploadedFile(new FileInfo(file)));
                }
                else
                {
                    mFiles = null;
                }
            }
        }

        public Task RefreshAsync(CancellationToken? token = null) => Task.Run(() => Refresh(), token ?? CancellationToken.None);

        public void Delete() => UploadedContainerFactory.DeleteDirectory(mDirectoryInfo.FullName);

        public Task DeleteAsync(CancellationToken? token = null) => Task.Run(() => Delete(), token ?? CancellationToken.None);

        public int Count => mFiles?.Count ?? 0;

        public UploadedFile this[int index]
        {
            get
            {
                if (mFiles == null)
                    throw new IndexOutOfRangeException();
                return mFiles[index];
            }
        }

        public UploadedFile this[string name, bool create]
        {
            get
            {
                lock (SyncRoot)
                {
                    if (mFiles == null && !create)
                        return null;

                    if (mFiles != null)
                    {
                        foreach (UploadedFile file in mFiles)
                            if (String.Compare(file.Name, name, StringComparison.OrdinalIgnoreCase) == 0)
                                return file;
                    }

                    if (create)
                    {
                        if (!Exists)
                        {
                            Directory.CreateDirectory(mDirectoryInfo.FullName);
                            Refresh();
                        }

                        UploadedFile f = new UploadedFile(new FileInfo(Path.Combine(mDirectoryInfo.FullName, name)));
                        mFiles.Add(f);
                        return f;
                    }

                    return null;
                }
            }
        }

        public IEnumerator<UploadedFile> GetEnumerator() => mFiles?.GetEnumerator() ?? (new List<UploadedFile>()).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
