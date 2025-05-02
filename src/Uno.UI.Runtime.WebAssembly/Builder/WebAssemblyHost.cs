#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;

namespace Uno.UI.Hosting.WebAssembly;

internal partial class WebAssemblyBrowserHost : UnoPlatformHost, ISkiaApplicationHost
{
	private Func<Application> _appBuilder;

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
	}

	protected override void Initialize()
	{
	}

	protected async override Task RunLoop()
	{
		void CreateApp(ApplicationInitializationCallbackParams _)
		{
			_appBuilder();
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
}
