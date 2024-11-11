#nullable enable

using System;
using Uno.UI.Runtime.Skia.Win32;

namespace Uno.UI.Runtime.Skia;

internal class Win32HostBuilder : IPlatformHostBuilder
{
	public Win32HostBuilder()
	{
	}

	public bool IsSupported
		=> OperatingSystem.IsWindows();

	public SkiaHost Create(Func<Microsoft.UI.Xaml.Application> appBuilder)
		=> new Win32Host(appBuilder);
}
