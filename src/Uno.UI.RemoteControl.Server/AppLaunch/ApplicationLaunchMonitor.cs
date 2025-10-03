using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.UI.RemoteControl.Server.AppLaunch;

/// <summary>
/// In-memory monitor for application launch events and connection matching.
/// - Stores launch signals and matches incoming connections to prior launches.
/// - Uses a value-type composite key (no string concatenations) to minimize allocations. 
/// - Automatically handles timeouts using internal Task-based scheduling.
/// </summary>
public sealed class ApplicationLaunchMonitor : IDisposable
{
	/// <summary>
	/// Options that control the behavior of <see cref="ApplicationLaunchMonitor"/>.
	/// </summary>
	public class Options
	{
		/// <summary>
		/// Timeout after which a registered launch is considered expired. Defaults to 60 seconds.
		/// </summary>
		public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(60);

		/// <summary>
		/// Callback invoked when an application is registered.
		/// </summary>
		public Action<LaunchEvent>? OnRegistered { get; set; }

		/// <summary>
		/// Callback invoked when a registered application timed out without connecting.
		/// </summary>
		public Action<LaunchEvent>? OnTimeout { get; set; }

		/// <summary>
		/// Callback invoked when a registered application successfully connected.
		/// </summary>
		public Action<LaunchEvent>? OnConnected { get; set; }
	}

	/// <summary>
	/// Describes a single launch event recorded by the monitor.
	/// </summary>
	public sealed record LaunchEvent(Guid Mvid, string Platform, bool IsDebug, DateTimeOffset RegisteredAt);

	private readonly TimeProvider _timeProvider;
	private readonly Options _options;

	// Non-allocating composite key (avoids string creation per lookup)
	private readonly record struct Key(Guid Mvid, string Platform, bool IsDebug);

	private readonly ConcurrentDictionary<Key, ConcurrentQueue<LaunchEvent>> _pending = new();

	// Track timeout timers for each launch event
	private readonly ConcurrentDictionary<LaunchEvent, IDisposable> _timeoutTasks = new();

	/// <summary>
	/// Creates a new instance of <see cref="ApplicationLaunchMonitor"/>.
	/// </summary>
	/// <param name="timeProvider">Optional time provider used for internal timing and for tests. If null, the system time provider is used.</param>
	/// <param name="options">Optional configuration for the monitor. If null, default options are used.</param>
	public ApplicationLaunchMonitor(TimeProvider? timeProvider = null, Options? options = null)
	{
		_timeProvider = timeProvider ?? TimeProvider.System;
		_options = options ?? new Options();
	}

	/// <summary>
	/// Register that an application was launched.
	/// Automatically starts the timeout countdown from the current time provider value.
	/// Multiple registrations for the same key are kept and consumed in FIFO order.
	/// </summary>
	/// <param name="mvid">The MVID of the root/head application.</param>
	/// <param name="platform">The platform used to run the application. Cannot be null or empty.</param>
	/// <param name="isDebug">Whether the debugger is used.</param>
	public void RegisterLaunch(Guid mvid, string platform, bool isDebug)
	{
		ArgumentException.ThrowIfNullOrEmpty(platform);

		var now = _timeProvider.GetUtcNow();
		var ev = new LaunchEvent(mvid, platform, isDebug, now);
		var key = new Key(mvid, platform, isDebug);

		var queue = _pending.GetOrAdd(key, static _ => new ConcurrentQueue<LaunchEvent>());
		queue.Enqueue(ev);

		// Schedule automatic timeout
		ScheduleTimeout(ev, key);

		try
		{
			_options.OnRegistered?.Invoke(ev);
		}
		catch
		{
			// best-effort, swallow
		}
	}

