#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Uno.Foundation.Logging;

namespace Uno.HotReload.Client;

/// <summary>
/// Process-wide state machine that tracks per-phase pause counters and pending
/// type lists. Owned by <see cref="UIUpdate"/> and <see cref="HotReloadUIPauseHandle"/>.
/// </summary>
/// <remarks>
/// <b>Drain semantics:</b> every phase is pause-counted independently so callers can check
/// whether a specific phase is blocked. However, the <see cref="DrainRequested"/> event is
/// raised only when <em>all</em> counters reach zero simultaneously, meaning the drain
/// processes every queued phase in one pass. This makes the "HD pauses VisualTree only"
/// scenario work correctly: ResourceDictionary updates are applied inline (unblocked), while
/// VisualTree updates queue until the pause is released — and then everything is applied in a
/// single, ordered drain.
/// </remarks>
internal static class PendingUIUpdates
{
	private static readonly Logger _log = typeof(PendingUIUpdates).Log();

	// Per-phase pause counters.
	private static int _resourceDictionariesPauseCount;
	private static int _visualTreePauseCount;

	// Active handles — used to surface "who is keeping this paused" in diagnostic messages.
	private static ImmutableHashSet<HotReloadUIPauseHandle> _activeHandles
		= ImmutableHashSet<HotReloadUIPauseHandle>.Empty;

	// Per-phase pending type sets.
	private static ImmutableHashSet<Type> _pendingResourceDictionariesUpdates
		= ImmutableHashSet<Type>.Empty;
	private static ImmutableHashSet<Type> _pendingVisualTreeUpdates
		= ImmutableHashSet<Type>.Empty;

	/// <summary>
	/// Raised when all phase counters transition to zero and at least one pending
	/// type has been queued. The event carries a snapshot of types for each phase.
	/// Subscribers are responsible for dispatching to the appropriate thread.
	/// </summary>
	internal static event EventHandler<DrainEventArgs>? DrainRequested;

	/// <summary>Event args for <see cref="DrainRequested"/>.</summary>
	internal sealed class DrainEventArgs : EventArgs
	{
		internal DrainEventArgs(Type[] resourceDictionaries, Type[] visualTree)
		{
			ResourceDictionaries = resourceDictionaries;
			VisualTree = visualTree;
		}

		public Type[] ResourceDictionaries { get; }
		public Type[] VisualTree { get; }
		public bool HasAny => ResourceDictionaries.Length > 0 || VisualTree.Length > 0;
	}

	/// <summary>True if at least one handle currently pauses any of the flags in <paramref name="phase"/>.</summary>
	public static bool IsPaused(HotReloadUIPhases phase)
	{
		if ((phase & HotReloadUIPhases.ResourceDictionaries) != 0
			&& Volatile.Read(ref _resourceDictionariesPauseCount) > 0)
		{
			return true;
		}

		if ((phase & HotReloadUIPhases.VisualTree) != 0
			&& Volatile.Read(ref _visualTreePauseCount) > 0)
		{
			return true;
		}

		return false;
	}

	/// <summary>
	/// Adds <paramref name="types"/> to the pending list for every phase in
	/// <paramref name="phase"/> that is currently paused. No-op for unpaused phases — the
	/// caller applies updates for those phases directly.
	/// </summary>
	public static void Enqueue(HotReloadUIPhases phase, IReadOnlyCollection<Type> types)
	{
		if (types is null || types.Count == 0)
		{
			return;
		}

		if ((phase & HotReloadUIPhases.ResourceDictionaries) != 0
			&& Volatile.Read(ref _resourceDictionariesPauseCount) > 0)
		{
			ImmutableInterlocked.Update(ref _pendingResourceDictionariesUpdates, AddAll, types);
		}

		if ((phase & HotReloadUIPhases.VisualTree) != 0
			&& Volatile.Read(ref _visualTreePauseCount) > 0)
		{
			ImmutableInterlocked.Update(ref _pendingVisualTreeUpdates, AddAll, types);
		}

		static ImmutableHashSet<Type> AddAll(ImmutableHashSet<Type> set, IReadOnlyCollection<Type> additions)
		{
			var b = set.ToBuilder();
			foreach (var t in additions)
			{
				b.Add(t);
			}

			return b.ToImmutable();
		}
	}

	/// <summary>
	/// Returns a human-readable list of "who is currently pausing" for diagnostics.
	/// </summary>
	internal static string GetPauseHoldersSummary()
		=> _activeHandles.Count == 0
			? "(none)"
			: string.Join(", ", _activeHandles.Select(h => h.ToString()));

	/// <summary>Increments the pause counter for every phase in phases.</summary>
	internal static void Acquire(HotReloadUIPauseHandle handle)
	{
		var phases = handle.Phases;
		ImmutableInterlocked.Update(ref _activeHandles, static (set, h) => set.Add(h), handle);

		if ((phases & HotReloadUIPhases.ResourceDictionaries) != 0)
		{
			Interlocked.Increment(ref _resourceDictionariesPauseCount);
		}

		if ((phases & HotReloadUIPhases.VisualTree) != 0)
		{
			Interlocked.Increment(ref _visualTreePauseCount);
		}
	}

