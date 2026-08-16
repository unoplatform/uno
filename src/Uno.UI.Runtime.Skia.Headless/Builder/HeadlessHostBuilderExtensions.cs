#nullable enable

using System;
using Uno.UI.Runtime.Skia;

namespace Uno.UI.Hosting;

// Uniquely named (not "HostBuilder") so the type doesn't collide with the other Skia hosts, which each
// declare their own Uno.UI.Hosting.HostBuilder — referencing two of them would otherwise trip CS0433.
public static class HeadlessHostBuilderExtensions
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
