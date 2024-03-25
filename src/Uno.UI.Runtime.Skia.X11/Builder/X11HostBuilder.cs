using System;
using System.Text.RegularExpressions;
using Uno.WinUI.Runtime.Skia.X11;

namespace Uno.UI.Runtime.Skia;

internal partial class X11HostBuilder : IPlatformHostBuilder
{
	[GeneratedRegex(@"^(?:(?<hostname>[\w\.-]+):)?(?<displaynumber>\d+)(?:\.(?<screennumber>\d+))?$")]
	private static partial Regex DisplayRegex();

	public X11HostBuilder()
	{
	}

	public bool IsSupported
		=> OperatingSystem.IsLinux() &&
			Environment.GetEnvironmentVariable("DISPLAY") is { } displayString &&
			DisplayRegex().Match(displayString).Success;

	public SkiaHost Create(Func<Microsoft.UI.Xaml.Application> appBuilder)
		=> new X11ApplicationHost(appBuilder);
}
