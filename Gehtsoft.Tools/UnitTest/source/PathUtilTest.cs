using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gehtsoft.Tools.FileUtils;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Gehtsoft.Tools.UnitTest
{
    [TestFixture]
    public class PathUtilTest
    {
        [Test]
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

            Assert.IsTrue(Directory.Exists(basePath));
            Assert.IsTrue(Directory.Exists(Path.Combine(basePath, "folder1")));
            Assert.IsTrue(Directory.Exists(Path.Combine(basePath, "folder1", "folder11")));
            Assert.IsTrue(Directory.Exists(Path.Combine(basePath, "folder2")));
            Assert.IsTrue(File.Exists(Path.Combine(basePath, "folder1", "file1")));
            Assert.IsTrue(File.Exists(Path.Combine(basePath, "folder1", "folder11", "file1")));
            Assert.IsTrue(File.Exists(Path.Combine(basePath, "folder1", "folder11", "file2")));
            Assert.IsTrue(File.Exists(Path.Combine(basePath, "folder2", "file1")));

            Assert.IsTrue(PathUtil.DeleteDirectory(basePath));
            Assert.IsFalse(Directory.Exists(basePath));
        }

        [Test]
        public void TestUnsuccessfulDelete()
        {
            string basePath = Path.Combine(TypePathUtil.TypeFolder(typeof(PathUtilTest)), "foldertest2");
            Directory.CreateDirectory(Path.Combine(basePath, "folder1"));
            Directory.CreateDirectory(Path.Combine(basePath, "folder1", "folder11"));
            Directory.CreateDirectory(Path.Combine(basePath, "folder1", "folder12"));
            Directory.CreateDirectory(Path.Combine(basePath, "folder2"));
            File.WriteAllText(Path.Combine(Path.Combine(basePath, "folder1", "folder11", "file1")), "");
            File.WriteAllText(Path.Combine(Path.Combine(basePath, "folder1", "folder11", "file2")), "");
            File.WriteAllText(Path.Combine(Path.Combine(basePath, "folder1", "file1")), "");
            File.WriteAllText(Path.Combine(Path.Combine(basePath, "folder2", "file1")), "");

            Assert.IsTrue(Directory.Exists(basePath));
            Assert.IsTrue(Directory.Exists(Path.Combine(basePath, "folder1")));
            Assert.IsTrue(Directory.Exists(Path.Combine(basePath, "folder1", "folder11")));
            Assert.IsTrue(Directory.Exists(Path.Combine(basePath, "folder1", "folder12")));
            Assert.IsTrue(Directory.Exists(Path.Combine(basePath, "folder2")));
            Assert.IsTrue(File.Exists(Path.Combine(basePath, "folder1", "file1")));
            Assert.IsTrue(File.Exists(Path.Combine(basePath, "folder1", "folder11", "file1")));
            Assert.IsTrue(File.Exists(Path.Combine(basePath, "folder1", "folder11", "file2")));
            Assert.IsTrue(File.Exists(Path.Combine(basePath, "folder2", "file1")));

            using (FileStream fs = new FileStream(Path.Combine(basePath, "folder1", "folder11", "file2"), FileMode.Open, FileAccess.Read))
                Assert.IsFalse(PathUtil.DeleteDirectory(basePath));

            Assert.IsTrue(Directory.Exists(basePath));
            Assert.IsTrue(Directory.Exists(Path.Combine(basePath, "folder1")));
            Assert.IsTrue(Directory.Exists(Path.Combine(basePath, "folder1", "folder11")));
            Assert.IsFalse(Directory.Exists(Path.Combine(basePath, "folder1", "folder12")));
            Assert.IsFalse(Directory.Exists(Path.Combine(basePath, "folder2")));
            Assert.IsFalse(File.Exists(Path.Combine(basePath, "folder1", "file1")));
            Assert.IsFalse(File.Exists(Path.Combine(basePath, "folder1", "folder11", "file1")));
            Assert.IsTrue(File.Exists(Path.Combine(basePath, "folder1", "folder11", "file2")));
            Assert.IsFalse(File.Exists(Path.Combine(basePath, "folder2", "file1")));
        }

        [Test]
        public void RelativePathTest()
        {
            Assert.AreEqual(@"..\dir3", PathUtil.RelativePath(@"c:\dir1\dir2", @"c:\dir1\dir3"));
            Assert.AreEqual(@"..\..\dir3\dir4", PathUtil.RelativePath(@"c:\dir1\dir2", @"c:\dir3\dir4"));
            Assert.Throws<ArgumentException>(() => PathUtil.RelativePath(@"c:\dir1", @"d:\dir1"));
        }

    }
}
