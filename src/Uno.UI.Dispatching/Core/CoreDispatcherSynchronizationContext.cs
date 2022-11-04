using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Uno.Disposables;

namespace Uno.UI.Dispatching
{
    /// <summary>
    /// Provides a CoreDispatched Synchronization context, to allow for async methods to keep the dispatcher priority.
    /// </summary>
    internal sealed class CoreDispatcherSynchronizationContext : SynchronizationContext
    {
        private readonly CoreDispatcher _dispatcher;
        private readonly CoreDispatcherPriority _priority;

        public CoreDispatcherSynchronizationContext(CoreDispatcher dispatcher, CoreDispatcherPriority priority)
        {
            _priority = priority;
            _dispatcher = dispatcher;
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            _ = _dispatcher.RunAsync(_priority, () => d(state));
        }

        public override void Send(SendOrPostCallback d, object state)
        {
            if (_dispatcher.HasThreadAccess)
            {
                d(state);
            }
            else
            {
                _dispatcher
                    .RunAsync(_priority, () => d(state))
                    .AsTask(CancellationToken.None)
                    .Wait();
            }
        }

        /// <summary>
        /// Creates a scoped assignment of <see cref="SynchronizationContext.Current"/>.
        /// </summary>
        /// <returns></returns>
        public IDisposable Apply()
        {
            var current = SynchronizationContext.Current;

            SetSynchronizationContext(this);

            return Disposable.Create(() => SetSynchronizationContext(current));
        }
    }
}
