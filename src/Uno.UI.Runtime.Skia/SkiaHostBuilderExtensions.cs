namespace Uno.UI.Runtime.Skia
{
	public static class SkiaHostBuilderExtensions
	{
		public static ISkiaHostBuilder App(this ISkiaHostBuilder builder, Func<Microsoft.UI.Xaml.Application> appBuilder)
		{
			builder.AppBuilder = appBuilder;
			return builder;
		}

		public static ISkiaHostBuilder AfterInit(this ISkiaHostBuilder builder, Action action)
		{
			builder.AfterInit = action;
			return builder;
		}
	}
}
