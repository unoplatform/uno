#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Uno.UI.RemoteControl.Server.AppLaunch;

/// <summary>
/// In-memory monitor for application launch events and connection matching.
/// - Stores launch signals and matches incoming connections to prior launches.
/// - Uses a value-type composite key (no string concatenations) to minimize allocations. 
/// - Automatically handles timeouts using internal Task-based scheduling.
/// </summary>
public sealed class ApplicationLaunchMonitor : IDisposable
{
	internal const string DefaultPlatform = "Unspecified";

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
		/// The boolean parameter indicates whether the launch previously timed out (OnTimeout was invoked)
		/// before the connection.
		/// </summary>
		public Action<LaunchEvent, bool>? OnConnected { get; set; }

		/// <summary>
		/// How long to retain launch entries before scavenging them from internal storage.
		/// This is independent from <see cref="Timeout"/> which only triggers the OnTimeout callback.
		/// Default: 0.5 hour.
		/// </summary>
		public TimeSpan Retention { get; set; } = TimeSpan.FromHours(0.5);

		/// <summary>
		/// Retry window for pending connections (connections that arrived before any matching registration).
		/// After this delay, a best-effort match is attempted, and if still unmatched, the connection is
		/// classified as non-IDE initiated. Default: 2 seconds.
		/// </summary>
		public TimeSpan PendingRetryWindow { get; set; } = TimeSpan.FromSeconds(2);

		/// <summary>
		/// Retention duration for pending connections allowing a late match (registration after connection).
		/// Default: 1 hour.
		/// </summary>
		public TimeSpan PendingRetention { get; set; } = TimeSpan.FromHours(1);

		/// <summary>
		/// Callback invoked when a pending connection is classified, either as non-IDE initiated (no match)
		/// or as IDE-initiated (late match found). For late matches, the associated <see cref="LaunchEvent"/>
		/// is provided.
		/// </summary>
		public Action<PendingClassification>? OnPendingClassified { get; set; }

