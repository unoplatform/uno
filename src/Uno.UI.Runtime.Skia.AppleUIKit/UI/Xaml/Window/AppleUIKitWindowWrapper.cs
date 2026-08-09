#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using CoreGraphics;
using Foundation;
using Microsoft.UI.Xaml;
using UIKit;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia.AppleUIKit;
using Uno.UI.Runtime.Skia.AppleUIKit.UI.Xaml;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.UI.Core;
using Windows.UI.ViewManagement;

namespace Uno.UI.Xaml.Controls;

internal class NativeWindowWrapper : NativeWindowWrapperBase
{
	private static readonly object _visibilityGate = new();
	private static int _visibleWindowCount;

	private readonly RootViewController _mainController;
	private readonly DisplayInformation _displayInformation;
	private readonly InputPane _inputPane;
	private readonly XamlRoot _xamlRoot;
	private readonly CompositeDisposable _subscriptions = new();

	// Captured at construction: ContentHostOverride is ambient static state that is only valid
	// while the window is being created, but it is needed again when the scene connects.
	private readonly bool _isAlcHosted;
	private readonly bool _requiresScene;

	private AppleUIKitWindow? _nativeWindow;
	private bool _isPendingShow;
	private bool _pendingShowActivate;
	private bool _isCountedVisible;
	private bool _isSceneDisconnected;

	public NativeWindowWrapper(Window window, XamlRoot xamlRoot) : base(window, xamlRoot)
	{
		_xamlRoot = xamlRoot;
		_isAlcHosted = Window.ContentHostOverride is not null;
		_requiresScene = UnoUISceneDelegate.HasSceneManifest && !_isAlcHosted;

		_mainController = new RootViewController();
		_mainController.SetXamlRoot(xamlRoot);
		XamlRootMap.Register(xamlRoot, _mainController);
		_mainController.View!.BackgroundColor = UIColor.Clear;
		_mainController.NavigationBarHidden = true;

		_inputPane = InputPane.GetForCurrentView();

#if !__TVOS__
		var keyboardWillShow = UIKeyboard.Notifications.ObserveWillShow(OnKeyboardWillShow);
		var keyboardWillHide = UIKeyboard.Notifications.ObserveWillHide(OnKeyboardWillHide);
		_subscriptions.Add(Disposable.Create(() =>
		{
			keyboardWillShow.Dispose();
			keyboardWillHide.Dispose();
		}));
#endif

		_displayInformation = DisplayInformation.GetForCurrentViewSafe() ?? throw new InvalidOperationException("DisplayInformation must be available when the window is initialized");
		_displayInformation.DpiChanged += OnDpiChanged;
		_subscriptions.Add(Disposable.Create(() => _displayInformation.DpiChanged -= OnDpiChanged));
		DispatchDpiChanged();

		if (_requiresScene)
		{
			// The scene supplies the native window later; seed a placeholder size so the first
			// layout pass has something to measure against.
			Bounds = new Rect(default, new Size(InitialWidth, InitialHeight));
		}
		else
		{
			if (!UnoUISceneDelegate.HasSceneManifest)
			{
				Instance ??= this;
			}

			// Must run after _mainController exists - SetNativeWindow starts observing it.
			SetNativeWindow(new AppleUIKitWindow());
		}
	}

	/// <summary>
	/// Gets the wrapper driving app lifecycle for apps that do not use the scene lifecycle, where a
	/// single window owns the whole app.
	/// </summary>
	internal static NativeWindowWrapper? Instance { get; private set; }

	/// <summary>
	/// Gets a value indicating whether this window is backed by its own scene. ALC-hosted windows
	/// render into their host's window, so they must never own or request one.
	/// </summary>
	internal bool RequiresScene => _requiresScene;

	public override AppleUIKitWindow? NativeWindow => _nativeWindow;

	internal RootViewController MainController => _mainController;

	[MemberNotNull(nameof(_nativeWindow))]
	internal void SetNativeWindow(AppleUIKitWindow nativeWindow)
	{
		_nativeWindow = nativeWindow;

#if __MACCATALYST__
		_nativeWindow.SetOwner(CoreWindow.GetForCurrentThreadSafe());
#endif

		// Must run synchronously with UnoUIApplicationDelegate.FinishedLaunching for the initial
		// window, otherwise a black screen may appear before the first frame is drawn. ALC-hosted
		// windows are skipped because this calls MakeKeyAndVisible eagerly, which would cover the
		// host's own window.
		if (!_isAlcHosted)
		{
			NativeWindowHelpers.TryCreateExtendedSplashScreen(_nativeWindow);
		}

		ObserveOrientationAndSize();

		if (_isPendingShow)
		{
			_isPendingShow = false;
			base.Show(_pendingShowActivate);
		}
	}

	public override void Show(bool activateWindow)
	{
		if (_nativeWindow is null)
		{
			// Under the scene lifecycle the native window only exists once UIKit connects the
			// scene. Defer so IsVisible and Shown are not raised before anything is on screen.
			_isPendingShow = true;
			_pendingShowActivate = activateWindow;
			return;
		}

		base.Show(activateWindow);
	}

