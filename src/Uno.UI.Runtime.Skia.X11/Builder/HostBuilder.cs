using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia;
using Windows.UI.WebUI;

namespace Uno.UI.Hosting;

public static class HostBuilder
{
	public static IUnoPlatformHostBuilder UseX11(this IUnoPlatformHostBuilder builder)
	{
		LinuxBackgroundTaskExtensions.Register();
		builder.AddHostBuilder(() => new X11HostBuilder());
		return builder;
	}

	public static IUnoPlatformHostBuilder UseX11(this IUnoPlatformHostBuilder builder, Action<X11HostBuilder> action)
	{
		// Eager: AddHostBuilder defers the callback, so a null would only fault when the host is built.
		ArgumentNullException.ThrowIfNull(action);
		LinuxBackgroundTaskExtensions.Register();

		builder.AddHostBuilder(() =>
		{
			var x11Builder = new X11HostBuilder();
			if (((IPlatformHostBuilder)x11Builder).IsSupported)
			{
				action.Invoke(x11Builder);
			}
			return x11Builder;
		});

		return builder;
	}
}
