using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Gehtsoft.Tools.Structures;
using Gehtsoft.Tools.TypeUtils;
using Xunit;

namespace Gehtsoft.Tools.UnitTest
{
    public class StructTest
    {
        [Fact]
        public void FibHeapTest()
        {
            FibonacciHeap<int, int> heap = new FibonacciHeap<int, int>();
            Dictionary<int, bool> found = new Dictionary<int, bool>();
            Random r = new Random();

            for (int i = 0; i < 1000; i++)
                heap.Add(r.Next(1, 10), i);

            for (int i = 0; i < 1000; i++)
                found[i] = false;

            foreach (FibonacciHeap<int, int>.Node node in heap)
                found[node.Value] = true;

            Assert.Equal(1000, heap.Count);

            Assert.True(found.Keys.All(v => found[v]));

            for (int i = 0; i < 1000; i++)
                found[i] = false;

            int previousMinimum = 0;
            while (heap.Minimum != null)
            {
                Assert.True(heap.Minimum.Key >= previousMinimum);
                Assert.False(found[heap.Minimum.Value]);
                found[heap.Minimum.Value] = true;
                heap.ExtractMin();
            }

            Assert.Empty(heap);
            Assert.True(found.Keys.All(v => found[v]));

            for (int i = 0; i < 1000; i++)
                heap.Add(r.Next(1, 10), i);
            for (int i = 0; i < 1000; i++)
                found[i] = false;
            for (int i = 0; i < 1000; i++)
                found[i] = false;

            previousMinimum = 0;
            while (heap.Count > 500)
            {
                Assert.True(heap.Minimum.Key >= previousMinimum);
                Assert.False(found[heap.Minimum.Value]);
                found[heap.Minimum.Value] = true;
                heap.ExtractMin();
            }

            foreach (FibonacciHeap<int, int>.Node node in heap)
                found[node.Value] = true;
            Assert.True(found.Keys.All(v => found[v]));

            heap.Clear();

            heap.Add(1, 0);
            FibonacciHeap<int, int>.Node n = heap.Add(1, 1);
            heap.Add(1, 2);

            heap.Remove(n);
            bool[] found1 = new bool[3];
            while (heap.Minimum != null)
            {
                found1[heap.Minimum.Value] = true;
                heap.ExtractMin();
            }

            Assert.True(found1[0]);
            Assert.False(found1[1]);
            Assert.True(found1[2]);
        }

        [Fact]
        public void FixedCircularBufferTest()
        {
            FixedCircularBuffer<int> buffer;

            buffer = new FixedCircularBuffer<int>(10);

            for (int i = 0; i < 10; i++)
                buffer.Add(i);

            for (int i = 0; i < 10; i++)
                Assert.Equal(i, buffer[i]);

            int k = 0;
            foreach (int i in buffer)
                Assert.Equal(k++, i);

            Assert.Throws<InvalidOperationException>(() => buffer.Add(10));

            for (int i = 0; i < 5; i++)
                buffer.RemoveAt(0);

            for (int i = 0; i < 5; i++)
                buffer.Add(i);

            for (int i = 0; i < 5; i++)
                Assert.Equal(i + 5, buffer[i]);

            for (int i = 5; i < 10; i++)
                Assert.Equal(i - 5, buffer[i]);

            buffer.RemoveAt(4);
            for (int i = 0; i < 4; i++)
                buffer.RemoveAt(0);

            for (int i = 0; i < 5; i++)
                Assert.Equal(i, buffer[i]);

            buffer.Clear();

            buffer.Add(3);
            buffer.Add(5);
            buffer.Insert(1, 4);
            buffer.Insert(0, 1);
            buffer.Insert(1, 2);
            Assert.Equal(1, buffer[0]);
            Assert.Equal(2, buffer[1]);
            Assert.Equal(3, buffer[2]);
            Assert.Equal(4, buffer[3]);
            Assert.Equal(5, buffer[4]);
        }

