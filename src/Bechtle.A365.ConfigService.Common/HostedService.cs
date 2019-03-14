using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Bechtle.A365.ConfigService.Common
{
    public abstract class HostedService : IHostedService
    {
        // by David Fowler: https://gist.github.com/davidfowl/a7dd5064d9dcf35b6eae1a7953d615e3
        private CancellationTokenSource _cancellationTokenSource;
        private Task _executingTask;
        private readonly IServiceProvider _serviceProvider;

        /// <inheritdoc />
        protected HostedService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <inheritdoc />
        public Task StartAsync(CancellationToken cancellationToken)
        {
            // create a linked token so we can trigger cancellation outside of this token's cancellation
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // store the task we're executing
            _executingTask = ExecuteAsync(_cancellationTokenSource.Token);

            // if the task is completed then return it, otherwise it's running
            return _executingTask.IsCompleted ? _executingTask : Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            // Stop called without start
            if (_executingTask is null)
                return;

            // signal cancellation to the executing method
            _cancellationTokenSource.Cancel();

            // wait until the task completed or the stop token triggers
            await Task.WhenAny(_executingTask, Task.Delay(-1, cancellationToken));

            // throw if cancellation triggered
            cancellationToken.ThrowIfCancellationRequested();
        }

        /// <summary>
        ///     Derived classes should override this and execute a
        ///     long running method until cancellation is requested
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected abstract Task ExecuteAsync(CancellationToken cancellationToken);

        protected IServiceScope GetNewScope() => _serviceProvider.CreateScope();
    }
}