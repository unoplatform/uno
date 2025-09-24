using System;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia.Linux.FrameBuffer;

namespace Uno.UI.Runtime.Skia;

internal class FramebufferHostBuilder : IPlatformHostBuilder
{
	private float? _displayScale;

	internal FramebufferHostBuilder()
	{
	}

	/// <summary>
	/// Gets the configured display scale
	/// </summary>
	internal float? ConfiguredDisplayScale => _displayScale;

	/// <summary>
	/// Sets the display scale to override framebuffer default scale
	/// </summary>
	/// <param name="scale">The display scale value to use</param>
	/// <returns>The current builder instance for chaining</returns>
	/// <remarks>This value can be overridden by the UNO_DISPLAY_SCALE_OVERRIDE environment variable</remarks>
	public FramebufferHostBuilder DisplayScale(float? scale)
	{
		_displayScale = scale;
		return this;
	}

	public bool IsSupported
		=> OperatingSystem.IsLinux();

	public UnoPlatformHost Create(Func<Microsoft.UI.Xaml.Application> appBuilder, Type appType)
		=> new FrameBufferHost(appBuilder, this);
}
