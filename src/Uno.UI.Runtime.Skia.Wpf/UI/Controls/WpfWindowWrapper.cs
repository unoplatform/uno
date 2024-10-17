#nullable enable

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using Microsoft.UI.Windowing;
using Windows.UI.Xaml;
using Uno.Disposables;
using Uno.UI.Xaml.Controls;
using Windows.Graphics;
using Windows.UI.Core;
using Windows.UI.Core.Preview;
using WinUIApplication = Windows.UI.Xaml.Application;
using WinUIWindow = Windows.UI.Xaml.Window;

namespace Uno.UI.Runtime.Skia.Wpf.UI.Controls;

internal class WpfWindowWrapper : NativeWindowWrapperBase
{
	private readonly UnoWpfWindow _wpfWindow;
	private bool _isFullScreen;
	private bool _wasShown;

	public WpfWindowWrapper(UnoWpfWindow wpfWindow, WinUIWindow window, XamlRoot xamlRoot) : base(window, xamlRoot)
	{
		_wpfWindow = wpfWindow ?? throw new ArgumentNullException(nameof(wpfWindow));
		_wpfWindow.Activated += OnNativeActivated;
		_wpfWindow.Deactivated += OnNativeDeactivated;
		_wpfWindow.IsVisibleChanged += OnNativeIsVisibleChanged;
		_wpfWindow.Closing += OnNativeClosing;
		_wpfWindow.DpiChanged += OnNativeDpiChanged;
		_wpfWindow.StateChanged += OnNativeStateChanged;
		_wpfWindow.Host.SizeChanged += (_, e) => OnHostSizeChanged(e.NewSize);
		OnHostSizeChanged(new Size(_wpfWindow.Width, _wpfWindow.Height));
		_wpfWindow.LocationChanged += OnNativeLocationChanged;
		_wpfWindow.SizeChanged += OnNativeSizeChanged;
		UpdateSizeFromNative();
		UpdatePositionFromNative();
	}

	private void OnNativeSizeChanged(object sender, System.Windows.SizeChangedEventArgs e) => UpdateSizeFromNative();

	private void UpdateSizeFromNative()
	{
		if (!_wasShown)
		{
			Size = new() { Width = (int)_wpfWindow.Width, Height = (int)_wpfWindow.Height };
		}
		else
		{
			Size = new() { Width = (int)_wpfWindow.ActualWidth, Height = (int)_wpfWindow.ActualHeight };
		}
	}

	private void OnNativeLocationChanged(object? sender, EventArgs e) => UpdatePositionFromNative();

	private void UpdatePositionFromNative() =>
		Position = new() { X = (int)_wpfWindow.Left, Y = (int)_wpfWindow.Top };

	private void OnNativeStateChanged(object? sender, EventArgs e) => UpdateIsVisible();

	private void OnNativeDpiChanged(object sender, DpiChangedEventArgs e) => RasterizationScale = (float)e.NewDpi.DpiScaleX;

	public override string Title
	{
		get => _wpfWindow.Title;
		set => _wpfWindow.Title = value;
	}

	public override object NativeWindow => _wpfWindow;

	protected override void ShowCore()
	{
		RasterizationScale = (float)VisualTreeHelper.GetDpi(_wpfWindow.Host).DpiScaleX;
		_wpfWindow.Show();
		_wasShown = true;
		UpdatePositionFromNative();
	}

	public override void Activate() => _wpfWindow.Activate();

	public override void Close()
	{
		base.Close();
		_wpfWindow.Close();
	}

	public override void ExtendContentIntoTitleBar(bool extend)
	{
		base.ExtendContentIntoTitleBar(extend);
		_wpfWindow.ExtendContentIntoTitleBar(extend);
	}

	private void OnHostSizeChanged(Size size)
	{
		Bounds = new Windows.Foundation.Rect(default, new Windows.Foundation.Size(size.Width, size.Height));
		VisibleBounds = Bounds;
	}

	private void OnNativeClosing(object? sender, CancelEventArgs e)
	{
		var closingArgs = RaiseClosing();
		if (closingArgs.Cancel)
		{
			e.Cancel = true;
			return;
		}

		// Closing should continue, perform suspension.
		WinUIApplication.Current.RaiseSuspending();
	}

	private void OnNativeDeactivated(object? sender, EventArgs e) =>
		ActivationState = CoreWindowActivationState.Deactivated;

	private void OnNativeActivated(object? sender, EventArgs e) =>
		ActivationState = CoreWindowActivationState.PointerActivated;

	private void OnNativeIsVisibleChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e) => UpdateIsVisible();

	private void UpdateIsVisible()
	{
		var isVisible = _wpfWindow.IsVisible && _wpfWindow.WindowState != WindowState.Minimized;
		if (isVisible == IsVisible)
		{
			return;
		}

		if (isVisible)
		{
			WinUIApplication.Current?.RaiseLeavingBackground(() => IsVisible = isVisible);
		}
		else
		{
			IsVisible = isVisible;
			WinUIApplication.Current?.RaiseEnteredBackground(null);
		}
	}

	protected override IDisposable ApplyFullScreenPresenter()
	{
		_isFullScreen = true;
		_wpfWindow.WindowStyle = WindowStyle.None;
		_wpfWindow.WindowState = WindowState.Maximized;

		return Disposable.Create(() =>
		{
			if (!_isFullScreen)
			{
				return;
			}

			_isFullScreen = false;
			_wpfWindow.WindowStyle = WindowStyle.SingleBorderWindow;
			_wpfWindow.WindowState = WindowState.Normal;
		});
	}

	protected override IDisposable ApplyOverlappedPresenter(OverlappedPresenter presenter)
	{
		presenter.SetNative(new NativeOverlappedPresenter(_wpfWindow));
		return Disposable.Create(() => presenter.SetNative(null));
	}

	public override void Move(PointInt32 position)
	{
		_wpfWindow.Left = position.X;
		_wpfWindow.Top = position.Y;

		if (!_wasShown)
		{
			// Set Position and trigger AppWindow.Changed
			UpdatePositionFromNative();
		}
	}

	public override void Resize(SizeInt32 size)
	{
		_wpfWindow.Width = size.Width;
		_wpfWindow.Height = size.Height;

		if (!_wasShown)
		{
			// Set size and trigger AppWindow.Changed
			UpdateSizeFromNative();
		}
	}
}
