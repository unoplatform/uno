using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Uno.Foundation.Extensibility;
using Uno.UI.Hosting;
using Uno.UI.Xaml.Controls;
using Uno.UI.Xaml.Controls.Extensions;
using Windows.UI.Core;

namespace Uno.UI.Runtime.Skia.Android;

public class AndroidSkiaHost : ISkiaApplicationHost
{
	private Func<Application> _appBuilder;

	/// <summary>
	/// Creates a host for an Uno Skia Android application.
	/// </summary>
	/// <param name="appBuilder">App builder.</param>
	/// <remarks>
	/// Environment.CommandLine is used to fill LaunchEventArgs.Arguments.
	/// </remarks>
	public AndroidSkiaHost(Func<Application> appBuilder)
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
			ApiExtensibility.Register<TextBoxView>(typeof(IOverlayTextBoxViewExtension), o => new AndroidInvisibleTextBoxViewExtension(o));
			ApiExtensibility.Register<ContentPresenter>(typeof(ContentPresenter.INativeElementHostingExtension), o => new AndroidSkiaNativeElementHostingExtension(o));
			ApiExtensibility.Register<CoreWebView2>(typeof(INativeWebViewProvider), o => new AndroidNativeWebViewProvider(o));
			ApiExtensibility.Register(typeof(ISkiaNativeDatePickerProviderExtension), _ => new AndroidSkiaDatePickerProvider());

			Application.Start(_ => _appBuilder());
		}
		catch (Exception e)
		{
			Console.WriteLine($"App failed to initialize: {e}");
		}

		return Task.CompletedTask;
	}
}
