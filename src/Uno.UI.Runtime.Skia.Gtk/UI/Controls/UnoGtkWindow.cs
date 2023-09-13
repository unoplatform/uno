#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices.JavaScript;
using Gtk;
using Uno.Foundation.Logging;
using Uno.UI.Runtime.Skia.Gtk.UI.Core;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.UI.Core.Preview;
using Windows.UI.ViewManagement;
using XamlRoot = Windows.UI.Xaml.XamlRoot;
using IOPath = System.IO.Path;
using WinUIApplication = Microsoft.UI.Xaml.Application;
using WinUIWindow = Microsoft.UI.Xaml.Window;

namespace Uno.UI.Runtime.Skia.Gtk.UI.Controls;

internal class UnoGtkWindow : Window
{
	private readonly WinUIWindow _winUIWindow;

	public UnoGtkWindow(WinUIWindow winUIWindow, XamlRoot xamlRoot) : base(WindowType.Toplevel)
	{
		_winUIWindow = winUIWindow ?? throw new ArgumentNullException(nameof(winUIWindow));
		_winUIWindow.Showing += OnShowing;
		_winUIWindow.NativeWindow = this;

		Size preferredWindowSize = ApplicationView.PreferredLaunchViewSize;
		if (preferredWindowSize != Size.Empty)
		{
			SetDefaultSize((int)preferredWindowSize.Width, (int)preferredWindowSize.Height);
		}
		else
		{
			SetDefaultSize(1024, 800);
		}
		SetPosition(WindowPosition.Center);
		Realized += (s, e) =>
		{
			// Load the correct cursors before the window is shown
			// but after the window has been initialized.
			Cursors.EnsureLoaded();
		};



		Host = new UnoGtkWindowHost(this, winUIWindow);
		GtkManager.XamlRootMap.Register(xamlRoot, Host);

		ApplicationView.GetForCurrentView().PropertyChanged += OnApplicationViewPropertyChanged;
	}

	internal UnoGtkWindowHost Host { get; }

	private async void OnShowing(object? sender, EventArgs e)
	{
		try
		{
			await Host.InitializeAsync();
			ShowAll();
		}
		catch (Exception ex)
		{
			this.Log().Error("Failed to initialize the UnoGtkWindow", ex);
		}
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
			else if (Microsoft.UI.Xaml.Media.Imaging.BitmapImage.GetScaledPath(basePath) is { } scaledPath && File.Exists(scaledPath))
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

	internal void UpdateWindowPropertiesFromCoreApplication()
	{
		var coreApplicationView = CoreApplication.GetCurrentView();
		Decorated = !coreApplicationView.TitleBar.ExtendViewIntoTitleBar;
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
