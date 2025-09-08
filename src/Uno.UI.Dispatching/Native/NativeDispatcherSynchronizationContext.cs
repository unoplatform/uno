using System.Threading;

namespace Uno.UI.Dispatching
{
	/// <summary>
	/// A synchronization context that always schedules on the Normal priority.
	/// </summary>
	internal sealed class NativeDispatcherSynchronizationContext : SynchronizationContext
	{
		private readonly NativeDispatcher _dispatcher;
		private const NativeDispatcherPriority Priority = NativeDispatcherPriority.Normal;

		public NativeDispatcherSynchronizationContext(NativeDispatcher dispatcher)
		{
			_dispatcher = dispatcher;
		}

		public override void Post(SendOrPostCallback d, object state)
		{
			_dispatcher.Enqueue(() => d(state), Priority);
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
					.EnqueueAsync(() => d(state), Priority)
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
