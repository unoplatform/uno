using System;
using Uno.WinUI.Runtime.Skia.X11;

namespace Uno.UI.Runtime.Skia;

internal class X11HostBuilder : IPlatformHostBuilder
{
	public X11HostBuilder()
	{
	}

	public bool IsSupported
		=> OperatingSystem.IsLinux();

	public SkiaHost Create(Func<Microsoft.UI.Xaml.Application> appBuilder)
		=> new X11ApplicationHost(appBuilder);
}
