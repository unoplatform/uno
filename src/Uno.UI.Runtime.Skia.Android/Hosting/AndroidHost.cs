using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Uno.Foundation.Extensibility;
using Uno.UI.Hosting;
using Uno.UI.Xaml.Controls;
using Uno.UI.Xaml.Controls.Extensions;
using Uno.WinUI.Runtime.Skia.Android;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using Uno.WinUI.Runtime.Skia.Android.UI.Xaml.Controls.TextBox;

namespace Uno.UI.Runtime.Skia.Android;

public class AndroidHost : ISkiaApplicationHost
{
	private Func<Application> _appBuilder;

	/// <summary>
	/// Creates a host for an Uno Skia Android application.
	/// </summary>
	/// <param name="appBuilder">App builder.</param>
	/// <remarks>
	/// Environment.CommandLine is used to fill LaunchEventArgs.Arguments.
	/// </remarks>
	public AndroidHost(Func<Application> appBuilder)
	{
		_appBuilder = appBuilder;
	}

	public Task Run()
	{
		try
		{
			ApiExtensibility.Register(typeof(INativeWindowFactoryExtension), o => new AndroidSkiaWindowFactory());
			ApiExtensibility.Register(typeof(IUnoCorePointerInputSource), o => AndroidCorePointerInputSource.Instance);
			ApiExtensibility.Register(typeof(IUnoKeyboardInputSource), o => AndroidKeyboardInputSource.Instance);
			ApiExtensibility.Register(typeof(ITextBoxNotificationsProviderSingleton), _ => AndroidSkiaTextBoxNotificationsProviderSingleton.Instance);
			ApiExtensibility.Register<ContentPresenter>(typeof(ContentPresenter.INativeElementHostingExtension), o => new AndroidSkiaNativeElementHostingExtension(o));
			ApiExtensibility.Register<CoreWebView2>(typeof(INativeWebViewProvider), o => new AndroidNativeWebViewProvider(o));
			ApiExtensibility.Register(typeof(ISkiaNativeDatePickerProviderExtension), _ => new AndroidSkiaDatePickerProvider());
			ApiExtensibility.Register(typeof(ISkiaNativeTimePickerProviderExtension), _ => new AndroidSkiaTimePickerProvider());
			ApiExtensibility.Register(typeof(IInputPaneExtension), _ => new InputPaneExtension());
			ApiExtensibility.Register<MediaPlayerPresenter>(typeof(IMediaPlayerPresenterExtension), o => new AndroidSkiaMediaPlayerPresenterExtension(o));
			ApiExtensibility.Register(typeof(IFontFallbackService), _ => AndroidSkiaFontFallbackService.Instance);

			void CreateApp(ApplicationInitializationCallbackParams _)
			{
				var app = _appBuilder();
				app.Host = this;
			}
			Application.Start(CreateApp);
		}
		catch (Exception e)
		{
			Console.WriteLine($"App failed to initialize: {e}");
		}

		return Task.CompletedTask;
	}
}
