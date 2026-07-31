#nullable enable

using System;

namespace Uno.UI.Hosting;

public static class AndroidHostBuilderExtensions
{
	public static IUnoPlatformHostBuilder UseAndroid(this IUnoPlatformHostBuilder builder)
	{
		builder.AddHostBuilder(() => new AndroidHostBuilder());
		return builder;
	}

	public static IUnoPlatformHostBuilder UseAndroid(this IUnoPlatformHostBuilder builder, Action<IAndroidSkiaHostBuilder> action)
	{
		builder.AddHostBuilder(() =>
		{
			var androidBuilder = new AndroidHostBuilder();
			if (((IPlatformHostBuilder)androidBuilder).IsSupported)
			{
				action.Invoke(androidBuilder);
			}

			return androidBuilder;
		});

		return builder;
	}
}
