#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using Uno.Foundation.Logging;
using Windows.Foundation;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Windows.UI.ViewManagement;
using Uno.Helpers.Theming;
using Uno.UI.Core;
using Microsoft.UI.Windowing;
using Uno.Disposables;

#if !HAS_UNO_WINUI
using Microsoft.UI.Xaml.Controls;
#endif

#if HAS_UNO_WINUI
using WindowSizeChangedEventArgs = Microsoft.UI.Xaml.WindowSizeChangedEventArgs;
using WindowActivatedEventArgs = Microsoft.UI.Xaml.WindowActivatedEventArgs;
using Microsoft.UI.Input;
#else
using WindowSizeChangedEventArgs = Windows.UI.Core.WindowSizeChangedEventArgs;
using WindowActivatedEventArgs = Windows.UI.Core.WindowActivatedEventArgs;
#endif

namespace Uno.UI.Xaml.Controls;

internal abstract partial class BaseWindowImplementation : IWindowImplementation
{
	private CoreWindowActivationState _lastActivationState = CoreWindowActivationState.Deactivated;
	private Size _lastSize = new Size(-1, -1);

	private bool _isClosing;
	private bool _isClosed;

	private bool _contentLoaded;
	private bool _activationRequested;
	private bool _splashDismissed;

	private AppWindowClosingEventArgs? _appWindowClosingEventArgs;

#pragma warning disable CS0649
	public BaseWindowImplementation(Window window)
	{
		Window = window ?? throw new System.ArgumentNullException(nameof(window));

		ApplicationHelper.AddWindow(window);
	}

	public event WindowSizeChangedEventHandler? SizeChanged;
	public event WindowActivatedEventHandler? Activated;
	public event TypedEventHandler<object, WindowEventArgs>? Closed;
	public event WindowVisibilityChangedEventHandler? VisibilityChanged;

	protected Window Window { get; }

	public INativeWindowWrapper? NativeWindowWrapper { get; private set; }

	public abstract CoreWindow? CoreWindow { get; }

	public bool Visible => NativeWindowWrapper?.IsVisible ?? false;

	public abstract UIElement? Content { get; set; }

	public abstract XamlRoot? XamlRoot { get; }

	public string Title
	{
		get => NativeWindowWrapper!.Title;
		set => NativeWindowWrapper!.Title = value;
	}

	public Rect Bounds => NativeWindowWrapper?.Bounds ?? default;

	public object? NativeWindow => NativeWindowWrapper?.NativeWindow;

	public virtual void Initialize()
	{
		InitializeNativeWindow();
	}

	public virtual void Activate()
	{
		if (NativeWindowFactory.SupportsMultipleWindows && _isClosed)
		{
			throw new InvalidOperationException("Cannot reactivate a closed window.");
		}

		if (NativeWindowWrapper is null)
		{
			throw new InvalidOperationException("Native window is not initialized.");
		}

		if (!NativeWindowWrapper.WasShown)
		{
			SetVisibleBoundsFromNative();
		}

		_activationRequested = true;
		TryActivate();
	}

	[MemberNotNull(nameof(NativeWindowWrapper))]
	protected void InitializeNativeWindow()
	{
		if (XamlRoot is null)
		{
			throw new InvalidOperationException("XamlRoot is not initialized for this window.");
		}

		var nativeWindow = NativeWindowFactory.CreateWindow(Window, XamlRoot);

		if (nativeWindow is null)
		{
			throw new InvalidOperationException("This platform does not support creating secondary windows yet.");
		}

		nativeWindow.ActivationChanged += OnNativeActivationChanged;
		nativeWindow.VisibilityChanged += OnNativeVisibilityChanged;
		nativeWindow.SizeChanged += OnNativeSizeChanged;
		nativeWindow.Closing += OnNativeClosing;
		nativeWindow.Shown += OnNativeShown;
		nativeWindow.VisibleBoundsChanged += OnNativeVisibleBoundsChanged;

		NativeWindowWrapper = nativeWindow;
		Window.AppWindow.SetNativeWindow(nativeWindow);
		InputNonClientPointerSource.EnsureForAppWindow(Window.AppWindow);
		OnNativeSizeChanged(null, new Size(nativeWindow.Bounds.Width, nativeWindow.Bounds.Height));
		SetVisibleBoundsFromNative();
	}

