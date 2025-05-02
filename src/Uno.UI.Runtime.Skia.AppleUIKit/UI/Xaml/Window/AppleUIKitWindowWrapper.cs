using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using CoreGraphics;
using Foundation;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using UIKit;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Runtime.Skia.AppleUIKit;
using Uno.UI.Runtime.Skia.AppleUIKit.UI.Xaml;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.UI.Core;
using Windows.UI.ViewManagement;

namespace Uno.UI.Xaml.Controls;

internal class NativeWindowWrapper : NativeWindowWrapperBase
{
	private AppleUIKitWindow? _nativeWindow;

	private RootViewController _mainController;
	private readonly DisplayInformation _displayInformation;
	private InputPane _inputPane;
	private XamlRoot _xamlRoot;
	private bool _isPendingShow;

#if !__TVOS__
	private NSObject? _orientationRegistration;
#endif

	public NativeWindowWrapper(Window window, XamlRoot xamlRoot) : base(window, xamlRoot)
	{
		if (!UnoUISceneDelegate.HasSceneManifest())
		{
			SetNativeWindow(new AppleUIKitWindow());
		}
		else
		{
			Bounds = new Rect(default, new Windows.Foundation.Size(1025, 768));
		}

		_mainController = new RootViewController();
		_mainController.SetXamlRoot(xamlRoot);
		_xamlRoot = xamlRoot;
		XamlRootMap.Register(xamlRoot, _mainController);
		_mainController.View!.BackgroundColor = UIColor.Clear;
		_mainController.NavigationBarHidden = true;

		// This method needs to be called synchronously with `UnoSkiaAppDelegate.FinishedLaunching`
		// otherwise, a black screen may appear. 
		NativeWindowHelpers.TryCreateExtendedSplashScreen(_nativeWindow);

		_inputPane = InputPane.GetForCurrentView();

#if !__TVOS__
		UIKeyboard.Notifications.ObserveWillShow(OnKeyboardWillShow);
		UIKeyboard.Notifications.ObserveWillHide(OnKeyboardWillHide);
#endif

		_displayInformation = DisplayInformation.GetForCurrentViewSafe() ?? throw new InvalidOperationException("DisplayInformation must be available when the window is initialized");
		_displayInformation.DpiChanged += (s, e) => DispatchDpiChanged();
		DispatchDpiChanged();

		AwaitingScene.Enqueue(this);
	}

	[MemberNotNull(nameof(_nativeWindow))]
	internal void SetNativeWindow(AppleUIKitWindow nativeWindow)
	{
		_nativeWindow = nativeWindow;

#if __MACCATALYST__
		_nativeWindow.SetOwner(CoreWindow.GetForCurrentThreadSafe());
#endif

		// This method needs to be called synchronously with `UnoSkiaAppDelegate.FinishedLaunching`
		// otherwise, a black screen may appear. 
		TryCreateExtendedSplashscreen();

		ObserveOrientationAndSize();

		if (_isPendingShow)
		{
			ShowCore();
		}
	}

	public static Queue<NativeWindowWrapper> AwaitingScene { get; } = new();

	public override AppleUIKitWindow? NativeWindow => _nativeWindow;

	private void DispatchDpiChanged() =>
		RasterizationScale = (float)_displayInformation.RawPixelsPerViewPixel;

	protected override void ShowCore()
	{
		if (_nativeWindow is null)
		{
			// In case of scene delegate apps, the native window
			// may be created after Show is called.
			_isPendingShow = true;
			return;
		}

		_isPendingShow = false;

		if (_xamlRoot.Content is FrameworkElement { IsLoaded: false } fe)
		{
			void OnLoaded(object sender, object args)
			{
				if (this.Log().IsDebugEnabled())
				{
					this.Log().Debug($"ShowCore: Root loaded");
				}

				NativeWindowHelpers.TransitionFromSplashScreen(_nativeWindow, _mainController);
			}

			fe.Loaded += OnLoaded;
		}
		else
		{
			if (this.Log().IsDebugEnabled())
			{
				this.Log().Debug($"ShowCore: Root already loaded");
			}

			NativeWindowHelpers.TransitionFromSplashScreen(_nativeWindow, _mainController);
		}
	}

	internal RootViewController MainController => _mainController;

