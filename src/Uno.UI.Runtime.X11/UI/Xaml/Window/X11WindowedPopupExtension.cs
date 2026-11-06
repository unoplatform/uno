#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.UI.Xaml;
using Uno.Foundation.Logging;
using Uno.UI.Hosting;
using Uno.UI.Xaml.Controls;
using Windows.Graphics;

namespace Uno.UI.Runtime.X11;

internal sealed class X11WindowedPopupExtension : IWindowedPopupExtension
{
	private const string NET_WORKAREA = "_NET_WORKAREA";
	private const string NET_CURRENT_DESKTOP = "_NET_CURRENT_DESKTOP";

	private static readonly Lazy<IntPtr> _queryDisplay = new(() => XLib.XOpenDisplay(IntPtr.Zero));
	private const long MonitorCacheLifetimeMilliseconds = 1000;

	private static bool? _isShapeAvailable;
	private static bool _hasLoggedUnsupported;

	// Asked about on every layout of an open popup. No RandR event is selected, hence the lifetime.
	private static volatile MonitorCache? _monitorCache;

	private sealed record MonitorCache(List<RectInt32> Monitors, List<RectInt32> WorkAreas, RectInt32 Screen, long Timestamp);

	internal static X11WindowedPopupExtension Instance { get; } = new();

	internal static void InvalidateMonitorCache() => _monitorCache = null;

	private static X11XamlRootHost? GetOwner(XamlRoot xamlRoot) => XamlRootMap.GetHostForRoot(xamlRoot) as X11XamlRootHost;

	public bool IsEnabledByDefault => true;

	public bool IsSupported(XamlRoot xamlRoot)
	{
		if (GetOwner(xamlRoot) is not { Closed.IsCompleted: false } owner)
		{
			return false;
		}

		if (!WindowedPopupGraphics.CanReplayOnOwnContext(owner.GraphicsContextKind))
		{
			LogUnsupported($"{owner.GraphicsContextKind} frames can't be replayed on a popup's window");
			return false;
		}

		if (!owner.IsXI2Available)
		{
			LogUnsupported("XInput 2.2 isn't available");
			return false;
		}

		return IsShapeAvailable();
	}

	// Every popup asks on every open.
	private void LogUnsupported(string reason)
	{
		if (!_hasLoggedUnsupported)
		{
			_hasLoggedUnsupported = true;
			this.LogWarn()?.Warn($"Popups stay inside their window: {reason}.");
		}
	}

	public IWindowedPopupSite? TryCreateSite(XamlRoot xamlRoot, IWindowedPopupContent content)
		=> GetOwner(xamlRoot) is { } owner ? X11WindowedPopupSite.TryCreate(owner, content) : null;

	public PointInt32 GetScreenOffset(XamlRoot xamlRoot)
		=> GetOwner(xamlRoot) is { Closed.IsCompleted: false } owner ? owner.GetClientScreenOrigin() : default;

	// There's no system setting: WinUI's failure case, the left-handed menu.
	public bool IsRightHanded => false;

	public bool TryGetMonitorBounds(XamlRoot xamlRoot, PointInt32 screenPoint, out RectInt32 monitor, out RectInt32 workArea)
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
			var cache = _monitorCache;
			if (cache is null || Environment.TickCount64 - cache.Timestamp > MonitorCacheLifetimeMilliseconds)
			{
				_monitorCache = cache = QueryMonitors(display);
			}

			if (!X11MonitorMath.TryFindNearest(cache.Monitors, screenPoint, out monitor))
			{
				// No RandR, or no active CRTC: the screen is the only monitor there is.
				monitor = cache.Screen;
			}

			workArea = X11MonitorMath.GetWorkArea(monitor, cache.WorkAreas);
			return true;
		}
		catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
		{
			this.LogWarn()?.Warn($"The monitors can't be queried: {e.Message}");
			return false;
		}
	}

	private static MonitorCache QueryMonitors(IntPtr display)
	{
		using var lockDisposable = X11Helper.XLock(display);

		var root = XLib.XDefaultRootWindow(display);
		XWindowAttributes attributes = default;
		_ = XLib.XGetWindowAttributes(display, root, ref attributes);

		return new MonitorCache(
			GetMonitors(display, root),
			GetWorkAreas(display, root),
			new RectInt32(0, 0, attributes.width, attributes.height),
			Environment.TickCount64);
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

		// A window manager can publish -1 when no desktop is current.
		var desktop = ReadCardinals(display, root, NET_CURRENT_DESKTOP) is { Length: > 0 } current ? Math.Max((int)current[0], 0) : 0;

		// Mutter publishes one per monitor. _NET_WORKAREA has one per desktop, spanning every monitor.
		var perMonitor = ReadCardinals(display, root, "_GTK_WORKAREAS_D" + desktop.ToString(CultureInfo.InvariantCulture));
		if (perMonitor is { Length: >= 4 })
		{
			for (var i = 0; i + 3 < perMonitor.Length; i += 4)
			{
				workAreas.Add(new RectInt32((int)perMonitor[i], (int)perMonitor[i + 1], (int)perMonitor[i + 2], (int)perMonitor[i + 3]));
			}
		}
		else if (ReadCardinals(display, root, NET_WORKAREA) is { } perDesktop && desktop < perDesktop.Length / 4)
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
