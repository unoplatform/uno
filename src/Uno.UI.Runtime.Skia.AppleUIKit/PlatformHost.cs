using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UIKit;
using Uno.Foundation.Extensibility;
using Uno.UI.Hosting;
using Uno.UI.Xaml.Controls;
using Uno.UI.Xaml.Controls.Extensions;
using Uno.WinUI.Runtime.Skia.AppleUIKit;
using Uno.WinUI.Runtime.Skia.AppleUIKit.Extensions;
using Uno.WinUI.Runtime.Skia.AppleUIKit.UI.Xaml;
using Windows.UI.Core;

namespace Uno.UI.Runtime.Skia.AppleUIKit;

public class PlatformHost : ISkiaApplicationHost
{
	private static Func<Application>? _appBuilder;

	/// <summary>
	/// Creates a host for an Uno Skia Android application.
	/// </summary>
	/// <param name="appBuilder">App builder.</param>
	/// <remarks>
	/// Environment.CommandLine is used to fill LaunchEventArgs.Arguments.
	/// </remarks>
	public PlatformHost(Func<Application> appBuilder)
	{
		_appBuilder = appBuilder;
	}

	internal static Func<Application>? AppBuilder => _appBuilder;

	public Task Run()
	{
		try
		{
			ExtensionsRegistrar.Register();

			UIApplication.Main(Environment.GetCommandLineArgs(), null, typeof(UnoSkiaAppDelegate));
		}
		catch (Exception e)
		{
			Console.WriteLine($"App failed to initialize: {e}");
		}

		return Task.CompletedTask;
	}
}
