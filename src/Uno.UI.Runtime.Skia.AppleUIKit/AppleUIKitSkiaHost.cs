using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using UIKit;
using Uno.Foundation.Extensibility;
using Uno.UI.Hosting;
using Uno.UI.Xaml.Controls;

namespace Uno.UI.Runtime.Skia.AppleUIKit;

public class AppleUIKitSkiaHost : ISkiaApplicationHost, IXamlRootHost
{
	private readonly Func<Application> _appBuilder;
	private readonly string[] _args;

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

	public Task Run()
	{
		try
		{
			ApiExtensibility.Register(typeof(INativeWindowFactoryExtension), o => new SingletonWindowFactory(NativeWindowWrapper.Instance));

			Application.Start(_ => _appBuilder());

			UIApplication.Main(_args, null, typeof(App));
		}
		catch (Exception e)
		{
			Console.WriteLine($"App failed to initialize: {e}");
		}

		return Task.CompletedTask;
	}

	void IXamlRootHost.InvalidateRender()
	{
	}

	UIElement? IXamlRootHost.RootElement => Window.Current!.RootElement;
}
