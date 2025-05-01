#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using UIKit;
using Uno.UI.Hosting;

namespace Uno.UI.Hosting.UIKit;

internal class AppleUIKitHost : UnoPlatformHost
{
	private readonly Type _appType;

	/// <summary>
	/// Creates a host for an Uno Skia Apple UIKit application.
	/// </summary>
	/// <param name="appBuilder">App builder.</param>
	/// <remarks>
	/// Environment.CommandLine is used to fill LaunchEventArgs.Arguments.
	/// </remarks>
	public AppleUIKitHost(Type appType)
	{
		_appType = appType ?? throw new ArgumentNullException(nameof(appType));
	}

	internal static ApplicationInitializationCallback? CreateAppAction { get; private set; }

	protected override void Initialize()
	{
	}

	protected override Task RunLoop()
	{
		try
		{
			if (_appType is null)
			{
				throw new InvalidOperationException("UIApplicationDelegate must be provided for UIKit native");
			}

			UIApplication.Main(Environment.GetCommandLineArgs(), null, _appType);

			return Task.CompletedTask;
		}
		catch (Exception e)
		{
			Console.WriteLine($"App failed to initialize: {e}");

			throw;
		}
	}
}
