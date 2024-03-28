#nullable enable

using System;
using System.ComponentModel;
using System.Windows;
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
		_wpfWindow.Host.SizeChanged += OnHostSizeChanged;
		_wpfWindow.Activated += OnNativeActivated;
		_wpfWindow.Deactivated += OnNativeDeactivated;
		_wpfWindow.IsVisibleChanged += OnNativeIsVisibleChanged;
		_wpfWindow.Closing += OnNativeClosing;
		_wpfWindow.Closed += OnNativeClosed;
		_wpfWindow.DpiChanged += OnDpiChanged;
		_wpfWindow.SizeChanged += OnWindowSizeChanged;
	}

	private void OnWindowSizeChanged(object sender, System.Windows.SizeChangedEventArgs e) =>
		_contentIsland.RaiseStateChanged(ContentIslandStateChangedEventArgs.ActualSizeChange);

	private void OnDpiChanged(object sender, DpiChangedEventArgs e) =>
		_contentIsland.RaiseStateChanged(ContentIslandStateChangedEventArgs.RasterizationScaleChange);

	public override string Title
	{
		get => _wpfWindow.Title;
		set => _wpfWindow.Title = value;
	}

	public override object NativeWindow => _wpfWindow;

	protected override void ShowCore() => _wpfWindow.Show();

	public override void Activate() => _wpfWindow.Activate();

	public override void Close() => _wpfWindow.Close();

	private void OnHostSizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
	{
		Bounds = new Windows.Foundation.Rect(default, new Windows.Foundation.Size(e.NewSize.Width, e.NewSize.Height));
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

	private void OnNativeIsVisibleChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
	{
		var isVisible = (bool)e.NewValue;
		if (isVisible)
		{
			WinUIApplication.Current?.RaiseLeavingBackground(() => Visible = isVisible);
		}
		else
		{
			Visible = isVisible;
			WinUIApplication.Current?.RaiseEnteredBackground(null);
		}

		_contentIsland.RaiseStateChanged(ContentIslandStateChangedEventArgs.SiteVisibleChange);
	}

	protected override IDisposable ApplyFullScreenPresenter()
	{
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
}