        [Fact]
        public void CacheTest()
        {
            {
                Cache<string> cache = null;
                try
                {
                    cache = new CacheWithActionFactory<string>(args =>
                    {
                        if (args == null)
                            return null;
                        if (args.Length == 0)
                            return "";
                        StringBuilder builder = new StringBuilder();
                        foreach (object v in args)
                            builder.Append(v?.ToString() ?? "");
                        return builder.ToString();
                    }, TimeSpan.FromMilliseconds(250));
                    string s1, s2;

                    s1 = cache.Get();
                    Assert.Equal("", s1);
                    s2 = cache.Get("a", 1);
                    Assert.Equal("a1", s2);

                    Assert.Same(s2, cache.Get("a", 1));
                    Thread.Sleep(300);
                    Assert.NotSame(s2, cache.Get("a", 1));
                    Thread.Sleep(300);
                    Assert.NotNull(cache.GetEvenIfExpired("a", 1));

                    cache.EnableAutoCleanup(TimeSpan.FromMilliseconds(100));
                    Thread.Sleep(200);
                    Assert.Null(cache.GetEvenIfExpired("a", 1));
                    Assert.NotNull(cache.Get("a", 1));
                    Thread.Sleep(500);
                    Assert.Null(cache.GetEvenIfExpired("a", 1));
                }
                finally
                {
                    if (cache != null)
                    {
                        Assert.True(cache.IsCleanupAlive);
                        cache.Dispose();
                        Thread.Sleep(50);
                        Assert.False(cache.IsCleanupAlive);
                    }
                }
            }

            using (var cache = new Cache<string>(TimeSpan.FromSeconds(0.5)))
            {
                Assert.Null(cache[1]);
                cache[1] = "abc";
                Assert.NotNull(cache[1]);
                Thread.Sleep(550);
                Assert.Null(cache[1]);
            }
        }

        [Fact]
        public void TestCacheCleanup()
        {
            Cache<string> cache = new Cache<string>(TimeSpan.FromMilliseconds(250));
            cache.EnableAutoCleanup(TimeSpan.FromMilliseconds(50));

            cache.Set("a", "a");
            Assert.Equal("a", cache.Get("a"));
            Thread.Sleep(100);
            Assert.Equal("a", cache.Get("a"));
            Thread.Sleep(200);
            Assert.Equal("a", cache.Get("a"));
            Thread.Sleep(300);
            Assert.Null(cache.Get("a"));
        }

        [Fact]
        public async Task AsyncCacheTest()
        {
            AsyncCacheWithActionFactory<string> cache = new AsyncCacheWithActionFactory<string>((args, token) =>
            {
                if (args == null)
                    return null;
                if (args.Length == 0)
                    return Task.FromResult("");
                StringBuilder builder = new StringBuilder();
                foreach (object v in args)
                    builder.Append(v?.ToString() ?? "");
                return Task.FromResult(builder.ToString());
            }, TimeSpan.FromSeconds(0.5));

            string s1, s2;

            s1 = await cache.GetAsync();
            Assert.Equal("", s1);
            s2 = await cache.GetAsync("a", 1);
            Assert.Equal("a1", s2);

            Assert.Same(s2, cache.Get("a", 1));
            Thread.Sleep(550);
            Assert.NotSame(s2, cache.Get("a", 1));
            Thread.Sleep(550);
            Assert.NotNull(cache.GetEvenIfExpired("a", 1));

            cache.EnableAutoCleanup(new TimeSpan(0, 0, 1));
            Thread.Sleep(1100);
            Assert.Null(cache.GetEvenIfExpired("a", 1));
            Assert.NotNull(cache.Get("a", 1));
            Thread.Sleep(1100);
            Assert.Null(cache.GetEvenIfExpired("a", 1));
        }

