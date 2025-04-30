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

	public SkiaHost Create(Func<Microsoft.UI.Xaml.Application> appBuilder)
		=> new MacSkiaHost(appBuilder);

	UnoPlatformHost IPlatformHostBuilder.Create(Func<Application> appBuilder) => Create(appBuilder);
}
