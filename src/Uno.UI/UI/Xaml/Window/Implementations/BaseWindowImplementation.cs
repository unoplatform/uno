#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using Uno.Foundation.Logging;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.ViewManagement;
using Uno.Helpers.Theming;
using Uno.UI.Core;
using Microsoft.UI.Windowing;
using Windows.UI.Core.Preview;
using Uno.Disposables;

#if !HAS_UNO_WINUI
using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
#endif

#if HAS_UNO_WINUI
using WindowSizeChangedEventArgs = Windows.UI.Xaml.WindowSizeChangedEventArgs;
using WindowActivatedEventArgs = Windows.UI.Xaml.WindowActivatedEventArgs;
#else
using WindowSizeChangedEventArgs = Windows.UI.Core.WindowSizeChangedEventArgs;
using WindowActivatedEventArgs = Windows.UI.Core.WindowActivatedEventArgs;
#endif

namespace Uno.UI.Xaml.Controls;

internal abstract class BaseWindowImplementation : IWindowImplementation
{
	private bool _wasShown;
	private CoreWindowActivationState _lastActivationState = CoreWindowActivationState.Deactivated;
	private Size _lastSize = new Size(-1, -1);

	private bool _isClosing;
	private bool _isClosed;

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

	protected INativeWindowWrapper? NativeWindowWrapper { get; private set; }

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

		if (!_wasShown)
		{
			_wasShown = true;

			SetVisibleBoundsFromNative();
			NativeWindowWrapper?.Show();
		}
		else
		{
			NativeWindowWrapper?.Activate();
		}

		OnActivationStateChanged(CoreWindowActivationState.CodeActivated);
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

#if __SKIA__
			// Legacy system close handling
			var manager = SystemNavigationManagerPreview.GetForCurrentView();
			if (manager is { HasConfirmedClose: false })
			{
				if (!manager.RequestAppClose())
				{
					// App closing was prevented, handle event
					e.Cancel = true;

					if (NativeWindowFactory.SupportsClosingCancellation)
					{
						return;
					}
				}
			}
#endif

			if (e.Cancel && !NativeWindowFactory.SupportsClosingCancellation)
			{
				if (this.Log().IsWarningEnabled(LogLevel.Warning))
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
		ApplicationView.GetForWindowId(Window.AppWindow.Id).SetVisibleBounds(NativeWindowWrapper?.VisibleBounds ?? default);
	}

	protected virtual void OnSizeChanged(Size newSize) { }

	private void OnNativeVisibilityChanged(object? sender, bool isVisible)
	{
		if (!_wasShown)
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
		if (!_wasShown)
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
		KeyboardStateTracker.Reset();
	}

	public bool Close()
	{
		if (!_isClosed && !_isClosing)
		{
			bool cancelGuard = false;
			using var guard = Disposable.Create(() =>
			{
				if (cancelGuard)
				{
					return;
				}
				// in case of any failure, closing and closed states
				// will reset so that closing operation can be attempted again
				_isClosing = false;
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
					if (this.Log().IsWarningEnabled(LogLevel.Warning))
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
			}
			else
			{
				// Just reset the window to not shown state so it can be reactivated
				_wasShown = false;
			}

			// _windowChrome.SetDesktopWindow(null);

			// Mark Desktop Window instance as 'closed'
			_isClosed = true;
			cancelGuard = true; // success, no need to reset closing and closed states

			// if (!_minimizedOrHidden)
			{
				// if this call fails, we don't want to reset shutdown status
				// beter to crash
				RaiseWindowVisibilityChangedEvent(false);
			}

			if (NativeWindowFactory.SupportsMultipleWindows)
			{
				// Close native window, cleanup, and unregister from hwnd mapping from DXamlCore
				Shutdown();
			}

			return true;
		}

		return false;
	}

	private void Shutdown()
	{
		ApplicationHelper.RemoveWindow(Window);

		if (NativeWindowWrapper is null)
		{
			throw new InvalidOperationException("Native window is not initialized.");
		}

		// If AppWindow closing is in progress, we don't need to do anything here.
		if (_appWindowClosingEventArgs is null)
		{
			NativeWindowWrapper.Close();
		}
	}

	private void RaiseWindowVisibilityChangedEvent(bool isVisible)
	{
		var args = new VisibilityChangedEventArgs() { Visible = isVisible };

		CoreWindow?.OnVisibilityChanged(args);
		VisibilityChanged?.Invoke(Window, args);
	}
}
