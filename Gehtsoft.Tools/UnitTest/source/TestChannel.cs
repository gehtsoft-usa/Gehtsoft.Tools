using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Gehtsoft.Tools.Structures.Channels;
using NUnit.Framework;

namespace Gehtsoft.Tools.UnitTest
{
    [TestFixture]
    public class TestChannel
    {
        [Test]
        public void TestChannel1()
        {
            Channel<int?> channel = new Channel<int?>();
            Assert.IsFalse(channel.IsClosed);
            Assert.IsTrue(channel.IsEmpty);
            Assert.IsNull(channel.Peek());
            for (int i = 0; i < 1000; i++)
                Assert.IsTrue(channel.Post(i));
            Assert.IsFalse(channel.IsClosed);
            Assert.IsFalse(channel.IsEmpty);
            Assert.AreEqual(1000, channel.Count);
            Assert.AreEqual(0, channel.Peek());
            Assert.AreEqual(1000, channel.Count);

            for (int i = 0; i < 1000; i++)
            {
                Assert.AreEqual(i, channel.Peek());
                Assert.AreEqual(i, channel.Receive());
                Assert.AreEqual(1000 - i - 1, channel.Count);
            }

            Task<int?> receive = channel.ReceiveAsync();
            Assert.IsFalse(receive.IsCompleted);
            Thread.Sleep(10);
            channel.Post(123);
            Thread.Sleep(10);
            Assert.IsTrue(receive.IsCompleted);
            Assert.AreEqual(123, receive.Result);


            channel.Close();
            Assert.Throws<ChannelIsClosedException>(() => channel.Post(1));
            while (!channel.IsEmpty)
                channel.Receive();
            Assert.Throws<ChannelIsClosedException>(() => channel.Receive());
        }

        [Test]
        public void TestChannel2()
        {
            Channel<int?> channel = new Channel<int?>(5);
            for (int i = 0; i < 5; i++)
            {
                Assert.IsTrue(channel.Post(i));
            }
            Assert.AreEqual(5, channel.Count);
            Assert.IsFalse(channel.Post(6));
            Assert.AreEqual(5, channel.Count);
            Task sender = channel.SendAsync(7);
            Thread.Sleep(10);
            Assert.AreEqual(5, channel.Count);
            Assert.IsFalse(sender.IsCompleted);
            bool got7 = false;
            while (!channel.IsEmpty)
            {
                got7 = got7 | (channel.Receive() == 7);
                Thread.Sleep(10);
            }
            Thread.Sleep(10);
            Assert.IsTrue(sender.IsCompleted);

            Task filler = Task.Run(() => {
                for (int i = 0; i < 10; i++) 
                    channel.Send(i);
            });

            Thread.Sleep(10);
            Assert.AreEqual(5, channel.Count);
            Assert.IsFalse(filler.IsCompleted);
            while (!channel.IsEmpty)
            {
               Thread.Sleep(10);
                if (channel.Receive() == 9)
                    break;
            }
            Thread.Sleep(10);
            Assert.IsTrue(filler.IsCompleted);
        }

        [Test]
        public void TestPrioritizedChannel()
        {
            PrioritizedChannel<int> channel = new PrioritizedChannel<int>();
            Random r = new Random();

            for (int i = 0; i < 10; i++)
            {
                Dictionary<int, bool> check = new Dictionary<int, bool>();
                for (int j = 0; j < 1000; j++)
                {
                    int v = r.Next(1, 100000);
                    check[v] = false;
                    channel.Post(v);
                }

                int cc = 0;
                while (!channel.IsEmpty)
                {
                    
                    cc++;
                    int v0 = channel.Peek();
                    int v = channel.Receive();
                    Assert.AreEqual(v0, v);
                    check[v] = true;
                }

                Assert.AreEqual(1000, cc);
                foreach (int k in check.Keys)
                {
                    Assert.IsTrue(check[k]);
                }
            }



        }

        [Test]
        public void SelectorTest()
        {
            Channel<int?> channel1 = new Channel<int?>();
            Channel<string> channel2 = new Channel<string>();
            ChannelSelector selector = new ChannelSelector(channel1, channel2);

            for (int i = 0; i < 50; i++)
            {
                int i1 = i;
                Task.Run(() =>
                {
                    channel1.Post(i1);
                    channel2.Post("text" + i1);
                });

                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(1));

                Task<bool> receiver = Task.Run<bool>(() =>
                {
                    bool receivedInt = false;
                    bool receivedString = false;
                    bool receivedCorrectInt = false;
                    bool receivedCorrectString = false;

                    while (!receivedInt || !receivedString)
                    {
                        if (i1 % 10 == 0)
                        {
                            Thread.Sleep(10);
                            object[] all = selector.SelectAll(cancellationTokenSource.Token);
                            foreach (object r in all)
                            {
                                if (r is int i2)
                                {
                                    receivedInt = true;
                                    receivedCorrectInt = i2 == i1;
                                }

                                if (r is string s)
                                {
                                    receivedString = true;
                                    receivedCorrectString = s == "text" + i1;
                                }
                            }
                        }
                        else
                        {
                            object r = selector.Select(cancellationTokenSource.Token);
                            if (r is int i2)
                            {
                                receivedInt = true;
                                receivedCorrectInt = i2 == i1;
                            }

                            if (r is string s)
                            {
                                receivedString = true;
                                receivedCorrectString = s == "text" + i1;
                            }
                        }
                    }
                    return receivedCorrectInt && receivedCorrectString;
                });

                Thread.Sleep(50);
                Assert.IsTrue(receiver.IsCompleted);
                Assert.IsTrue(receiver.Result);
                while (!receiver.IsCompleted)
                {
                    Console.WriteLine("Cancelling...");
                    cancellationTokenSource.Cancel();
                }
            }
        }

        [Test]
        public void MTChannelTest()
        {
            Channel<int> channel = new Channel<int>();
            ConcurrentDictionary<int, int> handled = new ConcurrentDictionary<int, int>();

            int WORKERS = 10;

            Task[] handlers = new Task[WORKERS];
            for (int i = 0; i < WORKERS; i++)
            {
                int currentTask = i;
                handlers[i] = Task.Run(() => 
                {
                    while (true)
                    {
                        try
                        {
                            int message = channel.Receive();
                            handled[message] = currentTask;
                            Thread.Sleep(1);
                        }
                        catch (ChannelIsClosedException )
                        {
                            break;
                        }

                    }
                });
            }

            Task sender = Task.Run(() => {
                for (int i = 0; i < 10000; i++)
                    channel.Send(i);
                Thread.Sleep(2000);
                channel.Close();
            });

            Task.WaitAll(handlers);
            Assert.AreEqual(10000, handled.Count);
            int[] handledCount = new int[WORKERS];
            for (int i = 0; i < 10000; i++)
            {
                Assert.IsTrue(handled.ContainsKey(i));
                Assert.GreaterOrEqual(handled[i], 0);
                Assert.LessOrEqual(handled[i], WORKERS);
                handledCount[handled[i]]++;

            }

            for (int i = 0; i < WORKERS; i++)
                Assert.Greater(handledCount[i], 10000 / (WORKERS * 2));

            
            for (int i = 0; i < handledCount.Length; i++)
                Console.Write("{0}={1},", i, handledCount[i]);
        }
    }
}
