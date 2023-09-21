using System;
using System.Linq;
using System.Runtime.InteropServices;
using AppKit;
using CoreGraphics;
using Foundation;
using Uno.UI;
using Uno.UI.Controls;
using Uno.UI.Xaml.Core;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls;
using Size = Windows.Foundation.Size;

namespace Uno.UI.Xaml.Controls;

internal class NativeWindowWrapper : INativeWindowWrapper
{
	private static readonly Lazy<NativeWindowWrapper> _instance = new(() => new NativeWindowWrapper());

	private Uno.UI.Controls.Window _window;

	private Size _previousWindowSize = new Size(-1, -1);

	private RootViewController _mainController;
	private object _windowResizeNotificationObject;

	public NativeWindowWrapper()
	{
		var style = NSWindowStyle.Closable | NSWindowStyle.Resizable | NSWindowStyle.Titled | NSWindowStyle.Miniaturizable;

		var preferredWindowSize = ApplicationView.PreferredLaunchViewSize;
		if (preferredWindowSize != Windows.Foundation.Size.Empty)
		{
			var rect = new CoreGraphics.CGRect(100, 100, (int)preferredWindowSize.Width, (int)preferredWindowSize.Height);
			_window = new Uno.UI.Controls.Window(rect, style, NSBackingStore.Buffered, false);
		}
		else
		{
			var rect = new CoreGraphics.CGRect(100, 100, 1024, 768);
			_window = new Uno.UI.Controls.Window(rect, style, NSBackingStore.Buffered, false);
		}

		_mainController = Windows.UI.Xaml.Window.ViewControllerGenerator?.Invoke() ?? new RootViewController();

		ObserveOrientationAndSize();
	}

	internal static NativeWindowWrapper Instance => _instance.Value;

	public bool Visible { get; private set; }

	public event EventHandler<Size> SizeChanged;
	public event EventHandler<CoreWindowActivationState> ActivationChanged;
	public event EventHandler<bool> VisibilityChanged;
	public event EventHandler Closed;
	public event EventHandler Shown;

	internal NSWindow NativeWindow => _window;

	internal RootViewController MainController => _mainController;

	public void Activate() { } //TODO:MZ: Handle activation

	public void Show()
	{
		_window.ContentViewController = _mainController;
		_window.Display();
		_window.MakeKeyAndOrderFront(NSApplication.SharedApplication);
		Shown?.Invoke(this, EventArgs.Empty);
	}

	internal Size GetWindowSize()
	{
#if __IOS__
		var nativeFrame = NativeWindow?.Frame ?? CGRect.Empty;

		return new Size(nativeFrame.Width, nativeFrame.Height);
#else //TODO:MZ: Move to macOS file
		var applicationFrameSize = NSScreen.MainScreen.VisibleFrame;
		return new CGSize(applicationFrameSize.Width, applicationFrameSize.Height);
#endif
	}

	internal void OnNativeVisibilityChanged(bool visible)
	{
		Visible = visible;
		VisibilityChanged?.Invoke(this, visible);
	}

	internal void OnNativeActivated(CoreWindowActivationState state) => ActivationChanged?.Invoke(this, state);

	internal void OnNativeClosed() => Closed?.Invoke(this, EventArgs.Empty); //TODO:MZ: Handle closing

	internal void RaiseNativeSizeChanged()
	{
		var newWindowSize = new Size(_window.Frame.Width, _window.Frame.Height);

		if (_previousWindowSize != newWindowSize)
		{
			_previousWindowSize = newWindowSize;

			var applicationView = ApplicationView.GetForCurrentView();
			if (applicationView != null)
			{
				applicationView.SetCoreBounds(_window, new Rect(default, newWindowSize));
			}

			SizeChanged?.Invoke(this, newWindowSize);
		}
	}

	private void ObserveOrientationAndSize()
	{
		_windowResizeNotificationObject = NSNotificationCenter.DefaultCenter.AddObserver(
			new NSString("NSWindowDidResizeNotification"), ResizeObserver, null);

		RaiseNativeSizeChanged();
	}

	private void ResizeObserver(NSNotification obj) => RaiseNativeSizeChanged();
}
