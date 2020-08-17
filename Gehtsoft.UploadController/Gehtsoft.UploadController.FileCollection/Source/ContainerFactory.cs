using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Gehtsoft.UploadController.FileCollection
{
    public class UploadedContainerFactory
    {
        private DirectoryInfo mBaseFolder;

        public UploadedContainerFactory(string baseFolder)
        {
            mBaseFolder = new DirectoryInfo(baseFolder);
        }

        public UploadedFileContainer GetContainer(string type, string id)
        {
            CheckDirectory();

            var container = new UploadedFileContainer(new DirectoryInfo(Path.Combine(mBaseFolder.FullName, type, id)), type, id);
            container.Refresh();
            return container;
        }

        private void CheckDirectory()
        {
            if (!mBaseFolder.Exists)
                Directory.CreateDirectory(mBaseFolder.FullName);
        }

        private Task CheckDirectoryAsync(CancellationToken? token)
        {
            Action action = () => { 
                if (!mBaseFolder.Exists) 
                    Directory.CreateDirectory(mBaseFolder.FullName); 
            };

            if (token == null)
                return Task.Run(action);
                    else
                return Task.Run(action, token.Value);
        }

        public async Task<UploadedFileContainer> GetContainerAsync(string type, string id, CancellationToken? token = null)
        {
            await CheckDirectoryAsync(token);

            var container = new UploadedFileContainer(new DirectoryInfo(Path.Combine(mBaseFolder.FullName, type, id)), type, id);
            await container.RefreshAsync(token);
            return container;
        }

        public UploadedFileContainer GetContainer(string type, int id) => GetContainer(type, id.ToString("D12"));

        public Task<UploadedFileContainer> GetContainerAsync(string type, int id, CancellationToken? token = null) => GetContainerAsync(type, id.ToString("D12"), token);

        public UploadedFileContainer GetContainer(string type, Guid id) => GetContainer(type, id.ToString("N"));

        public Task<UploadedFileContainer> GetContainerAsync(string type, Guid id, CancellationToken? token = null) => GetContainerAsync(type, id.ToString("N"), token);

        public UploadedFileContainer[] GetAllContainers(string type)
        {
            CheckDirectory();
            string path = Path.Combine(mBaseFolder.FullName, type);
            if (Directory.Exists(path))
            {
                string[] folders = Directory.GetDirectories(path);
                if (folders.Length == 0)
                    return null;
                UploadedFileContainer[] containers = new UploadedFileContainer[folders.Length];
                for (int i = 0; i < folders.Length; i++)
                {
                    DirectoryInfo di = new DirectoryInfo(folders[i]);
                    containers[i] = new UploadedFileContainer(di, type, di.Name);
                    containers[i].Refresh();
                }

                return containers;
            }
            return null;
        }

        public Task<UploadedFileContainer[]> GetAllContainersAsync(string type, CancellationToken? token = null)
        {
            return Task.Run(() => GetAllContainers(type), token ?? CancellationToken.None);
        }

        public string[] GetAllTypes()
        {
            CheckDirectory();

            string[] folders = Directory.GetDirectories(mBaseFolder.FullName);
            if (folders.Length == 0)
                return null;
            string[] types = new string[folders.Length];
            for (int i = 0; i < folders.Length; i++)
                types[i] = (new DirectoryInfo(folders[i])).Name;
            return types;
        }

        public Task<string[]> GetAllTypesAsync(CancellationToken? token = null) => Task.Run(() => GetAllTypes(), token ?? CancellationToken.None);

        public bool ClearType(string type) => DeleteDirectory(Path.Combine(mBaseFolder.FullName, type));

        public Task<bool> ClearTypeAsync(string type, CancellationToken? token = null) => Task.Run(() => ClearType(type), token ?? CancellationToken.None);

        public bool Clear() => DeleteDirectory(mBaseFolder.FullName);

        public Task<bool> ClearAsync(CancellationToken? token = null) => Task.Run(() => Clear(), token ?? CancellationToken.None);

        internal static bool DeleteDirectory(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (!Directory.Exists(path))
                return true;

            string[] content;
            bool success = true;

            content = Directory.GetFiles(path);

            foreach (string file in content)
            {
                try
                {
                    File.Delete(file);
                }
                catch (IOException)
                {
                    success = false;
                }
                catch (UnauthorizedAccessException)
                {
                    success = false;
                }
            }

            content = Directory.GetDirectories(path);
            foreach (string folder in content)
            {
                success &= DeleteDirectory(folder);
            }

            if (success)
            {
                try
                {
                    Directory.Delete(path);
                }
                catch (IOException)
                {
                    return false;
                }
                catch (UnauthorizedAccessException)
                {
                    return false;
                }
            }
            return success;
        }
    }
}
