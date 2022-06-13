using FluentAssertions;
using Gehtsoft.Tools2.Structure;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;


namespace Gehtsoft.Tools2.UnitTest
{
    public class MutexSlimTest
    {
        private readonly ITestOutputHelper mTestOutputHelper;

        public MutexSlimTest(ITestOutputHelper testOutputHelper)
        {
            mTestOutputHelper = testOutputHelper;
        }

        [Fact]
        public void UnlockedOnCreation()
        {
            using var ms = new MutexSlim();
            ms.IsLocked.Should().BeFalse();
        }

        [Fact]
        public void LockFromNew()
        {
            using var ms = new MutexSlim();
            ms.Wait(0).Should().BeTrue();
            ms.IsLocked.Should().BeTrue();
            ms.IsLockedByMe.Should().BeTrue();
        }

        [Fact]
        public void CanEnterTheSameThreat()
        {
            using var ms = new MutexSlim();
            ms.Wait(0).Should().BeTrue();
            ms.Wait(0).Should().BeTrue();
            ms.Release();
            ms.IsLocked.Should().BeTrue();
            ms.Release();
            ms.IsLocked.Should().BeFalse();
        }

        [Fact]
        public void UnlockedAfterRelease()
        {
            using var ms = new MutexSlim();
            ms.Wait(0).Should().BeTrue();
            ms.Release();
            ms.IsLocked.Should().BeFalse();
        }

        [Fact]
        public void LockAfterUnlock()
        {
            using var ms = new MutexSlim();
            ms.Wait(0).Should().BeTrue();
            ms.IsLocked.Should().BeTrue();
            ms.Release();
            ms.Wait(0).Should().BeTrue();
            ms.IsLocked.Should().BeTrue();
        }

        [Fact]
        public void CantEnterFromAnotherThreatWhileLocked()
        {
            using var ms = new MutexSlim();
            bool? r = null;
            
            Thread t = new Thread(() => r = ms.Wait(0));
            using (var l = ms.Lock())
            {
                t.Start();
                t.Join();
            }

            r.Should().NotBeNull();
            r.Should().BeFalse();
        }

        [Fact]
        public void LockByLockObject()
        {
            using var ms = new MutexSlim();
            using (var l = ms.Lock())
                ms.IsLocked.Should().BeTrue();
            ms.IsLocked.Should().BeFalse();
        }

        [Fact]
        public async Task LockByLockObjectAsync()
        {
            using var ms = new MutexSlim();
            using (var l = await ms.LockAsync())
                ms.IsLocked.Should().BeTrue();
            ms.IsLocked.Should().BeFalse();
        }

        [Fact]
        public void CanWaitInAnotherThreat()
        {
            using var ms = new MutexSlim();
            bool? r = null;

            Thread t = new Thread(() => r = ms.Wait(10000));
            using (var l = ms.Lock())
                t.Start();

            t.Join();

            r.Should().NotBeNull();
            r.Should().BeTrue();
            ms.IsLocked.Should().BeTrue();
            ms.IsLockedByMe.Should().BeFalse();
        }

        [Fact]
        public void PerformanceTest()
        {
            var mutex = new Mutex();
            var mutexSlim = new MutexSlim();

            var sw = new Stopwatch();

            sw.Reset();
            sw.Start();
            for (int i = 0; i < 50000; i++)
            {
                mutex.WaitOne();
                mutex.ReleaseMutex();
            }
            sw.Stop();
            var mutexTime = sw.Elapsed;

            sw.Reset();
            sw.Start();
            for (int i = 0; i < 50000; i++)
            {
                mutexSlim.Wait();
                mutexSlim.Release();
            }
            sw.Stop();
            var mutexSlimTime = sw.Elapsed;

            mTestOutputHelper.WriteLine("Mutex {0} Slim {1}", mutexTime, mutexSlimTime);

            mutexSlimTime.Should().BeLessThan(mutexTime);
        }
    }
}
