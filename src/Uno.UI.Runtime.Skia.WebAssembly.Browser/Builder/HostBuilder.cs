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
		// Eager: AddHostBuilder defers the callback, so a null would only fault when the host is built.
		ArgumentNullException.ThrowIfNull(action);

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
