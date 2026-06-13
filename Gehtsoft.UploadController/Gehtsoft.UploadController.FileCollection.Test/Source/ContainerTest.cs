using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gehtsoft.Tools2.Extensions;
using Xunit;

namespace Gehtsoft.UploadController.FileCollection.Test.Source
{
    public class ContainerTest
    {
        private string mBasePath;

        public ContainerTest()
        {
            Clear();
        }

        private void Clear()
        {
            mBasePath = Path.Combine(typeof(ContainerTest).TypeFolder(), "containers");
            if (Directory.Exists(mBasePath))
            {
                UploadedContainerFactory factory = new UploadedContainerFactory(mBasePath);
                Assert.True(factory.Clear());
            }
            Assert.False(Directory.Exists(mBasePath));
        }

        [Fact]
        public void Test()
        {
            Clear();
            UploadedContainerFactory factory = new UploadedContainerFactory(Path.Combine(mBasePath, "test1"));
            Assert.Null(factory.GetAllTypes());
            Assert.Null(factory.GetAllContainers("type1"));

            UploadedFileContainer container = factory.GetContainer("type1", 1);
            Assert.False(Directory.Exists(container.FullName));
            Assert.False(container.Exists);
            Assert.Equal(0, container.Count);
            Assert.NotNull(container.GetEnumerator());
            Assert.False(container.GetEnumerator().MoveNext());

            Assert.Throws<IndexOutOfRangeException>(() => Assert.NotNull(container[0]));
            Assert.Null(container["file1.txt", false]);
            UploadedFile file = container["file1.txt", true];
            Assert.NotNull(file);
            Assert.False(file.Exists);
            Assert.Equal("file1.txt", file.Name);
            Assert.Equal("text/plain", file.MineType);

            Assert.True(Directory.Exists(container.FullName));
            Assert.True(container.Exists);
            Assert.Equal(1, container.Count);

            FileInfo fi = new FileInfo(file.FullName);
            Assert.Equal(container.FullName, fi.DirectoryName);
            Assert.Equal("file1.txt", fi.Name);

            file.AppendChunk(new byte[] {1, 2, 3});
            Assert.True(file.Exists);
            Assert.Equal(3, file.Length);
            file.AppendChunk(new byte[] {4, 5, 6});
            Assert.Equal(6, file.Length);

            Assert.Equal(file.Directory, container.FullName);

            byte[] c = file.ReadAll();
            Assert.Equal(new byte[] {1, 2, 3, 4, 5, 6}, c);
            c = file.ReadPiece(1, 2);
            Assert.Equal(new byte[] {2, 3}, c);


            string[] types = factory.GetAllTypes();
            Assert.NotNull(types);
            Assert.Single(types);
            Assert.Equal("type1", types[0]);
            

            UploadedFileContainer[] containers = factory.GetAllContainers("type1");
            Assert.NotNull(containers);
            Assert.Single(containers);
            Assert.Equal(container.ContainerID, containers[0].ContainerID);
            Assert.Equal("type1", containers[0].ContainerType);

            UploadedFileContainer container1 = factory.GetContainer("type2", 1);
            UploadedFile file11 = container1["file1.txt", true];
            file11.AppendChunk(new byte[] {1, 2, 3});
            UploadedFile file12 = container1["file2.txt", true];
            file12.AppendChunk(new byte[] {1, 2, 3});

            Assert.True(file11.Exists);
            Assert.True(file12.Exists);
            Assert.True(container1.Exists);

            file12.Delete();
            Assert.True(file11.Exists);
            Assert.False(file12.Exists);
            Assert.True(container1.Exists);

            container1.Delete();
            Assert.False(file11.Exists);
            Assert.False(file12.Exists);
            Assert.False(container1.Exists);


        }

        [Fact]
        public async Task TestAsync()
        {
            Clear();
            UploadedContainerFactory factory = new UploadedContainerFactory(Path.Combine(mBasePath, "test1"));
            Assert.Null(await factory.GetAllTypesAsync());
            Assert.Null(await factory.GetAllContainersAsync("type1"));

            UploadedFileContainer container = await factory.GetContainerAsync("type1", 1);
            Assert.False(Directory.Exists(container.FullName));
            Assert.False(container.Exists);
            Assert.Equal(0, container.Count);
            Assert.NotNull(container.GetEnumerator());
            Assert.False(container.GetEnumerator().MoveNext());

            Assert.Throws<IndexOutOfRangeException>(() => Assert.NotNull(container[0]));
            Assert.Null(container["file1.txt", false]);
            UploadedFile file = container["file1.txt", true];
            Assert.NotNull(file);
            Assert.False(file.Exists);
            Assert.Equal("file1.txt", file.Name);
            Assert.Equal("text/plain", file.MineType);

            Assert.True(Directory.Exists(container.FullName));
            Assert.True(container.Exists);
            Assert.Equal(1, container.Count);

            FileInfo fi = new FileInfo(file.FullName);
            Assert.Equal(container.FullName, fi.DirectoryName);
            Assert.Equal("file1.txt", fi.Name);

            await file.AppendChunkAsync(new byte[] { 1, 2, 3 });
            Assert.True(file.Exists);
            Assert.Equal(3, file.Length);
            await file.AppendChunkAsync(new byte[] { 4, 5, 6 });
            Assert.Equal(6, file.Length);

            Assert.Equal(file.Directory, container.FullName);

            byte[] c = await file.ReadAllAsync();
            Assert.Equal(new byte[] { 1, 2, 3, 4, 5, 6 }, c);
            c = await file.ReadPieceAsync(1, 2);
            Assert.Equal(new byte[] { 2, 3 }, c);


            string[] types = factory.GetAllTypes();
            Assert.NotNull(types);
            Assert.Single(types);
            Assert.Equal("type1", types[0]);


            UploadedFileContainer[] containers = factory.GetAllContainers("type1");
            Assert.NotNull(containers);
            Assert.Single(containers);
            Assert.Equal(container.ContainerID, containers[0].ContainerID);
            Assert.Equal("type1", containers[0].ContainerType);

            UploadedFileContainer container1 = factory.GetContainer("type2", 1);
            UploadedFile file11 = container1["file1.txt", true];
            file11.AppendChunk(new byte[] { 1, 2, 3 });
            UploadedFile file12 = container1["file2.txt", true];
            file12.AppendChunk(new byte[] { 1, 2, 3 });

            Assert.True(file11.Exists);
            Assert.True(file12.Exists);
            Assert.True(container1.Exists);

            file12.Delete();
            Assert.True(file11.Exists);
            Assert.False(file12.Exists);
            Assert.True(container1.Exists);

            container1.Delete();
            Assert.False(file11.Exists);
            Assert.False(file12.Exists);
            Assert.False(container1.Exists);
        }
    }
}
