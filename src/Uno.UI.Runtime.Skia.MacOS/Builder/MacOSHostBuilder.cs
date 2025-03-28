using System;
using Uno.UI.Runtime.Skia.MacOS;

namespace Uno.UI.Runtime.Skia;

internal class MacOSHostBuilder : IPlatformHostBuilder
{
	public MacOSHostBuilder()
	{
	}

	public bool IsSupported
		=> OperatingSystem.IsMacOS();

	public SkiaHost Create(Func<Windows.UI.Xaml.Application> appBuilder)
		=> new MacSkiaHost(appBuilder);
}
