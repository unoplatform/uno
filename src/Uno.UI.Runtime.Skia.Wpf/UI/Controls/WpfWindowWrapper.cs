#nullable enable

using System;
using System.ComponentModel;
using Uno.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Core.Preview;
using WinUIApplication = Windows.UI.Xaml.Application;

namespace Uno.UI.Runtime.Skia.Wpf.UI.Controls;

internal class WpfWindowWrapper : NativeWindowWrapperBase
{
	private readonly UnoWpfWindow _wpfWindow;

	public WpfWindowWrapper(UnoWpfWindow wpfWindow)
	{
		_wpfWindow = wpfWindow ?? throw new ArgumentNullException(nameof(wpfWindow));
		_wpfWindow.Host.SizeChanged += OnHostSizeChanged;
		_wpfWindow.Closing += OnNativeClosing;
		_wpfWindow.Activated += OnNativeActivated;
		_wpfWindow.Deactivated += OnNativeDeactivated;
		_wpfWindow.IsVisibleChanged += OnNativeIsVisibleChanged;
		_wpfWindow.Closed += OnNativeClosed;
	}

	public UnoWpfWindow NativeWindow => _wpfWindow;

	protected override void ShowCore() => _wpfWindow.Show();

	public override void Activate() => _wpfWindow.Activate();

	private void OnHostSizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
	{
		Bounds = new Rect(default, new Windows.Foundation.Size(e.NewSize.Width, e.NewSize.Height));
		VisibleBounds = Bounds;
	}

	private void OnNativeClosed(object? sender, EventArgs e) => RaiseClosed();

	private void OnNativeClosing(object? sender, CancelEventArgs e)
	{
		// TODO: Support multi-window approach properly #8341
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
		// TODO:MZ: Only do this if it is the last Window!
		WinUIApplication.Current.RaiseSuspending();
	}

	private void OnNativeDeactivated(object? sender, EventArgs e) => ActivationState = CoreWindowActivationState.Deactivated;

	private void OnNativeActivated(object? sender, EventArgs e) => ActivationState = CoreWindowActivationState.PointerActivated;

	private void OnNativeIsVisibleChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
	{
		Visible = _wpfWindow.IsVisible;
		// TODO:MZ: Incorporate RaiseLeavingBackground and RaiseEnteredBackground in general manner
		//if (isVisible)
		//{
		//	// TODO:MZ: Only do this for single Window (but visibilityChanged always)
		//	WinUIApplication.Current?.RaiseLeavingBackground(() => VisibilityChanged?.Invoke(this, isVisible));
		//}
		//else if (isVisible)
		//{
		//	VisibilityChanged?.Invoke(this, _wpfWindow.IsVisible);
		//	// TODO:MZ: Only do this for single Window!
		//	WinUIApplication.Current?.RaiseEnteredBackground(null);
		//}
	}
}
