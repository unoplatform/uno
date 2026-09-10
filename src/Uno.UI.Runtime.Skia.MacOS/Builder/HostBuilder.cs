using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia;
using Uno.UI.Runtime.Skia.MacOS;
using Windows.UI.WebUI;

namespace Uno.UI.Hosting;

public static class HostBuilder
{
	public static IUnoPlatformHostBuilder UseMacOS(this IUnoPlatformHostBuilder builder)
	{
		builder.AddHostBuilder(() => new MacOSHostBuilder());
		if (OperatingSystem.IsMacOS())
		{
			MacOSPasswordVaultExtension.Register();
		}
		return builder;
	}
}
