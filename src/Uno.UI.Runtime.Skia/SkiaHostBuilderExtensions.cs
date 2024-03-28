namespace Uno.UI.Runtime.Skia;

public static class SkiaHostBuilderExtensions
{
	/// <summary>
	/// Provides an <see cref="Windows.UI.Xaml.Application"/> instance to use when starting the app.
	/// </summary>
	public static ISkiaHostBuilder App(this ISkiaHostBuilder builder, Func<Windows.UI.Xaml.Application> appBuilder)
	{
		builder.AppBuilder = appBuilder;
		return builder;
	}

	/// <summary>
	/// Provides an action to be executed after the SkiaHost has been initialized, and before the run loop starts.
	/// </summary>
	public static ISkiaHostBuilder AfterInit(this ISkiaHostBuilder builder, Action action)
	{
		builder.AfterInit = action;
		return builder;
	}
}
