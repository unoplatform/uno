#nullable enable

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Uno.Foundation.Logging;
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

internal sealed unsafe class WaylandCursor : IDisposable
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
		if (!CursorLibrary.TryLoad())
		{
			return;
		}
		var integerScale = _surface.GetScale(scale);
		if (_theme == 0 || integerScale != _scale)
		{
			var theme = ThemeLoad(Environment.GetEnvironmentVariable("XCURSOR_THEME"), CursorSize * integerScale, _shm);
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

	// The theme size in logical pixels, as libXcursor and the toolkits read it.
	private static int CursorSize { get; } =
		int.TryParse(Environment.GetEnvironmentVariable("XCURSOR_SIZE"), NumberStyles.None, CultureInfo.InvariantCulture, out var size) && size > 0 ? size : 24;

	private static nint ThemeLoad(string? name, int size, nint shm)
	{
		var utf8 = Utf8StringMarshaller.ConvertToUnmanaged(name);
		try
		{
			return CursorLibrary.ThemeLoad(utf8, size, shm);
		}
		finally
		{
			Utf8StringMarshaller.Free(utf8);
		}
	}

	private static void ThemeDestroy(nint theme) => CursorLibrary.ThemeDestroy(theme);

	private static nint ThemeGetCursor(nint theme, string name)
	{
		var utf8 = Utf8StringMarshaller.ConvertToUnmanaged(name);
		try
		{
			return CursorLibrary.ThemeGetCursor(theme, utf8);
		}
		finally
		{
			Utf8StringMarshaller.Free(utf8);
		}
	}

	private static nint ImageGetBuffer(nint image) => CursorLibrary.ImageGetBuffer(image);

	// libwayland-cursor is only needed without cursor-shape-v1, so a system without it keeps the current cursor.
	private static class CursorLibrary
	{
		private static bool? _loaded;

		public static delegate* unmanaged[Cdecl]<byte*, int, nint, nint> ThemeLoad;
		public static delegate* unmanaged[Cdecl]<nint, void> ThemeDestroy;
		public static delegate* unmanaged[Cdecl]<nint, byte*, nint> ThemeGetCursor;
		public static delegate* unmanaged[Cdecl]<nint, nint> ImageGetBuffer;

		public static bool TryLoad()
		{
			if (_loaded is { } loaded)
			{
				return loaded;
			}
			if (NativeLibrary.TryLoad("libwayland-cursor.so.0", out var library)
				&& NativeLibrary.TryGetExport(library, "wl_cursor_theme_load", out var themeLoad)
				&& NativeLibrary.TryGetExport(library, "wl_cursor_theme_destroy", out var themeDestroy)
				&& NativeLibrary.TryGetExport(library, "wl_cursor_theme_get_cursor", out var themeGetCursor)
				&& NativeLibrary.TryGetExport(library, "wl_cursor_image_get_buffer", out var imageGetBuffer))
			{
				ThemeLoad = (delegate* unmanaged[Cdecl]<byte*, int, nint, nint>)themeLoad;
				ThemeDestroy = (delegate* unmanaged[Cdecl]<nint, void>)themeDestroy;
				ThemeGetCursor = (delegate* unmanaged[Cdecl]<nint, byte*, nint>)themeGetCursor;
				ImageGetBuffer = (delegate* unmanaged[Cdecl]<nint, nint>)imageGetBuffer;
				_loaded = true;
			}
			else
			{
				_loaded = false;
				if (typeof(WaylandCursor).Log().IsEnabled(LogLevel.Warning))
				{
					typeof(WaylandCursor).Log().Warn("libwayland-cursor.so.0 could not be loaded and the compositor lacks cursor-shape-v1; the cursor will not change.");
				}
			}
			return _loaded.Value;
		}
	}
}
