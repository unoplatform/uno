#nullable enable

using System;

namespace Uno.UI.Hosting;

public static class UnoPlatformHostBuilderExtensions
{
	/// <summary>
	/// Provides an <see cref="Microsoft.UI.Xaml.Application"/> instance to use when starting the app.
	/// </summary>
	public static IUnoPlatformHostBuilder App(this IUnoPlatformHostBuilder builder, Func<Microsoft.UI.Xaml.Application> appBuilder)
	{
		builder.AppBuilder = appBuilder;
		return builder;
	}

	/// <summary>
	/// Provides an action to be executed after the SkiaHost has been initialized, and before the run loop starts.
	/// </summary>
	public static IUnoPlatformHostBuilder AfterInit(this IUnoPlatformHostBuilder builder, Action action)
	{
		builder.AfterInit = action;
		return builder;
	}
}
