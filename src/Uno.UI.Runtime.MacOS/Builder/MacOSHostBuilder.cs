using Microsoft.UI.Xaml;
using Uno.UI.Hosting;
using Uno.UI.Runtime.MacOS;

namespace Uno.UI.Runtime;

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
