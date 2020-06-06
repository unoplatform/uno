using System;

namespace Uno.Helpers
{
	internal class StartStopEventWrapper<TDelegate>
		where TDelegate : Delegate
	{
		private readonly object _syncLock = new object();
		private readonly Action _onFirst;
		private readonly Action _onLast;

		public StartStopEventWrapper(
			Action onFirst,
			Action onLast)
		{
			_onFirst = onFirst;
			_onLast = onLast;
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
