using System;
using CoreGraphics;
using Foundation;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using UIKit;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Controls;
using Windows.Foundation;
using Windows.Graphics;
using Windows.Graphics;
using Windows.Graphics.Display;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using static Microsoft.UI.Xaml.Controls.Primitives.LoopingSelectorItem;
using MUXWindow = Microsoft.UI.Xaml.Window;
using NativeWindow = Uno.UI.Controls.Window;

namespace Uno.UI.Xaml.Controls;

internal class NativeWindowWrapper : NativeWindowWrapperBase, INativeWindowWrapper
{
	private NativeWindow _nativeWindow;

	private RootViewController _mainController;
#if !__TVOS__
	private NSObject _orientationRegistration;
#endif
	private readonly DisplayInformation _displayInformation;

	public NativeWindowWrapper(MUXWindow window, XamlRoot xamlRoot) : base(window, xamlRoot)
	{
		_nativeWindow = new NativeWindow();

		_mainController = MUXWindow.ViewControllerGenerator?.Invoke() ?? new RootViewController();
		_mainController.SetXamlRoot(xamlRoot);
		_mainController.View.BackgroundColor = UIColor.Clear;
		_mainController.NavigationBarHidden = true;

		ObserveOrientationAndSize();

		NativeWindowHelpers.TryCreateExtendedSplashScreen(_nativeWindow);

		SubscribeBackgroundNotifications();

#if __MACCATALYST__
		_nativeWindow.SetOwner(CoreWindow.GetForCurrentThreadSafe());
#endif

		_displayInformation = DisplayInformation.GetForCurrentViewSafe() ?? throw new InvalidOperationException("DisplayInformation must be available when the window is initialized");
		_displayInformation.DpiChanged += (s, e) => DispatchDpiChanged();
		DispatchDpiChanged();
	}

	public override NativeWindow NativeWindow => _nativeWindow;

	private void DispatchDpiChanged() =>
		RasterizationScale = (float)_displayInformation.RawPixelsPerViewPixel;

	protected override void ShowCore() => NativeWindowHelpers.TransitionFromSplashScreen(_nativeWindow, _mainController);

	internal RootViewController MainController => _mainController;

	internal void OnNativeVisibilityChanged(bool visible) => IsVisible = visible;

	internal void OnNativeClosed() => RaiseClosing(); // TODO: Handle closing cancellation when multiwindow is supported #13847

	internal void RaiseNativeSizeChanged()
	{
		var newWindowSize = GetWindowSize();

		SetBoundsAndVisibleBounds(new Rect(default, newWindowSize), GetVisibleBounds(_nativeWindow, newWindowSize));
		var size = new Windows.Graphics.SizeInt32((int)(newWindowSize.Width * RasterizationScale), (int)(newWindowSize.Height * RasterizationScale));
		SetSizes(size, size);
	}

	private void ObserveOrientationAndSize()
	{
#if !__TVOS__
		_orientationRegistration = UIApplication
			.Notifications
			.ObserveDidChangeStatusBarOrientation(
				(sender, args) => RaiseNativeSizeChanged()
			);

		_orientationRegistration = UIApplication
			.Notifications
			.ObserveDidChangeStatusBarFrame(
				(sender, args) => RaiseNativeSizeChanged()
			);
#endif

		_nativeWindow.FrameChanged +=
			() => RaiseNativeSizeChanged();

		_mainController.VisibleBoundsChanged +=
			() => RaiseNativeSizeChanged();

		var statusBar = StatusBar.GetForCurrentView();
		statusBar.Showing += (o, e) => RaiseNativeSizeChanged();
		statusBar.Hiding += (o, e) => RaiseNativeSizeChanged();

		RaiseNativeSizeChanged();
	}

	public override Size GetWindowSize()
	{
		var nativeFrame = NativeWindow?.Frame ?? CGRect.Empty;

		return new Size(nativeFrame.Width, nativeFrame.Height);
	}

	private Rect GetVisibleBounds(UIWindow keyWindow, Size windowSize)
	{
		var windowBounds = new Rect(default, windowSize);

		var inset = UseSafeAreaInsets
				? keyWindow.SafeAreaInsets
				: UIEdgeInsets.Zero;

#if !__TVOS__
		// Not respecting its own documentation. https://developer.apple.com/documentation/uikit/uiview/2891103-safeareainsets?language=objc
		// iOS returns all zeros for SafeAreaInsets on non-iPhones and iOS11. (ignoring nav bars or status bars)
		// So we need to update the top inset depending of the status bar visibility on other devices
		var statusBarHeight = UIApplication.SharedApplication.StatusBarHidden
				? 0
				: UIApplication.SharedApplication.StatusBarFrame.Size.Height;
#else
		var statusBarHeight = 0;
#endif

		inset.Top = (nfloat)Math.Max(inset.Top, statusBarHeight);

		var newVisibleBounds = new Rect(
			x: windowBounds.Left + inset.Left,
			y: windowBounds.Top + inset.Top,
			width: windowBounds.Width - inset.Right - inset.Left,
			height: windowBounds.Height - inset.Top - inset.Bottom
		);

		return newVisibleBounds;
	}

	private static bool UseSafeAreaInsets => UIDevice.CurrentDevice.CheckSystemVersion(11, 0);

#if !__TVOS__
	protected override IDisposable ApplyFullScreenPresenter()
	{
		CoreDispatcher.CheckThreadAccess();
		UIApplication.SharedApplication.StatusBarHidden = true;
		return Disposable.Create(() => UIApplication.SharedApplication.StatusBarHidden = false);
	}
#endif

	private void SubscribeBackgroundNotifications()
	{
		if (UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
		{
			NSNotificationCenter.DefaultCenter.AddObserver(UIScene.DidEnterBackgroundNotification, OnEnteredBackground);
			NSNotificationCenter.DefaultCenter.AddObserver(UIScene.WillEnterForegroundNotification, OnLeavingBackground);
			NSNotificationCenter.DefaultCenter.AddObserver(UIScene.DidActivateNotification, OnActivated);
			NSNotificationCenter.DefaultCenter.AddObserver(UIScene.WillDeactivateNotification, OnDeactivated);
		}
		else
		{
			NSNotificationCenter.DefaultCenter.AddObserver(UIApplication.DidEnterBackgroundNotification, OnEnteredBackground);
			NSNotificationCenter.DefaultCenter.AddObserver(UIApplication.WillEnterForegroundNotification, OnLeavingBackground);
			NSNotificationCenter.DefaultCenter.AddObserver(UIApplication.DidBecomeActiveNotification, OnActivated);
			NSNotificationCenter.DefaultCenter.AddObserver(UIApplication.WillResignActiveNotification, OnDeactivated);
		}
	}

	private void OnEnteredBackground(NSNotification notification)
	{
		OnNativeVisibilityChanged(false);

		Application.Current.RaiseEnteredBackground(() => Application.Current.RaiseSuspending());
	}

	private void OnLeavingBackground(NSNotification notification)
	{
		Application.Current.RaiseResuming();
		Application.Current.RaiseLeavingBackground(() => OnNativeVisibilityChanged(true));
	}

	private void OnActivated(NSNotification notification) => ActivationState = CoreWindowActivationState.CodeActivated;

	private void OnDeactivated(NSNotification notification) => ActivationState = CoreWindowActivationState.Deactivated;
}
