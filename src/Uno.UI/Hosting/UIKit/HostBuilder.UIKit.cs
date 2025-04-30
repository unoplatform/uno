#nullable enable

using System;

namespace Uno.UI.Hosting;

public static class HostBuilder
{
	public static IUnoPlatformHostBuilder UseAppleUIKit(this IUnoPlatformHostBuilder builder, Action<IAppleUIKitHostBuilder>? appleUIKitBuilder = null)
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
