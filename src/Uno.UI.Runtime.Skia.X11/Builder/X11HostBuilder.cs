using System;
using System.Text.RegularExpressions;
using Uno.WinUI.Runtime.Skia.X11;

namespace Uno.UI.Runtime.Skia;

internal partial class X11HostBuilder : IPlatformHostBuilder
{
	// [hostname]:display[.screen], e.g. 127.0.0.1:0.0 or most likely just :0
	[GeneratedRegex(@"^(?:(?<hostname>[\w\.-]+))?:(?<displaynumber>\d+)(?:\.(?<screennumber>\d+))?$")]
	private static partial Regex DisplayRegex();

	private int _renderFrameRate = 60;

	public X11HostBuilder()
	{
	}

	/// <summary>
	/// Sets the FPS that the application should try to achieve.
	/// </summary>
	public X11HostBuilder RenderFrameRate(int renderFrameRate)
	{
		_renderFrameRate = renderFrameRate;
		return this;
	}

	public bool IsSupported
		=> OperatingSystem.IsLinux() &&
			Environment.GetEnvironmentVariable("DISPLAY") is { } displayString &&
			DisplayRegex().Match(displayString).Success;

	public SkiaHost Create(Func<Windows.UI.Xaml.Application> appBuilder)
		=> new X11ApplicationHost(appBuilder, _renderFrameRate);
}
