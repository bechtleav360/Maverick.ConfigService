using System;
using System.Collections.Generic;
using System.Linq;

namespace Bechtle.A365.ConfigService.Common.Utilities
{
    public class AggregateDisposable : IDisposable
    {
        private readonly List<IDisposable> _disposables;

        public AggregateDisposable(params IDisposable[] disposables)
        {
            _disposables = disposables.ToList();
        }

        public void Dispose()
        {
            foreach (var disposable in _disposables)
                disposable.Dispose();
        }
    }
}