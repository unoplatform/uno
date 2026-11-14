#nullable enable

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using Uno.UI.Runtime.Wayland.Protocols;
using Uno.UI.Runtime.Wayland.Rendering;
using Windows.ApplicationModel.DataTransfer.DragDrop.Core;

namespace Uno.UI.Runtime.Wayland;

// BGRA8 premultiplied pixels of the drag visual and the icon's top-left corner relative to the pointer, in logical pixels.
internal sealed record WaylandDragIconImage(int Width, int Height, byte[] Pixels, (double X, double Y)? Anchor);

// The wl_surface shown under the pointer during an outgoing drag: the snapshot Uno's DragView would show,
// or a generic icon when none was rendered yet. Created and disposed on the dispatch thread.
internal sealed class WaylandDragIcon : IDisposable
{
	private readonly WlSurface _surface;
	private readonly WpViewport? _viewport;
	private readonly WaylandShmBuffer _buffer;
	private readonly WaylandDragIconLayout _layout;
	private bool _disposed;

	private WaylandDragIcon(WlSurface surface, WpViewport? viewport, WaylandShmBuffer buffer, WaylandDragIconLayout layout)
	{
		_surface = surface;
		_viewport = viewport;
		_buffer = buffer;
		_layout = layout;
	}

	internal WlSurface Surface => _surface;

	// Uno renders the drag visual during a later render pass; the drag waits briefly for it.
	private static readonly TimeSpan _captureTimeout = TimeSpan.FromMilliseconds(150);

	/// <summary>UI thread: copies the bitmap Uno rendered for the drag visual, if its pixels become available in time.</summary>
	internal static async Task<WaylandDragIconImage?> CaptureAsync(CoreDragInfo info)
	{
		if (info.DragUI is not DragUI { Content: RenderTargetBitmap bitmap } ui)
		{
			return null;
		}
		var deadline = Environment.TickCount64 + (long)_captureTimeout.TotalMilliseconds;
		while (bitmap.PixelWidth <= 0 || bitmap.PixelHeight <= 0)
		{
			if (Environment.TickCount64 >= deadline)
			{
				return null;
			}
			await Task.Delay(16);
		}
		var pixels = (await bitmap.GetPixelsAsync()).ToArray();
		var (width, height) = (bitmap.PixelWidth, bitmap.PixelHeight);
		return pixels.Length < width * height * 4 ? null : new(width, height, pixels, ui.Anchor is { } anchor ? (anchor.X, anchor.Y) : null);
	}

	/// <summary>Dispatch thread: creates the surface without a buffer; it is attached by <see cref="Present"/> once it has the icon role.</summary>
	internal static WaylandDragIcon Create(WaylandConnection connection, WaylandDragIconImage? image, double scale)
	{
		var hasViewport = connection.Viewporter is not null;
		var layout = image is null
			? WaylandDragIconLayout.Compute((int)Math.Round(WaylandDragIconPixels.FallbackLogicalSize * scale), (int)Math.Round(WaylandDragIconPixels.FallbackLogicalSize * scale), scale, (-8, -8), hasViewport)
			: WaylandDragIconLayout.Compute(image.Width, image.Height, scale, image.Anchor, hasViewport);
		var surface = connection.Compositor.CreateSurface();
		WpViewport? viewport = null;
		WaylandShmBuffer? buffer = null;
		try
		{
			viewport = connection.Viewporter?.GetViewport(surface);
			buffer = new WaylandShmBuffer(connection.Shm, layout.BufferWidth, layout.BufferHeight, released: null);
			var pixels = new byte[buffer.Length];
			WaylandDragIconPixels.Render(pixels, layout.BufferWidth, layout.BufferHeight, image);
			Marshal.Copy(pixels, 0, buffer.Memory, pixels.Length);
			return new WaylandDragIcon(surface, viewport, buffer, layout);
		}
		catch
		{
			buffer?.Dispose();
			viewport?.Dispose();
			surface.Dispose();
			throw;
		}
	}

	/// <summary>Dispatch thread, after wl_data_device.start_drag gave the surface its role.</summary>
	internal void Present()
	{
		if (_disposed)
		{
			return;
		}
		if (_surface.Version >= 3)
		{
			_surface.SetBufferScale(_layout.BufferScale);
		}
		_viewport?.SetDestination(_layout.LogicalWidth, _layout.LogicalHeight);
		// The offset places the icon relative to the hotspot; attach carried it before wl_surface v5.
		if (_surface.Version >= 5)
		{
			_surface.Offset(_layout.OffsetX, _layout.OffsetY);
			_surface.Attach(_buffer.Proxy, 0, 0);
		}
		else
		{
			_surface.Attach(_buffer.Proxy, _layout.OffsetX, _layout.OffsetY);
		}
		_surface.Damage(0, 0, int.MaxValue, int.MaxValue);
		_surface.Commit();
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		_disposed = true;
		_viewport?.Dispose();
		_surface.Dispose();
		_buffer.Dispose();
	}
}