	/// <summary>
	/// Schedules a timeout task for the given launch event.
	/// </summary>
	/// <param name="launchEvent">The launch event to schedule timeout for.</param>
	/// <param name="key">The key for the launch event.</param>
	private void ScheduleTimeout(LaunchEvent launchEvent, Key key)
	{
		// Create a one-shot timer using the injected TimeProvider. When it fires, it will invoke HandleTimeout.
		var timer = _timeProvider.CreateTimer(
			static s =>
			{
				var (self, ev, k) = ((ApplicationLaunchMonitor, LaunchEvent, Key))s!;

				// Remove and dispose the timer entry if still present
				if (self._timeoutTasks.TryRemove(ev, out var t))
				{
					try { t.Dispose(); } catch { }
				}

				try
				{
					self.HandleTimeout(ev, k);
				}
				catch
				{
					// swallow
				}
			},
			(this, launchEvent, key),
			_options.Timeout,
			Timeout.InfiniteTimeSpan);

		_timeoutTasks[launchEvent] = timer;
	}

	/// <summary>
	/// Handles timeout for a specific launch event.
	/// </summary>
	/// <param name="launchEvent">The launch event that timed out.</param>
	/// <param name="key">The key for the launch event.</param>
	private void HandleTimeout(LaunchEvent launchEvent, Key key)
	{
		// Remove the timed out event from the pending queue
		if (_pending.TryGetValue(key, out var queue))
		{
			var tempQueue = new List<LaunchEvent>();
			LaunchEvent? removedEvent = null;

			// Collect all items except the one that timed out
			while (queue.TryDequeue(out var ev))
			{
				if (ev.Equals(launchEvent) && removedEvent == null)
				{
					removedEvent = ev;
				}
				else
				{
					tempQueue.Add(ev);
				}
			}

			// Put back the non-timed-out events
			foreach (var ev in tempQueue)
			{
				queue.Enqueue(ev);
			}

			// If queue is empty, remove it
			if (queue.IsEmpty)
			{
				_pending.TryRemove(key, out _);
			}

			// Invoke timeout callback for the removed event
			if (removedEvent != null)
			{
				try
				{
					_options.OnTimeout?.Invoke(removedEvent);
				}
				catch
				{
					// swallow
				}
			}
		}
	}

	/// <summary>
	/// Reports an application successfully connecting back to development server.
	/// If a matching registered launch exists, it consumes the oldest registration and the OnConnected callback is invoked for it.
	/// Cancels the timeout task for the connected launch.
	/// </summary>
	/// <param name="mvid">The MVID of the root/head application being connected.</param>
	/// <param name="platform">The name of the platform from which the connection is reported. Cannot be null or empty.</param>
	/// <param name="isDebug">true if the connection is from a debug build; otherwise, false.</param>
	public bool ReportConnection(Guid mvid, string platform, bool isDebug)
	{
		ArgumentException.ThrowIfNullOrEmpty(platform);

		var key = new Key(mvid, platform, isDebug);
		if (_pending.TryGetValue(key, out var queue))
		{
			if (queue.TryDequeue(out var ev))
			{
				// Cancel / dispose the timeout timer for this event
				if (_timeoutTasks.TryRemove(ev, out var timeoutTimer))
				{
					try { timeoutTimer.Dispose(); } catch { }
				}

				// If queue is now empty, remove it from dictionary
				if (queue.IsEmpty)
				{
					_pending.TryRemove(key, out _);
				}

				try
				{
					_options.OnConnected?.Invoke(ev);
					return true;
				}
				catch
				{
					// swallow
				}
			}
		}

		return false;
	}

	/// <summary>
	/// Disposes of all resources used by the ApplicationLaunchMonitor.
	/// Cancels all pending timeout tasks and clears all tracking data.
	/// </summary>
	public void Dispose()
	{
		// Dispose all timers and clear pending timeout tasks
		foreach (var kvp in _timeoutTasks.ToArray())
		{
			var timer = kvp.Value;
			try { timer.Dispose(); } catch { }
		}

		_timeoutTasks.Clear();
		_pending.Clear();
	}
}
