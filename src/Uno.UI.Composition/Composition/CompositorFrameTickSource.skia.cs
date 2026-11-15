#nullable enable

using System;
using System.Runtime.CompilerServices;
using Microsoft.UI.Composition;
using Uno.Foundation.Logging;

namespace Uno.UI.Composition;

/// <summary>
/// Ticks drivers that have no visual to name a window by, such as a free-standing InteractionTracker, on every
/// window's frames, the way WinUI's compositor ticks a tracker regardless of which window shows it.
/// </summary>
internal sealed class CompositorFrameTickSource : IFrameTickSource
{
	private readonly Compositor _compositor;
	private readonly ConditionalWeakTable<ICompositionTarget, object?> _hosts = new();
	private EventHandler<long>? _frameStarting;
	private long _lastTimestamp;

	internal CompositorFrameTickSource(Compositor compositor) => _compositor = compositor;

	public event EventHandler<long>? FrameStarting
	{
		add
		{
			if (value is null)
			{
				return;
			}

			var wasEmpty = _frameStarting is null;
			_frameStarting += value;
			if (wasEmpty)
			{
				_compositor.AddFrameDriver();
				foreach (var (host, _) in _hosts)
				{
					host.RequestNewFrame();
				}
			}
		}
		remove
		{
			if (value is null || _frameStarting is not { } frameStarting)
			{
				return;
			}

			_frameStarting = frameStarting - value;
			if (_frameStarting is null)
			{
				_compositor.RemoveFrameDriver();
			}
		}
	}

	public long FrameIntervalInTicks { get; private set; }

	internal bool HasDrivers => _frameStarting is not null;

	internal bool HasHosts
	{
		get
		{
			foreach (var _ in _hosts)
			{
				return true;
			}

			return false;
		}
	}

	internal void AddHost(ICompositionTarget host) => _hosts.AddOrUpdate(host, null);

	internal void RemoveHost(ICompositionTarget host)
	{
		_hosts.Remove(host);

		// With no window left no frame will ever tick them, and IsAnimating would report them forever.
		if (_frameStarting is not null && !HasHosts)
		{
			_frameStarting = null;
			_compositor.RemoveFrameDriver();
		}
	}

	/// <summary>Raised from the tick, after the windows' own drivers, with the latest of their frame times.</summary>
	internal void RaiseFrameStarting(long timestamp, long frameIntervalInTicks)
	{
		if (_frameStarting is not { } frameStarting)
		{
			return;
		}

		// The windows' clocks are not in phase, and a driver must never see time go backwards.
		_lastTimestamp = Math.Max(_lastTimestamp, timestamp);
		FrameIntervalInTicks = frameIntervalInTicks;

		// One try/catch per driver: a multicast invocation stops at the first handler that throws.
		foreach (var handler in Delegate.EnumerateInvocationList(frameStarting))
		{
			try
			{
				handler(this, _lastTimestamp);
			}
			catch (Exception e) when (e is not (OutOfMemoryException or StackOverflowException or AccessViolationException))
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error("A compositor frame driver threw; the frame is still recorded.", e);
				}
			}
		}
	}
}
