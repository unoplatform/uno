using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.Runtime.Skia;
namespace Uno.WinUI.Runtime.Skia.X11;

// https://www.x.org/releases/X11R7.6/doc/xextproto/shape.html
// Thanks to Jörg Seebohn for providing an example on how to use X SHAPE
// https://gist.github.com/je-so/903479/834dfd78705b16ec5f7bbd10925980ace4049e17
internal partial class X11NativeElementHostingExtension : ContentPresenter.INativeElementHostingExtension
{
	private static Dictionary<X11XamlRootHost, HashSet<X11NativeElementHostingExtension>> _hostToNativeElementHosts = new();
	private Rect? _lastFinalRect;
	private Rect? _lastArrangeRect;
	private Rect? _lastClipRect;
	private bool _layoutDirty = true;
	private bool? _xShapesPresent;
	private readonly ContentPresenter _presenter;
	private readonly IntPtr _display;

	public X11NativeElementHostingExtension(ContentPresenter contentPresenter)
	{
		_presenter = contentPresenter;
		_display = ((X11XamlRootHost)X11Manager.XamlRootMap.GetHostForRoot(_presenter.XamlRoot!)!).RootX11Window.Display;
	}

	private XamlRoot? XamlRoot => _presenter.XamlRoot;

	internal static IEnumerable<XRectangle> GetNativeElementRects(X11XamlRootHost host)
	{
		if (_hostToNativeElementHosts.TryGetValue(host, out var set))
		{
			foreach (var hostingExtension in set)
			{
				if (hostingExtension._lastFinalRect is { } rect)
				{
					yield return new XRectangle
					{
						X = (short)rect.X,
						Y = (short)rect.Y,
						H = (short)rect.Height,
						W = (short)rect.Width
					};
				}
			}
		}
	}

	public bool IsNativeElement(object content)
	{
		if (content is not X11NativeWindow nativeWindow)
		{
			return false;
		}

		using var _1 = X11Helper.XLock(_display);

		var _3 = XLib.XQueryTree(_display, XLib.XDefaultRootWindow(_display), out IntPtr root, out _, out var children, out _);
		var _4 = XLib.XFree(children);

		// _NET_CLIENT_LIST only identifies top-level windows, not subwindows.
		var status = XLib.XGetWindowProperty(
			_display,
			root,
			X11Helper.GetAtom(_display, X11Helper._NET_CLIENT_LIST),
			0,
			new IntPtr(0x7fffffff),
			false,
			X11Helper.AnyPropertyType,
			out _,
			out _,
			out var length,
			out _,
			out IntPtr windowArray);

		if (status == X11Helper.Success)
		{
			unsafe
			{
				var span = new Span<IntPtr>(windowArray.ToPointer(), (int)length);
				foreach (var window in span)
				{
					if (window == nativeWindow.WindowId)
					{
						return true;
					}
				}
			}
		}

		return FindWindowById(_display, nativeWindow.WindowId, root) != IntPtr.Zero;
	}
	public void AttachNativeElement(object content)
	{
		if (content is X11NativeWindow nativeWindow
			&& XamlRoot is { } xamlRoot
			&& X11Manager.XamlRootMap.GetHostForRoot(xamlRoot) is X11XamlRootHost host)
		{
			using var _1 = X11Helper.XLock(_display);

			host.AttachSubWindow(nativeWindow.WindowId);

			using var _5 = X11Helper.XLock(_display);
			var _6 = X11Helper.XRaiseWindow(host.TopX11Window.Display, host.TopX11Window.Window);

			if (!_hostToNativeElementHosts.TryGetValue(host, out var set))
			{
				set = _hostToNativeElementHosts[host] = new HashSet<X11NativeElementHostingExtension>();
			}
			set.Add(this);

			xamlRoot.InvalidateRender += UpdateLayout;
		}
		else
		{
			throw new InvalidOperationException($"{nameof(AttachNativeElement)} called in an invalid state.");
		}
	}

	public void DetachNativeElement(object content)
	{
		if (content is X11NativeWindow nativeWindow
			&& XamlRoot is { } xamlRoot
			&& X11Manager.XamlRootMap.GetHostForRoot(xamlRoot) is X11XamlRootHost host)
		{
			using var _1 = X11Helper.XLock(_display);
			var _2 = XLib.XQueryTree(_display, nativeWindow.WindowId, out IntPtr root, out _, out var children, out _);
			var _3 = XLib.XFree(children);
			var _4 = X11Helper.XReparentWindow(_display, nativeWindow.WindowId, root, 0, 0);
			var _5 = XLib.XSync(_display, false);

			var set = _hostToNativeElementHosts[host];
			set.Remove(this);
			if (set.Count == 0)
			{
				_hostToNativeElementHosts.Remove(host);
			}

			_lastClipRect = null;
			_lastArrangeRect = null;
			_lastFinalRect = null;

			xamlRoot.InvalidateRender -= UpdateLayout;
			host.QueueUpdateTopWindowClipRect();
		}
		else
		{
			throw new InvalidOperationException($"{nameof(DetachNativeElement)} called in an invalid state.");
		}
	}

