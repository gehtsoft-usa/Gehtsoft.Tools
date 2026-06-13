using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Gehtsoft.Tools.Structures.Channels;
using Xunit;

namespace Gehtsoft.Tools.UnitTest
{
    public class TestChannel
    {
        [Fact]
        public async Task TestChannel1()
        {
            Channel<int?> channel = new Channel<int?>();
            Assert.False(channel.IsClosed);
            Assert.True(channel.IsEmpty);
            Assert.Null(channel.Peek());
            for (int i = 0; i < 1000; i++)
                Assert.True(channel.Post(i));
            Assert.False(channel.IsClosed);
            Assert.False(channel.IsEmpty);
            Assert.Equal(1000, channel.Count);
            Assert.Equal(0, channel.Peek());
            Assert.Equal(1000, channel.Count);

            for (int i = 0; i < 1000; i++)
            {
                Assert.Equal(i, channel.Peek());
                Assert.Equal(i, channel.Receive(TestContext.Current.CancellationToken));
                Assert.Equal(1000 - i - 1, channel.Count);
            }

            Task<int?> receive = channel.ReceiveAsync(TestContext.Current.CancellationToken);
            Assert.False(receive.IsCompleted);
            Thread.Sleep(10);
            channel.Post(123);
            Thread.Sleep(10);
            Assert.True(receive.IsCompleted);
            Assert.Equal(123, await receive);


            channel.Close();
            Assert.Throws<ChannelIsClosedException>(() => channel.Post(1));
            while (!channel.IsEmpty)
                channel.Receive(TestContext.Current.CancellationToken);
            Assert.Throws<ChannelIsClosedException>(() => channel.Receive(TestContext.Current.CancellationToken));
        }

        [Fact]
        public void TestChannel2()
        {
            Channel<int?> channel = new Channel<int?>(5);
            for (int i = 0; i < 5; i++)
            {
                Assert.True(channel.Post(i));
            }
            Assert.Equal(5, channel.Count);
            Assert.False(channel.Post(6));
            Assert.Equal(5, channel.Count);
            Task sender = channel.SendAsync(7, TestContext.Current.CancellationToken);
            Thread.Sleep(10);
            Assert.Equal(5, channel.Count);
            Assert.False(sender.IsCompleted);
            bool got7 = false;
            while (!channel.IsEmpty)
            {
                got7 = got7 | (channel.Receive(TestContext.Current.CancellationToken) == 7);
                Thread.Sleep(10);
            }
            Thread.Sleep(10);
            Assert.True(sender.IsCompleted);

            Task filler = Task.Run(() => {
                for (int i = 0; i < 10; i++)
                    channel.Send(i);
            }, TestContext.Current.CancellationToken);

            Thread.Sleep(10);
            Assert.Equal(5, channel.Count);
            Assert.False(filler.IsCompleted);
            while (!channel.IsEmpty)
            {
               Thread.Sleep(10);
                if (channel.Receive(TestContext.Current.CancellationToken) == 9)
                    break;
            }
            Thread.Sleep(10);
            Assert.True(filler.IsCompleted);
        }

        [Fact]
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
                    int v = channel.Receive(TestContext.Current.CancellationToken);
                    Assert.Equal(v0, v);
                    check[v] = true;
                }

                Assert.Equal(1000, cc);
                foreach (int k in check.Keys)
                {
                    Assert.True(check[k]);
                }
            }



        }

        [Fact]
        public async Task SelectorTest()
        {
            Channel<int?> channel1 = new Channel<int?>();
            Channel<string> channel2 = new Channel<string>();
            ChannelSelector selector = new ChannelSelector(channel1, channel2);

            for (int i = 0; i < 50; i++)
            {
                int i1 = i;
                _ = Task.Run(() =>
                {
                    channel1.Post(i1);
                    channel2.Post("text" + i1);
                }, TestContext.Current.CancellationToken);

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
                Assert.True(receiver.IsCompleted);
                Assert.True(await receiver);
                while (!receiver.IsCompleted)
                {
                    Console.WriteLine("Cancelling...");
                    cancellationTokenSource.Cancel();
                }
            }
        }

        [Fact]
        public async Task MTChannelTest()
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
                }, TestContext.Current.CancellationToken);
            }

            Task sender = Task.Run(() => {
                for (int i = 0; i < 10000; i++)
                    channel.Send(i);
                Thread.Sleep(2000);
                channel.Close();
            }, TestContext.Current.CancellationToken);

            await Task.WhenAll(handlers);
            Assert.Equal(10000, handled.Count);
            int[] handledCount = new int[WORKERS];
            for (int i = 0; i < 10000; i++)
            {
                Assert.True(handled.ContainsKey(i));
                Assert.True(handled[i] >= 0);
                Assert.True(handled[i] <= WORKERS);
                handledCount[handled[i]]++;

            }

            for (int i = 0; i < WORKERS; i++)
                Assert.True(handledCount[i] > 10000 / (WORKERS * 2));

            
            for (int i = 0; i < handledCount.Length; i++)
                Console.Write("{0}={1},", i, handledCount[i]);
        }
    }
}
