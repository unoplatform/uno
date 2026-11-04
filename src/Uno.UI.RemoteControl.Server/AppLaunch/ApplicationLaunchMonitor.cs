#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Uno.UI.RemoteControl.Server.AppLaunch;

/// <summary>
/// In-memory monitor for application launch events and connection matching.
/// - Stores launch signals and matches incoming connections to prior launches.
/// - Uses a value-type composite key (no string concatenations) to minimize allocations. 
/// - Automatically handles timeouts using internal Task-based scheduling.
/// </summary>
public sealed class ApplicationLaunchMonitor : IApplicationLaunchMonitor, IDisposable
{
	internal const string DefaultPlatform = "Unspecified";

	/// <summary>
	/// Options that control the behavior of <see cref="ApplicationLaunchMonitor"/>.
	/// </summary>
	public class Options
	{
		/// <summary>
		/// Timeout after which a registered launch is considered expired. Defaults to 60 seconds.
		/// A connection pairs with a launch that has not timed out before one that has, and pairs across IsDebug values
		/// only within this timeout.
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
		/// before the connection. The launch's IsDebug can differ from the one the connection reported.
		/// </summary>
		public Action<LaunchEvent, bool>? OnConnected { get; set; }

		/// <summary>
		/// How long to retain launch entries before scavenging them from internal storage.
		/// This is independent from <see cref="Timeout"/>, after which a launch is reported to OnTimeout but stays matchable.
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
		/// Default: 5 minutes.
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
	/// A classification result for a pending connection. When <c>WasIdeInitiated</c> is true,
	/// <c>IsDebug</c> is the launch's value, as for <see cref="Options.OnConnected"/>; otherwise it is the
	/// connection's.
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
	private readonly record struct Key(Guid Mvid, string Platform, bool IsDebug)
	{
		// The IDE reports whether it launched the debugger, the app whether one was attached when it connected:
		// the two can differ for the same launch, so a key with the other value is the fallback match.
		public Key WithOtherIsDebug() => this with { IsDebug = !IsDebug };
	}

	// Registered launches per key, oldest first. Guarded by a lock on the dictionary, because a connection can take a
	// launch that is not the first of its list (see TryDequeueLaunch).
	private readonly Dictionary<Key, List<LaunchEvent>> _pending = new();

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
	/// Multiple registrations for the same key are kept; <see cref="ReportConnection"/> says which one a connection takes.
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

