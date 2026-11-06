#nullable enable

using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Uno.Foundation.Logging;
using Uno.UI.Hosting;
using Uno.UI.Xaml.Controls;
using Windows.Graphics;

namespace Uno.UI.Runtime.MacOS;

internal sealed class MacOSWindowedPopupExtension : IWindowedPopupExtension
{
	private static readonly Lazy<bool> _hasNativeSupport = new(ProbeNativeSupport);

	internal static MacOSWindowedPopupExtension Instance { get; } = new();

	private static MacOSWindowHost? GetOwner(XamlRoot xamlRoot) => XamlRootMap.GetHostForRoot(xamlRoot) as MacOSWindowHost;

	// The native side hasn't been run on a real Mac yet.
	public bool IsEnabledByDefault => false;

	public bool IsSupported(XamlRoot xamlRoot)
		=> _hasNativeSupport.Value && GetOwner(xamlRoot) is { } owner && WindowedPopupGraphics.CanReplayOnOwnContext(owner.GraphicsContextKind);

	public IWindowedPopupSite? TryCreateSite(XamlRoot xamlRoot, IWindowedPopupContent content)
		=> GetOwner(xamlRoot) is { } owner ? MacOSWindowedPopupSite.TryCreate(owner, content) : null;

	public PointInt32 GetScreenOffset(XamlRoot xamlRoot)
	{
		if (GetOwner(xamlRoot) is not { } owner)
		{
			return default;
		}

		var scale = owner.RasterizationScale;
		NativeUno.uno_window_convert_local_to_screen(owner.NativeHandle, 0, 0, out var x, out var y);
		return new PointInt32((int)Math.Round(x * scale), (int)Math.Round(y * scale));
	}

	// There's no system setting: WinUI's failure case, the left-handed menu.
	public bool IsRightHanded => false;

	public bool TryGetMonitorBounds(XamlRoot xamlRoot, PointInt32 screenPoint, out RectInt32 monitor, out RectInt32 workArea)
	{
		monitor = default;
		workArea = default;

		if (!_hasNativeSupport.Value || GetOwner(xamlRoot) is not { } owner)
		{
			return false;
		}

		var scale = owner.RasterizationScale;
		if (!NativeUno.uno_screen_get_bounds(
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
			typeof(MacOSWindowedPopupExtension).LogWarn()?.Warn($"Probing for windowed popups failed: {e.Message}");
		}

		typeof(MacOSWindowedPopupExtension).LogWarn()?.Warn("libUnoNativeMac has no support for windowed popups, so popups stay inside their window.");
		return false;
	}
}
