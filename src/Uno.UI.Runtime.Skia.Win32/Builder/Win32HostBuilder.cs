using System;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia.Win32;

namespace Uno.UI.Hosting;

internal class Win32HostBuilder : IPlatformHostBuilder
{
	private bool _preloadMediaPlayer;

	public Win32HostBuilder()
	{
	}

	public bool IsSupported
		=> OperatingSystem.IsWindows();

	public Win32HostBuilder PreloadMediaPlayer(bool preload)
	{
		_preloadMediaPlayer = preload;
		return this;
	}

	public UnoPlatformHost Create(Func<Microsoft.UI.Xaml.Application> appBuilder, Type appType)
		=> new Win32Host(appBuilder, _preloadMediaPlayer);
}
