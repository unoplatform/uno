#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using Uno.UI.RemoteControl.HotReload.Messages;

namespace Uno.UI.RemoteControl;

public static class DevServerDiagnostics
{
	// The AsyncLocal stores a mutable HOLDER rather than the sink itself. ExecutionContext
	// snapshots are immutable: when a sink set on a thread's ambient context flows into
	// long-lived captures (timers, CancellationTokenSource registrations, pooled connections,
	// the UI thread's ambient context), those snapshots can never be scrubbed from outside.
	// A sink owned by a collectible AssemblyLoadContext (a secondary app's
	// RemoteControlClient.StatusSink) would then pin that ALC for the captures' lifetime —
	// for process-lifetime timers, forever. The holder lives in this shared (default-ALC)
	// assembly, so snapshots only ever capture a default-ALC object; clearing the holder's
	// Sink propagates to every snapshot at once because they all reference the same holder.
	private static readonly AsyncLocal<SinkHolder> _current = new();

	// Weak registry of every holder handed out, so ClearCollectibleSinks can reach holders
	// captured in contexts that are no longer the current flow.
	private static readonly List<WeakReference<SinkHolder>> _holders = new();
	private static readonly object _holdersGate = new();

	/// <summary>
	/// Gets the current (async-local) sink for dev-server diagnostics.
	/// </summary>
	/// <remarks>
	/// The sink is stored behind a shared holder: a sink observed through a captured
	/// ExecutionContext snapshot can be detached after the fact by
	/// <see cref="ClearCollectibleSinks"/> (the snapshot then observes <c>NullSink</c>).
	/// </remarks>
	public static ISink Current
	{
		get => _current.Value?.Sink ?? NullSink.Instance;
		set
		{
			var holder = new SinkHolder(value);
			lock (_holdersGate)
			{
				_holders.RemoveAll(static wr => !wr.TryGetTarget(out _));
				_holders.Add(new WeakReference<SinkHolder>(holder));
			}

			_current.Value = holder;
		}
	}

	/// <summary>
	/// Clears the current async-local sink, reverting to the default <see cref="NullSink"/>.
	/// Called during <see cref="RemoteControlClient"/> disposal to break the
	/// AsyncLocal → StatusSink → RemoteControlClient reference chain.
	/// </summary>
	public static void ResetCurrent()
		=> _current.Value = new SinkHolder(NullSink.Instance);

	/// <summary>
	/// Detaches every tracked sink whose type is collectible (<see cref="Type.IsCollectible"/>),
	/// including sinks captured in ExecutionContext snapshots that are not reachable from the
	/// calling flow (timers, CTS registrations, pooled connections). Call during secondary-app
	/// (ALC) teardown: without this, those captures pin the unloading ALC for their own lifetime.
	/// </summary>
	/// <remarks>
	/// Per-TFM behavior: on net5+ only collectible sinks are detached (a sink from a
	/// session-lifetime collectible context is also detached and is expected to re-register on
	/// its next connection); the netstandard2.0 build cannot test collectibility and clears
	/// EVERY tracked sink as a teardown-time best effort.
	/// </remarks>
	public static void ClearCollectibleSinks()
	{
		lock (_holdersGate)
		{
			for (var i = _holders.Count - 1; i >= 0; i--)
			{
				if (!_holders[i].TryGetTarget(out var holder))
				{
					_holders.RemoveAt(i);
					continue;
				}

#if NET5_0_OR_GREATER
				if (holder.Sink is { } sink && sink.GetType().IsCollectible)
				{
					holder.Sink = null;
				}
#else
				// Assembly.IsCollectible is unavailable on this target; collectible ALCs do not
				// exist there either, but the shared assembly may still be serving a runtime
				// that has them — clear every tracked sink (teardown-time best effort).
				holder.Sink = null;
#endif
			}
		}
	}

	public interface ISink
	{
		void ReportInvalidFrame<TContent>(Frame frame);
	}

	private sealed class SinkHolder(ISink? sink)
	{
		public ISink? Sink { get; set; } = sink;
	}

	private class NullSink : ISink
	{
		public static NullSink Instance { get; } = new();

		public void ReportInvalidFrame<TContent>(Frame frame)
		{
		}
	}
}
