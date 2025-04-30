using System;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia.Win32;

namespace Uno.UI.Hosting;

internal class Win32HostBuilder : IPlatformHostBuilder
{
	public Win32HostBuilder()
	{
	}

	public bool IsSupported
		=> OperatingSystem.IsWindows();

	public UnoPlatformHost Create(Func<Microsoft.UI.Xaml.Application> appBuilder)
		=> new Win32Host(appBuilder);
}
