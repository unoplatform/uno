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
	public static ISkiaHostBuilder UseWindows(this ISkiaHostBuilder builder, Action<IWindowsSkiaHostBuilder> windowsBuilder = null)
	{
		builder.AddHostBuilder(() =>
		{
			var wpfBuilder = new WpfHostBuilder();
			if (wpfBuilder.IsSupported)
			{
				windowsBuilder?.Invoke(wpfBuilder);
			}
			return wpfBuilder;
		});

		return builder;
	}

	public static IWindowsSkiaHostBuilder WpfApplication(this IWindowsSkiaHostBuilder builder, Func<System.Windows.Application> action)
	{
		builder.WpfApplication = action;

		return builder;
	}
}