	public void ArrangeNativeElement(object content, Rect arrangeRect, Rect clipRect)
	{
		_lastArrangeRect = arrangeRect;
		_lastClipRect = clipRect;
		_layoutDirty = true;
		// we don't update the layout right now. We wait for the next render to happen, as
		// xlib calls are expensive and it's better to update the layout once at the end when multiple arrange
		// calls are fired sequentially.
	}

	private void UpdateLayout()
	{
		if (!_layoutDirty)
		{
			return;
		}
		_layoutDirty = false;
		if (_presenter.Content is X11NativeWindow nativeWindow &&
			_lastArrangeRect is { } arrangeRect &&
			_lastClipRect is { } clipRect &&
			XamlRoot is { } xamlRoot &&
			X11Manager.XamlRootMap.GetHostForRoot(xamlRoot) is X11XamlRootHost host)
		{
			using var _1 = X11Helper.XLock(_display);
			if (arrangeRect.Width <= 0 || arrangeRect.Height <= 0)
			{
				arrangeRect.Size = new Size(1, 1);
			}
			var _2 = XLib.XResizeWindow(_display, nativeWindow.WindowId, (int)arrangeRect.Width, (int)arrangeRect.Height);
			var _3 = X11Helper.XMoveWindow(_display, nativeWindow.WindowId, (int)arrangeRect.X, (int)arrangeRect.Y);

			_xShapesPresent ??= X11Helper.XShapeQueryExtension(_display, out _, out _);
			if (_xShapesPresent.Value)
			{
				var region = X11Helper.CreateRegion((short)clipRect.Left, (short)clipRect.Top, (short)clipRect.Width, (short)clipRect.Height);
				using var _4 = Disposable.Create(() =>
				{
					var _ = X11Helper.XDestroyRegion(region);
				});
				X11Helper.XShapeCombineRegion(_display, nativeWindow.WindowId, X11Helper.ShapeBounding, 0, 0, region, X11Helper.ShapeSet);
			}
			else
			{
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().Warn("Unable to clip an embedded X11 window, the X server doesn't support the X Nonrectangular Window Shape Extension Protocol.");
				}
			}

			XLib.XSync(_display, false);

			var clipInGlobalCoordinates = new Rect(
				arrangeRect.X + clipRect.X,
				arrangeRect.Y + clipRect.Y,
				clipRect.Width,
				clipRect.Height);
			_lastFinalRect = arrangeRect.IntersectWith(clipInGlobalCoordinates);

			host.QueueUpdateTopWindowClipRect();
		}
	}

	public Size MeasureNativeElement(object content, Size childMeasuredSize, Size availableSize) => availableSize;

	public void ChangeNativeElementVisibility(object content, bool visible)
	{
		if (content is X11NativeWindow nativeWindow)
		{
			if (visible)
			{
				var _3 = XLib.XMapWindow(_display, nativeWindow.WindowId);
			}
			else
			{
				var _3 = X11Helper.XUnmapWindow(_display, nativeWindow.WindowId);
			}
		}
	}

	// This doesn't seem to work as most (all?) WMs won't change the opacity for subwindows, only top-level windows
	public void ChangeNativeElementOpacity(object content, double opacity)
	{
		// if (IsNativeElementAttached(owner, content) && content is X11Window x11Window)
		// {
		// 	// The spec requires a value between 0 and max int, not 0 and 1
		// 	var actualOpacity = (IntPtr)(opacity * uint.MaxValue);
		//
		// 	// if (opacity == 1)
		// 	// {
		// 	// 	XLib.XDeleteProperty(
		// 	// 		x11Window.Display,
		// 	// 		x11Window.Window,
		// 	// 		X11Helper.GetAtom(x11Window.Display, X11Helper._NET_WM_WINDOW_OPACITY));
		// 	// }
		// 	// else
		// 	{
		// 		var tmp = new IntPtr[]
		// 		{
		// 			actualOpacity
		// 		};
		// 		XLib.XChangeProperty(
		// 			x11Window.Display,
		// 			x11Window.Window,
		// 			X11Helper.GetAtom(x11Window.Display, X11Helper._NET_WM_WINDOW_OPACITY),
		// 			X11Helper.GetAtom(x11Window.Display, X11Helper.XA_CARDINAL),
		// 			32,
		// 			PropertyMode.Replace,
		// 			actualOpacity,
		// 			1);
		// 	}
		// }
	}
}
