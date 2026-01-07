using System;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Uno.Extensions.ApplicationModel.Core;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.Media.Playback;
using Uno.UI.Hosting;
using Uno.UI.NativeElementHosting;
using Uno.UI.Xaml.Controls;
using Uno.UI.Xaml.Controls.Extensions;
using Windows.Graphics.Display;
using Windows.Media.Playback;
using Microsoft.UI.Xaml.Media;

namespace Uno.UI.Runtime.Skia.WebAssembly.Browser;

internal partial class WebAssemblyBrowserHost : SkiaHost, ISkiaApplicationHost, IXamlRootHost
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
	public WebAssemblyBrowserHost(Func<Application> appBuilder)
	{
		_appBuilder = appBuilder;

		_coreApplicationExtension = new CoreApplicationExtension(_terminationGate);
	}

	protected override void Initialize()
	{
		ApiExtensibility.Register(typeof(Uno.ApplicationModel.Core.ICoreApplicationExtension), o => _coreApplicationExtension!);
		ApiExtensibility.Register(typeof(Windows.UI.Core.IUnoCorePointerInputSource), o => new BrowserPointerInputSource());
		ApiExtensibility.Register(typeof(Windows.UI.Core.IUnoKeyboardInputSource), o => new BrowserKeyboardInputSource());
		ApiExtensibility.Register(typeof(INativeWindowFactoryExtension), o => new WebAssemblyWindowFactoryExtension(this));
		ApiExtensibility.Register<TextBoxView>(typeof(IOverlayTextBoxViewExtension), o => new BrowserInvisibleTextBoxViewExtension(o));
		ApiExtensibility.Register<ContentPresenter>(typeof(ContentPresenter.INativeElementHostingExtension), o => new BrowserNativeElementHostingExtension(o));
		ApiExtensibility.Register<MediaPlayer>(typeof(IMediaPlayerExtension), o => new BrowserMediaPlayerExtension(o));
		ApiExtensibility.Register<MediaPlayerPresenter>(typeof(IMediaPlayerPresenterExtension), o => new BrowserMediaPlayerPresenterExtension(o));
		ApiExtensibility.Register<CoreWebView2>(typeof(INativeWebViewProvider), o => new BrowserWebViewProvider(o));
		ApiExtensibility.Register<Windows.UI.ViewManagement.InputPane>(
			typeof(Windows.UI.ViewManagement.IInputPaneExtension),
			_ => new Uno.WinUI.Runtime.Skia.WebAssembly.InputPaneExtension());

		NativeMethods.PersistBootstrapperLoader();

		CompositionTarget.FrameRenderingOptions = (false, false);
		_renderer = new BrowserRenderer(this);
	}

	protected async override Task RunLoop()
	{
		void CreateApp(ApplicationInitializationCallbackParams _)
		{
			BrowserHtmlElement.Initialize();

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

		try
		{
			Application.Start(CreateApp);

			await Task.Delay(-1);
		}
		catch (Exception e)
		{
			Console.WriteLine($"App failed to initialize: {e}");

			throw;
		}
	}

	void IXamlRootHost.InvalidateRender()
	{
		_renderer?.InvalidateRender();
		Window.CurrentSafe!.RootElement?.XamlRoot?.InvalidateOverlays();
	}

	UIElement? IXamlRootHost.RootElement => Window.CurrentSafe!.RootElement;

	private static partial class NativeMethods
	{
		[JSImport("globalThis.Uno.UI.Runtime.Skia.WebAssemblyWindowWrapper.persistBootstrapperLoader")]
		public static partial void PersistBootstrapperLoader();
	}
}
