using System;

namespace Uno.UI.Hosting;

public static class HostBuilder
{
	public static IUnoPlatformHostBuilder UseWebAssembly(this IUnoPlatformHostBuilder builder)
	{
		builder.AddHostBuilder(() => new WebAssemblyHostBuilder());
		return builder;
	}

	public static IUnoPlatformHostBuilder UseWebAssembly(this IUnoPlatformHostBuilder builder, Action<WebAssemblyHostBuilder> action)
	{
		builder.AddHostBuilder(() =>
		{
			var webAssemblyHostBuilder = new WebAssemblyHostBuilder();
			if (((IPlatformHostBuilder)webAssemblyHostBuilder).IsSupported)
			{
				action.Invoke(webAssemblyHostBuilder);
			}
			return webAssemblyHostBuilder;
		});

		return builder;
	}
}
