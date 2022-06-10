using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gehtsoft.Tools2.Extensions;
using NUnit.Framework;

namespace Gehtsoft.UploadController.FileCollection.Test.Source
{
    [TestFixture]
    public class ContainerTest
    {
        private string mBasePath;

        [OneTimeSetUp]
        public void Setup()
        {
            Clear();
        }

        private void Clear()
        {
            mBasePath = Path.Combine(typeof(ContainerTest).TypeFolder(), "containers");
            if (Directory.Exists(mBasePath))
            {
                UploadedContainerFactory factory = new UploadedContainerFactory(mBasePath);
                Assert.IsTrue(factory.Clear());
            }
            Assert.IsFalse(Directory.Exists(mBasePath));
        }

        [Test]
        public void Test()
        {
            Clear();
            UploadedContainerFactory factory = new UploadedContainerFactory(Path.Combine(mBasePath, "test1"));
            Assert.IsNull(factory.GetAllTypes());
            Assert.IsNull(factory.GetAllContainers("type1"));

            UploadedFileContainer container = factory.GetContainer("type1", 1);
            Assert.IsFalse(Directory.Exists(container.FullName));
            Assert.IsFalse(container.Exists);
            Assert.AreEqual(0, container.Count);
            Assert.IsNotNull(container.GetEnumerator());
            Assert.IsFalse(container.GetEnumerator().MoveNext());

            Assert.Throws<IndexOutOfRangeException>(() => Assert.IsNotNull(container[0]));
            Assert.IsNull(container["file1.txt", false]);
            UploadedFile file = container["file1.txt", true];
            Assert.IsNotNull(file);
            Assert.IsFalse(file.Exists);
            Assert.AreEqual("file1.txt", file.Name);
            Assert.AreEqual("text/plain", file.MineType);

            Assert.IsTrue(Directory.Exists(container.FullName));
            Assert.IsTrue(container.Exists);
            Assert.AreEqual(1, container.Count);

            FileInfo fi = new FileInfo(file.FullName);
            Assert.AreEqual(container.FullName, fi.DirectoryName);
            Assert.AreEqual("file1.txt", fi.Name);

            file.AppendChunk(new byte[] {1, 2, 3});
            Assert.IsTrue(file.Exists);
            Assert.AreEqual(3, file.Length);
            file.AppendChunk(new byte[] {4, 5, 6});
            Assert.AreEqual(6, file.Length);

            Assert.AreEqual(file.Directory, container.FullName);

            byte[] c = file.ReadAll();
            Assert.AreEqual(new byte[] {1, 2, 3, 4, 5, 6}, c);
            c = file.ReadPiece(1, 2);
            Assert.AreEqual(new byte[] {2, 3}, c);


            string[] types = factory.GetAllTypes();
            Assert.IsNotNull(types);
            Assert.AreEqual(1, types.Length);
            Assert.AreEqual("type1", types[0]);
            

            UploadedFileContainer[] containers = factory.GetAllContainers("type1");
            Assert.IsNotNull(containers);
            Assert.AreEqual(1, containers.Length);
            Assert.AreEqual(container.ContainerID, containers[0].ContainerID);
            Assert.AreEqual("type1", containers[0].ContainerType);

            UploadedFileContainer container1 = factory.GetContainer("type2", 1);
            UploadedFile file11 = container1["file1.txt", true];
            file11.AppendChunk(new byte[] {1, 2, 3});
            UploadedFile file12 = container1["file2.txt", true];
            file12.AppendChunk(new byte[] {1, 2, 3});

            Assert.IsTrue(file11.Exists);
            Assert.IsTrue(file12.Exists);
            Assert.IsTrue(container1.Exists);

            file12.Delete();
            Assert.IsTrue(file11.Exists);
            Assert.IsFalse(file12.Exists);
            Assert.IsTrue(container1.Exists);

            container1.Delete();
            Assert.IsFalse(file11.Exists);
            Assert.IsFalse(file12.Exists);
            Assert.IsFalse(container1.Exists);


        }

        [Test]
        public void TestAsync()
        {
            Clear();
            UploadedContainerFactory factory = new UploadedContainerFactory(Path.Combine(mBasePath, "test1"));
            Assert.IsNull(factory.GetAllTypesAsync().Result);
            Assert.IsNull(factory.GetAllContainersAsync("type1").Result);

            UploadedFileContainer container = factory.GetContainerAsync("type1", 1).Result;
            Assert.IsFalse(Directory.Exists(container.FullName));
            Assert.IsFalse(container.Exists);
            Assert.AreEqual(0, container.Count);
            Assert.IsNotNull(container.GetEnumerator());
            Assert.IsFalse(container.GetEnumerator().MoveNext());

            Assert.Throws<IndexOutOfRangeException>(() => Assert.IsNotNull(container[0]));
            Assert.IsNull(container["file1.txt", false]);
            UploadedFile file = container["file1.txt", true];
            Assert.IsNotNull(file);
            Assert.IsFalse(file.Exists);
            Assert.AreEqual("file1.txt", file.Name);
            Assert.AreEqual("text/plain", file.MineType);

            Assert.IsTrue(Directory.Exists(container.FullName));
            Assert.IsTrue(container.Exists);
            Assert.AreEqual(1, container.Count);

            FileInfo fi = new FileInfo(file.FullName);
            Assert.AreEqual(container.FullName, fi.DirectoryName);
            Assert.AreEqual("file1.txt", fi.Name);

            file.AppendChunkAsync(new byte[] { 1, 2, 3 }).Wait();
            Assert.IsTrue(file.Exists);
            Assert.AreEqual(3, file.Length);
            file.AppendChunkAsync(new byte[] { 4, 5, 6 }).Wait();
            Assert.AreEqual(6, file.Length);

            Assert.AreEqual(file.Directory, container.FullName);

            byte[] c = file.ReadAllAsync().Result;
            Assert.AreEqual(new byte[] { 1, 2, 3, 4, 5, 6 }, c);
            c = file.ReadPieceAsync(1, 2).Result;
            Assert.AreEqual(new byte[] { 2, 3 }, c);


            string[] types = factory.GetAllTypes();
            Assert.IsNotNull(types);
            Assert.AreEqual(1, types.Length);
            Assert.AreEqual("type1", types[0]);


            UploadedFileContainer[] containers = factory.GetAllContainers("type1");
            Assert.IsNotNull(containers);
            Assert.AreEqual(1, containers.Length);
            Assert.AreEqual(container.ContainerID, containers[0].ContainerID);
            Assert.AreEqual("type1", containers[0].ContainerType);

            UploadedFileContainer container1 = factory.GetContainer("type2", 1);
            UploadedFile file11 = container1["file1.txt", true];
            file11.AppendChunk(new byte[] { 1, 2, 3 });
            UploadedFile file12 = container1["file2.txt", true];
            file12.AppendChunk(new byte[] { 1, 2, 3 });

            Assert.IsTrue(file11.Exists);
            Assert.IsTrue(file12.Exists);
            Assert.IsTrue(container1.Exists);

            file12.Delete();
            Assert.IsTrue(file11.Exists);
            Assert.IsFalse(file12.Exists);
            Assert.IsTrue(container1.Exists);

            container1.Delete();
            Assert.IsFalse(file11.Exists);
            Assert.IsFalse(file12.Exists);
            Assert.IsFalse(container1.Exists);
        }
    }
}
