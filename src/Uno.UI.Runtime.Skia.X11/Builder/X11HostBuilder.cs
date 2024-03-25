using System;
using System.Text.RegularExpressions;
using Uno.WinUI.Runtime.Skia.X11;

namespace Uno.UI.Runtime.Skia;

internal class X11HostBuilder : IPlatformHostBuilder
{
	private static readonly Regex DisplayRegex = new Regex(@"^(?:(?<hostname>[\w\.-]+):)?(?<displaynumber>\d+)(?:\.(?<screennumber>\d+))?$", RegexOptions.Compiled);

	public X11HostBuilder()
	{
	}

	public bool IsSupported
		=> OperatingSystem.IsLinux() &&
			Environment.GetEnvironmentVariable("DISPLAY") is { } displayString &&
			DisplayRegex.Match(displayString).Success;

	public SkiaHost Create(Func<Microsoft.UI.Xaml.Application> appBuilder)
		=> new X11ApplicationHost(appBuilder);
}
