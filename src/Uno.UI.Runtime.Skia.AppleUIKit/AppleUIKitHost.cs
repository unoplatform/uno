using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml;
using UIKit;
using Uno.WinUI.Runtime.Skia.AppleUIKit.Extensions;

namespace Uno.UI.Runtime.Skia.AppleUIKit;

public class AppleUIKitHost : ISkiaApplicationHost
{
	private Type? _uiApplicationDelegateOverride;

	/// <summary>
	/// Creates a host for an Uno Skia Android application.
	/// </summary>
	/// <param name="appBuilder">App builder.</param>
	/// <remarks>
	/// Environment.CommandLine is used to fill LaunchEventArgs.Arguments.
	/// </remarks>
	public AppleUIKitHost(Func<Application> appBuilder)
	{
		CreateAppAction = (ApplicationInitializationCallbackParams _) =>
		{
			var app = appBuilder.Invoke();
			app.Host = this;
		};
	}

	public void SetUIApplicationDelegate<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>()
		where T : UnoUIApplicationDelegate
	{
		_uiApplicationDelegateOverride = typeof(T);
	}

	internal static ApplicationInitializationCallback? CreateAppAction { get; private set; }

	public void Run()
	{
		try
		{
			ExtensionsRegistrar.Register();

			var delegateType = _uiApplicationDelegateOverride ?? typeof(UnoUIApplicationDelegate);

			UIApplication.Main(Environment.GetCommandLineArgs(), null, delegateType);
		}
		catch (Exception e)
		{
			Console.WriteLine($"App failed to initialize: {e}");
		}
	}
}
