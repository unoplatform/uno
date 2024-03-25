using System;
using Uno.WinUI.Runtime.Skia.X11;

namespace Uno.UI.Runtime.Skia;

internal class X11HostBuilder : IPlatformHostBuilder
{
	public X11HostBuilder()
	{
	}

	public bool IsSupported
		=> OperatingSystem.IsLinux() &&
			// DISPLAY should look like ":0"
			Environment.GetEnvironmentVariable("DISPLAY") is { } displayString &&
			displayString.StartsWith(":") &&
			int.TryParse(displayString[1..], out _);

	public SkiaHost Create(Func<Microsoft.UI.Xaml.Application> appBuilder)
		=> new X11ApplicationHost(appBuilder);
}
