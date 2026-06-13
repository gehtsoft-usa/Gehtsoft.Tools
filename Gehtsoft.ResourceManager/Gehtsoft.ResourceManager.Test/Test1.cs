using System;
using System.IO;
using System.Threading;
using Xunit;
using Gehtsoft.Tools.FileUtils;

namespace Gehtsoft.ResourceManager.Test
{
    public class Test1
    {
        [Fact]
        public void TestXmlAndBasics()
        {
            TextMessageBlock[] blocks = TextMessageLoader.LoadXml("main", Path.Combine(TypePathUtil.TypeFolder(this.GetType()), "component1.xml"));
            Assert.NotNull(blocks);
            Assert.Equal(2, blocks.Length);
            Assert.Equal("main", blocks[0].Component);
            Assert.Equal("main", blocks[1].Component);
            Assert.Equal("en", blocks[0].Language);
            Assert.Equal("ru", blocks[1].Language);

            Assert.Equal(4, blocks[0].Count);
            Assert.Equal(2, blocks[1].Count);

            ResourceManager.ResourcePoolResolver = new TheadPoolResolver();
            ResourceManager.AddResources(blocks);

            TestXmlAndBasicsOtherThreadException = null;
            Thread thread = new Thread(TestXmlAndBasicsOtherThread);
            thread.Start();

            ResourceManager.Initialize("ru");

            Assert.Throws<ArgumentException>(() => { string s = ResourceManager.Messages["non-existent-message-id"]; });
            Assert.Equal("message1 on top", ResourceManager.Messages["message1"]);
            Assert.Equal("message1ruru", ResourceManager.Messages["group1.message1"]);
            Assert.Equal("message2enus", ResourceManager.Messages["group1.message2"]);

            Assert.Equal("message1 on top", Main.message1);
            Assert.Equal("message1ruru", Main.group1.message1);
            Assert.Equal("message2enus", Main.group1.message2);
            Assert.Equal("тест 123", Main.group1.message3(123));

            while (thread.IsAlive)
                Thread.Sleep(10);

            if (TestXmlAndBasicsOtherThreadException != null)
                throw TestXmlAndBasicsOtherThreadException;
        }

        private static Exception TestXmlAndBasicsOtherThreadException;

        private void TestXmlAndBasicsOtherThread()
        {
            try
            {
                ResourceManager.Initialize("en");
                Assert.Throws<ArgumentException>(() => { string s = ResourceManager.Messages["non-existent-message-id"]; });
                Assert.Equal("message1 on top", ResourceManager.Messages["message1"]);
                Assert.Equal("message1enus", ResourceManager.Messages["group1.message1"]);
                Assert.Equal("message2enus", ResourceManager.Messages["group1.message2"]);

                Assert.Equal("message1 on top", Main.message1);
                Assert.Equal("message1enus", Main.group1.message1);
                Assert.Equal("message2enus", Main.group1.message2);
                Assert.Equal("test 123", Main.group1.message3(123));
            }
            catch (Exception e)
            {
                TestXmlAndBasicsOtherThreadException = e;
            }
        }

        [Fact]
        public void TestResources()
        {
            TextMessageLoader.AddSupportedCulture("en");
            TextMessageLoader.AddSupportedCulture("ru");
            TextMessageBlock[] blocks = TextMessageLoader.LoadResource("main", "Gehtsoft.ResourceManager.Test.Resource1", TypePathUtil.TypeFileName(this.GetType()));
            Assert.NotNull(blocks);
            Assert.Equal(2, blocks.Length);
            TextMessageBlockPool blockpool = new TextMessageBlockPool();
            foreach(TextMessageBlock block in blocks)
                blockpool.Add(block);
            TextMessagePool pool = new TextMessagePool();
            pool.Add(blockpool["main", "en"]);
            Assert.Equal("resourcemessage1enus", pool["message1"]);
            Assert.Equal("resourcemessage2enus", pool["message2"]);
            pool.Add(blockpool["main", "ru"]);
            Assert.Equal("resourcemessage1enus", pool["message1"]);
            Assert.Equal("message2ruru", pool["message2"]);

        }
    }
}
