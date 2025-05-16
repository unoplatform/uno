using System;
using System.Text.RegularExpressions;
using Uno.WinUI.Runtime.Skia.X11;

namespace Uno.UI.Hosting;

public partial class X11HostBuilder : IPlatformHostBuilder
{
	// [hostname]:display[.screen], e.g. 127.0.0.1:0.0 or most likely just :0
	[GeneratedRegex(@"^(?:(?<hostname>[\w\.-]+))?:(?<displaynumber>\d+)(?:\.(?<screennumber>\d+))?$")]
	private static partial Regex DisplayRegex();

	private int _renderFrameRate = 60;
	private bool _preloadMediaPlayer;

	internal X11HostBuilder()
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

	public X11HostBuilder PreloadMediaPlayer(bool preload)
	{
		_preloadMediaPlayer = preload;
		return this;
	}

	bool IPlatformHostBuilder.IsSupported
		=> OperatingSystem.IsLinux() &&
			Environment.GetEnvironmentVariable("DISPLAY") is { } displayString &&
			DisplayRegex().Match(displayString).Success;

	UnoPlatformHost IPlatformHostBuilder.Create(Func<Microsoft.UI.Xaml.Application> appBuilder, Type appType)
		=> new X11ApplicationHost(appBuilder, _renderFrameRate, preloadVlc: _preloadMediaPlayer);
}
