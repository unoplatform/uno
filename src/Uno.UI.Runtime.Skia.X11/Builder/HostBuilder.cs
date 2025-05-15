using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.Hosting;
using Windows.UI.WebUI;

namespace Uno.UI.Hosting;

public static class HostBuilder
{
	public static IUnoPlatformHostBuilder UseX11(this IUnoPlatformHostBuilder builder)
	{
		builder.AddHostBuilder(() => new X11HostBuilder());
		return builder;
	}

	public static IUnoPlatformHostBuilder UseX11(this IUnoPlatformHostBuilder builder, bool preloadMediaPlayer)
	{
		builder.AddHostBuilder(() => new X11HostBuilder().PreloadMediaPlayer(preloadMediaPlayer));
		return builder;
	}
}
