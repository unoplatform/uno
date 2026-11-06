#nullable enable

using Microsoft.UI.Xaml;
using Uno.UI.Composition.Drawing;
using Uno.UI.Hosting;
using Uno.UI.Xaml.Controls;
using Windows.Graphics;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Uno.UI.Runtime.Win32;

internal sealed class Win32WindowedPopupExtension : IWindowedPopupExtension
{
	internal static Win32WindowedPopupExtension Instance { get; } = new();

	private static Win32WindowWrapper? GetOwner(XamlRoot xamlRoot) => XamlRootMap.GetHostForRoot(xamlRoot) as Win32WindowWrapper;

	public bool IsSupported(XamlRoot xamlRoot)
		// A popup window's frames replay on a context of its own: only the Skia kinds have recordings that can.
		=> GetOwner(xamlRoot) is { GraphicsContextKind: GraphicsContextKind.Vulkan or GraphicsContextKind.OpenGL or GraphicsContextKind.Software };

	public IWindowedPopupSite? TryCreateSite(XamlRoot xamlRoot, IWindowedPopupContent content)
		=> GetOwner(xamlRoot) is { } owner ? Win32WindowedPopupSite.TryCreate(owner, content) : null;

	public PointInt32 GetScreenOffset(XamlRoot xamlRoot)
	{
		if (GetOwner(xamlRoot) is not { } owner)
		{
			return default;
		}

		var origin = default(System.Drawing.Point);
		return PInvoke.ClientToScreen(owner.Hwnd, ref origin) ? new PointInt32(origin.X, origin.Y) : default;
	}

	public unsafe bool IsRightHanded
	{
		get
		{
			const uint SPI_GETHANDEDNESS = 0x1024;
			const uint HANDEDNESS_RIGHT = 1;

			uint handedness = 0;
			return PInvoke.SystemParametersInfo((SYSTEM_PARAMETERS_INFO_ACTION)SPI_GETHANDEDNESS, 0, &handedness, 0)
				&& handedness == HANDEDNESS_RIGHT;
		}
	}

	public bool TryGetMonitorBounds(PointInt32 screenPoint, out RectInt32 monitor, out RectInt32 workArea)
	{
		monitor = default;
		workArea = default;

		var hMonitor = PInvoke.MonitorFromPoint(new System.Drawing.Point(screenPoint.X, screenPoint.Y), MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);
		if (hMonitor == default)
		{
			return false;
		}

		var info = new MONITORINFO { cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<MONITORINFO>() };
		if (!PInvoke.GetMonitorInfo(hMonitor, ref info))
		{
			return false;
		}

		monitor = new RectInt32(info.rcMonitor.X, info.rcMonitor.Y, info.rcMonitor.Width, info.rcMonitor.Height);
		workArea = new RectInt32(info.rcWork.X, info.rcWork.Y, info.rcWork.Width, info.rcWork.Height);
		return true;
	}
}
