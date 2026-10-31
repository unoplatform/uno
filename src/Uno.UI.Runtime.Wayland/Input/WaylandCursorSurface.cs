#nullable enable

using System;
using Uno.UI.Runtime.Wayland.Native;
using Uno.UI.Runtime.Wayland.Protocols;

namespace Uno.UI.Runtime.Wayland.Input;

internal sealed class WaylandCursorSurface : IWaylandCursorSurface
{
	private const uint WlSurfaceAttachOpcode = 1;
	private readonly WlPointer _pointer;
	private readonly WlSurface _surface;
	private bool _disposed;

	public WaylandCursorSurface(WlPointer pointer, WlSurface surface)
	{
		_pointer = pointer;
		_surface = surface;
	}

	public int GetScale(double scale) => _surface.Version >= 3 ? Math.Clamp((int)Math.Ceiling(scale), 1, 8) : 1;

	public void Show(uint serial, nint buffer, int width, int height, int hotspotX, int hotspotY, int scale)
	{
		if (_disposed)
		{
			return;
		}
		_pointer.SetCursor(serial, _surface, hotspotX, hotspotY);
		if (_surface.Version >= 3)
		{
			_surface.SetBufferScale(scale);
		}
		AttachBorrowed(buffer);
		_surface.Damage(0, 0, int.MaxValue, int.MaxValue);
		_surface.Commit();
	}

	public void Hide(uint serial)
	{
		if (_disposed)
		{
			return;
		}
		_pointer.SetCursor(serial, null, 0, 0);
		_surface.Attach(null, 0, 0);
		_surface.Commit();
	}

	// libwayland-cursor owns the theme's wl_buffer proxies, so marshal the raw pointer without wrapping it.
	private unsafe void AttachBorrowed(nint buffer)
	{
		using (_surface.Display.Lock())
		{
			using var args = new RequestArguments(3);
			args.Pointer[0].Pointer = buffer;
			_surface.MarshalRequest(WlSurfaceAttachOpcode, null, _surface.Version, 1, false, args);
		}
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		_disposed = true;
		_surface.Attach(null, 0, 0);
		_surface.Commit();
		_surface.Dispose();
	}
}