		lock (_pending)
		{
			if (!_pending.TryGetValue(key, out var launches))
			{
				_pending[key] = launches = new List<LaunchEvent>();
			}

			launches.Add(ev);
		}

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
		if (TryTakePendingConnection(key) is { } pending)
		{
			try { pending.RetryTimer?.Dispose(); } catch { }
			// Consume the launch event without invoking OnConnected to avoid double telemetry
			if (TryDequeueLaunch(key, out var matched, out _))
			{
				ClassifyAsIdeInitiated(pending, matched);
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
	/// If a registered launch with the same MVID and platform exists, it consumes one and invokes OnConnected for it:
	/// the oldest launch with the same IsDebug that has not timed out, then the oldest with the other IsDebug that has
	/// not timed out, then the oldest timed-out launch with the same IsDebug (OnConnected then reports it as timed out).
	/// Cancels the timeout task for the connected launch.
	/// </summary>
	/// <param name="mvid">The MVID of the root/head application being connected.</param>
	/// <param name="platform">The name of the platform from which the connection is reported. Cannot be null or empty.</param>
	/// <param name="isDebug">true if the connection is from a debug build; otherwise, false.</param>
	public bool ReportConnection(Guid mvid, string? platform, bool isDebug)
	{
		platform ??= DefaultPlatform;

		var key = new Key(mvid, platform, isDebug);

		// Consume a registered launch if one matches, cancelling its timeout timer, and invoke OnConnected with it.
		if (TryDequeueLaunch(key, out var ev, out var wasTimedOut))
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
	/// Attempts to dequeue the registered launch a connection with the specified key matches, cancelling its timeout
	/// timer and indicating whether the launch had previously timed out. Launches that have not timed out come first,
	/// the oldest with the same IsDebug value and then the oldest with the other one; otherwise the oldest timed-out
	/// launch with the same IsDebug value.
	/// </summary>
	private bool TryDequeueLaunch(Key key, [NotNullWhen(true)] out LaunchEvent? ev, out bool wasTimedOut)
	{
		wasTimedOut = false;

		lock (_pending)
		{
			var cutoff = GetTimeoutCutoff();
			ev = TryTakeLaunch(key, cutoff)
				?? TryTakeLaunch(key.WithOtherIsDebug(), cutoff)
				?? TryTakeLaunch(key, DateTimeOffset.MinValue);
		}

		if (ev is null)
		{
			return false;
		}

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

		if (_timedOut.TryRemove(ev, out var flag))
		{
			wasTimedOut = flag;
		}

		return true;
	}

	/// <summary>
	/// Removes and returns the oldest launch for the key registered after the cutoff. Call under the lock on _pending.
	/// </summary>
	private LaunchEvent? TryTakeLaunch(Key key, DateTimeOffset registeredAfter)
	{
		if (!_pending.TryGetValue(key, out var launches))
		{
			return null;
		}

		var index = launches.FindIndex(launch => launch.RegisteredAt > registeredAfter);
		if (index < 0)
		{
			return null;
		}

		var ev = launches[index];
		launches.RemoveAt(index);
		if (launches.Count == 0)
		{
			_pending.Remove(key);
		}

		return ev;
	}

	/// <summary>
	/// Removes the pending connection a newly registered launch matches, in the same order as a connection takes a
	/// launch: one within the timeout with the same IsDebug value, then with the other one; otherwise an older one with
	/// the same IsDebug value.
	/// </summary>
	private PendingItem? TryTakePendingConnection(Key key)
	{
		var cutoff = GetTimeoutCutoff();
		return TryTakePendingConnection(key, cutoff)
			?? TryTakePendingConnection(key.WithOtherIsDebug(), cutoff)
			?? TryTakePendingConnection(key, DateTimeOffset.MinValue);
	}

	private PendingItem? TryTakePendingConnection(Key key, DateTimeOffset connectedAfter)
		=> _pendingConnections.TryGetValue(key, out var pending)
			&& pending.ConnectedAt > connectedAfter
			&& _pendingConnections.TryRemove(key, out pending)
				? pending
				: null;

	/// <summary>
	/// Launches registered, or connections made, at or before this time are past the timeout.
	/// </summary>
	private DateTimeOffset GetTimeoutCutoff()
		=> _timeProvider.GetUtcNow() - _options.Timeout;

	/// <summary>
	/// Reports a pending connection as IDE-initiated. Like <see cref="Options.OnConnected"/>, it carries the launch's
	/// IsDebug value, which can differ from the connection's.
	/// </summary>
	private void ClassifyAsIdeInitiated(PendingItem pending, LaunchEvent launch)
	{
		try
		{
			_options.OnPendingClassified?.Invoke(new PendingClassification(
				launch.Mvid,
				launch.Platform,
				launch.IsDebug,
				pending.ConnectedAt,
				true,
				launch));
		}
		catch
		{
			// best-effort, swallow: a failing callback must not stop the monitor from matching
		}
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
		if (TryDequeueLaunch(key, out var ev, out _))
		{
			if (_pendingConnections.TryRemove(key, out var pending))
			{
				try { pending.RetryTimer?.Dispose(); } catch { }
				ClassifyAsIdeInitiated(pending, ev);
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
		// Clean registered launches
		var scavenged = new List<LaunchEvent>();
		lock (_pending)
		{
			var emptied = new List<Key>();
			foreach (var kvp in _pending)
			{
				kvp.Value.RemoveAll(ev =>
				{
					if (ev.RegisteredAt >= cutoff)
					{
						return false;
					}

					scavenged.Add(ev);
					return true;
				});

				if (kvp.Value.Count == 0)
				{
					emptied.Add(kvp.Key);
				}
			}

			foreach (var key in emptied)
			{
				_pending.Remove(key);
			}
		}

		foreach (var ev in scavenged)
		{
			// dispose any associated timeout task
			if (_timeoutTasks.TryRemove(ev, out var t))
			{
				try { t.Dispose(); } catch { /* best-effort */ }
			}
			// remove any record of timed-out state for scavenged items
			_timedOut.TryRemove(ev, out _);
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
		lock (_pending)
		{
			_pending.Clear();
		}

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
