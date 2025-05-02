#nullable enable

using System;

namespace Uno.UI.Hosting;

public static class HostBuilder
{
	public static IUnoPlatformHostBuilder UseWebAssembly(this IUnoPlatformHostBuilder builder)
	{
		builder.AddHostBuilder(() => new WebAssemblyHostBuilder());

		return builder;
	}
}
