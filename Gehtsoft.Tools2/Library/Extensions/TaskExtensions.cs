using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Gehtsoft.Tools2.Extensions
{
    /// <summary>
    /// Task extensions
    /// </summary>
    public static class TaskExtension
    {
        /// <summary>
        /// Wait for task finished and return the value
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="task"></param>
        /// <returns></returns>
        public static T WaitAndReturn<T>(this Task<T> task)
        {
            task.Wait();
            return task.Result;
        }

        /// <summary>
        /// Run async task synchronously
        /// </summary>
        /// <param name="task"></param>
        public static void RunCoreSync(this Task task) => task.ConfigureAwait(false).GetAwaiter().GetResult();

        /// <summary>
        /// Run async task synchronously and returna  value
        /// </summary>
        /// <param name="task"></param>
        public static T RunCoreSync<T>(this Task<T> task) => task.ConfigureAwait(false).GetAwaiter().GetResult();

    }
}