	private void OnNativeClosing(object? sender, Microsoft.UI.Windowing.AppWindowClosingEventArgs e)
	{
		// Only raise AppWindow.Closing if the window is not closed programmatically.
		if (!_isClosing)
		{
			using var cleanup = Disposable.Create(() =>
			{
				_appWindowClosingEventArgs = null;
			});

			_appWindowClosingEventArgs = e;

			Window.AppWindow.RaiseClosing(e);

			if (e.Cancel && NativeWindowFactory.SupportsClosingCancellation)
			{
				return;
			}

			if (e.Cancel && !NativeWindowFactory.SupportsClosingCancellation)
			{
				if (this.Log().IsWarningEnabled())
				{
					this.Log().Warn("Closing event was cancelled, but the platform does not support cancellation.");
				}
			}


			// If the closing event was not cancelled, close the window.
			e.Cancel = !Close();
		}
	}

	private void OnNativeShown(object? sender, EventArgs e) => ContentManager.TryLoadRootVisual(XamlRoot!);

	private void OnNativeSizeChanged(object? sender, Size size)
	{
		if (_lastSize == size)
		{
			return;
		}

		_lastSize = size;

		OnSizeChanged(size);
#if __SKIA__ || __WASM__
		XamlRoot?.InvalidateMeasure(); //TODO:MZ: Should notify before or after?
#endif
		var windowSizeChanged = new WindowSizeChangedEventArgs(size);
#if HAS_UNO_WINUI
		// There are two "versions" of WindowSizeChangedEventArgs in Uno currently
		// when using WinUI, we need to use "legacy" version to work with CoreWindow
		// (which will eventually be removed as a legacy API as well.
		var coreWindowSizeChangedEventArgs = new Windows.UI.Core.WindowSizeChangedEventArgs(size);
#else
		var coreWindowSizeChangedEventArgs = windowSizeChanged;
#endif
#if !HAS_UNO_WINUI // CoreWindow has a different WindowSizeChangedEventArgs type, let's skip raising it completely.
		CoreWindow?.OnSizeChanged(coreWindowSizeChangedEventArgs);
#endif
		SizeChanged?.Invoke(Window, windowSizeChanged);

		XamlRoot?.RaiseChangedEvent();
	}

	private void OnNativeVisibleBoundsChanged(object? sender, Rect args) => SetVisibleBoundsFromNative();

	private void SetVisibleBoundsFromNative()
	{
		if (XamlRoot?.VisualTree is { } visualTree && NativeWindowWrapper is { } wrapper)
		{
			visualTree.VisibleBounds = wrapper.VisibleBounds;
		}
	}

	protected virtual void OnSizeChanged(Size newSize) { }

	private void OnNativeVisibilityChanged(object? sender, bool isVisible)
	{
		if (NativeWindowWrapper is not { WasShown: true })
		{
			return;
		}

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().LogDebug($"Window visibility changing to {isVisible}");
		}

		RaiseWindowVisibilityChangedEvent(isVisible);
		SystemThemeHelper.RefreshSystemTheme();
	}

	private void OnNativeActivationChanged(object? sender, CoreWindowActivationState state)
	{
		if (NativeWindowWrapper is not { WasShown: true })
		{
			return;
		}

		OnActivationStateChanged(state);
	}

	private void OnActivationStateChanged(CoreWindowActivationState state)
	{
		if (_lastActivationState == state)
		{
			return;
		}

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().LogDebug($"Window activating with {state} state.");
		}

		_lastActivationState = state;
		var activatedEventArgs = new WindowActivatedEventArgs(state);
#if HAS_UNO_WINUI
		// There are two "versions" of WindowActivatedEventArgs in Uno currently
		// when using WinUI, we need to use "legacy" version to work with CoreWindow
		// (which will eventually be removed as a legacy API as well.
		var coreWindowActivatedEventArgs = new Windows.UI.Core.WindowActivatedEventArgs(state);
#else
		var coreWindowActivatedEventArgs = activatedEventArgs;
