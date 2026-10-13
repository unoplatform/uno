#nullable enable

using System;
using System.Threading;
using Uno.UI.Runtime.Wayland.Protocols;

namespace Uno.UI.Runtime.Wayland.Rendering;

// All calls run on the render owner thread; frame callbacks arrive on the dispatch thread and are posted back to it.
internal sealed class WaylandFrameScheduler : IDisposable
{
	// Compositors withhold frame callbacks (and may hold buffers) while a surface is hidden. When a pending frame
	// makes no progress for this long, the stall callback lets the UI keep producing frames without presenting them.
	internal static readonly TimeSpan StallInterval = TimeSpan.FromMilliseconds(100);

	private readonly WlSurface _surface;
	private readonly Func<bool> _render;
	private readonly Action _stalled;
	private readonly Action? _frameOpportunity;
	private readonly Action<Action> _postToOwner;
	private readonly Timer _stallTimer;
	private WlCallback? _callback;
	private bool _dirty;
	private bool _stallArmed;
	private int _presented;
	private int _presentedWhenArmed;
	private bool _disposed;

	public WaylandFrameScheduler(WlSurface surface, Func<bool> render, Action stalled, Action<Action> postToOwner, Action? frameOpportunity = null)
	{
		_surface = surface;
		_render = render;
		_stalled = stalled;
		_frameOpportunity = frameOpportunity;
		_postToOwner = postToOwner;
		_stallTimer = new Timer(_ => _postToOwner(OnStallTimer));
	}

	public void Invalidate()
	{
		if (_disposed)
		{
			return;
		}
		_dirty = true;
		TryRender();
		if (_dirty)
		{
			ArmStallTimer();
		}
	}

	public void BufferAvailable() => TryRender();

	private void TryRender()
	{
		if (_disposed || !_dirty || _callback is not null)
		{
			return;
		}
		_dirty = false;
		_callback = _surface.Frame();
		_callback.Done += OnDone;
		try
		{
			if (_render())
			{
				_presented++;
			}
			else
			{
				CancelCallback();
				_dirty = true;
			}
		}
		catch
		{
			CancelCallback();
			_dirty = true;
			throw;
		}
	}

	private void ArmStallTimer()
	{
		if (!_stallArmed)
		{
			_stallArmed = true;
			_presentedWhenArmed = _presented;
			_stallTimer.Change(StallInterval, Timeout.InfiniteTimeSpan);
		}
	}

	private void OnStallTimer()
	{
		_stallArmed = false;
		if (_disposed || !_dirty)
		{
			return;
		}
		if (_presented != _presentedWhenArmed)
		{
			// Frames are still being presented; keep watching the pending one.
			ArmStallTimer();
			return;
		}
		// Not re-armed here: the UI's next invalidation does that, so an idle UI stops producing frames.
		_stalled();
		_frameOpportunity?.Invoke();
	}

	private void OnDone(object? sender, WlCallback.DoneEventArgs args)
	{
		var callback = (WlCallback)sender!;
		_postToOwner(() =>
		{
			if (_callback != callback)
			{
				return;
			}
			CancelCallback();
			TryRender();
			_frameOpportunity?.Invoke();
		});
	}

	private void CancelCallback()
	{
		if (_callback is { } callback)
		{
			_callback = null;
			callback.Done -= OnDone;
			callback.Dispose();
		}
	}

	public void Dispose()
	{
		_disposed = true;
		_stallTimer.Dispose();
		CancelCallback();
	}
}
