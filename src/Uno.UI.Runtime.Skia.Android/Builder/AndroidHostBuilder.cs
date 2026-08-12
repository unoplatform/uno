#nullable enable

using System;
using Uno.UI.Runtime.Skia.Android;

namespace Uno.UI.Hosting;

internal sealed class AndroidHostBuilder : IPlatformHostBuilder, IAndroidSkiaHostBuilder
{
	private bool? _useVulkan;
	private bool? _useOpenGL;

	internal AndroidHostBuilder()
	{
	}

	public IAndroidSkiaHostBuilder UseVulkan(bool enabled = true)
	{
		_useVulkan = enabled;
		return this;
	}

	public IAndroidSkiaHostBuilder UseOpenGL(bool enabled = true)
	{
		_useOpenGL = enabled;
		return this;
	}

	bool IPlatformHostBuilder.IsSupported
		=> OperatingSystem.IsAndroid();

	UnoPlatformHost IPlatformHostBuilder.Create(Func<Microsoft.UI.Xaml.Application> appBuilder, Type appType)
	{
		if (_useVulkan is { } useVulkan)
		{
			FeatureConfiguration.Rendering.UseVulkanOnSkiaAndroid = useVulkan;
		}

		if (_useOpenGL is { } useOpenGL)
		{
			FeatureConfiguration.Rendering.UseOpenGLOnSkiaAndroid = useOpenGL;
		}

		return new AndroidHost(appBuilder);
	}
}
