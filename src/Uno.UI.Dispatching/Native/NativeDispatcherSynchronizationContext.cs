using System.Threading;

namespace Uno.UI.Dispatching
{
	/// <summary>
	/// Provides a CoreDispatched Synchronization context, to allow for async methods to keep the dispatcher priority.
	/// </summary>
	internal sealed class NativeDispatcherSynchronizationContext : SynchronizationContext
	{
		private readonly NativeDispatcher _dispatcher;
		private readonly NativeDispatcherPriority _priority;

		public NativeDispatcherSynchronizationContext(NativeDispatcher dispatcher, NativeDispatcherPriority priority)
		{
			_dispatcher = dispatcher;
			_priority = priority;
		}

		public override void Post(SendOrPostCallback d, object state)
		{
			_dispatcher.Enqueue(() => d(state), _priority);
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
					.EnqueueAsync(() => d(state), _priority)
					.Wait();
			}
		}

		/// <summary>
		/// Creates a scoped assignment of <see cref="SynchronizationContext.Current"/>.
		/// </summary>
		public Scope Apply()
		{
			var previous = SynchronizationContext.Current;

			SetSynchronizationContext(this);

			return new Scope(previous);
		}

		internal readonly ref struct Scope
		{
			private readonly SynchronizationContext _previous;

			public Scope(SynchronizationContext previous)
			{
				_previous = previous;
			}

			public void Dispose() => SetSynchronizationContext(_previous);
		}
	}
}
