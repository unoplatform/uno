#nullable enable

using System;

namespace Uno.UI.Hosting;

public static class HostBuilder
{
	/// <summary>
	/// Adds the Wayland host. Register it before <c>UseX11</c> so that it is preferred when a Wayland compositor is available.
	/// </summary>
	public static IUnoPlatformHostBuilder UseWayland(this IUnoPlatformHostBuilder builder)
	{
		builder.AddHostBuilder(() => new WaylandHostBuilder());
		return builder;
	}

	/// <summary>
	/// Adds the Wayland host. Register it before <c>UseX11</c> so that it is preferred when a Wayland compositor is available.
	/// </summary>
	public static IUnoPlatformHostBuilder UseWayland(this IUnoPlatformHostBuilder builder, Action<WaylandHostBuilder> action)
	{
		// Eager: AddHostBuilder defers the callback, so a null would only fault when the host is built.
		ArgumentNullException.ThrowIfNull(action);

		builder.AddHostBuilder(() =>
		{
			var waylandBuilder = new WaylandHostBuilder();
			if (((IPlatformHostBuilder)waylandBuilder).IsSupported)
			{
				action.Invoke(waylandBuilder);
			}
			return waylandBuilder;
		});

		return builder;
	}
}
