using System;

namespace Uno.Helpers
{
	internal class StartStopEventWrapper<TDelegate>
		where TDelegate : Delegate
	{
		private readonly object _syncLock;
		private readonly Action _onFirst;
		private readonly Action _onLast;

		/// <summary>
		/// Creates a wrapper around an event, which needs to be synchronized
		/// and needs to run an action when first subscriber is added and when
		/// last subscriber is removed.
		/// </summary>
		/// <param name="onFirst">Action to run when first subscriber is added.</param>
		/// <param name="onLast">Action to run when last subscriber is removed.</param>
		/// <param name="sharedLock">Optional shared object to lock on (when multiple events
		/// rely on the same native platform operation.</param>
		public StartStopEventWrapper(
			Action onFirst,
			Action onLast,
			object sharedLock = null)
		{
			_onFirst = onFirst;
			_onLast = onLast;
			_syncLock = sharedLock ?? new object();
		}

		public TDelegate Event { get; private set; } = null;

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
					Event = (TDelegate)Delegate.Remove(Event, handler);
					if (Event == null)
					{
						_onLast();
					}
				}
			}
		}
	}
}
