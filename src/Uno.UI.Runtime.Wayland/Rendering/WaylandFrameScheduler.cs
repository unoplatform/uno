#nullable enable

using System;
using Uno.UI.Runtime.Wayland.Protocols;

namespace Uno.UI.Runtime.Wayland.Rendering;

// All calls run on the render owner thread; frame callbacks arrive on the dispatch thread and are posted back to it.
// Compositors withhold frame callbacks while a surface is hidden, and no frame is produced then.
internal sealed class WaylandFrameScheduler : IDisposable
{
	private readonly WlSurface _surface;
	private readonly Func<bool> _render;
	private readonly Action? _frameOpportunity;
	private readonly Action<Action> _postToOwner;
	private WlCallback? _callback;
	private bool _dirty;
	private bool _disposed;

	public WaylandFrameScheduler(WlSurface surface, Func<bool> render, Action<Action> postToOwner, Action? frameOpportunity = null)
	{
		_surface = surface;
		_render = render;
		_frameOpportunity = frameOpportunity;
		_postToOwner = postToOwner;
	}

	public void Invalidate()
	{
		if (_disposed)
		{
			return;
		}
		_dirty = true;
		TryRender();
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
			if (!_render())
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
		CancelCallback();
	}
}
