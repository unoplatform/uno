using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Uno.Foundation.Extensibility;
using Uno.UI.Hosting;
using Uno.UI.Xaml.Controls;

namespace Uno.UI.Runtime.Skia.Android;

public class AndroidSkiaHost : ISkiaApplicationHost, IXamlRootHost
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

			Application.Start(_ => _appBuilder());
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
