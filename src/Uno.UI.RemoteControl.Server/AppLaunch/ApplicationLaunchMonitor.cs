#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

	// The longest a TimeProvider timer can wait.
	internal static readonly TimeSpan MaxTimeout = TimeSpan.FromMilliseconds(uint.MaxValue - 1);

	/// <summary>
	/// Options that control the behavior of <see cref="ApplicationLaunchMonitor"/>.
	/// </summary>
	public class Options
	{
		/// <summary>
		/// Timeout after which a registered launch is considered expired. Defaults to 60 seconds. It must be positive and
		/// at most about 49 days, the longest a timer can wait; the monitor's constructor throws otherwise.
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
		/// The boolean parameter indicates whether the launch's timeout had fired before the connection, in which case
		/// OnTimeout is invoked for it too, possibly concurrently. The launch's IsDebug can differ from the one the
		/// connection reported.
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

	// Guards the launches and the pending connections, so that a pairing takes both in one step and whether a launch was
	// matched or timed out is decided once. Callbacks and timer disposals run outside it.
	private readonly object _gate = new();

	// Registered launches per key, oldest first. A connection can take one that is not the first of its list.
	private readonly Dictionary<Key, List<LaunchEntry>> _pending = new();

	// Connections that arrived without a matching registration, one per key.
	private readonly Dictionary<Key, PendingConnection> _pendingConnections = new();

	// Periodic scavenger timer (removes very old entries beyond Retention)
	private readonly IDisposable? _scavengeTimer;

	// Once listed, an entry changes only under _gate, and once IsRemoved is set none of its fields changes again, so a
	// removed entry can be read outside the lock. The same holds for PendingConnection.
	private sealed class LaunchEntry(LaunchEvent launch)
	{
		public LaunchEvent Event { get; } = launch;

		public IDisposable? TimeoutTimer { get; set; }

		public bool TimedOut { get; set; }

		// Matched, scavenged or disposed: the launch's timeout is no longer reported.
		public bool IsRemoved { get; set; }
	}

	private sealed class PendingConnection(Key key, DateTimeOffset connectedAt)
	{
		public Key Key { get; } = key;

		public DateTimeOffset ConnectedAt { get; } = connectedAt;

		public IDisposable? RetryTimer { get; set; }

		// Matched, replaced, scavenged or disposed.
		public bool IsRemoved { get; set; }
	}

	/// <summary>
	/// Creates a new instance of <see cref="ApplicationLaunchMonitor"/>.
	/// </summary>
	/// <param name="timeProvider">Optional time provider used for internal timing and for tests. If null, the system time provider is used.</param>
	/// <param name="options">Optional configuration for the monitor. If null, default options are used.</param>
	public ApplicationLaunchMonitor(TimeProvider? timeProvider = null, Options? options = null)
	{
		_timeProvider = timeProvider ?? TimeProvider.System;
		_options = options ?? new Options();
		if (_options.Timeout <= TimeSpan.Zero || _options.Timeout > MaxTimeout)
		{
			throw new ArgumentOutOfRangeException(nameof(options), _options.Timeout, "Options.Timeout must be positive and at most about 49 days.");
		}

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
		var launch = new LaunchEntry(ev);

		// Schedule automatic timeout before the launch can be matched, so that a match always finds the timer to cancel
		launch.TimeoutTimer = ScheduleAppLaunchTimeout(launch);

		PendingConnection? pending;
		LaunchEntry? matched = null;
		lock (_gate)
		{
			if (!_pending.TryGetValue(key, out var launches))
			{
				_pending[key] = launches = new List<LaunchEntry>();
			}

			launches.Add(launch);

			// If there's already a pending connection for this key (late match), take it and a launch in one step
			pending = TryTakePendingConnection(key, now - _options.Timeout);
			if (pending is not null)
			{
				matched = TryTakeLaunch(key);
			}
		}

		try
		{
			_options.OnRegistered?.Invoke(ev);
		}
		catch
		{
			// best-effort, swallow
		}

		// The launch just added is always takeable, so a taken connection always comes with a launch
		Debug.Assert(pending is null || matched is not null);
		if (pending is not null && matched is not null)
		{
			// Consume the launch event without invoking OnConnected to avoid double telemetry
			CompleteLateMatch(pending, matched);
		}
	}

	/// <summary>
	/// Schedules a one-shot timeout timer for the given launch, using the injected TimeProvider.
	/// </summary>
	private IDisposable ScheduleAppLaunchTimeout(LaunchEntry launch)
		=> _timeProvider.CreateTimer(
			static s =>
			{
				var (self, l) = ((ApplicationLaunchMonitor, LaunchEntry))s!;
				try
				{
					self.HandleTimeout(l);
				}
				catch
				{
					// swallow
				}
			},
			(this, launch),
			_options.Timeout,
			Timeout.InfiniteTimeSpan);

	/// <summary>
	/// Handles the timeout of a launch, unless a connection already took it.
	/// </summary>
	private void HandleTimeout(LaunchEntry launch)
	{
		IDisposable? timer;
		lock (_gate)
		{
			if (launch.IsRemoved)
			{
				return;
			}

			// Instead of removing the timed-out launch, we keep it available for future connections, and record that it
			// timed out so the connection that takes it reports it.
			launch.TimedOut = true;
			timer = launch.TimeoutTimer;
			launch.TimeoutTimer = null;
		}

		DisposeTimer(timer);
		try
		{
			_options.OnTimeout?.Invoke(launch.Event);
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

		// Read the clock outside the lock: a test time provider can run due timers when read
		var now = _timeProvider.GetUtcNow();
		LaunchEntry? matched;
		PendingConnection? pending = null;
		PendingConnection? replaced = null;
		lock (_gate)
		{
			matched = TryTakeLaunch(key);
			if (matched is null)
			{
				// No registered launch to consume: keep the connection pending, replacing an older one for the key
				pending = new PendingConnection(key, now);
				if (_pendingConnections.Remove(key, out replaced))
				{
					replaced.IsRemoved = true;
				}

				_pendingConnections[key] = pending;
			}
		}

		if (matched is not null)
		{
			DisposeTimer(matched.TimeoutTimer);
			try
			{
				_options.OnConnected?.Invoke(matched.Event, matched.TimedOut);
			}
			catch
			{
				// swallow
			}

			return true; // successfully consumed a registered launch
		}

		DisposeTimer(replaced?.RetryTimer);
		SchedulePendingRetry(pending!);
		return false;
	}

	/// <summary>
	/// Takes the registered launch a connection with the specified key matches, so that its timeout is no longer
	/// reported. Launches that have not timed out come first, the oldest with the same IsDebug value and then, when
	/// allowed, the oldest with the other one; otherwise the oldest timed-out launch with the same IsDebug value.
	/// Call under _gate.
	/// </summary>
	private LaunchEntry? TryTakeLaunch(Key key, bool allowOtherIsDebug = true)
		=> TryTakeOldestLaunch(key, timedOut: false)
			?? (allowOtherIsDebug ? TryTakeOldestLaunch(key.WithOtherIsDebug(), timedOut: false) : null)
			?? TryTakeOldestLaunch(key, timedOut: true);

	private LaunchEntry? TryTakeOldestLaunch(Key key, bool timedOut)
	{
		Debug.Assert(Monitor.IsEntered(_gate));
		if (!_pending.TryGetValue(key, out var launches))
		{
			return null;
		}

		for (var i = 0; i < launches.Count; i++)
		{
			var launch = launches[i];
			if (launch.TimedOut == timedOut)
			{
				launch.IsRemoved = true;
				launches.RemoveAt(i);
				if (launches.Count == 0)
				{
					_pending.Remove(key);
				}

				return launch;
			}
		}

		return null;
	}

	/// <summary>
	/// Takes the pending connection a newly registered launch matches, in the same order as a connection takes a
	/// launch: one within the timeout with the same IsDebug value, then with the other one; otherwise an older one with
	/// the same IsDebug value. Connections made at or before the cutoff are past the timeout. Call under _gate.
	/// </summary>
	private PendingConnection? TryTakePendingConnection(Key key, DateTimeOffset timeoutCutoff)
		=> TryTakePendingConnectionAfter(key, timeoutCutoff)
			?? TryTakePendingConnectionAfter(key.WithOtherIsDebug(), timeoutCutoff)
			?? TryTakePendingConnectionAfter(key, DateTimeOffset.MinValue);

	private PendingConnection? TryTakePendingConnectionAfter(Key key, DateTimeOffset connectedAfter)
	{
		Debug.Assert(Monitor.IsEntered(_gate));
		if (!_pendingConnections.TryGetValue(key, out var pending) || pending.ConnectedAt <= connectedAfter)
		{
			return null;
		}

		RemovePendingConnection(pending);
		return pending;
	}

	// Call under _gate.
	private void RemovePendingConnection(PendingConnection pending)
	{
		Debug.Assert(Monitor.IsEntered(_gate));
		pending.IsRemoved = true;
		if (_pendingConnections.TryGetValue(pending.Key, out var current) && current == pending)
		{
			_pendingConnections.Remove(pending.Key);
		}
	}

	/// <summary>
	/// Completes a late match taken under _gate: cancels both timers and reports the connection as IDE-initiated.
	/// </summary>
	private void CompleteLateMatch(PendingConnection pending, LaunchEntry launch)
	{
		DisposeTimer(pending.RetryTimer);
		DisposeTimer(launch.TimeoutTimer);
		ClassifyAsIdeInitiated(pending, launch.Event);
	}

	/// <summary>
	/// Reports a pending connection as IDE-initiated. Like <see cref="Options.OnConnected"/>, it carries the launch's
	/// IsDebug value, which can differ from the connection's.
	/// </summary>
	private void ClassifyAsIdeInitiated(PendingConnection pending, LaunchEvent launch)
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

	private void ClassifyAsNotIdeInitiated(PendingConnection pending)
	{
		try
		{
			_options.OnPendingClassified?.Invoke(new PendingClassification(
				pending.Key.Mvid,
				pending.Key.Platform,
				pending.Key.IsDebug,
				pending.ConnectedAt,
				false,
				null));
		}
		catch
		{
			// best-effort, swallow: a failing callback must not stop the monitor from matching
		}
	}

	private void SchedulePendingRetry(PendingConnection pending)
	{
		IDisposable? timer = null;
		try
		{
			timer = _timeProvider.CreateTimer(
				static s =>
				{
					var (self, p) = ((ApplicationLaunchMonitor, PendingConnection))s!;
					self.HandlePendingRetry(p);
				},
				(this, pending),
				_options.PendingRetryWindow,
				Timeout.InfiniteTimeSpan);
		}
		catch { /* best-effort */ }

		lock (_gate)
		{
			if (!pending.IsRemoved)
			{
				pending.RetryTimer = timer;
				timer = null;
			}
		}

		// Matched or replaced while the timer was created: its retry is not needed
		DisposeTimer(timer);
	}

	private void HandlePendingRetry(PendingConnection pending)
	{
		// Like a late registration, take a launch with the other IsDebug only for a connection within the timeout
		var withinTimeout = pending.ConnectedAt > _timeProvider.GetUtcNow() - _options.Timeout;
		LaunchEntry? matched;
		lock (_gate)
		{
			if (pending.IsRemoved)
			{
				return; // matched, replaced, scavenged or disposed since the retry was scheduled
			}

			// Try to find a launch now
			matched = TryTakeLaunch(pending.Key, allowOtherIsDebug: withinTimeout);
			if (matched is not null)
			{
				RemovePendingConnection(pending);
			}
		}

		if (matched is not null)
		{
			CompleteLateMatch(pending, matched);
		}
		else
		{
			// Still unmatched: classify as non-IDE initiated but keep it for a late match
			ClassifyAsNotIdeInitiated(pending);
		}
	}

	/// <summary>
	/// Runs a scavenging pass removing launches whose RegisteredAt is older than Retention, and pending connections
	/// older than PendingRetention. This reclaims memory for entries that will never connect.
	/// </summary>
	private void RunScavengePass()
	{
		var now = _timeProvider.GetUtcNow();
		var cutoff = now - _options.Retention;
		var pendingCutoff = now - _options.PendingRetention;
		var scavengedLaunches = new List<LaunchEntry>();
		var scavengedConnections = new List<PendingConnection>();
		lock (_gate)
		{
			// Clean registered launches
			var emptied = new List<Key>();
			foreach (var kvp in _pending)
			{
				kvp.Value.RemoveAll(launch =>
				{
					if (launch.Event.RegisteredAt >= cutoff)
					{
						return false;
					}

					launch.IsRemoved = true;
					scavengedLaunches.Add(launch);
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

			// Clean pending connections older than retention
			foreach (var pending in _pendingConnections.Values)
			{
				if (pending.ConnectedAt < pendingCutoff)
				{
					scavengedConnections.Add(pending);
				}
			}

			foreach (var pending in scavengedConnections)
			{
				RemovePendingConnection(pending);
			}
		}

		foreach (var launch in scavengedLaunches)
		{
			DisposeTimer(launch.TimeoutTimer);
		}

		foreach (var pending in scavengedConnections)
		{
			DisposeTimer(pending.RetryTimer);
		}
	}

	private static void DisposeTimer(IDisposable? timer)
	{
		try
		{
			timer?.Dispose();
		}
		catch
		{
			// best-effort, swallow: a failed disposal must not stop matching or scavenging
		}
	}

	/// <summary>
	/// Disposes of all resources used by the ApplicationLaunchMonitor.
	/// Cancels all pending timeout tasks and clears all tracking data.
	/// </summary>
	public void Dispose()
	{
		_scavengeTimer?.Dispose();

		var launches = new List<LaunchEntry>();
		List<PendingConnection> connections;
		lock (_gate)
		{
			foreach (var keyLaunches in _pending.Values)
			{
				foreach (var launch in keyLaunches)
				{
					launch.IsRemoved = true;
					launches.Add(launch);
				}
			}

			_pending.Clear();

			connections = new List<PendingConnection>(_pendingConnections.Values);
			foreach (var pending in connections)
			{
				pending.IsRemoved = true;
			}

			_pendingConnections.Clear();
		}

		foreach (var launch in launches)
		{
			DisposeTimer(launch.TimeoutTimer);
		}

		// Flush pending connections as non-IDE initiated
		foreach (var pending in connections)
		{
			DisposeTimer(pending.RetryTimer);
			ClassifyAsNotIdeInitiated(pending);
		}
	}
}
