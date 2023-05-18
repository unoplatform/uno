#nullable enable

using System;
using System.ComponentModel;
using System.Windows;
using Uno.UI.Runtime.Skia.Wpf.Extensions;
using Uno.UI.Runtime.Skia.Wpf.Hosting;
using Uno.UI.XamlHost.Skia.Wpf;
using Windows.UI.Core.Preview;
using Windows.UI.Xaml.Input;
using UnoApplication = Windows.UI.Xaml.Application;
using WinUI = Windows.UI.Xaml;
using WpfApplication = System.Windows.Application;

namespace Uno.UI.Skia.Platform;

// TODO:MZ: Rename to WpfApplicationHost???
public class WpfHost : IWpfApplicationHost
{
	private readonly Func<UnoApplication> _appBuilder;

	[ThreadStatic] private static WpfHost? _current;

	private bool _ignorePixelScaling;
	private bool _isVisible = true;

	static WpfHost()
	{
		WpfExtensionsRegistrar.Register();
	}

	public WpfHost(global::System.Windows.Threading.Dispatcher dispatcher, Func<WinUI.Application> appBuilder)
	{
		_current = this;
		_appBuilder = appBuilder;

		Windows.UI.Core.CoreDispatcher.DispatchOverride = d => dispatcher.BeginInvoke(d);
		Windows.UI.Core.CoreDispatcher.HasThreadAccessOverride = dispatcher.CheckAccess;

		WpfApplication.Current.Activated += Current_Activated;
		WpfApplication.Current.Deactivated += Current_Deactivated;
		WpfApplication.Current.MainWindow.StateChanged += MainWindow_StateChanged;
		WpfApplication.Current.MainWindow.Closing += MainWindow_Closing;

		// App needs to be created after the native overlay layer is properly initialized
		// otherwise the initially focused input element would cause exception.
		StartApp();
	}

	public static WpfHost? Current => _current;

	private HostPointerHandler? _hostPointerHandler;

	bool IWpfApplicationHost.IsIsland => false;

	/// <summary>
	/// Gets or sets the current Skia Render surface type.
	/// </summary>
	/// <remarks>If <c>null</c>, the host will try to determine the most compatible mode.</remarks>
	public RenderSurfaceType? RenderSurfaceType { get; set; }

	WinUI.UIElement? IWpfApplicationHost.RootElement => null;

	bool IWpfApplicationHost.IgnorePixelScaling => throw new NotImplementedException();

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
		UnoApplication.Current.RaiseSuspending();
	}

	private void StartApp()
	{
		void CreateApp(WinUI.ApplicationInitializationCallbackParams _)
		{
			var app = _appBuilder();
			app.Host = this;
		}

		WinUI.Application.StartWithArguments(CreateApp);
		_hostPointerHandler = new HostPointerHandler(this);
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

	private void Current_Deactivated(object? sender, EventArgs e)
	{
		var winUIWindow = WinUI.Window.Current;
		winUIWindow?.OnActivated(Windows.UI.Core.CoreWindowActivationState.Deactivated);
	}

	private void Current_Activated(object? sender, EventArgs e)
	{
		var winUIWindow = WinUI.Window.Current;
		winUIWindow?.OnActivated(Windows.UI.Core.CoreWindowActivationState.CodeActivated);
	}

	public bool IgnorePixelScaling
	{
		get => _ignorePixelScaling;
		set
		{
			_ignorePixelScaling = value;
			InvalidateVisual();
		}
	}
}