        [Fact]
        public void FastScrollTest()
        {
            FastScrollList<int> fsl = new FastScrollList<int>(32_768);

            for (int i = 0; i < 32768; i++)
                fsl.AddToEnd(i * 2);

            Assert.True(fsl.IsFull);
            Assert.Equal(32768, fsl.Count);
            for (int i = 0; i < 32768; i++)
                Assert.Equal(i * 2, fsl[i]);

            for (int i = 0; i < 32768; i++)
            {
                fsl.RemoveFromTop(1);
                fsl.AddToEnd(i + 32000);
            }
            for (int i = 0; i < 32768; i++)
                Assert.Equal(i + 32000, fsl[i]);

            for (int i = 0; i < 32768; i++)
                fsl[i] = i;

            for (int i = 0; i < 32768; i++)
                Assert.Equal(i, fsl[i]);

            fsl.Clear();
            Assert.Empty(fsl);

            for (int i = 0; i < 32768; i++)
                fsl.AddToEnd(i);
            for (int i = 0; i < 32768; i++)
                Assert.Equal(i, fsl[i]);

            fsl.RemoveFromTop(1);
            fsl.RemoveFromTop(5999);

            Assert.Equal(32768 - 6000, fsl.Count);
            for (int i = 0; i < fsl.Count; i++)
                Assert.Equal(i + 6000, fsl[i]);

            int x = 0;
            foreach (int v in fsl)
            {
                Assert.Equal(x + 6000, v);
                x++;
            }
        }

        [Fact]
        public void FastScrollPerformanceTest()
        {
            Stopwatch stopwatch = new Stopwatch();
            List<int> list = new List<int>();
            FastScrollList<int> fsl = new FastScrollList<int>(65536);

            stopwatch.Reset();
            stopwatch.Start();

            for (int i = 0; i < 300_000; i++)
            {
                if (list.Count >= 65536)
                    list.RemoveAt(0);
                list.Add(i);
            }

            stopwatch.Stop();
            Console.WriteLine("List Fill {0}", stopwatch.ElapsedMilliseconds);

            Assert.Equal(65536, list.Count);

            stopwatch.Reset();
            stopwatch.Start();

            for (int i = 0; i < 10_000; i++)
            {
                int s = 0;
                for (int j = 0; j < list.Count; j++)
                    s += list[j];
                Assert.True(s != 5675345);
            }

            stopwatch.Stop();
            Console.WriteLine("List Scan {0}", stopwatch.ElapsedMilliseconds);

            stopwatch.Reset();
            stopwatch.Start();
            for (int i = 0; i < 10_000; i++)
            {
                int s = 0;
                for (int j = list.Count - 5000; j < list.Count; j++)
                    s += list[j];
                Assert.True(s != 5675345);
            }

            stopwatch.Stop();
            Console.WriteLine("List Scan Last 5000 {0}", stopwatch.ElapsedMilliseconds);

            stopwatch.Reset();
            stopwatch.Start();

            for (int i = 0; i < 300_000; i++)
            {
                if (fsl.Count >= 65536)
                    fsl.RemoveFromTop(1);
                fsl.AddToEnd(i);
            }
            stopwatch.Stop();
            Assert.Equal(65536, fsl.Count);
            Console.WriteLine("FSL Fill {0}", stopwatch.ElapsedMilliseconds);

            stopwatch.Reset();
            stopwatch.Start();

            for (int i = 0; i < 10_000; i++)
            {
                int s = 0;
                for (int j = 0; j < fsl.Count; j++)
                    s += fsl.GetAt(j);
                Assert.True(s != 5675345);
            }

            stopwatch.Stop();
            Console.WriteLine("FSL Scan {0}", stopwatch.ElapsedMilliseconds);

            stopwatch.Reset();
            stopwatch.Start();

            for (int i = 0; i < 10_000; i++)
            {
                int s = 0;
                for (int j = fsl.Count - 5000; j < fsl.Count; j++)
                    s += fsl[j];
                Assert.True(s != 5675345);
            }

            stopwatch.Stop();
            Console.WriteLine("FSL Scan 5000 {0}", stopwatch.ElapsedMilliseconds);

            stopwatch.Reset();
            stopwatch.Start();

            for (int i = 0; i < 10_000; i++)
            {
                int s = 0;
                foreach (int j in fsl)
                    s += j;

                Assert.True(s != 5675345);
            }

            stopwatch.Stop();
            Console.WriteLine("FSL Enum {0}", stopwatch.ElapsedMilliseconds);
        }

