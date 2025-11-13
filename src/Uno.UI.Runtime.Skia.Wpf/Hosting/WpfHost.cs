#nullable enable

using System;
using System.Threading.Tasks;
using System.Windows.Threading;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Runtime.Skia.Wpf.Extensions;
using Uno.UI.Runtime.Skia.Wpf.Extensions.UI.Xaml.Controls;
using Uno.UI.Runtime.Skia.Wpf.Hosting;
using Uno.UI.Runtime.Skia.Wpf.UI.Controls;
using Uno.UI.Xaml.Controls;
using WinUI = Microsoft.UI.Xaml;
using WinUIApplication = Microsoft.UI.Xaml.Application;
using WpfApplication = System.Windows.Application;

namespace Uno.UI.Runtime.Skia.Wpf;

public class WpfHost : SkiaHost, IWpfApplicationHost
{
	private readonly Dispatcher _dispatcher;
	private readonly Func<WinUIApplication> _appBuilder;
	private readonly WpfApplication? _wpfApp;

	[ThreadStatic] private static WpfHost? _current;

	private bool _ignorePixelScaling;

	static WpfHost()
		=> WpfExtensionsRegistrar.Register();

	public WpfHost(Dispatcher dispatcher, Func<WinUIApplication> appBuilder)
	{
		_current = this;
		_dispatcher = dispatcher;
		_appBuilder = appBuilder;
	}

	internal WpfHost(Func<WinUIApplication> appBuilder, Func<WpfApplication>? wpfAppBuilder)
	{
		_wpfApp = wpfAppBuilder?.Invoke() ?? new WpfApplication();

		_current = this;
		_dispatcher = _wpfApp.Dispatcher;
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
			if (WpfApplication.Current.MainWindow is UnoWpfWindow window)
			{
				window.InvalidateVisual();
			}
		}
	}

	protected override void Initialize()
	{
		InitializeDispatcher();
	}

	protected override Task RunLoop()
	{
		// App needs to be created after the native overlay layer is properly initialized
		// otherwise the initially focused input element would cause exception.
		StartApp();

		_wpfApp?.Run();

		return Task.CompletedTask;
	}

	private void InitializeDispatcher()
	{
		Windows.UI.Core.CoreDispatcher.DispatchOverride = (d, p) => _dispatcher.BeginInvoke(d, p == Uno.UI.Dispatching.NativeDispatcherPriority.Idle ? DispatcherPriority.SystemIdle : DispatcherPriority.Render);
		Windows.UI.Core.CoreDispatcher.HasThreadAccessOverride = _dispatcher.CheckAccess;
	}

	private void StartApp()
	{
		void CreateApp(WinUI.ApplicationInitializationCallbackParams _)
		{
			var app = _appBuilder();
			app.Host = this;
		}

		WinUIApplication.Start(CreateApp);
	}

	public override string ToString() =>
		"If you are seeing this, make sure to follow the \"Migrating WpfHost\" section of Migrating from " +
		"previous releases article in the Uno Platform documentation at " +
		"https://aka.platform.uno/uno5-wpfhost-migration. " +
		"WpfHost is used at the application level instead of window level starting Uno Platform 5.0.";
}
