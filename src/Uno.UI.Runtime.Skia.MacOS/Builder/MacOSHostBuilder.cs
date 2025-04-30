using Microsoft.UI.Xaml;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia.MacOS;

namespace Uno.UI.Runtime.Skia;

internal class MacOSHostBuilder : IPlatformHostBuilder
{
	public MacOSHostBuilder()
	{
	}

	public bool IsSupported
		=> OperatingSystem.IsMacOS();

	public SkiaHost Create(Func<Microsoft.UI.Xaml.Application> appBuilder, Type appType)
		=> new MacSkiaHost(appBuilder);

	UnoPlatformHost IPlatformHostBuilder.Create(Func<Microsoft.UI.Xaml.Application> appBuilder, Type appType) => Create(appBuilder, appType);
}
