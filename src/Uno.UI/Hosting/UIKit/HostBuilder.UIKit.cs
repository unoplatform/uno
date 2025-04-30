#nullable enable

using System;
using Uno.UI.Hosting.UIKit;

namespace Uno.UI.Hosting;

public static class HostBuilder
{
	public static IUnoPlatformHostBuilder UseAppleUIKit(this IUnoPlatformHostBuilder builder)
	{
		builder.AddHostBuilder(() => new AppleUIKitHost());

		return builder;
	}
}
