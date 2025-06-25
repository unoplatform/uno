using System;

namespace Uno.UI.Hosting;

public static class HostBuilderExtensions
{
	public static IUnoPlatformHostBuilder UseWin32(this IUnoPlatformHostBuilder builder)
	{
		builder.AddHostBuilder(() => new Win32HostBuilder());
		return builder;
	}

	public static IUnoPlatformHostBuilder UseWin32(this IUnoPlatformHostBuilder builder, Action<Win32HostBuilder> action)
	{
		builder.AddHostBuilder(() =>
		{
			var win32Builder = new Win32HostBuilder();
			if (((IPlatformHostBuilder)win32Builder).IsSupported)
			{
				action.Invoke(win32Builder);
			}
			return win32Builder;
		});

		return builder;
	}
}