	protected override void ShowCore()
	{
		var nativeWindow = _nativeWindow;

		if (nativeWindow is null)
		{
			return;
		}

		if (_xamlRoot.Content is FrameworkElement { IsLoaded: false } fe)
		{
			void OnLoaded(object sender, object args)
			{
				fe.Loaded -= OnLoaded;

				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"ShowCore: Root loaded");
				}

				NativeWindowHelpers.TransitionFromSplashScreen(nativeWindow, _mainController);
			}

			fe.Loaded += OnLoaded;
		}
		else
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"ShowCore: Root already loaded");
			}

			NativeWindowHelpers.TransitionFromSplashScreen(nativeWindow, _mainController);
		}
	}

	protected override void CloseCore()
	{
		MarkHidden();

		if (!_isSceneDisconnected &&
			Window != Microsoft.UI.Xaml.Window.InitialWindow &&
			_nativeWindow?.WindowScene?.Session is { } session)
		{
			// A secondary window must also tear down its scene, otherwise the OS keeps showing it
			// in the app switcher after the XAML window is gone.
			UIApplication.SharedApplication.RequestSceneSessionDestruction(session, null, null);
		}

		SceneWindowRegistry.Remove(this);
		_subscriptions.Dispose();

		base.CloseCore();
	}

	internal void OnSceneEnteredForeground()
	{
		if (MarkVisible())
		{
			// The first window returning to the foreground drives the app-level events.
			Application.Current?.RaiseResuming();
			Application.Current?.RaiseLeavingBackground(() => OnNativeVisibilityChanged(true));
		}
		else
		{
			OnNativeVisibilityChanged(true);
		}
	}

	internal void OnSceneEnteredBackground()
	{
		OnNativeVisibilityChanged(false);

		if (MarkHidden())
		{
			Application.Current?.RaiseEnteredBackground(() => Application.Current?.RaiseSuspending());
		}
	}

	internal void OnSceneActivationChanged(CoreWindowActivationState state) => OnNativeActivated(state);

	internal void OnSceneDisconnected()
	{
		_isSceneDisconnected = true;

		MarkHidden();
		OnNativeVisibilityChanged(false);
		OnNativeClosed();

		Close();

		XamlRootMap.Unregister(_xamlRoot);
		_nativeWindow = null;
	}

	internal void OnNativeVisibilityChanged(bool visible) => IsVisible = visible;

	internal void OnNativeActivated(CoreWindowActivationState state) => ActivationState = state;

	internal void OnNativeClosed() => RaiseClosing();

	/// <returns><see langword="true"/> when this window became the first visible one.</returns>
	private bool MarkVisible()
	{
		lock (_visibilityGate)
		{
			if (_isCountedVisible)
			{
				return false;
			}

			_isCountedVisible = true;
			return ++_visibleWindowCount == 1;
		}
	}

	/// <returns><see langword="true"/> when this window was the last visible one.</returns>
	private bool MarkHidden()
	{
		lock (_visibilityGate)
		{
			if (!_isCountedVisible)
			{
				return false;
			}

			_isCountedVisible = false;
			return --_visibleWindowCount == 0;
		}
	}

	private void OnDpiChanged(DisplayInformation sender, object args) => DispatchDpiChanged();

	private void DispatchDpiChanged() =>
		RasterizationScale = (float)_displayInformation.RawPixelsPerViewPixel;

	internal void RaiseNativeSizeChanged()
	{
		if (_nativeWindow is null)
		{
			return;
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
			return;
		}

		var nativeWindow = _nativeWindow;

#if !__TVOS__
		var orientationRegistration = UIApplication
			.Notifications
			.ObserveDidChangeStatusBarOrientation((sender, args) => RaiseNativeSizeChanged());

		var statusBarFrameRegistration = UIApplication
			.Notifications
			.ObserveDidChangeStatusBarFrame((sender, args) => RaiseNativeSizeChanged());

		_subscriptions.Add(Disposable.Create(() =>
		{
			orientationRegistration.Dispose();
			statusBarFrameRegistration.Dispose();
		}));
#endif

		void OnFrameChanged() => RaiseNativeSizeChanged();
		nativeWindow.FrameChanged += OnFrameChanged;
		_subscriptions.Add(Disposable.Create(() => nativeWindow.FrameChanged -= OnFrameChanged));

		void OnVisibleBoundsChanged() => RaiseNativeSizeChanged();
		_mainController.VisibleBoundsChanged += OnVisibleBoundsChanged;
		_subscriptions.Add(Disposable.Create(() => _mainController.VisibleBoundsChanged -= OnVisibleBoundsChanged));

		var statusBar = StatusBar.GetForCurrentView();
		void OnStatusBarVisibilityChanged(StatusBar sender, object args) => RaiseNativeSizeChanged();
		statusBar.Showing += OnStatusBarVisibilityChanged;
		statusBar.Hiding += OnStatusBarVisibilityChanged;
		_subscriptions.Add(Disposable.Create(() =>
		{
			statusBar.Showing -= OnStatusBarVisibilityChanged;
			statusBar.Hiding -= OnStatusBarVisibilityChanged;
		}));

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
