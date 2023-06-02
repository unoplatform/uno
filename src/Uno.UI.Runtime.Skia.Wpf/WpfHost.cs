#nullable enable

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using Uno.UI.Runtime.Skia.Wpf.Extensions;
using Uno.UI.Runtime.Skia.Wpf.Hosting;
using Uno.UI.Skia.Wpf;
using Uno.UI.XamlHost.Skia.Wpf;
using Windows.UI.Core.Preview;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using WinUI = Windows.UI.Xaml;
using WinUIApplication = Windows.UI.Xaml.Application;
using WpfApplication = System.Windows.Application;

namespace Uno.UI.Skia;

// TODO:MZ: Rename to WpfApplicationHost???
public class WpfHost : IWpfApplicationHost
{
	private readonly Dispatcher _dispatcher;
	private readonly Func<WinUIApplication> _appBuilder;

	[ThreadStatic] private static WpfHost? _current;

	private bool _ignorePixelScaling;
	private bool _isVisible = true;

	static WpfHost() => WpfExtensionsRegistrar.Register();

	public WpfHost(Dispatcher dispatcher, Func<WinUIApplication> appBuilder)
	{
		_current = this;
		_dispatcher = dispatcher;
		_appBuilder = appBuilder;
	}

	internal static WpfHost? Current => _current;

	/// <summary>
	/// Gets or sets the current Skia Render surface type.
	/// </summary>
	/// <remarks>If <c>null</c>, the host will try to determine the most compatible mode.</remarks>
	public RenderSurfaceType? RenderSurfaceType { get; set; }

	public bool IgnorePixelScaling
	{
		get => _ignorePixelScaling;
		set
		{
			_ignorePixelScaling = value;
			// TODO:MZ: InvalidateVisual();
		}
	}

	public void Run()
	{
		InitializeDispatcher();

		// App needs to be created after the native overlay layer is properly initialized
		// otherwise the initially focused input element would cause exception.
		// TODO:MZ: Verify this is not broken after the changes
		StartApp();

		SetupMainWindow();
	}

	private void InitializeDispatcher()
	{
		Windows.UI.Core.CoreDispatcher.DispatchOverride = d => _dispatcher.BeginInvoke(d);
		Windows.UI.Core.CoreDispatcher.HasThreadAccessOverride = _dispatcher.CheckAccess;
	}

	private void SetupMainWindow()
	{
		WpfApplication.Current.MainWindow = new UnoWpfWindow(WinUI.Window.Current);
		WpfApplication.Current.MainWindow.Activated += MainWindow_Activated;
		WpfApplication.Current.MainWindow.Deactivated += MainWindow_Deactivated;
		WpfApplication.Current.MainWindow.StateChanged += MainWindow_StateChanged;
		WpfApplication.Current.MainWindow.Closing += MainWindow_Closing;
	}

	private void MainWindow_Closing(object? sender, CancelEventArgs e)
	{
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

	private void StartApp()
	{
		void CreateApp(WinUI.ApplicationInitializationCallbackParams _)
		{
			var app = _appBuilder();
			app.Host = this;
		}

		WinUIApplication.StartWithArguments(CreateApp);
	}

	private void MainWindow_StateChanged(object? sender, EventArgs e)
	{
		var wpfWindow = WpfApplication.Current.MainWindow;
		var winUIWindow = WinUI.Window.Current;
		var application = WinUI.Application.Current;
		var wasVisible = _isVisible;

		_isVisible = wpfWindow.WindowState != WindowState.Minimized;

		if (wasVisible && !_isVisible)
		{
			winUIWindow.OnVisibilityChanged(false);
			application?.RaiseEnteredBackground(null);
		}
		else if (!wasVisible && _isVisible)
		{
			application?.RaiseLeavingBackground(() => winUIWindow?.OnVisibilityChanged(true));
		}
	}

	private void MainWindow_Deactivated(object? sender, EventArgs e)
	{
		var winUIWindow = WinUI.Window.Current;
		winUIWindow?.RaiseActivated(Windows.UI.Core.CoreWindowActivationState.Deactivated);
	}

	private void MainWindow_Activated(object? sender, EventArgs e)
	{
		var winUIWindow = WinUI.Window.Current;
		winUIWindow?.RaiseActivated(Windows.UI.Core.CoreWindowActivationState.CodeActivated);
	}
}