        [Fact]
        public void MutexSlimTest()
        {
            MutexSlim mutex = new MutexSlim();
            Assert.False(mutex.IsLocked);
            Assert.False(mutex.IsLockedByMe);

            Assert.True(mutex.Wait(TimeSpan.FromMilliseconds(0), TestContext.Current.CancellationToken));
            Assert.True(mutex.IsLocked);
            Assert.True(mutex.IsLockedByMe);

            Assert.True(mutex.Wait(TimeSpan.FromMilliseconds(0), TestContext.Current.CancellationToken));
            Assert.True(mutex.IsLocked);
            Assert.True(mutex.IsLockedByMe);

            mutex.Release();
            Assert.True(mutex.IsLocked);
            Assert.True(mutex.IsLockedByMe);

            mutex.Release();
            Assert.False(mutex.IsLockedByMe);
            Assert.False(mutex.IsLockedByMe);

            AutoResetEvent ev = new AutoResetEvent(false);
            Task.Run(() =>
            {
                mutex.Wait();
                ev.WaitOne();
                Thread.Sleep(50);
                mutex.Release();
            }, TestContext.Current.CancellationToken);

            Thread.Sleep(50);
            Assert.True(mutex.IsLocked);
            Assert.False(mutex.IsLockedByMe);

            Assert.False(mutex.Wait(TimeSpan.FromMilliseconds(0), TestContext.Current.CancellationToken));
            ev.Set();
            Assert.False(mutex.Wait(TimeSpan.FromMilliseconds(0), TestContext.Current.CancellationToken));
            Assert.True(mutex.Wait(TimeSpan.FromMilliseconds(500), TestContext.Current.CancellationToken));
            Assert.True(mutex.IsLocked);
            Assert.True(mutex.IsLockedByMe);

            mutex.Release();
        }

        [Fact]
        public void MutexSlimDictonaryTest()
        {
            using (MutexSlimDictionary<string> mutex = new MutexSlimDictionary<string>())
            {
                Assert.False(mutex.Get("m1").IsLocked);
                Assert.False(mutex.Get("m1").IsLockedByMe);

                Assert.True(mutex.Get("m1").Wait(TimeSpan.FromMilliseconds(0), TestContext.Current.CancellationToken));
                Assert.True(mutex.Get("m1").IsLocked);
                Assert.True(mutex.Get("m1").IsLockedByMe);
                Assert.False(mutex.Get("m2").IsLocked);
                Assert.False(mutex.Get("m2").IsLockedByMe);

                Assert.True(mutex.Get("m1").Wait(TimeSpan.FromMilliseconds(0), TestContext.Current.CancellationToken));
                Assert.True(mutex.Get("m1").IsLocked);
                Assert.True(mutex.Get("m1").IsLockedByMe);

                mutex.Get("m1").Release();
                Assert.True(mutex.Get("m1").IsLocked);
                Assert.True(mutex.Get("m1").IsLockedByMe);

                mutex.Get("m1").Release();
                Assert.False(mutex.Get("m1").IsLockedByMe);
                Assert.False(mutex.Get("m1").IsLockedByMe);

                AutoResetEvent ev = new AutoResetEvent(false);
                Task.Run(() =>
                {
                    mutex.Get("m1").Wait();
                    ev.WaitOne();
                    Thread.Sleep(50);
                    mutex.Get("m1").Release();
                }, TestContext.Current.CancellationToken);

                Thread.Sleep(50);
                Assert.True(mutex.Get("m1").IsLocked);
                Assert.False(mutex.Get("m1").IsLockedByMe);

                Assert.False(mutex.Get("m1").Wait(TimeSpan.FromMilliseconds(0), TestContext.Current.CancellationToken));
                ev.Set();
                Assert.False(mutex.Get("m1").Wait(TimeSpan.FromMilliseconds(0), TestContext.Current.CancellationToken));
                Assert.True(mutex.Get("m1").Wait(TimeSpan.FromMilliseconds(500), TestContext.Current.CancellationToken));
                Assert.True(mutex.Get("m1").IsLocked);
                Assert.True(mutex.Get("m1").IsLockedByMe);

                mutex.Get("m1").Release();
            }
        }
    }
}