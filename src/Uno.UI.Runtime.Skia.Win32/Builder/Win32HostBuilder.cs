using System;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia.Win32;

namespace Uno.UI.Hosting;

public class Win32HostBuilder : IPlatformHostBuilder
{
	private bool _preloadMediaPlayer;

	internal Win32HostBuilder()
	{
	}

	public Win32HostBuilder PreloadMediaPlayer(bool preload)
	{
		_preloadMediaPlayer = preload;
		return this;
	}

	bool IPlatformHostBuilder.IsSupported
		=> OperatingSystem.IsWindows();

	UnoPlatformHost IPlatformHostBuilder.Create(Func<Microsoft.UI.Xaml.Application> appBuilder, Type appType)
		=> new Win32Host(appBuilder, _preloadMediaPlayer);
}
