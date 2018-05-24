using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Uno.Disposables;

namespace Windows.UI.Core
{
    /// <summary>
    /// Provides a CoreDispatched Synchronization context, to allow for async methods to keep the dispatcher priority.
    /// </summary>
    internal class CoreDispatcherSynchronizationContext : SynchronizationContext
    {
        private CoreDispatcher _dispatcher;
        private CoreDispatcherPriority _priority;

        public CoreDispatcherSynchronizationContext(CoreDispatcher dispatcher, CoreDispatcherPriority priority)
        {
            _priority = priority;
            _dispatcher = dispatcher;
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            _dispatcher.RunAsync(_priority, () => d(state));
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
        /// <param name="dispatcher"></param>
        /// <param name="priority"></param>
        /// <returns></returns>
        public static IDisposable Apply(CoreDispatcher dispatcher, CoreDispatcherPriority priority)
        {
            var current = SynchronizationContext.Current;

            SetSynchronizationContext(new CoreDispatcherSynchronizationContext(dispatcher, priority));

            return Disposable.Create(() => SetSynchronizationContext(current));
        }
    }
}
