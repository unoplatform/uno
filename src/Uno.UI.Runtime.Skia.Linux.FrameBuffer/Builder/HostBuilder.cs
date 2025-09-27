using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.Runtime.Skia;
using Windows.UI.WebUI;

namespace Uno.UI.Hosting;

public static class HostBuilder
{
	public static IUnoPlatformHostBuilder UseLinuxFrameBuffer(this IUnoPlatformHostBuilder builder)
	{
		builder.AddHostBuilder(() => new FramebufferHostBuilder());
		return builder;
	}
}
