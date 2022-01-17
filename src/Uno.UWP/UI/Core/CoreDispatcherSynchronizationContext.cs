using System;
using System.Threading;
using Uno.Disposables;

namespace Windows.UI.Core
{
	/// <summary>
	/// Provides a CoreDispatched Synchronization context, to allow for async methods to keep the dispatcher priority.
	/// </summary>
	internal sealed class CoreDispatcherSynchronizationContext : SynchronizationContext
    {
     	private readonly Action<SendOrPostCallback, object> _postAction;
		private readonly Action<SendOrPostCallback, object> _sendAction;

		public CoreDispatcherSynchronizationContext(CoreDispatcher dispatcher, CoreDispatcherPriority priority)
        {
			_postAction = (d, state) =>
				{
					dispatcher.RunAsync(priority, () => d(state));
				};
			_sendAction = (d, state) =>
				 {
					 if (dispatcher.HasThreadAccess)
					 {
						 d(state);
					 }
					 else
					 {
						 dispatcher
							 .RunAsync(priority, () => d(state))
							 .AsTask(CancellationToken.None)
							 .Wait();
					 }
				 };
		}

		public CoreDispatcherSynchronizationContext(SynchronizationContext inerContex)
		{
			_postAction = (d, state) => inerContex.Post(d, state);
			_sendAction = (d, state) => inerContex.Send(d, state);
		}

		public override void Post(SendOrPostCallback d, object state) =>
			_postAction(d, state);


		public override void Send(SendOrPostCallback d, object state) =>
			_sendAction(d, state);

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
