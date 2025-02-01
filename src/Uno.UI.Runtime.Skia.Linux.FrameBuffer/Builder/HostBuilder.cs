using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.WebUI;

namespace Uno.UI.Runtime.Skia;

public static class HostBuilder
{
	public static ISkiaHostBuilder UseLinuxFrameBuffer(this ISkiaHostBuilder builder)
	{
		builder.AddHostBuilder(() => new FramebufferHostBuilder());
		return builder;
	}
}
