#nullable enable

using System;

namespace Uno.Helpers
{
	/// <summary>
	/// Creates a wrapper around an event, which needs to be synchronized
	/// and needs to run an action when first subscriber is added and when
	/// last subscriber is removed. The operations executed when first subscriber
	/// is added and last subscriber is removed will execute within
	/// a synchronization lock, so please avoid blocking within the actions.
	/// </summary>
	internal class StartStopEventWrapper<TDelegate>
		where TDelegate : Delegate
	{
		private readonly object _syncLock;
		private readonly Action _onFirst;
		private readonly Action _onLast;

		/// <summary>
		/// Creates a new instance of start-stop event wrapper.
		/// </summary>
		/// <param name="onFirst">Action to run when first subscriber is added.
		/// This will run within a synchronization lock so it should not involve blocking operations.</param>
		/// <param name="onLast">Action to run when last subscriber is removed.
		/// This will run within a synchronization lock so it should not involve blocking operations.</param>
		/// <param name="sharedLock">Optional shared object to lock on (when multiple events
		/// rely on the same native platform operation.</param>
		public StartStopEventWrapper(
			Action onFirst,
			Action onLast,
			object? sharedLock = null)
		{
			_onFirst = onFirst;
			_onLast = onLast;
			_syncLock = sharedLock ?? new object();
		}

		public TDelegate? Event { get; private set; } = null;

		public void AddHandler(TDelegate handler)
		{
			lock (_syncLock)
			{
				var firstSubscriber = Event == null;
				Event = (TDelegate)Delegate.Combine(Event, handler);
				if (firstSubscriber)
				{
					_onFirst();
				}
			}
		}

		public void RemoveHandler(TDelegate handler)
		{
			lock (_syncLock)
			{
				if (Event != null)
				{
					Event = (TDelegate?)Delegate.Remove(Event, handler);
					if (Event == null)
					{
						_onLast();
					}
				}
			}
		}
	}
}
