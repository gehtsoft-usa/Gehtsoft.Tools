using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HeyRed.Mime;

namespace Gehtsoft.UploadController.FileCollection
{
    public class UploadedFile
    {
        private FileInfo mFileInfo;

        public object SyncRoot { get; set; } = new object();

        public bool Exists => File.Exists(mFileInfo.FullName);

        public long Length => Exists ? (new FileInfo(mFileInfo.FullName).Length) : 0;

        public string Name => mFileInfo.Name;

        public string MineType => MimeTypesMap.GetMimeType(mFileInfo.Name);

        public string Directory => mFileInfo.DirectoryName;

        public string FullName => mFileInfo.FullName;

        internal UploadedFile(FileInfo fileInfo)
        {
            mFileInfo = fileInfo;
        }

        public void AppendChunk(byte[] chunk, int offset = 0, int? length = null)
        {
            if (chunk == null)
                throw new ArgumentNullException(nameof(chunk));
            if (chunk.Length == 0)
                return;

            using (FileStream fs = new FileStream(mFileInfo.FullName, FileMode.Append, FileAccess.Write, FileShare.Read))
                fs.Write(chunk, offset, length ?? chunk.Length);
        }

        public async Task AppendChunkAsync(byte[] chunk, int offset = 0, int? length = null, CancellationToken? token = null)
        {
            if (chunk == null)
                throw new ArgumentNullException(nameof(chunk));
            if (chunk.Length == 0)
                return;

            using (FileStream fs = new FileStream(mFileInfo.FullName, FileMode.Append, FileAccess.Write, FileShare.Read))
                await fs.WriteAsync(chunk, offset, length ?? chunk.Length, token ?? CancellationToken.None);
        }

        public byte[] ReadAll()
        {
            using (FileStream fs = new FileStream(mFileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                int length = (int) mFileInfo.Length;
                byte[] rs = new byte[length];
                fs.Read(rs, 0, length);
                return rs;
            }
        }
        public async Task<byte[]> ReadAllAsync(CancellationToken? token = null)
        {
            using (FileStream fs = new FileStream(mFileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                int length = (int)mFileInfo.Length;
                byte[] rs = new byte[length];
                await fs.ReadAsync(rs, 0, length, token ?? CancellationToken.None);
                return rs;
            }
        }

        public byte[] ReadPiece(int offset, int length)
        {
            using (FileStream fs = new FileStream(mFileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                fs.Position = offset;
                byte[] rs = new byte[length];
                fs.Read(rs, 0, length);
                return rs;
            }
        }
        public async Task<byte[]> ReadPieceAsync(int offset, int length, CancellationToken? token = null)
        {
            using (FileStream fs = new FileStream(mFileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                fs.Position = offset;
                byte[] rs = new byte[length];
                await fs.ReadAsync(rs, 0, length, token ?? CancellationToken.None);
                return rs;
            }
        }

        public void Delete()
        {
            File.Delete(mFileInfo.FullName);
        }

        public Task DeleteAsync(CancellationToken? token = null)
        {
            if (!File.Exists(mFileInfo.FullName))
                return Task.CompletedTask;
            return Task.Run(() => File.Delete(mFileInfo.FullName), token ?? CancellationToken.None);
        }
    }
}
