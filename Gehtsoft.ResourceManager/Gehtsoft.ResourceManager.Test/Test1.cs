using System;
using System.IO;
using System.Threading;
using NUnit.Framework;
using Gehtsoft.Tools.FileUtils;

namespace Gehtsoft.ResourceManager.Test
{
    [TestFixture]
    class Test1
    {
        [Test]
        public void TestXmlAndBasics()
        {
            TextMessageBlock[] blocks = TextMessageLoader.LoadXml("main", Path.Combine(TypePathUtil.TypeFolder(this.GetType()), "component1.xml"));
            Assert.IsNotNull(blocks);
            Assert.AreEqual(2, blocks.Length);
            Assert.AreEqual("main", blocks[0].Component);
            Assert.AreEqual("main", blocks[1].Component);
            Assert.AreEqual("en", blocks[0].Language);
            Assert.AreEqual("ru", blocks[1].Language);

            Assert.AreEqual(4, blocks[0].Count);
            Assert.AreEqual(2, blocks[1].Count);

            ResourceManager.ResourcePoolResolver = new TheadPoolResolver();
            ResourceManager.AddResources(blocks);

            TestXmlAndBasicsOtherThreadException = null;
            Thread thread = new Thread(TestXmlAndBasicsOtherThread);
            thread.Start();

            ResourceManager.Initialize("ru");

            Assert.Throws<ArgumentException>(() => { string s = ResourceManager.Messages["non-existent-message-id"]; });
            Assert.AreEqual("message1 on top", ResourceManager.Messages["message1"]);
            Assert.AreEqual("message1ruru", ResourceManager.Messages["group1.message1"]);
            Assert.AreEqual("message2enus", ResourceManager.Messages["group1.message2"]);

            Assert.AreEqual("message1 on top", Main.message1);
            Assert.AreEqual("message1ruru", Main.group1.message1);
            Assert.AreEqual("message2enus", Main.group1.message2);
            Assert.AreEqual("тест 123", Main.group1.message3(123));

            while (thread.IsAlive)
                Thread.Sleep(10);

            if (TestXmlAndBasicsOtherThreadException != null)
                throw TestXmlAndBasicsOtherThreadException;
        }

        private static Exception TestXmlAndBasicsOtherThreadException;

        public void TestXmlAndBasicsOtherThread()
        {
            try
            {
                ResourceManager.Initialize("en");
                Assert.Throws<ArgumentException>(() => { string s = ResourceManager.Messages["non-existent-message-id"]; });
                Assert.AreEqual("message1 on top", ResourceManager.Messages["message1"]);
                Assert.AreEqual("message1enus", ResourceManager.Messages["group1.message1"]);
                Assert.AreEqual("message2enus", ResourceManager.Messages["group1.message2"]);

                Assert.AreEqual("message1 on top", Main.message1);
                Assert.AreEqual("message1enus", Main.group1.message1);
                Assert.AreEqual("message2enus", Main.group1.message2);
                Assert.AreEqual("test 123", Main.group1.message3(123));
            }
            catch (Exception e)
            {
                TestXmlAndBasicsOtherThreadException = e;
            }
        }

        [Test]
        public void TestResources()
        {
            TextMessageLoader.AddSupportedCulture("en");
            TextMessageLoader.AddSupportedCulture("ru");
            TextMessageBlock[] blocks = TextMessageLoader.LoadResource("main", "Gehtsoft.ResourceManager.Test.Resource1", TypePathUtil.TypeFileName(this.GetType()));
            Assert.IsNotNull(blocks);
            Assert.AreEqual(2, blocks.Length);
            TextMessageBlockPool blockpool = new TextMessageBlockPool();
            foreach(TextMessageBlock block in blocks)
                blockpool.Add(block);
            TextMessagePool pool = new TextMessagePool();
            pool.Add(blockpool["main", "en"]);
            Assert.AreEqual("resourcemessage1enus", pool["message1"]);
            Assert.AreEqual("resourcemessage2enus", pool["message2"]);
            pool.Add(blockpool["main", "ru"]);
            Assert.AreEqual("resourcemessage1enus", pool["message1"]);
            Assert.AreEqual("message2ruru", pool["message2"]);

        }
    }
}
