using System;
using Uno.UI.Runtime.Skia.Linux.FrameBuffer;

namespace Uno.UI.Runtime.Skia;

internal class FramebufferHostBuilder : IPlatformHostBuilder
{
	public FramebufferHostBuilder()
	{
	}

	public bool IsSupported
		=> OperatingSystem.IsLinux();

	public SkiaHost Create(Func<Windows.UI.Xaml.Application> appBuilder)
		=> new FrameBufferHost(appBuilder);
}
