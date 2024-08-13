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
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Windowing;
using Uno.Disposables;
using Windows.Graphics;
using Windows.Graphics.Display;
using Size = Windows.Foundation.Size;

namespace Uno.UI.Xaml.Controls;

internal class NativeWindowWrapper : NativeWindowWrapperBase
{
	private static readonly Lazy<NativeWindowWrapper> _instance = new(() => new NativeWindowWrapper());

	private readonly DisplayInformation _displayInformation;

	private Uno.UI.Controls.Window _nativeWindow;

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
			_nativeWindow = new Uno.UI.Controls.Window(rect, style, NSBackingStore.Buffered, false);
		}
		else
		{
			var rect = new CoreGraphics.CGRect(100, 100, 1024, 768);
			_nativeWindow = new Uno.UI.Controls.Window(rect, style, NSBackingStore.Buffered, false);
		}

		_mainController = Microsoft.UI.Xaml.Window.ViewControllerGenerator?.Invoke() ?? new RootViewController();

		ObserveOrientationAndSize();

		_displayInformation = DisplayInformation.GetForCurrentViewSafe() ?? throw new InvalidOperationException("DisplayInformation must be available when the window is initialized");
		_displayInformation.DpiChanged += (s, e) => DispatchDpiChanged();
		DispatchDpiChanged();
	}

	public override string Title
	{
		get => IsKeyWindowInitialized() ? NSApplication.SharedApplication.KeyWindow.Title : base.Title;
		set
		{
			if (IsKeyWindowInitialized())
			{
				NSApplication.SharedApplication.KeyWindow.Title = value;
			}

			base.Title = value;
		}
	}

	private bool IsKeyWindowInitialized() => NSApplication.SharedApplication.KeyWindow != null;

	internal static NativeWindowWrapper Instance => _instance.Value;

	public override Uno.UI.Controls.Window NativeWindow => _nativeWindow;

	internal RootViewController MainController => _mainController;

	private void DispatchDpiChanged() =>
		RasterizationScale = (float)_displayInformation.RawPixelsPerViewPixel;

	protected override void ShowCore()
	{
		_nativeWindow.ContentViewController = _mainController;
		_nativeWindow.Display();
		_nativeWindow.MakeKeyAndOrderFront(NSApplication.SharedApplication);
	}

	internal Size GetWindowSize()
	{
		var applicationFrameSize = NSScreen.MainScreen.VisibleFrame;
		return new CGSize(applicationFrameSize.Width, applicationFrameSize.Height);
	}

	internal void OnNativeVisibilityChanged(bool visible) => IsVisible = visible;

	internal void OnNativeActivated(CoreWindowActivationState state) => ActivationState = state;

	internal AppWindowClosingEventArgs OnNativeClosing() => RaiseClosing();

	internal void RaiseNativeSizeChanged()
	{
		var newWindowSize = new Size(_nativeWindow.Frame.Width, _nativeWindow.Frame.Height);
		Bounds = new Rect(default, newWindowSize);
		VisibleBounds = Bounds;
		Size = newWindowSize.ToSizeInt32();
	}

	private void ObserveOrientationAndSize()
	{
		_windowResizeNotificationObject = NSNotificationCenter.DefaultCenter.AddObserver(
			new NSString("NSWindowDidResizeNotification"), ResizeObserver, null);

		RaiseNativeSizeChanged();
	}

	private void ResizeObserver(NSNotification obj) => RaiseNativeSizeChanged();

	protected override IDisposable ApplyFullScreenPresenter()
	{
		NSApplication.SharedApplication.KeyWindow.ToggleFullScreen(null);
		return Disposable.Create(() => NSApplication.SharedApplication.KeyWindow.ToggleFullScreen(null));
	}
}
