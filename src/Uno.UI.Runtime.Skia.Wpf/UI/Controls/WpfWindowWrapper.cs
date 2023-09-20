#nullable enable

using System;
using System.ComponentModel;
using Uno.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Core.Preview;
using WinUIApplication = Windows.UI.Xaml.Application;

namespace Uno.UI.Runtime.Skia.Wpf.UI.Controls;

internal class WpfWindowWrapper : INativeWindowWrapper
{
	private readonly UnoWpfWindow _wpfWindow;

	public WpfWindowWrapper(UnoWpfWindow wpfWindow)
	{
		_wpfWindow = wpfWindow ?? throw new ArgumentNullException(nameof(wpfWindow));
		_wpfWindow.Host.SizeChanged += OnHostSizeChanged;
		_wpfWindow.Closing += OnClosing;
		_wpfWindow.Activated += OnActivated;
		_wpfWindow.Deactivated += OnDeactivated;
		_wpfWindow.IsVisibleChanged += OnIsVisibleChanged;
		_wpfWindow.Closed += OnClosed;
	}

	public UnoWpfWindow NativeWindow => _wpfWindow;

	public bool Visible => _wpfWindow.IsVisible;

	public event EventHandler<Size>? SizeChanged;

	public event EventHandler<CoreWindowActivationState>? ActivationChanged;

	public event EventHandler<bool>? VisibilityChanged;

	public event EventHandler? Closed;
	public event EventHandler? Shown;

	public void Show()
	{
		_wpfWindow.Show();
		Shown?.Invoke(this, EventArgs.Empty);
	}

	public void Activate() => _wpfWindow.Activate();

	private void OnHostSizeChanged(object sender, System.Windows.SizeChangedEventArgs e) =>
		SizeChanged?.Invoke(this, new Windows.Foundation.Size(e.NewSize.Width, e.NewSize.Height));

	private void OnClosed(object? sender, EventArgs e) => Closed?.Invoke(this, EventArgs.Empty);

	private void OnClosing(object? sender, CancelEventArgs e)
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

	private void OnDeactivated(object? sender, EventArgs e) =>
		ActivationChanged?.Invoke(this, Windows.UI.Core.CoreWindowActivationState.Deactivated);

	private void OnActivated(object? sender, EventArgs e) =>
		ActivationChanged?.Invoke(this, Windows.UI.Core.CoreWindowActivationState.PointerActivated);

	private void OnIsVisibleChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
	{
		var isVisible = (bool)e.NewValue;

		if (isVisible)
		{
			// TODO:MZ: Only do this for single Window (but visibilityChanged always)
			WinUIApplication.Current?.RaiseLeavingBackground(() => VisibilityChanged?.Invoke(this, isVisible));
		}
		else if (isVisible)
		{
			VisibilityChanged?.Invoke(this, _wpfWindow.IsVisible);
			// TODO:MZ: Only do this for single Window!
			WinUIApplication.Current?.RaiseEnteredBackground(null);
		}
	}
}
