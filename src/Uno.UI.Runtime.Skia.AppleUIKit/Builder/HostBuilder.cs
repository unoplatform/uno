using System;
using Uno.UI.Runtime.Skia.AppleUIKit;

namespace Uno.UI.Hosting;

public static class HostBuilder
{
	public static IUnoPlatformHostBuilder UseAppleUIKit(this IUnoPlatformHostBuilder builder, Action<IAppleUIKitSkiaHostBuilder>? appleUIKitBuilder = null)
	{
		builder.AddHostBuilder(() =>
		{
			var platformBuilder = new AppleUIKitHostBuilder();
			if (appleUIKitBuilder is not null)
			{
				appleUIKitBuilder?.Invoke(platformBuilder);
			}
			return platformBuilder;
		});

		return builder;
	}
}
