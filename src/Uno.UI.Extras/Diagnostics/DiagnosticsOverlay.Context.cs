#nullable enable
#if WINUI || HAS_UNO_WINUI
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using Microsoft.UI.Dispatching;
using Uno.Foundation.Logging;

namespace Uno.Diagnostics.UI;

public sealed partial class DiagnosticsOverlay
{
	private sealed record ViewContext : IDiagnosticViewContext, IDisposable
	{
		private Queue<Action> _pending = new();
		private Queue<Action> _pending2 = new();
		private List<Action>? _recurrents;

		private readonly DiagnosticsOverlay _owner;
		private readonly DispatcherQueue _dispatcher;
		private DispatcherQueueTimer? _timer;
		private int _updatesScheduled;

		public static ViewContext? TryCreate(DiagnosticsOverlay owner)
		{
			if (owner._root.Content?.DispatcherQueue is { } dispatcher
				&& owner._context?._dispatcher != dispatcher)
			{
				return new ViewContext(owner, dispatcher);
			}

			return null;
		}

		private ViewContext(DiagnosticsOverlay owner, DispatcherQueue dispatcher)
		{
			_owner = owner;
			_dispatcher = dispatcher;
		}

		public void Schedule(Action action)
		{
			lock (_pending)
			{
				_pending.Enqueue(action);
			}

			if (Interlocked.Increment(ref _updatesScheduled) is 1)
			{
				_dispatcher.TryEnqueue(DispatcherQueuePriority.Low, DoUpdates);
			}
		}

		public void ScheduleRecurrent(Action action)
		{
			Interlocked.Increment(ref _updatesScheduled);

			if (_recurrents is null)
			{
				Interlocked.CompareExchange(ref _recurrents, new List<Action>(), null);
			}

			lock (_recurrents)
			{
				_recurrents.Add(action);
				if (_recurrents.Count == 1)
				{
					if (_timer is null)
					{
						_timer = _dispatcher.CreateTimer();
						_timer.Interval = TimeSpan.FromMilliseconds(1000);
						_timer.Tick += (snd, e) => DoUpdates();
					}

					_timer?.Start();
				}
			}
		}

		public void AbortRecurrent(Action action)
		{
			if (_recurrents is null)
			{
				return;
			}

			lock (_recurrents)
			{
				if (_recurrents.Remove(action))
				{
					if (_recurrents.Count == 0)
					{
						_timer?.Stop();
					}

					if (Interlocked.Decrement(ref _updatesScheduled) > 0)
					{
						_dispatcher.TryEnqueue(DispatcherQueuePriority.Low, DoUpdates);
					}
				}
			}
		}

		/// <inheritdoc />
		public void Notify(DiagnosticViewNotification notif)
			=> _owner.Notify(notif, this);

		private void DoUpdates()
		{
			DoPending();
			DoRecurrent();
		}

		private void DoPending()
		{
			Queue<Action> pending;
			lock (_pending)
			{
				pending = _pending;
				var pendingCount = pending.Count;
				if (pendingCount is 0)
				{
					return;
				}

				// Swap collections to avoid locking the first one for too long.
				var pending2 = _pending2;
				_pending = pending2;
				_pending2 = pending;

				// We need to decrement the pending count before releasing the lock so if someone request another one (pushed now in the pending2)
				// it will still be able to schedule an update if needed (i.e. if _updatesScheduled is 0).
				Interlocked.Add(ref _updatesScheduled, -pendingCount);
			}

			foreach (var update in pending)
			{
				try
				{
					update();
				}
				catch (Exception e)
				{
					this.Log().Error("Failed to execute update.", e);
				}
			}

			pending.Clear();
		}

		private void DoRecurrent()
		{
			if (_recurrents is { Count: > 0 } recurrent)
			{
				lock (recurrent)
				{
					foreach (var update in recurrent)
					{
						try
						{
							update();
						}
						catch (Exception e)
						{
							this.Log().Error("Failed to execute update.", e);
						}
					}
				}
			}
		}

		/// <inheritdoc />
		public void Dispose()
		{
			_updatesScheduled = -4096;
			_timer?.Stop();
		}
	}
}
#endif
