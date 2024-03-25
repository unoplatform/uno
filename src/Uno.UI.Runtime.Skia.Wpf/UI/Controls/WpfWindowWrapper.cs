#nullable enable

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using Microsoft.UI.Content;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Uno.Disposables;
using Uno.UI.Xaml.Controls;
using Windows.UI.Core;
using Windows.UI.Core.Preview;
using WinUIApplication = Microsoft.UI.Xaml.Application;

namespace Uno.UI.Runtime.Skia.Wpf.UI.Controls;

internal class WpfWindowWrapper : NativeWindowWrapperBase
{
	private readonly UnoWpfWindow _wpfWindow;
	private bool _isFullScreen;

	public WpfWindowWrapper(UnoWpfWindow wpfWindow, XamlRoot xamlRoot) : base(xamlRoot)
	{
		_wpfWindow = wpfWindow ?? throw new ArgumentNullException(nameof(wpfWindow));
		_wpfWindow.Activated += OnNativeActivated;
		_wpfWindow.Deactivated += OnNativeDeactivated;
		_wpfWindow.IsVisibleChanged += OnNativeIsVisibleChanged;
		_wpfWindow.Closing += OnNativeClosing;
		_wpfWindow.Closed += OnNativeClosed;
		_wpfWindow.DpiChanged += OnNativeDpiChanged;
		_wpfWindow.StateChanged += OnNativeStateChanged;
		_wpfWindow.Host.SizeChanged += (_, e) => OnHostSizeChanged(e.NewSize);
		OnHostSizeChanged(new Size(_wpfWindow.Width, _wpfWindow.Height));
		_wpfWindow.LocationChanged += OnNativeLocationChanged;
		_wpfWindow.SizeChanged += OnNativeSizeChanged;
		UpdateSizeFromNative();
		UpdatePositionFromNative();
	}
	
	private void OnNativeSizeChanged(object sender, SizeChangedEventArgs e) => UpdateSizeFromNative();

	private void UpdateSizeFromNative() =>
		Size = new() { Width = (int)_wpfWindow.Width, Height = (int)_wpfWindow.Height };

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
		UpdatePositionFromNative();
	}

	public override void Activate() => _wpfWindow.Activate();

	public override void Close() => _wpfWindow.Close();

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

	private void OnNativeClosed(object? sender, EventArgs e) => RaiseClosed();

	private void OnNativeClosing(object? sender, CancelEventArgs e)
	{
		var closingArgs = RaiseClosing();
		if (closingArgs.Cancel)
		{
			e.Cancel = true;
			return;
		}

		var manager = SystemNavigationManagerPreview.GetForCurrentView();
		if (!manager.HasConfirmedClose)
		{
			if (!manager.RequestAppClose())
			{
				e.Cancel = true;
				return;
			}
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
		if (isVisible == Visible)
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
	}

	public override void Resize(SizeInt32 size)
	{
		_wpfWindow.Width = size.Width;
		_wpfWindow.Height = size.Height;
	}
}
