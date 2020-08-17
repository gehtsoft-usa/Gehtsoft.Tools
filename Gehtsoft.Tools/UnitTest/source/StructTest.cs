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
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Gehtsoft.Tools.UnitTest
{
    [TestFixture]
    public class StructTest
    {
        [Test]
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

            Assert.AreEqual(1000, heap.Count);

            Assert.IsTrue(found.Keys.All(v => found[v]));

            for (int i = 0; i < 1000; i++)
                found[i] = false;

            int previousMinimum = 0;
            while (heap.Minimum != null)
            {
                Assert.IsTrue(heap.Minimum.Key >= previousMinimum);
                Assert.IsFalse(found[heap.Minimum.Value]);
                found[heap.Minimum.Value] = true;
                heap.ExtractMin();
            }

            Assert.AreEqual(0, heap.Count);
            Assert.IsTrue(found.Keys.All(v => found[v]));

            for (int i = 0; i < 1000; i++)
                heap.Add(r.Next(1, 10), i);
            for (int i = 0; i < 1000; i++)
                found[i] = false;
            for (int i = 0; i < 1000; i++)
                found[i] = false;

            previousMinimum = 0;
            while (heap.Count > 500)
            {
                Assert.IsTrue(heap.Minimum.Key >= previousMinimum);
                Assert.IsFalse(found[heap.Minimum.Value]);
                found[heap.Minimum.Value] = true;
                heap.ExtractMin();
            }

            foreach (FibonacciHeap<int, int>.Node node in heap)
                found[node.Value] = true;
            Assert.IsTrue(found.Keys.All(v => found[v]));

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

            Assert.IsTrue(found1[0]);
            Assert.IsFalse(found1[1]);
            Assert.IsTrue(found1[2]);
        }

        [Test]
        public void FixedCircularBufferTest()
        {
            FixedCircularBuffer<int> buffer;

            buffer = new FixedCircularBuffer<int>(10);

            for (int i = 0; i < 10; i++)
                buffer.Add(i);

            for (int i = 0; i < 10; i++)
                Assert.AreEqual(i, buffer[i]);

            int k = 0;
            foreach (int i in buffer)
                Assert.AreEqual(k++, i);

            Assert.Throws<InvalidOperationException>(() => buffer.Add(10));

            for (int i = 0; i < 5; i++)
                buffer.RemoveAt(0);

            for (int i = 0; i < 5; i++)
                buffer.Add(i);

            for (int i = 0; i < 5; i++)
                Assert.AreEqual(i + 5, buffer[i]);

            for (int i = 5; i < 10; i++)
                Assert.AreEqual(i - 5, buffer[i]);

            buffer.RemoveAt(4);
            for (int i = 0; i < 4; i++)
                buffer.RemoveAt(0);

            for (int i = 0; i < 5; i++)
                Assert.AreEqual(i, buffer[i]);

            buffer.Clear();

            buffer.Add(3);
            buffer.Add(5);
            buffer.Insert(1, 4);
            buffer.Insert(0, 1);
            buffer.Insert(1, 2);
            Assert.AreEqual(1, buffer[0]);
            Assert.AreEqual(2, buffer[1]);
            Assert.AreEqual(3, buffer[2]);
            Assert.AreEqual(4, buffer[3]);
            Assert.AreEqual(5, buffer[4]);
        }

        [Test]
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
                    Assert.AreEqual("", s1);
                    s2 = cache.Get("a", 1);
                    Assert.AreEqual("a1", s2);

                    Assert.AreSame(s2, cache.Get("a", 1));
                    Thread.Sleep(300);
                    Assert.AreNotSame(s2, cache.Get("a", 1));
                    Thread.Sleep(300);
                    Assert.IsNotNull(cache.GetEvenIfExpired("a", 1));

                    cache.EnableAutoCleanup(TimeSpan.FromMilliseconds(100));
                    Thread.Sleep(200);
                    Assert.IsNull(cache.GetEvenIfExpired("a", 1));
                    Assert.IsNotNull(cache.Get("a", 1));
                    Thread.Sleep(500);
                    Assert.IsNull(cache.GetEvenIfExpired("a", 1));
                }
                finally
                {
                    if (cache != null)
                    {
                        Assert.IsTrue(cache.IsCleanupAlive);
                        cache.Dispose();
                        Thread.Sleep(50);
                        Assert.IsFalse(cache.IsCleanupAlive);
                    }
                }
            }

            using (var cache = new Cache<string>(TimeSpan.FromSeconds(0.5)))
            {
                Assert.IsNull(cache[1]);
                cache[1] = "abc";
                Assert.IsNotNull(cache[1]);
                Thread.Sleep(550);
                Assert.IsNull(cache[1]);
            }
        }

        [Test]
        public void TestCacheCleanup()
        {
            Cache<string> cache = new Cache<string>(TimeSpan.FromMilliseconds(250));
            cache.EnableAutoCleanup(TimeSpan.FromMilliseconds(50));

            cache.Set("a", "a");
            Assert.AreEqual("a", cache.Get("a"));
            Thread.Sleep(100);
            Assert.AreEqual("a", cache.Get("a"));
            Thread.Sleep(200);
            Assert.AreEqual("a", cache.Get("a"));
            Thread.Sleep(300);
            Assert.AreEqual(null, cache.Get("a"));
        }

        [Test]
        public void AsyncCacheTest()
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

            s1 = cache.GetAsync().Result;
            Assert.AreEqual("", s1);
            s2 = cache.GetAsync("a", 1).Result;
            Assert.AreEqual("a1", s2);

            Assert.AreSame(s2, cache.Get("a", 1));
            Thread.Sleep(550);
            Assert.AreNotSame(s2, cache.Get("a", 1));
            Thread.Sleep(550);
            Assert.IsNotNull(cache.GetEvenIfExpired("a", 1));

            cache.EnableAutoCleanup(new TimeSpan(0, 0, 1));
            Thread.Sleep(1100);
            Assert.IsNull(cache.GetEvenIfExpired("a", 1));
            Assert.IsNotNull(cache.Get("a", 1));
            Thread.Sleep(1100);
            Assert.IsNull(cache.GetEvenIfExpired("a", 1));
        }

        [Test]
        public void FastScrollTest()
        {
            FastScrollList<int> fsl = new FastScrollList<int>(32_768);

            for (int i = 0; i < 32768; i++)
                fsl.AddToEnd(i * 2);

            Assert.IsTrue(fsl.IsFull);
            Assert.AreEqual(32768, fsl.Count);
            for (int i = 0; i < 32768; i++)
                Assert.AreEqual(i * 2, fsl[i]);

            for (int i = 0; i < 32768; i++)
            {
                fsl.RemoveFromTop(1);
                fsl.AddToEnd(i + 32000);
            }
            for (int i = 0; i < 32768; i++)
                Assert.AreEqual(i + 32000, fsl[i]);

            for (int i = 0; i < 32768; i++)
                fsl[i] = i;

            for (int i = 0; i < 32768; i++)
                Assert.AreEqual(i, fsl[i]);

            fsl.Clear();
            Assert.AreEqual(0, fsl.Count);

            for (int i = 0; i < 32768; i++)
                fsl.AddToEnd(i);
            for (int i = 0; i < 32768; i++)
                Assert.AreEqual(i, fsl[i]);

            fsl.RemoveFromTop(1);
            fsl.RemoveFromTop(5999);

            Assert.AreEqual(32768 - 6000, fsl.Count);
            for (int i = 0; i < fsl.Count; i++)
                Assert.AreEqual(i + 6000, fsl[i]);

            int x = 0;
            foreach (int v in fsl)
            {
                Assert.AreEqual(x + 6000, v);
                x++;
            }
        }

        [Test]
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

            Assert.AreEqual(65536, list.Count);

            stopwatch.Reset();
            stopwatch.Start();

            for (int i = 0; i < 10_000; i++)
            {
                int s = 0;
                for (int j = 0; j < list.Count; j++)
                    s += list[j];
                Assert.IsTrue(s != 5675345);
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
                Assert.IsTrue(s != 5675345);
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
            Assert.AreEqual(65536, fsl.Count);
            Console.WriteLine("FSL Fill {0}", stopwatch.ElapsedMilliseconds);

            stopwatch.Reset();
            stopwatch.Start();

            for (int i = 0; i < 10_000; i++)
            {
                int s = 0;
                for (int j = 0; j < fsl.Count; j++)
                    s += fsl.GetAt(j);
                Assert.IsTrue(s != 5675345);
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
                Assert.IsTrue(s != 5675345);
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

                Assert.IsTrue(s != 5675345);
            }

            stopwatch.Stop();
            Console.WriteLine("FSL Enum {0}", stopwatch.ElapsedMilliseconds);
        }

        [Test]
        public void MutexSlimTest()
        {
            MutexSlim mutex = new MutexSlim();
            Assert.IsFalse(mutex.IsLocked);
            Assert.IsFalse(mutex.IsLockedByMe);

            Assert.IsTrue(mutex.Wait(TimeSpan.FromMilliseconds(0)));
            Assert.IsTrue(mutex.IsLocked);
            Assert.IsTrue(mutex.IsLockedByMe);

            Assert.IsTrue(mutex.Wait(TimeSpan.FromMilliseconds(0)));
            Assert.IsTrue(mutex.IsLocked);
            Assert.IsTrue(mutex.IsLockedByMe);

            mutex.Release();
            Assert.IsTrue(mutex.IsLocked);
            Assert.IsTrue(mutex.IsLockedByMe);

            mutex.Release();
            Assert.IsFalse(mutex.IsLockedByMe);
            Assert.IsFalse(mutex.IsLockedByMe);

            AutoResetEvent ev = new AutoResetEvent(false);
            Task.Run(() =>
            {
                mutex.Wait();
                ev.WaitOne();
                Thread.Sleep(50);
                mutex.Release();
            });

            Thread.Sleep(50);
            Assert.IsTrue(mutex.IsLocked);
            Assert.IsFalse(mutex.IsLockedByMe);

            Assert.IsFalse(mutex.Wait(TimeSpan.FromMilliseconds(0)));
            ev.Set();
            Assert.IsFalse(mutex.Wait(TimeSpan.FromMilliseconds(0)));
            Assert.IsTrue(mutex.Wait(TimeSpan.FromMilliseconds(500)));
            Assert.IsTrue(mutex.IsLocked);
            Assert.IsTrue(mutex.IsLockedByMe);

            mutex.Release();
        }

        [Test]
        public void MutexSlimDictonaryTest()
        {
            using (MutexSlimDictionary<string> mutex = new MutexSlimDictionary<string>())
            {
                Assert.IsFalse(mutex.Get("m1").IsLocked);
                Assert.IsFalse(mutex.Get("m1").IsLockedByMe);

                Assert.IsTrue(mutex.Get("m1").Wait(TimeSpan.FromMilliseconds(0)));
                Assert.IsTrue(mutex.Get("m1").IsLocked);
                Assert.IsTrue(mutex.Get("m1").IsLockedByMe);
                Assert.IsFalse(mutex.Get("m2").IsLocked);
                Assert.IsFalse(mutex.Get("m2").IsLockedByMe);

                Assert.IsTrue(mutex.Get("m1").Wait(TimeSpan.FromMilliseconds(0)));
                Assert.IsTrue(mutex.Get("m1").IsLocked);
                Assert.IsTrue(mutex.Get("m1").IsLockedByMe);

                mutex.Get("m1").Release();
                Assert.IsTrue(mutex.Get("m1").IsLocked);
                Assert.IsTrue(mutex.Get("m1").IsLockedByMe);

                mutex.Get("m1").Release();
                Assert.IsFalse(mutex.Get("m1").IsLockedByMe);
                Assert.IsFalse(mutex.Get("m1").IsLockedByMe);

                AutoResetEvent ev = new AutoResetEvent(false);
                Task.Run(() =>
                {
                    mutex.Get("m1").Wait();
                    ev.WaitOne();
                    Thread.Sleep(50);
                    mutex.Get("m1").Release();
                });

                Thread.Sleep(50);
                Assert.IsTrue(mutex.Get("m1").IsLocked);
                Assert.IsFalse(mutex.Get("m1").IsLockedByMe);

                Assert.IsFalse(mutex.Get("m1").Wait(TimeSpan.FromMilliseconds(0)));
                ev.Set();
                Assert.IsFalse(mutex.Get("m1").Wait(TimeSpan.FromMilliseconds(0)));
                Assert.IsTrue(mutex.Get("m1").Wait(TimeSpan.FromMilliseconds(500)));
                Assert.IsTrue(mutex.Get("m1").IsLocked);
                Assert.IsTrue(mutex.Get("m1").IsLockedByMe);

                mutex.Get("m1").Release();
            }
        }
    }
}