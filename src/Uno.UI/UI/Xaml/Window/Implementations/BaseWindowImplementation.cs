#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using Uno.Foundation.Logging;
using Windows.Foundation;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Windows.UI.ViewManagement;
using Uno.Helpers.Theming;

#if !HAS_UNO_WINUI
using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
#endif

#if HAS_UNO_WINUI
using WindowSizeChangedEventArgs = Microsoft.UI.Xaml.WindowSizeChangedEventArgs;
using WindowActivatedEventArgs = Microsoft.UI.Xaml.WindowActivatedEventArgs;
#else
using WindowSizeChangedEventArgs = Windows.UI.Core.WindowSizeChangedEventArgs;
using WindowActivatedEventArgs = Windows.UI.Core.WindowActivatedEventArgs;

#endif

namespace Uno.UI.Xaml.Controls;

internal abstract class BaseWindowImplementation : IWindowImplementation
{
	private bool _wasShown;
	private CoreWindowActivationState _lastActivationState = CoreWindowActivationState.Deactivated;

#pragma warning disable CS0649
	public BaseWindowImplementation(Window window)
	{
		Window = window ?? throw new System.ArgumentNullException(nameof(window));
	}

	public event WindowSizeChangedEventHandler? SizeChanged;
	public event WindowActivatedEventHandler? Activated;
	public event TypedEventHandler<object, WindowEventArgs>? Closed;
	public event WindowVisibilityChangedEventHandler? VisibilityChanged;

	protected Window Window { get; }

	protected INativeWindowWrapper? NativeWindowWrapper { get; private set; }

	public abstract CoreWindow? CoreWindow { get; }

	public bool Visible => NativeWindowWrapper?.Visible ?? false;

	public abstract UIElement? Content { get; set; }

	public abstract XamlRoot? XamlRoot { get; }

	public Rect Bounds => NativeWindowWrapper?.Bounds ?? default;

	public object? NativeWindow => NativeWindowWrapper?.NativeWindow;

	public virtual void Initialize()
	{
		InitializeNativeWindow();
	}

	public virtual void Activate()
	{
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
		nativeWindow.Closed += OnNativeClosed;
		nativeWindow.Shown += OnNativeShown;
		nativeWindow.VisibleBoundsChanged += OnNativeVisibleBoundsChanged;

		NativeWindowWrapper = nativeWindow;
	}

	private void OnNativeClosing(object? sender, Microsoft.UI.Windowing.AppWindowClosingEventArgs e) => Window.AppWindow.RaiseClosing(e);

	private void OnNativeShown(object? sender, EventArgs e) => ContentManager.TryLoadRootVisual(XamlRoot!);

	private void OnNativeClosed(object? sender, EventArgs args) => Closed?.Invoke(this, new WindowEventArgs());

	private void OnNativeSizeChanged(object? sender, Size size)
	{
		OnSizeChanged(size);
#if __SKIA__ || __WASM__
		XamlRoot?.InvalidateMeasure(); //TODO:MZ: Should notify before or after?
#endif
		XamlRoot?.NotifyChanged();
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
		SizeChanged?.Invoke(this, windowSizeChanged);
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

		var args = new VisibilityChangedEventArgs() { Visible = isVisible };

		CoreWindow?.OnVisibilityChanged(args);
		VisibilityChanged?.Invoke(this, args);
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
		Activated?.Invoke(this, activatedEventArgs);
		SystemThemeHelper.RefreshSystemTheme();
	}

	public void Close() => NativeWindowWrapper?.Close();
}
