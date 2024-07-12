using System;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.ApplicationModel.Core;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.UI.Hosting;
using Windows.Graphics.Display;
using Windows.Media.Playback;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.Media.Playback;
using Uno.UI.Xaml.Controls;
using Uno.UI.Xaml.Controls.Extensions;

namespace Uno.UI.Runtime.Skia.WebAssembly.Browser;

public class PlatformHost : ISkiaApplicationHost, IXamlRootHost
{
	private readonly CoreApplicationExtension? _coreApplicationExtension;

	private Func<Application> _appBuilder;
	private BrowserRenderer? _renderer;
	private ManualResetEvent _terminationGate = new(false);

	/// <summary>
	/// Creates a host for a Uno Skia FrameBuffer application.
	/// </summary>
	/// <param name="appBuilder">App builder.</param>
	/// <remarks>
	/// Environment.CommandLine is used to fill LaunchEventArgs.Arguments.
	/// </remarks>
	public PlatformHost(Func<Application> appBuilder)
	{
		_appBuilder = appBuilder;

		_coreApplicationExtension = new CoreApplicationExtension(_terminationGate);
	}

	public async Task Run()
	{
		try
		{
			Initialize();

			await Task.Delay(-1);
		}
		catch (Exception e)
		{
			Console.WriteLine($"App failed to initialize: {e}");
		}
	}

	private void Initialize()
	{
		ApiExtensibility.Register(typeof(Uno.ApplicationModel.Core.ICoreApplicationExtension), o => _coreApplicationExtension!);
		ApiExtensibility.Register(typeof(Windows.UI.Core.IUnoCorePointerInputSource), o => new BrowserPointerInputSource());
		ApiExtensibility.Register(typeof(Windows.UI.Core.IUnoKeyboardInputSource), o => new BrowserKeyboardInputSource());
		ApiExtensibility.Register(typeof(INativeWindowFactoryExtension), o => new WebAssemblyWindowFactoryExtension(this));
		ApiExtensibility.Register<TextBoxView>(typeof(IOverlayTextBoxViewExtension), o => new BrowserInvisibleTextBoxViewExtension(o));
		ApiExtensibility.Register<ContentPresenter>(typeof(ContentPresenter.INativeElementHostingExtension), o => new BrowserNativeElementHostingExtension(o));
		ApiExtensibility.Register<MediaPlayer>(typeof(IMediaPlayerExtension), o => new BrowserMediaPlayerExtension(o));
		ApiExtensibility.Register<MediaPlayerPresenter>(typeof(IMediaPlayerPresenterExtension), o => new BrowserMediaPlayerPresenterExtension(o));

		void CreateApp(ApplicationInitializationCallbackParams _)
		{
			var app = _appBuilder();
			app.Host = this;

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"Display Information: " +
					$"ResolutionScale: {DisplayInformation.GetForCurrentView().ResolutionScale}, " +
					$"LogicalDpi: {DisplayInformation.GetForCurrentView().LogicalDpi}, " +
					$"RawPixelsPerViewPixel: {DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel}, " +
					$"DiagonalSizeInInches: {DisplayInformation.GetForCurrentView().DiagonalSizeInInches}, " +
					$"ScreenInRawPixels: {DisplayInformation.GetForCurrentView().ScreenWidthInRawPixels}x{DisplayInformation.GetForCurrentView().ScreenHeightInRawPixels}");
			}

			// Force initialization of the DisplayInformation
			DisplayInformation.GetForCurrentView();
		}

		_renderer = new BrowserRenderer(this);

		//CoreServices.Instance.ContentRootCoordinator.CoreWindowContentRootSet += OnCoreWindowContentRootSet;

		Application.Start(CreateApp);
	}

	//private void OnCoreWindowContentRootSet(object? sender, object e)
	//{
	//	var contentRoot = CoreServices.Instance
	//		.ContentRootCoordinator
	//		.CoreWindowContentRoot;
	//	var xamlRoot = contentRoot?.GetOrCreateXamlRoot();

	//	if (xamlRoot is null)
	//	{
	//		throw new InvalidOperationException("XamlRoot was not properly initialized");
	//	}

	//	contentRoot!.SetHost(this);
	//	AppManager.XamlRootMap.Register(xamlRoot, this);

	//	CoreServices.Instance.ContentRootCoordinator.CoreWindowContentRootSet -= OnCoreWindowContentRootSet;
	//}

	void IXamlRootHost.InvalidateRender()
	{
		_renderer?.InvalidateRender();
		Window.CurrentSafe!.RootElement?.XamlRoot?.InvalidateOverlays();
	}

	UIElement? IXamlRootHost.RootElement => Window.Current!.RootElement;
}
