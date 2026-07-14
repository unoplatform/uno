#nullable enable

using System;
using Uno.UI.Runtime.Skia;

namespace Uno.UI.Hosting;

public static class HostBuilder
{
	public static IUnoPlatformHostBuilder UseHeadless(this IUnoPlatformHostBuilder builder)
	{
		builder.AddHostBuilder(() => new HeadlessHostBuilder());
		return builder;
	}

	public static IUnoPlatformHostBuilder UseHeadless(this IUnoPlatformHostBuilder builder, Action<HeadlessHostBuilder> action)
	{
		builder.AddHostBuilder(() =>
		{
			var headlessBuilder = new HeadlessHostBuilder();
			action.Invoke(headlessBuilder);
			return headlessBuilder;
		});

		return builder;
	}
}
