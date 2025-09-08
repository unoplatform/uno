using System;
using System.Numerics;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.Disposables;
using Uno.Extensions;
using Uno.UI.Hosting;
using Uno.UI.NativeElementHosting;
using Uno.UI.Runtime.Skia;
namespace Uno.WinUI.Runtime.Skia.X11;

// https://www.x.org/releases/X11R7.6/doc/xextproto/shape.html
// Thanks to Jörg Seebohn for providing an example on how to use X SHAPE
// https://gist.github.com/je-so/903479/834dfd78705b16ec5f7bbd10925980ace4049e17
internal partial class X11NativeElementHostingExtension : ContentPresenter.INativeElementHostingExtension
{
	private Rect? _lastArrangeRect;
	private Rect? _lastClipRect;
	private bool _layoutDirty = true;
	private readonly ContentPresenter _presenter;

	private readonly Lazy<IntPtr> _display;
	private IntPtr Display => _display.Value;

	public X11NativeElementHostingExtension(ContentPresenter contentPresenter)
	{
		_presenter = contentPresenter;

		// _presenter.XamlRoot might not be set at the time of construction, so we need to make this lazy.
		// This implicitly means that we expect that the XamlRoot must be set by the time we need to do any
		// X11 calls (like attaching).
		_display = new Lazy<IntPtr>(
			() => ((X11XamlRootHost)XamlRootMap.GetHostForRoot(_presenter.XamlRoot!)!).RootX11Window.Display);
	}

	private XamlRoot? XamlRoot => _presenter.XamlRoot;

	public bool IsNativeElement(object content) => content is X11NativeWindow;

	public void AttachNativeElement(object content)
	{
		if (content is X11NativeWindow nativeWindow
			&& XamlRoot is { } xamlRoot
			&& XamlRootMap.GetHostForRoot(xamlRoot) is X11XamlRootHost host)
		{
			using var lockDiposable = X11Helper.XLock(Display);

			host.AttachSubWindow(nativeWindow.WindowId);
			_ = XLib.XMapWindow(Display, nativeWindow.WindowId);
			_ = X11Helper.XRaiseWindow(Display, host.TopX11Window.Window);
			host.RegisterInputFromNativeSubwindow(nativeWindow.WindowId);

			HideWindowFromTaskBar(nativeWindow);

			xamlRoot.RenderInvalidated += UpdateLayout;
			xamlRoot.QueueInvalidateRender(); // to force initial layout and clipping
		}
		else
		{
			throw new InvalidOperationException($"{nameof(AttachNativeElement)} called in an invalid state.");
		}
	}

	private unsafe void HideWindowFromTaskBar(X11NativeWindow nativeWindow)
	{
		// hide the embedded window from showing up in the taskbar/dock.
		var netWmStateAtom = X11Helper.GetAtom(Display, "_NET_WM_STATE");
		var netWmStateSkipTaskbarAtom = X11Helper.GetAtom(Display, "_NET_WM_STATE_SKIP_TASKBAR");
		if (netWmStateAtom != IntPtr.Zero && netWmStateSkipTaskbarAtom != IntPtr.Zero)
		{
			_ = XLib.XGetWindowProperty(
				Display,
				nativeWindow.WindowId,
				netWmStateAtom,
				IntPtr.Zero,
				X11Helper.LONG_LENGTH,
				false,
				X11Helper.AnyPropertyType,
				out var actualTypeAtom,
				out var actualFormat,
				out var nitems,
				out _,
				out var prop);
			using var atomsDisposable = new DisposableStruct<IntPtr>(static p => { _ = XLib.XFree(p); }, prop);

			if (actualFormat == 32)
			{
				var atomSpan = new Span<IntPtr>(prop.ToPointer(), (int)nitems);

				var foundSkipTaskbarAtom = false;
				foreach (var atom in atomSpan)
				{
					if (atom == netWmStateSkipTaskbarAtom)
					{
						foundSkipTaskbarAtom = true;
						break;
					}
				}

				if (!foundSkipTaskbarAtom)
				{
					_ = XLib.XChangeProperty(
						Display,
						nativeWindow.WindowId,
						netWmStateAtom,
						X11Helper.GetAtom(Display, X11Helper.XA_ATOM),
						32,
						PropertyMode.Append,
						new[]
						{
							X11Helper.GetAtom(Display, "_NET_WM_STATE_SKIP_TASKBAR")
						},
						1);
				}
			}
		}
	}

	public void DetachNativeElement(object content)
	{
		if (content is X11NativeWindow nativeWindow
			&& XamlRoot is { } xamlRoot
			&& XamlRootMap.GetHostForRoot(xamlRoot) is X11XamlRootHost host)
		{
			using var lockDiposable = X11Helper.XLock(Display);
			host.UnregisterInputFromNativeSubwindow(nativeWindow.WindowId);
			_ = XLib.XQueryTree(Display, nativeWindow.WindowId, out IntPtr root, out _, out var children, out _);
			_ = XLib.XFree(children);
			_ = X11Helper.XReparentWindow(Display, nativeWindow.WindowId, root, 0, 0);
			_ = XLib.XUnmapWindow(Display, nativeWindow.WindowId);
			_ = XLib.XSync(Display, false);

			_lastClipRect = null;
			_lastArrangeRect = null;

			xamlRoot.RenderInvalidated -= UpdateLayout;
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
		XamlRoot?.QueueInvalidateRender();
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
			XamlRootMap.GetHostForRoot(xamlRoot) is X11XamlRootHost host)
		{
			using var lockDiposable = X11Helper.XLock(Display);
			if (arrangeRect.Width <= 0 || arrangeRect.Height <= 0)
			{
				arrangeRect.Size = new Size(1, 1);
			}
			else
			{
				var scale = XamlRoot.GetDisplayInformation(xamlRoot).RawPixelsPerViewPixel;
				arrangeRect = Matrix3x2.CreateScale((float)scale).Transform(arrangeRect);
			}
			_ = XLib.XResizeWindow(Display, nativeWindow.WindowId, (int)arrangeRect.Width, (int)arrangeRect.Height);
			_ = X11Helper.XMoveWindow(Display, nativeWindow.WindowId, (int)arrangeRect.X, (int)arrangeRect.Y);

			XLib.XSync(Display, false);
		}
	}

	public Size MeasureNativeElement(object content, Size childMeasuredSize, Size availableSize) => availableSize;

	public void ChangeNativeElementVisibility(object content, bool visible)
	{
		// no need to do anything here, airspace clipping logic will take care of it automatically
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
