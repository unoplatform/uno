using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using UIKit;
using Uno.UI.Helpers;
using Uno.WinUI.Runtime.Skia.AppleUIKit.Extensions;

namespace Uno.UI.Runtime.Skia.AppleUIKit;

internal class AppleUIKitHost : SkiaHost, ISkiaApplicationHost
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

		PlatformRuntimeHelper.SkiaPlatform = UnoRuntimePlatform.SkiaAppleUIKit;
	}

	internal static ApplicationInitializationCallback? CreateAppAction { get; private set; }

	protected override void Initialize() => ExtensionsRegistrar.Register();

	protected override Task RunLoop()
	{
		try
		{
			CreateAppAction = (ApplicationInitializationCallbackParams _) =>
			{
				var app = _appBuilder.Invoke();
				app.Host = this;
			};

			var delegateType = _uiApplicationDelegateOverride ?? typeof(UnoUIApplicationDelegate);

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