	internal void OnNativeVisibilityChanged(bool visible) => IsVisible = visible;

	internal void OnNativeActivated(CoreWindowActivationState state) => ActivationState = state;

	internal void OnNativeClosed() => RaiseClosing(); // TODO: Handle closing when multiwindow #13847

	internal void RaiseNativeSizeChanged()
	{
		if (_nativeWindow is null)
		{
			throw new InvalidOperationException("Native window is not set.");
		}

		var newWindowSize = GetWindowSize();

		SetBoundsAndVisibleBounds(new Rect(default, newWindowSize), GetVisibleBounds(_nativeWindow, newWindowSize));
		var size = new Windows.Graphics.SizeInt32((int)(newWindowSize.Width * RasterizationScale), (int)(newWindowSize.Height * RasterizationScale));
		SetSizes(size, size);
	}

	private void ObserveOrientationAndSize()
	{
		if (_nativeWindow is null)
		{
			throw new InvalidOperationException("Native window is not set.");
		}

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

	internal Size GetWindowSize()
	{
		var nativeFrame = NativeWindow?.Frame ?? CGRect.Empty;

		return new Size(nativeFrame.Width, nativeFrame.Height);
	}

	private Rect GetVisibleBounds(UIKit.UIWindow keyWindow, Windows.Foundation.Size windowSize)
	{
		var windowBounds = new Windows.Foundation.Rect(default, windowSize);

		var inset = UseSafeAreaInsets
				? keyWindow.SafeAreaInsets
				: UIEdgeInsets.Zero;

#if !__TVOS__
		// Not respecting its own documentation. https://developer.apple.com/documentation/uikit/uiview/2891103-safeareainsets?language=objc
		// iOS returns all zeros for SafeAreaInsets on non-iPhones and iOS11. (ignoring nav bars or status bars)
		// So we need to update the top inset depending of the status bar visibility on other devices
#pragma warning disable CA1422 // Validate platform compatibility
		var statusBarHeight = UIApplication.SharedApplication.StatusBarHidden
				? 0
				: UIApplication.SharedApplication.StatusBarFrame.Size.Height;
#pragma warning restore CA1422 // Validate platform compatibility
#else
		var statusBarHeight = 0;
#endif

		inset.Top = (nfloat)Math.Max(inset.Top, statusBarHeight);

		var newVisibleBounds = new Windows.Foundation.Rect(
			x: windowBounds.Left + inset.Left,
			y: windowBounds.Top + inset.Top,
			width: windowBounds.Width - inset.Right - inset.Left,
			height: windowBounds.Height - inset.Top - inset.Bottom
		);

		return newVisibleBounds;
	}

	private static bool UseSafeAreaInsets => UIDevice.CurrentDevice.CheckSystemVersion(11, 0);

#if !__TVOS__
	private void OnKeyboardWillShow(object? sender, UIKeyboardEventArgs e)
	{
		try
		{
			if (e.Notification.UserInfo is null)
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug("[OnKeyboardWillShow] Notification UserInfo was null");
				}

				return;
			}

			_inputPane.OccludedRect = ((NSValue?)e.Notification.UserInfo.ObjectForKey(UIKeyboard.FrameEndUserInfoKey))?.CGRectValue ?? default;
		}
		catch (Exception ex)
		{
			// The app must not crash if any managed exception happens in the
			// native callback
			Application.Current.RaiseRecoverableUnhandledException(ex);
		}
	}

	private void OnKeyboardWillHide(object? sender, UIKeyboardEventArgs e)
	{
		try
		{
			_inputPane.OccludedRect = new Rect(0, 0, 0, 0);
		}
		catch (Exception ex)
		{
			// The app must not crash if any managed exception happens in the
			// native callback
			Application.Current.RaiseRecoverableUnhandledException(ex);
		}
	}
#endif

	protected override IDisposable ApplyFullScreenPresenter()
	{
#if !__TVOS__
		CoreDispatcher.CheckThreadAccess();
#pragma warning disable CA1422 // Validate platform compatibility
		UIApplication.SharedApplication.StatusBarHidden = true;
		return Disposable.Create(() => UIApplication.SharedApplication.StatusBarHidden = false);
#pragma warning restore CA1422 // Validate platform compatibility
#else
		return Disposable.Empty;
#endif
	}
}