#endif
		CoreWindow?.OnActivated(coreWindowActivatedEventArgs);
		Activated?.Invoke(Window, activatedEventArgs);
		SystemThemeHelper.RefreshSystemTheme();
		if (!FeatureConfiguration.DebugOptions.PreventKeyboardStateTrackerFromResettingOnWindowActivationChange)
		{
			KeyboardStateTracker.Reset();
		}
	}

	public bool Close()
	{
		if (!_isClosed && !_isClosing)
		{
			bool cancelGuard = false;
			using var guard = Disposable.Create(() =>
			{
				_isClosing = false;

				if (cancelGuard)
				{
					return;
				}

				// in case of any failure, closing and closed states
				// will reset so that closing operation can be attempted again
				_isClosed = false;
			});

			_isClosing = true;
			// Create and populate the window closed event args
			WindowEventArgs windowClosedEventArgs = new WindowEventArgs();
			windowClosedEventArgs.Handled = false;

			// Raise the window closed event
			Closed?.Invoke(Window, windowClosedEventArgs);

			if (windowClosedEventArgs.Handled)
			{
				// don't proceed to close if closing event has been handled
				// _isClosing will get reset to false

				if (!NativeWindowFactory.SupportsClosingCancellation)
				{
					if (this.Log().IsWarningEnabled())
					{
						this.Log().Warn("Window.Closed event was cancelled, but the platform does not support cancellation.");
					}
				}
				else
				{
					return false;
				}
			}

			// Window.PrepareToClose();

			if (NativeWindowFactory.SupportsMultipleWindows)
			{
				// set these to null before marking window as closed as they fail if called after m_bIsClosed is set
				// because they check if window is closed already
				Window.SetTitleBar(null);
				Window.Content = null;

				// _windowChrome.SetDesktopWindow(null);

				// Mark Desktop Window instance as 'closed'
				_isClosed = true;
			}

			cancelGuard = true; // success, no need to reset closing and closed states

			// if (!_minimizedOrHidden)
			{
				// if this call fails, we don't want to reset shutdown status
				// beter to crash
				RaiseWindowVisibilityChangedEvent(false);
			}


			// Close native window, cleanup, and unregister from hwnd mapping from DXamlCore
			Shutdown();

			return true;
		}

		return false;
	}

	private void Shutdown()
	{
		// Check if the application should exit based on DispatcherShutdownMode
		// This must be done BEFORE removing the window to get an accurate count
		// Use ApplicationHelper's internal synchronization for thread safety
		var shouldExitApplication = false;
		if (NativeWindowFactory.SupportsMultipleWindows)
		{
			shouldExitApplication = ShouldExitApplicationAndRemoveWindow();
		}

		if (NativeWindowWrapper is null)
		{
			throw new InvalidOperationException("Native window is not initialized.");
		}

		NativeWindowWrapper.Hide();

		// Allow the window to be re-shown on single-window targets.
		if (!NativeWindowFactory.SupportsMultipleWindows)
		{
			NativeWindowWrapper.WasShown = false;
		}

		// If AppWindow closing is in progress, it means the native window
		// itself triggered the closure and is already being closed.
		if (_appWindowClosingEventArgs is null)
		{
			NativeWindowWrapper.Close();
		}

		// Exit the application if needed (after all cleanup is done)
		if (shouldExitApplication)
		{
			Microsoft.UI.Xaml.Application.Current?.Exit();
		}
	}

	private bool ShouldExitApplicationAndRemoveWindow()
	{
		// Only check on platforms that support multiple windows
		if (!NativeWindowFactory.SupportsMultipleWindows)
		{
			return false;
		}

		var application = Microsoft.UI.Xaml.Application.Current;
		if (application is null)
		{
			return false;
		}

		// Lock on the windows collection to ensure thread-safe check and removal
		lock (Uno.UI.ApplicationHelper.WindowsInternal)
		{
			var shouldExit = false;

			if (application.DispatcherShutdownMode == DispatcherShutdownMode.OnLastWindowClose)
			{
				// Check if this is the last window (count includes the current window being closed)
				shouldExit = Uno.UI.ApplicationHelper.WindowsInternal.Count == 1;
			}

			// Remove the window from the collection while still holding the lock
			Uno.UI.ApplicationHelper.WindowsInternal.Remove(Window);

			return shouldExit;
		}
	}

	private void RaiseWindowVisibilityChangedEvent(bool isVisible)
	{
		var args = new VisibilityChangedEventArgs() { Visible = isVisible };

		CoreWindow?.OnVisibilityChanged(args);
		VisibilityChanged?.Invoke(Window, args);
	}

	public void NotifyContentLoaded()
	{
		_contentLoaded = true;

		TryActivate();
	}

	private void TryActivate()
	{
		// To actually activate, both conditions must be true:
		// 1. The content must be loaded
		// 2. The activation must be requested by the user
		if (_contentLoaded && _activationRequested)
		{
			NativeWindowWrapper?.Show(true);

			OnActivationStateChanged(CoreWindowActivationState.CodeActivated);

			if (!_splashDismissed)
			{
				DismissSplashScreenPlatform();
				_splashDismissed = true;
			}
		}
	}

	partial void DismissSplashScreenPlatform();

	public virtual void SetTitleBar(UIElement? titleBar) { }
}
