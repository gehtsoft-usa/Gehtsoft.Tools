using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Gehtsoft.Tools.FileUtils;
using Xunit;

namespace Gehtsoft.Tools.UnitTest
{
    public class PathUtilTest
    {
        [Fact]
        public void TestSuccessfulDelete()
        {
            string basePath = Path.Combine(TypePathUtil.TypeFolder(typeof(PathUtilTest)), "foldertest1");
            Directory.CreateDirectory(Path.Combine(basePath, "folder1"));
            Directory.CreateDirectory(Path.Combine(basePath, "folder1", "folder11"));
            Directory.CreateDirectory(Path.Combine(basePath, "folder2"));
            File.WriteAllText(Path.Combine(Path.Combine(basePath, "folder1", "folder11", "file1")), "");
            File.WriteAllText(Path.Combine(Path.Combine(basePath, "folder1", "folder11", "file2")), "");
            File.WriteAllText(Path.Combine(Path.Combine(basePath, "folder1", "file1")), "");
            File.WriteAllText(Path.Combine(Path.Combine(basePath, "folder2", "file1")), "");

            Assert.True(Directory.Exists(basePath));
            Assert.True(Directory.Exists(Path.Combine(basePath, "folder1")));
            Assert.True(Directory.Exists(Path.Combine(basePath, "folder1", "folder11")));
            Assert.True(Directory.Exists(Path.Combine(basePath, "folder2")));
            Assert.True(File.Exists(Path.Combine(basePath, "folder1", "file1")));
            Assert.True(File.Exists(Path.Combine(basePath, "folder1", "folder11", "file1")));
            Assert.True(File.Exists(Path.Combine(basePath, "folder1", "folder11", "file2")));
            Assert.True(File.Exists(Path.Combine(basePath, "folder2", "file1")));

            Assert.True(PathUtil.DeleteDirectory(basePath));
            Assert.False(Directory.Exists(basePath));
        }

        [Fact]
        public void TestUnsuccessfulDelete()
        {
            //This test relies on Windows file-locking: an open file blocks its deletion.
            //On POSIX systems a file open for read can still be unlinked, so the delete
            //would succeed and the assertions below would not hold.
            Assert.SkipUnless(RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
                "Open-file delete blocking is Windows-only behavior; POSIX allows unlinking an open file.");

            string basePath = Path.Combine(TypePathUtil.TypeFolder(typeof(PathUtilTest)), "foldertest2");
            Directory.CreateDirectory(Path.Combine(basePath, "folder1"));
            Directory.CreateDirectory(Path.Combine(basePath, "folder1", "folder11"));
            Directory.CreateDirectory(Path.Combine(basePath, "folder1", "folder12"));
            Directory.CreateDirectory(Path.Combine(basePath, "folder2"));
            File.WriteAllText(Path.Combine(Path.Combine(basePath, "folder1", "folder11", "file1")), "");
            File.WriteAllText(Path.Combine(Path.Combine(basePath, "folder1", "folder11", "file2")), "");
            File.WriteAllText(Path.Combine(Path.Combine(basePath, "folder1", "file1")), "");
            File.WriteAllText(Path.Combine(Path.Combine(basePath, "folder2", "file1")), "");

            Assert.True(Directory.Exists(basePath));
            Assert.True(Directory.Exists(Path.Combine(basePath, "folder1")));
            Assert.True(Directory.Exists(Path.Combine(basePath, "folder1", "folder11")));
            Assert.True(Directory.Exists(Path.Combine(basePath, "folder1", "folder12")));
            Assert.True(Directory.Exists(Path.Combine(basePath, "folder2")));
            Assert.True(File.Exists(Path.Combine(basePath, "folder1", "file1")));
            Assert.True(File.Exists(Path.Combine(basePath, "folder1", "folder11", "file1")));
            Assert.True(File.Exists(Path.Combine(basePath, "folder1", "folder11", "file2")));
            Assert.True(File.Exists(Path.Combine(basePath, "folder2", "file1")));

            using (FileStream fs = new FileStream(Path.Combine(basePath, "folder1", "folder11", "file2"), FileMode.Open, FileAccess.Read))
                Assert.False(PathUtil.DeleteDirectory(basePath));

            Assert.True(Directory.Exists(basePath));
            Assert.True(Directory.Exists(Path.Combine(basePath, "folder1")));
            Assert.True(Directory.Exists(Path.Combine(basePath, "folder1", "folder11")));
            Assert.False(Directory.Exists(Path.Combine(basePath, "folder1", "folder12")));
            Assert.False(Directory.Exists(Path.Combine(basePath, "folder2")));
            Assert.False(File.Exists(Path.Combine(basePath, "folder1", "file1")));
            Assert.False(File.Exists(Path.Combine(basePath, "folder1", "folder11", "file1")));
            Assert.True(File.Exists(Path.Combine(basePath, "folder1", "folder11", "file2")));
            Assert.False(File.Exists(Path.Combine(basePath, "folder2", "file1")));
        }

        [Fact]
        public void RelativePathTest()
        {
            Assert.Equal(@"..\dir3", PathUtil.RelativePath(@"c:\dir1\dir2", @"c:\dir1\dir3"));
            Assert.Equal(@"..\..\dir3\dir4", PathUtil.RelativePath(@"c:\dir1\dir2", @"c:\dir3\dir4"));
            Assert.Throws<ArgumentException>(() => PathUtil.RelativePath(@"c:\dir1", @"d:\dir1"));
        }

    }
}
