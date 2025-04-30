#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using UIKit;
using Uno.UI.Hosting;

#if SKIA_UIKIT
using HostType = Uno.UI.Runtime.Skia.SkiaHost;
#else
using HostType = Uno.UI.Hosting.UnoPlatformHost;
#endif

namespace Uno.UI.Hosting.UIKit;

internal class AppleUIKitHost : HostType, ISkiaApplicationHost
{
	private readonly Func<Application> _appBuilder;
	private readonly Type? _uiApplicationDelegateOverride;

	/// <summary>
	/// Creates a host for an Uno Skia Android application.
	/// </summary>
	/// <param name="appBuilder">App builder.</param>
	/// <remarks>
	/// Environment.CommandLine is used to fill LaunchEventArgs.Arguments.
	/// </remarks>
	public AppleUIKitHost(Func<Application> appBuilder, Type? uiApplicationDelegateOverride)
	{
		_appBuilder = appBuilder ?? throw new ArgumentNullException(nameof(appBuilder));
		_uiApplicationDelegateOverride = uiApplicationDelegateOverride;
	}

	internal static ApplicationInitializationCallback? CreateAppAction { get; private set; }

	protected override void Initialize()
	{
	}

	protected override Task RunLoop()
	{
		try
		{
			if (_uiApplicationDelegateOverride is null)
			{
				throw new InvalidOperationException("UIApplicationDelegate must be provided for UIKit native");
			}

			var delegateType = _uiApplicationDelegateOverride;

			UIApplication.Main(Environment.GetCommandLineArgs(), null, delegateType);

			return Task.CompletedTask;
		}
		catch (Exception e)
		{
			Console.WriteLine($"App failed to initialize: {e}");

			throw;
		}
	}
}
