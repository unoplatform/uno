#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Uno.Foundation.Logging;
using Uno.UI.Composition.Drawing;
using Uno.UI.Dispatching;
using Windows.Graphics;

namespace Uno.UI.Runtime.X11;

internal partial class X11XamlRootHost
{
	internal const int PopupGrabXI2Mask =
		(1 << (int)XiEventType.XI_ButtonPress) |
		(1 << (int)XiEventType.XI_ButtonRelease) |
		(1 << (int)XiEventType.XI_Motion) |
		(1 << (int)XiEventType.XI_Enter) |
		(1 << (int)XiEventType.XI_Leave);

	// Read by the event thread, which has to tell a popup's events from those of a hosted native window.
	private readonly ConcurrentDictionary<IntPtr, X11WindowedPopupSite> _popupSitesByWindow = new();

	// In stacking order. UI thread only.
	private readonly List<X11WindowedPopupSite> _popupSites = new();

	// The exit hook only knows the open hosts, and a closed host's sites can still be waiting for the UI thread.
	private static readonly ConcurrentDictionary<X11WindowedPopupSite, byte> _livePopupSites = new();

	private readonly Lock _clientScreenOriginGate = new();
	private PointInt32? _clientScreenOrigin;
	private int _clientScreenOriginVersion;

	internal GraphicsContextKind GraphicsContextKind { get; private set; }

	internal bool IsXI2Available => GetXI2Details(TopX11Window.Display).version >= XIVersion.XI2_2;

	/// <summary>The master pointer holding a mouse button down. Guarded by the top connection's lock.</summary>
	internal int? PressedMouseDevice => _pointerSource?.PressedMouseDevice;

	internal bool IsPopupWindow(IntPtr window) => _popupSitesByWindow.ContainsKey(window);

	// Popups ask on every frame, and each answer from the server is a round trip.
	internal PointInt32 GetClientScreenOrigin()
	{
		int version;
		lock (_clientScreenOriginGate)
		{
			if (_clientScreenOrigin is { } cached)
			{
				return cached;
			}

			version = _clientScreenOriginVersion;
		}

		var display = TopX11Window.Display;
		PointInt32 origin;
		using (X11Helper.XLock(display))
		{
			_ = XLib.XTranslateCoordinates(display, TopX11Window.Window, XLib.XDefaultRootWindow(display), 0, 0, out var x, out var y, out _);
			origin = new PointInt32(x, y);
		}

		lock (_clientScreenOriginGate)
		{
			// The window may have moved during the round trip, which makes this answer one to not keep.
			if (version == _clientScreenOriginVersion)
			{
				_clientScreenOrigin = origin;
			}
		}

		return origin;
	}

	// ICCCM 4.1.5: a window manager moving a client's frame sends the client a synthetic ConfigureNotify.
	private void InvalidateClientScreenOrigin()
	{
		lock (_clientScreenOriginGate)
		{
			_clientScreenOriginVersion++;
			_clientScreenOrigin = null;
		}
	}

	internal void AddPopupSite(X11WindowedPopupSite site)
	{
		var display = TopX11Window.Display;
		using (X11Helper.XLock(display))
		{
			SetXIEventMask(display, site.Window, XI2Mask | XI2_2Mask);
			_ = XLib.XSelectInput(display, site.Window, (IntPtr)EventMask.ExposureMask);
			_pointerSource?.ApplyCursor(site.Window);
			_ = XLib.XFlush(display);
		}

		_popupSitesByWindow[site.Window] = site;
		_popupSites.Add(site);
		_livePopupSites[site] = 0;
	}

	internal void RemovePopupSite(X11WindowedPopupSite site)
	{
		_livePopupSites.TryRemove(site, out _);
		_popupSites.Remove(site);

		foreach (var (window, candidate) in _popupSitesByWindow)
		{
			if (ReferenceEquals(candidate, site))
			{
				_popupSitesByWindow.TryRemove(window, out _);
			}
		}
	}

	internal void ForEachPopupWindow(Action<IntPtr> action)
	{
		foreach (var window in _popupSitesByWindow.Keys)
		{
			action(window);
		}
	}

	// A popup's window has no owner on the server: moving, raising, hiding and closing the owner are all up to it.

	private void OnConfigureForPopups()
	{
		foreach (var site in _popupSites.ToArray())
		{
			site.OnOwnerBoundsChanged();
		}

		RestackPopupSites();
	}

	/// <summary>Puts the windows above the owner, in the order they opened, <paramref name="topmost"/> last.</summary>
	internal void RestackPopupSites(X11WindowedPopupSite? topmost = null)
	{
		if (topmost is not null && _popupSites.Remove(topmost))
		{
			_popupSites.Add(topmost);
		}

		if (_popupSites.Count == 0 || Closed.IsCompleted)
		{
			return;
		}

		// A hidden window is stacked when it's shown, and the frame takes a round trip per level of the tree.
		IntPtr? sibling = null;
		foreach (var site in _popupSites)
		{
			if (site.IsShown)
			{
				sibling ??= GetOwnerFrame();
				site.StackAbove(sibling.Value);
				sibling = site.Window;
			}
		}
	}

	internal void ReleasePopupPointerCaptures()
	{
		foreach (var site in _popupSitesByWindow.Values)
		{
			site.ReleasePointerCapture();
		}
	}

	// A window manager raises the owner on a press, which would bury the popups.
	internal void QueueRestackPopupSitesForPress()
	{
		if (!_popupSitesByWindow.IsEmpty)
		{
			QueueAction(this, () => RestackPopupSites());
		}
	}

	private void QueueOwnerMappedForPopups(bool isMapped)
	{
		if (!_popupSitesByWindow.IsEmpty)
		{
			QueueAction(this, () =>
			{
				foreach (var site in _popupSites)
				{
					site.SetOwnerMapped(isMapped);
				}

				RestackPopupSites();
			});
		}
	}

	// The window manager's frame around the owner, the only ancestor which is a sibling of the popups' windows.
	private IntPtr GetOwnerFrame()
	{
		var display = RootX11Window.Display;
		using var lockDisposable = X11Helper.XLock(display);

		var current = RootX11Window.Window;
		while (XLib.XQueryTree(display, current, out var root, out var parent, out var children, out _) != 0)
		{
			_ = XLib.XFree(children);
			if (parent == root || parent == IntPtr.Zero)
			{
				break;
			}

			current = parent;
		}

		return current;
	}

	/// <summary>A GL driver's libraries are torn down at exit, under whatever thread is still drawing with them.</summary>
	internal static void StopRenderingForProcessExit()
	{
		foreach (var site in _livePopupSites.Keys)
		{
			site.StopRenderThread();
		}

		X11XamlRootHost[] hosts;
		lock (_x11WindowToXamlRootHostMutex)
		{
			hosts = [.. _x11WindowToXamlRootHost.Values];
		}

		foreach (var host in hosts)
		{
			host._renderLoopRunning = false;
			host._renderRequested.Set();
			host._renderThread.Join(TimeSpan.FromSeconds(2));
		}
	}

	private void ClosePopupSites()
	{
		foreach (var site in _popupSites.ToArray())
		{
			try
			{
				site.Dispose();
			}
			catch (Exception e)
			{
				this.LogError()?.Error("Failed to tear down a popup window.", e);
			}
		}
	}
}
