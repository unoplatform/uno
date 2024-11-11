using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;
using Windows.UI.WebUI;

namespace Uno.UI.Runtime.Skia;

public static class HostBuilder
{
	public static ISkiaHostBuilder UseWin32(this ISkiaHostBuilder builder)
	{
		builder.AddHostBuilder(() => new Win32HostBuilder());
		return builder;
	}
}
