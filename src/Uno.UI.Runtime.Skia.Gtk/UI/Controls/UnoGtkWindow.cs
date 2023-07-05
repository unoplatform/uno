using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Gtk;
using Uno.Foundation.Logging;
using Uno.UI.Runtime.Skia.GTK.UI.Core;
using Windows.Foundation;
using Windows.UI.Core.Preview;
using Windows.UI.ViewManagement;
using IOPath = System.IO.Path;
using WinUIApplication = Windows.UI.Xaml.Application;
using WinUIWindow = Windows.UI.Xaml.Window;

namespace Uno.UI.Runtime.Skia.GTK.UI.Controls;

internal class UnoGtkWindow : Gtk.Window
{
	private readonly WinUIWindow _winUIWindow;

	private bool _wasShown;

	private List<PendingWindowStateChangedInfo>? _pendingWindowStateChanged = new();

	public UnoGtkWindow(WinUIWindow winUIWindow) : base(WindowType.Toplevel)
	{
		_winUIWindow = winUIWindow ?? throw new ArgumentNullException(nameof(winUIWindow));
		_winUIWindow.Shown += OnShown;

		Size preferredWindowSize = ApplicationView.PreferredLaunchViewSize;
		if (preferredWindowSize != Size.Empty)
		{
			SetDefaultSize((int)preferredWindowSize.Width, (int)preferredWindowSize.Height);
		}
		else
		{
			SetDefaultSize(1024, 800);
		}
		SetPosition(Gtk.WindowPosition.Center);
		Realized += (s, e) =>
		{
			// Load the correct cursors before the window is shown
			// but after the window has been initialized.
			Cursors.EnsureLoaded();
		};

		DeleteEvent += WindowClosing;

		WindowStateEvent += OnWindowStateChanged;

		Host = new UnoGtkWindowHost(this, winUIWindow);

		ApplicationView.GetForCurrentView().PropertyChanged += OnApplicationViewPropertyChanged;
	}

	internal UnoGtkWindowHost Host { get; }

	private async void OnShown(object? sender, EventArgs e)
	{
		await Host.InitializeAsync();
		ShowAll();
		_wasShown = true;
		ReplayPendingWindowStateChanges();
	}

	private void WindowClosing(object sender, DeleteEventArgs args)
	{
		var manager = SystemNavigationManagerPreview.GetForCurrentView();
		if (!manager.HasConfirmedClose)
		{
			if (!manager.RequestAppClose())
			{
				// App closing was prevented, handle event
				args.RetVal = true;
				return;
			}
		}

		// Closing should continue, perform suspension.
		WinUIApplication.Current.RaiseSuspending();

		// All prerequisites passed, can safely close.
		args.RetVal = false;
		Gtk.Main.Quit();
	}

	internal void UpdateWindowPropertiesFromPackage()
	{
		if (Windows.ApplicationModel.Package.Current.Logo is Uri uri)
		{
			var basePath = uri.OriginalString.Replace('\\', IOPath.DirectorySeparatorChar);
			var iconPath = IOPath.Combine(Windows.ApplicationModel.Package.Current.InstalledPath, basePath);

			if (File.Exists(iconPath))
			{
				if (this.Log().IsEnabled(LogLevel.Information))
				{
					this.Log().Info($"Loading icon file [{iconPath}] from Package.appxmanifest file");
				}

				SetIconFromFile(iconPath);
			}
			else if (Windows.UI.Xaml.Media.Imaging.BitmapImage.GetScaledPath(basePath) is { } scaledPath && File.Exists(scaledPath))
			{
				if (this.Log().IsEnabled(LogLevel.Information))
				{
					this.Log().Info($"Loading icon file [{scaledPath}] scaled logo from Package.appxmanifest file");
				}

				SetIconFromFile(scaledPath);
			}
			else
			{
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().Warn($"Unable to find icon file [{iconPath}] specified in the Package.appxmanifest file.");
				}
			}
		}

		if (string.IsNullOrEmpty(ApplicationView.GetForCurrentView().Title))
		{
			ApplicationView.GetForCurrentView().Title = Windows.ApplicationModel.Package.Current.DisplayName;
		}
	}

	private void OnApplicationViewPropertyChanged(object? sender, PropertyChangedEventArgs e) => UpdateWindowPropertiesFromApplicationView();

	internal void UpdateWindowPropertiesFromApplicationView()
	{
		var appView = ApplicationView.GetForCurrentView();
		Title = appView.Title;
		SetSizeRequest((int)appView.PreferredMinSize.Width, (int)appView.PreferredMinSize.Height);
	}

	private void OnWindowStateChanged(object o, WindowStateEventArgs args)
	{
		var newState = args.Event.NewWindowState;
		var changedMask = args.Event.ChangedMask;

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"OnWindowStateChanged: {newState}/{changedMask}");
		}

		if (_wasShown)
		{
			ProcessWindowStateChanged(newState, changedMask);
		}
		else
		{
			// Store state changes to replay once the application has been
			// initalized completely (initialization can be delayed if the render
			// surface is automatically detected).
			_pendingWindowStateChanged?.Add(new(newState, changedMask));
		}
	}

	private void ReplayPendingWindowStateChanges()
	{
		if (_pendingWindowStateChanged is not null)
		{
			foreach (var state in _pendingWindowStateChanged)
			{
				ProcessWindowStateChanged(state.newState, state.changedMask);
			}

			_pendingWindowStateChanged = null;
		}
	}

	private void ProcessWindowStateChanged(Gdk.WindowState newState, Gdk.WindowState changedMask)
	{
		var winUIApplication = WinUIApplication.Current;

		var isVisible =
			!(newState.HasFlag(Gdk.WindowState.Withdrawn) ||
			newState.HasFlag(Gdk.WindowState.Iconified));

		var isVisibleChanged =
			changedMask.HasFlag(Gdk.WindowState.Withdrawn) ||
			changedMask.HasFlag(Gdk.WindowState.Iconified);

		var focused = newState.HasFlag(Gdk.WindowState.Focused);
		var focusChanged = changedMask.HasFlag(Gdk.WindowState.Focused);

		if (!focused && focusChanged)
		{
			_winUIWindow?.OnNativeActivated(Windows.UI.Core.CoreWindowActivationState.Deactivated);
		}

		if (isVisibleChanged)
		{
			if (isVisible)
			{
				winUIApplication?.RaiseLeavingBackground(() => _winUIWindow?.OnNativeVisibilityChanged(true));
			}
			else
			{
				_winUIWindow?.OnNativeVisibilityChanged(false);
				winUIApplication?.RaiseEnteredBackground(null);
			}
		}

		if (focused && focusChanged)
		{
			_winUIWindow?.OnNativeActivated(Windows.UI.Core.CoreWindowActivationState.CodeActivated);
		}
	}
}
