using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.WebUI;

namespace Uno.UI.Runtime.Skia;

public static class HostBuilder
{
	public static ISkiaHostBuilder UseX11(this ISkiaHostBuilder builder)
	{
		builder.AddHostBuilder(() => new X11HostBuilder());
		return builder;
	}
}
