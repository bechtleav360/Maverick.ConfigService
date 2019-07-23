using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Services
{
    /// <inheritdoc />
    public abstract class HostedServiceBase : IHostedService
    {
        private CancellationTokenSource _cts;
        private Task _executingTask;

        /// <summary>
        /// </summary>
        /// <param name="provider"></param>
        public HostedServiceBase(IServiceProvider provider)
        {
            Provider = provider;

            var factory = provider.GetService<ILoggerFactory>();

            if (factory == null)
                throw new ArgumentException("no instance of ILoggerFactory available in IServiceProvider");

            Logger = factory.CreateLogger(GetType());
        }

        /// <inheritdoc cref="ILogger" />
        protected ILogger Logger { get; }

        /// <inheritdoc cref="IServiceProvider" />
        protected IServiceProvider Provider { get; }

        /// <inheritdoc />
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _executingTask = ExecuteAsync(_cts.Token);

            return _executingTask.IsCompleted
                       ? _executingTask
                       : Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_executingTask == null)
                return;

            _cts.Cancel();

            await Task.WhenAny(_executingTask, Task.Delay(-1, cancellationToken));

            cancellationToken.ThrowIfCancellationRequested();
        }

        /// <summary>
        ///     Derived classes should override this and execute a long running method until
        ///     cancellation is requested
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected abstract Task ExecuteAsync(CancellationToken cancellationToken);
    }
}