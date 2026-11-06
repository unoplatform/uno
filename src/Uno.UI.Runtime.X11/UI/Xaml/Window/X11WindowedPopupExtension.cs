#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.UI.Xaml;
using Uno.Foundation.Logging;
using Uno.UI.Composition.Drawing;
using Uno.UI.Hosting;
using Uno.UI.Xaml.Controls;
using Windows.Graphics;

namespace Uno.UI.Runtime.X11;

internal sealed class X11WindowedPopupExtension : IWindowedPopupExtension
{
	private const string NET_WORKAREA = "_NET_WORKAREA";
	private const string NET_CURRENT_DESKTOP = "_NET_CURRENT_DESKTOP";

	private static readonly Lazy<IntPtr> _queryDisplay = new(() => XLib.XOpenDisplay(IntPtr.Zero));
	private static bool? _isShapeAvailable;

	internal static X11WindowedPopupExtension Instance { get; } = new();

	private static X11XamlRootHost? GetOwner(XamlRoot xamlRoot) => XamlRootMap.GetHostForRoot(xamlRoot) as X11XamlRootHost;

	public bool IsEnabledByDefault => true;

	public bool IsSupported(XamlRoot xamlRoot)
		// A popup window's frames replay on a context of its own: only the Skia kinds have recordings that can.
		=> GetOwner(xamlRoot) is { Closed.IsCompleted: false, GraphicsContextKind: GraphicsContextKind.Vulkan or GraphicsContextKind.OpenGL or GraphicsContextKind.OpenGLES or GraphicsContextKind.Software } owner
			&& owner.IsXI2Available
			&& IsShapeAvailable();

	public IWindowedPopupSite? TryCreateSite(XamlRoot xamlRoot, IWindowedPopupContent content)
		=> GetOwner(xamlRoot) is { } owner ? X11WindowedPopupSite.TryCreate(owner, content) : null;

	public PointInt32 GetScreenOffset(XamlRoot xamlRoot)
		=> GetOwner(xamlRoot) is { Closed.IsCompleted: false } owner ? owner.GetClientScreenOrigin() : default;

	// There's no system setting: WinUI's failure case, the left-handed menu.
	public bool IsRightHanded => false;

	public bool TryGetMonitorBounds(PointInt32 screenPoint, out RectInt32 monitor, out RectInt32 workArea)
	{
		monitor = default;
		workArea = default;

		var display = _queryDisplay.Value;
		if (display == IntPtr.Zero)
		{
			return false;
		}

		try
		{
			using var lockDisposable = X11Helper.XLock(display);

			var root = XLib.XDefaultRootWindow(display);
			if (!X11MonitorMath.TryFindNearest(GetMonitors(display, root), screenPoint, out monitor))
			{
				// No RandR, or no active CRTC: the screen is the only monitor there is.
				XWindowAttributes attributes = default;
				_ = XLib.XGetWindowAttributes(display, root, ref attributes);
				monitor = new RectInt32(0, 0, attributes.width, attributes.height);
			}

			workArea = X11MonitorMath.GetWorkArea(monitor, GetWorkAreas(display, root));
			return true;
		}
		catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
		{
			this.LogWarn()?.Warn($"The monitors can't be queried: {e.Message}");
			return false;
		}
	}

	private static bool IsShapeAvailable()
	{
		if (_isShapeAvailable is { } available)
		{
			return available;
		}

		var display = _queryDisplay.Value;
		if (display == IntPtr.Zero)
		{
			return false;
		}

		try
		{
			using var lockDisposable = X11Helper.XLock(display);
			available = X11Helper.XShapeQueryExtension(display, out _, out _);
		}
		catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
		{
			available = false;
		}

		if (!available)
		{
			typeof(X11WindowedPopupExtension).LogWarn()?.Warn("The X Shape extension isn't available, so popups stay inside their window.");
		}

		_isShapeAvailable = available;
		return available;
	}

	private static unsafe List<RectInt32> GetMonitors(IntPtr display, IntPtr root)
	{
		List<RectInt32> monitors = new();

		if (XLib.XRRQueryExtension(display, out _, out _) == 0)
		{
			return monitors;
		}

		var resources = X11Helper.XRRGetScreenResourcesCurrent(display, root);
		if (resources == null)
		{
			return monitors;
		}

		try
		{
			var crtcs = new ReadOnlySpan<IntPtr>((void*)resources->crtcs, resources->ncrtc);
			foreach (var crtc in crtcs)
			{
				var info = X11Helper.XRRGetCrtcInfo(display, new IntPtr(resources), crtc);
				if (info == null)
				{
					continue;
				}

				if (info->noutput > 0)
				{
					monitors.Add(new RectInt32(info->x, info->y, (int)info->width, (int)info->height));
				}

				X11Helper.XRRFreeCrtcInfo(info);
			}
		}
		finally
		{
			X11Helper.XRRFreeScreenResources(resources);
		}

		return monitors;
	}

	private static List<RectInt32> GetWorkAreas(IntPtr display, IntPtr root)
	{
		List<RectInt32> workAreas = new();

		var desktop = ReadCardinals(display, root, NET_CURRENT_DESKTOP) is { Length: > 0 } current ? (int)current[0] : 0;

		// Mutter publishes one per monitor. _NET_WORKAREA has one per desktop, spanning every monitor.
		var perMonitor = ReadCardinals(display, root, "_GTK_WORKAREAS_D" + desktop.ToString(CultureInfo.InvariantCulture));
		if (perMonitor is { Length: >= 4 })
		{
			for (var i = 0; i + 3 < perMonitor.Length; i += 4)
			{
				workAreas.Add(new RectInt32((int)perMonitor[i], (int)perMonitor[i + 1], (int)perMonitor[i + 2], (int)perMonitor[i + 3]));
			}
		}
		else if (ReadCardinals(display, root, NET_WORKAREA) is { } perDesktop && perDesktop.Length >= (desktop + 1) * 4)
		{
			var i = desktop * 4;
			workAreas.Add(new RectInt32((int)perDesktop[i], (int)perDesktop[i + 1], (int)perDesktop[i + 2], (int)perDesktop[i + 3]));
		}

		return workAreas;
	}

	private static unsafe nint[]? ReadCardinals(IntPtr display, IntPtr window, string name)
	{
		var atom = X11Helper.GetAtom(display, name, only_if_exists: true);
		if (atom == X11Helper.None)
		{
			return null;
		}

		_ = XLib.XGetWindowProperty(
			display,
			window,
			atom,
			0,
			X11Helper.LONG_LENGTH,
			false,
			X11Helper.GetAtom(display, X11Helper.XA_CARDINAL),
			out var actualType,
			out int actualFormat,
			out IntPtr count,
			out _,
			out IntPtr data);

		try
		{
			// Xlib hands format 32 back as an array of longs.
			return actualType != X11Helper.None && actualFormat == 32 && data != IntPtr.Zero
				? new ReadOnlySpan<nint>((void*)data, (int)count).ToArray()
				: null;
		}
		finally
		{
			if (data != IntPtr.Zero)
			{
				_ = XLib.XFree(data);
			}
		}
	}
}
