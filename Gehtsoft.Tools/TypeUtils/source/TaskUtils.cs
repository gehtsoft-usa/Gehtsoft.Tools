using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Gehtsoft.Tools.TypeUtils
{
    public static class TaskExtension
    {
        public static T WaitAndReturn<T>(this Task<T> task)
        {
            task.Wait();
            return task.Result;
        }

        public static void RunCoreSync(this Task task) => task.ConfigureAwait(false).GetAwaiter().GetResult();

        public static T RunCoreSync<T>(this Task<T> task) => task.ConfigureAwait(false).GetAwaiter().GetResult();

    }
}
