using System;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia.Linux.FrameBuffer;

namespace Uno.UI.Runtime.Skia;

internal class FramebufferHostBuilder : IPlatformHostBuilder
{
	public FramebufferHostBuilder()
	{
	}

	public bool IsSupported
		=> OperatingSystem.IsLinux();

	public UnoPlatformHost Create(Func<Microsoft.UI.Xaml.Application> appBuilder)
		=> new FrameBufferHost(appBuilder);
}
