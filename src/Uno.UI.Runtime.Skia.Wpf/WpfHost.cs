#nullable enable

using System;
using System.Windows.Threading;
using Uno.UI.Runtime.Skia.Wpf.Extensions;
using Uno.UI.Runtime.Skia.Wpf.Extensions.UI.Xaml.Controls;
using Uno.UI.Runtime.Skia.Wpf.Hosting;
using Uno.UI.Runtime.Skia.Wpf.UI.Controls;
using Uno.UI.Xaml.Controls;
using WinUI = Microsoft.UI.Xaml;
using WinUIApplication = Microsoft.UI.Xaml.Application;
using WpfApplication = System.Windows.Application;

namespace Uno.UI.Runtime.Skia.Wpf;

public class WpfHost : IWpfApplicationHost
{
	private readonly Dispatcher _dispatcher;
	private readonly Func<WinUIApplication> _appBuilder;

	[ThreadStatic] private static WpfHost? _current;

	private bool _ignorePixelScaling;

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
			if (WpfApplication.Current.MainWindow is UnoWpfWindow window)
			{
				window.InvalidateVisual();
			}
		}
	}

	public void Run()
	{
		InitializeDispatcher();

		// App needs to be created after the native overlay layer is properly initialized
		// otherwise the initially focused input element would cause exception.
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
		var unoWpfWindow = NativeWindowFactory.CreateWindow(WinUI.Window.Current) as WpfWindowWrapper;
		if (unoWpfWindow is null)
		{
			throw new InvalidOperationException("Window is not valid");
		}

		WpfApplication.Current.MainWindow = unoWpfWindow.NativeWindow;
		unoWpfWindow.NativeWindow.Activated += MainWindow_Activated;
	}

	internal event EventHandler? MainWindowShown;

	private void MainWindow_Activated(object? sender, EventArgs e) => MainWindowShown?.Invoke(this, EventArgs.Empty);

	private void StartApp()
	{
		void CreateApp(WinUI.ApplicationInitializationCallbackParams _)
		{
			var app = _appBuilder();
			app.Host = this;
		}

		WinUIApplication.StartWithArguments(CreateApp);
	}

	public override string ToString() =>
		"If you are seeing this, make sure to follow the \"Migrating WpfHost\" section of Migrating from " +
		"previous releases article in the Uno Platform documentation at " +
		"https://aka.platform.uno/uno5-wpfhost-migration. " +
		"WpfHost is used at the application level instead of window level starting Uno Platform 5.0.";
}