	/// <summary>
	/// Removes <paramref name="types"/> from the pending lists for every phase in
	/// <paramref name="phases"/>. Logs with caller info and the acquiring handle's identity.
	/// </summary>
	internal static void Drop(
		HotReloadUIPhases phases,
		IReadOnlyCollection<Type> types,
		string? acquiredBy,
		string? reason,
		string? caller,
		int line)
	{
		if (types is null || types.Count == 0)
		{
			return;
		}

		var rdBefore = 0;
		var rdAfter = 0;
		var vtBefore = 0;
		var vtAfter = 0;

		if ((phases & HotReloadUIPhases.ResourceDictionaries) != 0)
		{
			rdBefore = _pendingResourceDictionariesUpdates.Count;
			ImmutableInterlocked.Update(ref _pendingResourceDictionariesUpdates, RemoveAll, types);
			rdAfter = _pendingResourceDictionariesUpdates.Count;
		}

		if ((phases & HotReloadUIPhases.VisualTree) != 0)
		{
			vtBefore = _pendingVisualTreeUpdates.Count;
			ImmutableInterlocked.Update(ref _pendingVisualTreeUpdates, RemoveAll, types);
			vtAfter = _pendingVisualTreeUpdates.Count;
		}

		if (_log.IsEnabled(LogLevel.Information))
		{
			var reasonPart = reason is { Length: > 0 } ? $"reason='{reason}' " : string.Empty;
			_log.Info(
				$"[HotReload UI Pause] Drop phases={phases} {reasonPart}acquiredBy='{acquiredBy}' caller='{caller}@{line}' " +
				$"types=[{string.Join(", ", types.Select(static t => t.FullName ?? t.Name))}] " +
				$"pendingRD={rdBefore}->{rdAfter} pendingVT={vtBefore}->{vtAfter}");
		}

		static ImmutableHashSet<Type> RemoveAll(ImmutableHashSet<Type> set, IReadOnlyCollection<Type> removals)
		{
			var b = set.ToBuilder();
			foreach (var t in removals)
			{
				b.Remove(t);
			}

			return b.ToImmutable();
		}
	}

	/// <summary>
	/// Decrements the pause counter for every phase in phases.
	/// When <em>all</em> phase counters reach zero, raises <see cref="DrainRequested"/>
	/// with snapshots of the pending type lists.
	/// </summary>
	internal static void Release(HotReloadUIPauseHandle handle)
	{
		var phases = handle.Phases;
		ImmutableInterlocked.Update(ref _activeHandles, static (set, h) => set.Remove(h), handle);

		var rdNowZero = false;
		var vtNowZero = false;

		if ((phases & HotReloadUIPhases.ResourceDictionaries) != 0)
		{
			rdNowZero = Interlocked.Decrement(ref _resourceDictionariesPauseCount) == 0;
		}

		if ((phases & HotReloadUIPhases.VisualTree) != 0)
		{
			vtNowZero = Interlocked.Decrement(ref _visualTreePauseCount) == 0;
		}

		// Drain only when all phase counters are zero — this means every pending type
		// (for every phase) can be applied in one ordered pass.
		if (Volatile.Read(ref _resourceDictionariesPauseCount) != 0
			|| Volatile.Read(ref _visualTreePauseCount) != 0)
		{
			return; // Some phase is still paused — wait for the last handle.
		}

		var rdSnapshot = Interlocked.Exchange(ref _pendingResourceDictionariesUpdates, ImmutableHashSet<Type>.Empty);
		var vtSnapshot = Interlocked.Exchange(ref _pendingVisualTreeUpdates, ImmutableHashSet<Type>.Empty);

		if (rdSnapshot.Count == 0 && vtSnapshot.Count == 0)
		{
			return; // Nothing to drain.
		}

		var args = new DrainEventArgs(rdSnapshot.ToArray(), vtSnapshot.ToArray());

		if (_log.IsEnabled(LogLevel.Information))
		{
			_log.Info(
				$"[HotReload UI Pause] Drain: RD={args.ResourceDictionaries.Length} type(s), VT={args.VisualTree.Length} type(s)");
		}

		try
		{
			DrainRequested?.Invoke(null, args);
		}
		catch (Exception ex)
		{
			if (_log.IsEnabled(LogLevel.Error))
			{
				_log.Error("[HotReload UI Pause] DrainRequested handler threw — pending types may be lost.", ex);
			}
		}
	}

	// ── Test helpers ─────────────────────────────────────────────────────────

	/// <summary>Reset all state. For unit tests only.</summary>
	internal static void ResetForTest()
	{
		Volatile.Write(ref _resourceDictionariesPauseCount, 0);
		Volatile.Write(ref _visualTreePauseCount, 0);
		_activeHandles = ImmutableHashSet<HotReloadUIPauseHandle>.Empty;
		Volatile.Write(ref _pendingResourceDictionariesUpdates, ImmutableHashSet<Type>.Empty);
		Volatile.Write(ref _pendingVisualTreeUpdates, ImmutableHashSet<Type>.Empty);
	}

	/// <summary>Snapshot of pending counts. For unit tests only.</summary>
	internal static PendingCounts GetPendingCountsForTest()
		=> new(_pendingResourceDictionariesUpdates.Count, _pendingVisualTreeUpdates.Count);

	/// <summary>Snapshot counts struct.</summary>
	internal readonly record struct PendingCounts(int ResourceDictionaries, int VisualTree);
}