		/// <summary>
		/// How often the monitor runs a scavenging pass to remove very old entries.
		/// Default: 1 minute.
		/// </summary>
		public TimeSpan ScavengeInterval { get; set; } = TimeSpan.FromMinutes(5);
	}

	/// <summary>
	/// Describes a single launch event recorded by the monitor.
	/// </summary>
	public sealed record LaunchEvent(
		Guid Mvid,
		string Platform,
		bool IsDebug,
		string Ide,
		string Plugin,
		DateTimeOffset RegisteredAt);

	/// <summary>
	/// A classification result for a pending connection.
	/// </summary>
	public sealed record PendingClassification(
		Guid Mvid,
		string Platform,
		bool IsDebug,
		DateTimeOffset ConnectedAt,
		bool WasIdeInitiated,
		LaunchEvent? Launch);

	private readonly TimeProvider _timeProvider;
	private readonly Options _options;

	// Non-allocating composite key (avoids string creation per lookup)
	private readonly record struct Key(Guid Mvid, string Platform, bool IsDebug);

	private readonly ConcurrentDictionary<Key, ConcurrentQueue<LaunchEvent>> _pending = new();

	// Track timeout timers for each launch event
	private readonly ConcurrentDictionary<LaunchEvent, IDisposable> _timeoutTasks = new();

	// Track whether a given launch event has previously timed out
	private readonly ConcurrentDictionary<LaunchEvent, bool> _timedOut = new();

	// Periodic scavenger timer (removes very old entries beyond Retention)
	private readonly IDisposable? _scavengeTimer;

	// Pending connections map (connections that arrived without a matching registration)
	private readonly ConcurrentDictionary<Key, PendingItem> _pendingConnections = new();

	private sealed record PendingItem(DateTimeOffset ConnectedAt, IDisposable? RetryTimer);

	/// <summary>
	/// Creates a new instance of <see cref="ApplicationLaunchMonitor"/>.
	/// </summary>
	/// <param name="timeProvider">Optional time provider used for internal timing and for tests. If null, the system time provider is used.</param>
	/// <param name="options">Optional configuration for the monitor. If null, default options are used.</param>
	public ApplicationLaunchMonitor(TimeProvider? timeProvider = null, Options? options = null)
	{
		_timeProvider = timeProvider ?? TimeProvider.System;
		_options = options ?? new Options();

		// Start periodic scavenger to remove very old entries. Use a periodic timer via TimeProvider so tests can control it.
		try
		{
			_scavengeTimer = _timeProvider.CreateTimer(
				static s => ((ApplicationLaunchMonitor)s!).RunScavengePass(),
				this,
				_options.ScavengeInterval,
				_options.ScavengeInterval);
		}
		catch
		{
			// best-effort: if timer creation isn't supported, the monitor will still function but won't scavenge automatically.
			_scavengeTimer = null;
		}
	}

	/// <summary>
	/// Register that an application was launched.
	/// Automatically starts the timeout countdown from the current time provider value.
	/// Multiple registrations for the same key are kept and consumed in FIFO order.
	/// </summary>
	/// <param name="mvid">The MVID of the root/head application.</param>
	/// <param name="platform">The platform used to run the application. Cannot be null or empty.</param>
	/// <param name="isDebug">Whether the debugger is used.</param>
	/// <param name="ide">The IDE used to launch the application.</param>
	/// <param name="plugin">The Uno plugin version used to launch the application.</param>
	public void RegisterLaunch(Guid mvid, string? platform, bool isDebug, string ide, string plugin)
	{
		platform ??= DefaultPlatform;

		var now = _timeProvider.GetUtcNow();
		var ev = new LaunchEvent(mvid, platform, isDebug, ide, plugin, now);
		var key = new Key(mvid, platform, isDebug);

		var queue = _pending.GetOrAdd(key, static _ => new ConcurrentQueue<LaunchEvent>());
		queue.Enqueue(ev);

		// Schedule automatic timeout
		ScheduleAppLaunchTimeout(ev, key);

		try
		{
			_options.OnRegistered?.Invoke(ev);
		}
		catch
		{
			// best-effort, swallow
		}

		// If there's already a pending connection for this key (late match), classify it immediately as IDE-initiated
		if (_pendingConnections.TryRemove(key, out var pending))
		{
			try { pending.RetryTimer?.Dispose(); } catch { }
			// Consume the launch event without invoking OnConnected to avoid double telemetry
			if (TryDequeueLaunch(key, out var matched, out var wasTimedOut) && matched is not null)
			{
				// Don't invoke OnConnected; instead, classify the pending connection as IDE-initiated
				try
				{
					_options.OnPendingClassified?.Invoke(new PendingClassification(
						key.Mvid,
						key.Platform,
						key.IsDebug,
						pending.ConnectedAt,
						true,
						matched));
				}
				catch { }
			}
		}
	}

	/// <summary>
	/// Schedules a timeout task for the given launch event.
	/// </summary>
	/// <param name="launchEvent">The launch event to schedule timeout for.</param>
	/// <param name="key">The key for the launch event.</param>
	private void ScheduleAppLaunchTimeout(LaunchEvent launchEvent, Key key)
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
		// Instead of removing the timed-out event from pending, we keep it available for future connections
		// but still invoke the OnTimeout callback to inform listeners that the timeout elapsed.
		// Record the timed-out state so ReportConnection can notify callers that this event previously timed out.
		_timedOut[launchEvent] = true;
		try
		{
			_options.OnTimeout?.Invoke(launchEvent);
		}
		catch
		{
			// swallow
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
	public bool ReportConnection(Guid mvid, string? platform, bool isDebug)
	{
		platform ??= DefaultPlatform;

		var key = new Key(mvid, platform, isDebug);

		// Consume first the oldest pending event if present (registered launch).
		// When consuming, cancel its timeout timer and invoke OnConnected with it.
		if (TryDequeueLaunch(key, out var ev, out var wasTimedOut) && ev is not null)
		{
			try
			{
				_options.OnConnected?.Invoke(ev, wasTimedOut);
			}
			catch
			{
				// swallow
			}

			return true; // successfully consumed a registered launch
		}

		// No registered launch to consume: create a pending connection with a retry timer
		SchedulePendingConnection(key);
		return false;
	}

	/// <summary>
	/// Attempts to dequeue a registered launch for the specified key, cancelling its timeout timer and indicating
	/// whether the launch had previously timed out.
	/// </summary>
	private bool TryDequeueLaunch(Key key, out LaunchEvent? ev, out bool wasTimedOut)
	{
		wasTimedOut = false;
		ev = default!;
		if (_pending.TryGetValue(key, out var queue) && queue.TryDequeue(out ev))
		{
			// Cancel / dispose the timeout timer for this event if still present
			if (_timeoutTasks.TryRemove(ev, out var timeoutTimer))
			{
				try
				{
					timeoutTimer.Dispose();
				}
				catch
				{
					// Swallow any exceptions during timer disposal
				}
			}

			// If queue is now empty, remove it from dictionary
			if (queue.IsEmpty)
			{
				_pending.TryRemove(key, out _);
			}

			if (_timedOut.TryRemove(ev, out var flag))
			{
				wasTimedOut = flag;
			}

			return true;
		}

		return false;
	}

	private void SchedulePendingConnection(Key key)
	{
		var now = _timeProvider.GetUtcNow();
		// Replace any existing pending with latest timestamp
		if (_pendingConnections.TryRemove(key, out var existing))
		{
			try
			{
				existing.RetryTimer?.Dispose();
			}
			catch
			{
				// Swallow any exceptions during timer disposal
			}
		}

		IDisposable? timer = null;
		try
		{
			timer = _timeProvider.CreateTimer(
				static s =>
				{
					var (self, k) = ((ApplicationLaunchMonitor, Key))s!;
					self.HandlePendingRetry(k);
				},
				(this, key),
				_options.PendingRetryWindow,
				Timeout.InfiniteTimeSpan);
		}
		catch { /* best-effort */ }

		_pendingConnections[key] = new PendingItem(now, timer);
	}

	private void HandlePendingRetry(Key key)
	{
		// Try to find a launch now
		if (TryDequeueLaunch(key, out var ev, out var wasTimedOut))
		{
			if (_pendingConnections.TryRemove(key, out var pending))
			{
				try { pending.RetryTimer?.Dispose(); } catch { }
				try
				{
					_options.OnPendingClassified?.Invoke(new PendingClassification(
						key.Mvid,
						key.Platform,
						key.IsDebug,
						pending.ConnectedAt,
						true,
						ev));
				}
				catch { }
			}
		}
		else
		{
			// Still unmatched: classify as non-IDE initiated but keep it for a late match
			if (_pendingConnections.TryGetValue(key, out var pending))
			{
				try
				{
					_options.OnPendingClassified?.Invoke(new PendingClassification(
						key.Mvid,
						key.Platform,
						key.IsDebug,
						pending.ConnectedAt,
						false,
						null));
				}
				catch { }
			}
		}
	}

	/// <summary>
	/// Runs a scavenging pass removing entries whose RegisteredAt is older than Retention.
	/// This reclaims memory for entries that will never connect.
	/// </summary>
	private void RunScavengePass()
	{
		var cutoff = _timeProvider.GetUtcNow() - _options.Retention;
		// Clean pending queues
		foreach (var kvp in _pending.ToArray())
		{
			var key = kvp.Key;
			var queue = kvp.Value;
			var temp = new Queue<LaunchEvent>();
			while (queue.TryDequeue(out var ev))
			{
				if (ev.RegisteredAt >= cutoff)
				{
					temp.Enqueue(ev);
				}
				else
				{
					// dispose any associated timeout task
					if (_timeoutTasks.TryRemove(ev, out var t))
					{
						try { t.Dispose(); } catch { }
					}
					// remove any record of timed-out state for scavenged items
					_timedOut.TryRemove(ev, out _);
				}
			}

			// re-enqueue remaining
			while (temp.Count > 0)
			{
				queue.Enqueue(temp.Dequeue());
			}

			if (queue.IsEmpty)
			{
				_pending.TryRemove(key, out _);
			}
		}

		// Clean pending connections older than retention
		var pendingCutoff = _timeProvider.GetUtcNow() - _options.PendingRetention;
		foreach (var kvp in _pendingConnections.ToArray())
		{
			if (kvp.Value.ConnectedAt < pendingCutoff)
			{
				if (_pendingConnections.TryRemove(kvp.Key, out var pending))
				{
					try { pending.RetryTimer?.Dispose(); } catch { }
				}
			}
		}
	}

	/// <summary>
	/// Disposes of all resources used by the ApplicationLaunchMonitor.
	/// Cancels all pending timeout tasks and clears all tracking data.
	/// </summary>
	public void Dispose()
	{
		_scavengeTimer?.Dispose();

		// Dispose all timers and clear pending timeout tasks
		foreach (var kvp in _timeoutTasks.ToArray())
		{
			var timer = kvp.Value;
			try { timer.Dispose(); } catch { }
		}

		_timeoutTasks.Clear();
		_timedOut.Clear();
		_pending.Clear();

		// Flush pending connections as non-IDE initiated
		foreach (var kvp in _pendingConnections.ToArray())
		{
			if (_pendingConnections.TryRemove(kvp.Key, out var pending))
			{
				try { pending.RetryTimer?.Dispose(); } catch { }
				try
				{
					_options.OnPendingClassified?.Invoke(new PendingClassification(
						kvp.Key.Mvid,
						kvp.Key.Platform,
						kvp.Key.IsDebug,
						pending.ConnectedAt,
						false,
						null));
				}
				catch { }
			}
		}
	}
}
