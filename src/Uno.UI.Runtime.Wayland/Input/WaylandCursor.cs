#nullable enable

using System;
using System.Runtime.InteropServices;
using Windows.UI.Core;
using Uno.UI.Runtime.Wayland.Protocols;

namespace Uno.UI.Runtime.Wayland.Input;

internal interface IWaylandCursorSurface : IDisposable
{
	int GetScale(double scale);
	// wl_pointer.set_cursor(serial, surface, hotspot), attach, damage, commit.
	void Show(uint serial, nint buffer, int width, int height, int hotspotX, int hotspotY, int scale);
	void Hide(uint serial);
}

internal sealed unsafe partial class WaylandCursor : IDisposable
{
	private readonly IWaylandCursorSurface _surface;
	private readonly WpCursorShapeDeviceV1? _shapeDevice;
	private readonly nint _shm;
	private nint _theme;
	private int _scale;
	private bool _disposed;

	public WaylandCursor(nint shm, IWaylandCursorSurface surface, WpCursorShapeDeviceV1? shapeDevice)
	{
		_shm = shm;
		_surface = surface;
		_shapeDevice = shapeDevice;
	}

	public void Set(uint serial, CoreCursor? cursor, double scale)
	{
		if (_disposed)
		{
			return;
		}
		if (cursor is null)
		{
			Trace($"cursor: hidden serial={serial}");
			_surface.Hide(serial);
			return;
		}
		var (shape, name) = cursor.Type switch
		{
			CoreCursorType.Cross => (8u, "crosshair"),
			CoreCursorType.Hand => (4u, "pointer"),
			CoreCursorType.Help => (3u, "help"),
			CoreCursorType.IBeam => (9u, "text"),
			CoreCursorType.SizeAll => (13u, "move"),
			CoreCursorType.SizeNortheastSouthwest => (28u, "nesw-resize"),
			CoreCursorType.SizeNorthSouth => (27u, "ns-resize"),
			CoreCursorType.SizeNorthwestSoutheast => (29u, "nwse-resize"),
			CoreCursorType.SizeWestEast => (26u, "ew-resize"),
			CoreCursorType.UniversalNo => (15u, "not-allowed"),
			CoreCursorType.Wait => (6u, "wait"),
			CoreCursorType.UpArrow => (1u, "up-arrow"),
			_ => (1u, "default")
		};
		if (_shapeDevice is { } device)
		{
			Trace($"cursor: {cursor.Type} -> cursor-shape-v1 shape {shape} serial={serial}");
			device.SetShape(serial, shape);
			return;
		}
		var integerScale = _surface.GetScale(scale);
		if (_theme == 0 || integerScale != _scale)
		{
			var theme = ThemeLoad(Environment.GetEnvironmentVariable("XCURSOR_THEME"), 24 * integerScale, _shm);
			if (theme == 0)
			{
				return;
			}
			// The surface adapter must detach the previous buffer before destroying its theme.
			_surface.Hide(serial);
			if (_theme != 0)
			{
				ThemeDestroy(_theme);
			}
			_theme = theme;
			_scale = integerScale;
		}
		var nativeCursor = ThemeGetCursor(_theme, name);
		if (nativeCursor == 0)
		{
			nativeCursor = ThemeGetCursor(_theme, "left_ptr");
		}
		if (nativeCursor == 0)
		{
			return;
		}
		var images = (Cursor*)nativeCursor;
		if (images->Count == 0)
		{
			return;
		}
		var image = (Image*)images->Images[0];
		var buffer = ImageGetBuffer((nint)image);
		if (buffer != 0)
		{
			Trace($"cursor: {cursor.Type} -> theme cursor {name} {image->Width}x{image->Height} scale={_scale} serial={serial}");
			_surface.Show(serial, buffer, checked((int)image->Width), checked((int)image->Height), (int)image->HotspotX / _scale, (int)image->HotspotY / _scale, _scale);
		}
	}
	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		_disposed = true;
		_shapeDevice?.Dispose();
		_surface.Dispose();
		if (_theme != 0)
		{
			ThemeDestroy(_theme);
			_theme = 0;
		}
	}
	private static void Trace(FormattableString message)
	{
		if (WaylandDiagnostics.IsEnabled)
		{
			WaylandDiagnostics.Trace(FormattableString.Invariant(message));
		}
	}
	[StructLayout(LayoutKind.Sequential)]
	private struct Cursor
	{
		public uint Count; public nint* Images; public nint Name;
	}
	[StructLayout(LayoutKind.Sequential)]
	private struct Image
	{
		public uint Width, Height, HotspotX, HotspotY, Delay;
	}
	[LibraryImport("libwayland-cursor.so.0", EntryPoint = "wl_cursor_theme_load", StringMarshalling = StringMarshalling.Utf8)] private static partial nint ThemeLoad(string? name, int size, nint shm);
	[LibraryImport("libwayland-cursor.so.0", EntryPoint = "wl_cursor_theme_destroy")] private static partial void ThemeDestroy(nint theme);
	[LibraryImport("libwayland-cursor.so.0", EntryPoint = "wl_cursor_theme_get_cursor", StringMarshalling = StringMarshalling.Utf8)] private static partial nint ThemeGetCursor(nint theme, string name);
	[LibraryImport("libwayland-cursor.so.0", EntryPoint = "wl_cursor_image_get_buffer")] private static partial nint ImageGetBuffer(nint image);
}
