#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using Uno.Foundation.Logging;
using Windows.Foundation;
using Windows.UI.Core;
using Microsoft.UI.Xaml;

#if !HAS_UNO_WINUI
using Windows.UI.Xaml;
#endif

namespace Uno.UI.Xaml.Controls;

abstract partial class BaseWindowImplementation : IWindowImplementation
{
	private bool _wasActivated;
	private CoreWindowActivationState _lastActivationState;

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

	public Rect Bounds { get; private set; }

	public virtual void Activate()
	{
		_wasActivated = true;
		NativeWindowWrapper!.Show();
		// TODO:MZ: Raise activation if needed!
		//_lastActivationState = CoreWindowActivationState.CodeActivated;
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
		nativeWindow.Closed += OnNativeClosed;
		nativeWindow.Shown += OnNativeShown;

		NativeWindowWrapper = nativeWindow;
	}

	private void OnNativeShown(object? sender, EventArgs e) => ContentManager.TryLoadRootVisual(XamlRoot!);

	private void OnNativeClosed(object? sender, EventArgs args) => Closed?.Invoke(this, new WindowEventArgs());

	private void OnNativeSizeChanged(object? sender, Size size)
	{
		Bounds = new Rect(0, 0, size.Width, size.Height);
		OnSizeChanged(size);
#if __SKIA__ || __WASM__ // TODO:MZ: What about Android & iOS
		XamlRoot?.InvalidateMeasure(); // Should notify before or after?
#endif
		XamlRoot?.NotifyChanged();
		var windowSizeChanged = new WindowSizeChangedEventArgs(size);
		CoreWindow?.OnSizeChanged(windowSizeChanged);
		SizeChanged?.Invoke(this, windowSizeChanged);
	}

	protected virtual void OnSizeChanged(Size newSize) { }

	private void OnNativeVisibilityChanged(object? sender, bool isVisible)
	{
		if (!_wasActivated)
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
	}

	private void OnNativeActivationChanged(object? sender, CoreWindowActivationState state)
	{
		if (!_wasActivated)
		{
			return;
		}

		if (_lastActivationState != state)
		{
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
		}
	}

	public void Close() => throw new NotImplementedException();

	//TODO:MZ: Remove this
	//private void RootSizeChanged(object sender, SizeChangedEventArgs args) => _windowImplementation.Content?.XamlRoot?.NotifyChanged();

	//TODO:MZ: Remove this
	//private void RaiseSizeChanged(Windows.UI.Core.WindowSizeChangedEventArgs windowSizeChangedEventArgs)
	//{
	//	var baseSizeChanged = new WindowSizeChangedEventArgs(windowSizeChangedEventArgs.Size) { Handled = windowSizeChangedEventArgs.Handled };

	//	SizeChanged?.Invoke(this, baseSizeChanged);

	//	windowSizeChangedEventArgs.Handled = baseSizeChanged.Handled;

	//	CoreWindow.IShouldntUseGetForCurrentThread()?.OnSizeChanged(windowSizeChangedEventArgs);

	//	baseSizeChanged.Handled = windowSizeChangedEventArgs.Handled;

	//	foreach (var action in _sizeChangedHandlers)
	//	{
	//		action(this, baseSizeChanged);
	//	}
	//}

	//internal void OnNativeSizeChanged(Size size)
	//{
	//	var newBounds = new Rect(0, 0, size.Width, size.Height);

	//	if (newBounds != Bounds)
	//	{
	//		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
	//		{
	//			this.Log().Debug($"OnNativeSizeChanged: {size}");
	//		}

	//		Bounds = newBounds;

	//		if (_windowImplementation is CoreWindowWindow)
	//		{
	//			WinUICoreServices.Instance.MainRootVisual?.XamlRoot?.InvalidateMeasure();
	//		}
	//		else
	//		{
	//			if (Content?.XamlRoot is { } xamlRoot && xamlRoot.VisualTree.RootElement is XamlIsland xamlIsland)
	//			{
	//				xamlIsland.SetActualSize(newBounds.Size);
	//				xamlRoot.InvalidateMeasure();
	//			}
	//		}

	//		RaiseSizeChanged(new Windows.UI.Core.WindowSizeChangedEventArgs(size));

	//		ApplicationView.GetForCurrentView().SetVisibleBounds(newBounds);
	//	}
	//}
}
