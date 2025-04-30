#nullable enable

using System;

namespace Uno.UI.Hosting;

public static class UnoPlatformHostBuilderExtensions
{
	/// <summary>
	/// Provides an <see cref="Microsoft.UI.Xaml.Application"/> instance to use when starting the app.
	/// </summary>
	public static IUnoPlatformHostBuilder App<TApplication>(this IUnoPlatformHostBuilder builder, Func<TApplication> appBuilder)
		where TApplication : Microsoft.UI.Xaml.Application
	{
		builder.AppBuilder = appBuilder;
		builder.SetAppType<TApplication>();
		return builder;
	}

	/// <summary>
	/// Provides an action to be executed after the UnoPlatformHost has been initialized, and before the run loop starts.
	/// </summary>
	public static IUnoPlatformHostBuilder AfterInit(this IUnoPlatformHostBuilder builder, Action action)
	{
		builder.AfterInitAction = action;
		return builder;
	}
}
