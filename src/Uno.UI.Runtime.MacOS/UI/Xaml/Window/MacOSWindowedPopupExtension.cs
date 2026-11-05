#nullable enable

using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Uno.Foundation.Logging;
using Uno.UI.Composition.Drawing;
using Uno.UI.Hosting;
using Uno.UI.Xaml.Controls;
using Windows.Graphics;

namespace Uno.UI.Runtime.MacOS;

internal sealed class MacOSWindowedPopupExtension : IWindowedPopupExtension
{
	private static readonly Lazy<bool> _hasNativeSupport = new(ProbeNativeSupport);

	// A monitor is asked about in the physical pixels of the window that's placing a popup, right after asking for its offset.
	private double _lastOwnerScale = 1;

	internal static MacOSWindowedPopupExtension Instance { get; } = new();

	private static MacOSWindowHost? GetOwner(XamlRoot xamlRoot) => XamlRootMap.GetHostForRoot(xamlRoot) as MacOSWindowHost;

	public bool IsSupported(XamlRoot xamlRoot)
		// A popup window's frames replay on a context of its own: only the Skia kinds have recordings that can.
		=> _hasNativeSupport.Value && GetOwner(xamlRoot) is { GraphicsContextKind: GraphicsContextKind.Metal or GraphicsContextKind.Software };

	public IWindowedPopupSite? TryCreateSite(XamlRoot xamlRoot, IWindowedPopupContent content)
		=> GetOwner(xamlRoot) is { } owner ? MacOSWindowedPopupSite.TryCreate(owner, content) : null;

	public PointInt32 GetScreenOffset(XamlRoot xamlRoot)
	{
		if (GetOwner(xamlRoot) is not { } owner)
		{
			return default;
		}

		var scale = _lastOwnerScale = owner.RasterizationScale;
		NativeUno.uno_window_convert_local_to_screen(owner.NativeHandle, 0, 0, out var x, out var y);
		return new PointInt32((int)Math.Round(x * scale), (int)Math.Round(y * scale));
	}

	public bool TryGetMonitorBounds(PointInt32 screenPoint, out RectInt32 monitor, out RectInt32 workArea)
	{
		monitor = default;
		workArea = default;

		var scale = _lastOwnerScale;
		if (!_hasNativeSupport.Value
			|| !NativeUno.uno_screen_get_bounds(
				screenPoint.X / scale,
				screenPoint.Y / scale,
				out var mx, out var my, out var mw, out var mh,
				out var wx, out var wy, out var ww, out var wh))
		{
			return false;
		}

		monitor = ToPhysical(mx, my, mw, mh, scale);
		workArea = ToPhysical(wx, wy, ww, wh, scale);
		return true;
	}

	private static RectInt32 ToPhysical(double x, double y, double width, double height, double scale)
		=> new((int)Math.Round(x * scale), (int)Math.Round(y * scale), (int)Math.Round(width * scale), (int)Math.Round(height * scale));

	// The dylib is built separately from the managed host, which can run against one from before these exports.
	private static bool ProbeNativeSupport()
	{
		try
		{
			if (NativeLibrary.TryLoad("libUnoNativeMac.dylib", typeof(MacOSWindowedPopupExtension).Assembly, null, out var library)
				&& NativeLibrary.TryGetExport(library, nameof(NativeUno.uno_popup_window_create), out _)
				&& NativeLibrary.TryGetExport(library, nameof(NativeUno.uno_screen_get_bounds), out _))
			{
				return true;
			}
		}
		catch (Exception e)
		{
			typeof(MacOSWindowedPopupExtension).LogDebug()?.Debug($"Probing for windowed popups failed: {e.Message}");
		}

		typeof(MacOSWindowedPopupExtension).LogWarn()?.Warn("libUnoNativeMac has no support for windowed popups, so popups stay inside their window.");
		return false;
	}
}
