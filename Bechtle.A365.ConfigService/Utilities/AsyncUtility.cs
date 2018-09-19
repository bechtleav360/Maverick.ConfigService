using System;
using System.Threading;
using System.Threading.Tasks;

namespace Bechtle.A365.ConfigService.Utilities
{
    /// <summary>
    ///     utilities to run tasks synchronously
    /// </summary>
    public static class AsyncUtility
    {
        private static readonly TaskFactory TaskFactory = new TaskFactory(CancellationToken.None,
                                                                          TaskCreationOptions.None,
                                                                          TaskContinuationOptions.None,
                                                                          TaskScheduler.Default);

        /// <summary>
        ///     run the given task synchronously
        /// </summary>
        /// <param name="func"></param>
        /// <typeparam name="TResult"></typeparam>
        /// <returns></returns>
        public static TResult RunSync<TResult>(this Func<Task<TResult>> func)
            => TaskFactory
               .StartNew(func)
               .Unwrap()
               .GetAwaiter()
               .GetResult();

        /// <summary>
        ///     run the given task synchronously
        /// </summary>
        /// <param name="func"></param>
        public static void RunSync(this Func<Task> func)
            => TaskFactory
               .StartNew(func)
               .Unwrap()
               .GetAwaiter()
               .GetResult();

        /// <summary>
        ///     run the given task synchronously
        /// </summary>
        /// <param name="task"></param>
        /// <typeparam name="TResult"></typeparam>
        /// <returns></returns>
        public static TResult RunSync<TResult>(this Task<TResult> task)
            => TaskFactory
               .StartNew(() => task)
               .Unwrap()
               .GetAwaiter()
               .GetResult();

        /// <summary>
        ///     run the given task synchronously
        /// </summary>
        /// <param name="task"></param>
        public static void RunSync(this Task task)
            => TaskFactory
               .StartNew(() => task)
               .Unwrap()
               .GetAwaiter()
               .GetResult();
    }
}