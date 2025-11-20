using System;
using Uno.UI.Runtime.Skia;

namespace Uno.UI.Hosting;

public static class HostBuilder
{
	public static IUnoPlatformHostBuilder UseLinuxFrameBuffer(this IUnoPlatformHostBuilder builder)
	{
		builder.AddHostBuilder(() => new FramebufferHostBuilder());
		return builder;
	}

	public static IUnoPlatformHostBuilder UseX11(this IUnoPlatformHostBuilder builder, Action<FramebufferHostBuilder> action)
	{
		builder.AddHostBuilder(() =>
		{
			var fbBuilder = new FramebufferHostBuilder();
			if (((IPlatformHostBuilder)fbBuilder).IsSupported)
			{
				action.Invoke(fbBuilder);
			}
			return fbBuilder;
		});

		return builder;
	}
}
