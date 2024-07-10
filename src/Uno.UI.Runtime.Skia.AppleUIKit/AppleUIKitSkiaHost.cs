using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using UIKit;
using Uno.Foundation.Extensibility;
using Uno.UI.Hosting;
using Uno.UI.Xaml.Controls;
using Uno.WinUI.Runtime.Skia.AppleUIKit.UI.Xaml;
using Windows.UI.Core;

namespace Uno.UI.Runtime.Skia.AppleUIKit;

public class AppleUIKitSkiaHost : ISkiaApplicationHost
{
	private static Func<Application>? _appBuilder;
	private static string[]? _args;

	/// <summary>
	/// Creates a host for an Uno Skia Android application.
	/// </summary>
	/// <param name="appBuilder">App builder.</param>
	/// <remarks>
	/// Environment.CommandLine is used to fill LaunchEventArgs.Arguments.
	/// </remarks>
	public AppleUIKitSkiaHost(Func<Application> appBuilder, string[] args)
	{
		_appBuilder = appBuilder;
		_args = args;
	}

	internal static Func<Application>? AppBuilder => _appBuilder;

	internal static string[]? Args => _args;

	public Task Run()
	{
		try
		{
			ApiExtensibility.Register(typeof(INativeWindowFactoryExtension), o => new NativeWindowFactoryExtension());
			ApiExtensibility.Register<IXamlRootHost>(typeof(IUnoCorePointerInputSource), o => AppleUIKitCorePointerInputSource.Instance);

			UIApplication.Main(_args, null, typeof(UnoSkiaAppDelegate));
		}
		catch (Exception e)
		{
			Console.WriteLine($"App failed to initialize: {e}");
		}

		return Task.CompletedTask;
	}
}
