using System;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia.Win32;
using Uno.UI.Xaml;

namespace Uno.UI.Hosting;

public class Win32HostBuilder : IPlatformHostBuilder
{
	private bool _preloadMediaPlayer;
	private Win32RenderingBackend? _renderingBackend;

	internal Win32HostBuilder()
	{
	}

	public Win32HostBuilder PreloadMediaPlayer(bool preload)
	{
		_preloadMediaPlayer = preload;
		return this;
	}

	/// <summary>
	/// Sets the rendering backend for the Win32 host.
	/// This takes precedence over <see cref="FeatureConfiguration.Rendering.UseVulkanOnWin32"/>
	/// and <see cref="FeatureConfiguration.Rendering.UseOpenGLOnWin32"/> if set.
	/// </summary>
	public Win32HostBuilder RenderingBackend(Win32RenderingBackend backend)
	{
		_renderingBackend = backend;
		return this;
	}

	bool IPlatformHostBuilder.IsSupported
		=> OperatingSystem.IsWindows();

	UnoPlatformHost IPlatformHostBuilder.Create(Func<Microsoft.UI.Xaml.Application> appBuilder, Type appType)
	{
		if (_renderingBackend is { } backend)
		{
			ApplyRenderingBackend(backend);
		}

		return new Win32Host(appBuilder, _preloadMediaPlayer);
	}

	private static void ApplyRenderingBackend(Win32RenderingBackend backend)
	{
		switch (backend)
		{
			case Win32RenderingBackend.Vulkan:
				FeatureConfiguration.Rendering.UseVulkanOnWin32 = true;
				break;
			case Win32RenderingBackend.OpenGL:
				FeatureConfiguration.Rendering.UseVulkanOnWin32 = false;
				FeatureConfiguration.Rendering.UseOpenGLOnWin32 = true;
				break;
			case Win32RenderingBackend.Software:
				FeatureConfiguration.Rendering.UseVulkanOnWin32 = false;
				FeatureConfiguration.Rendering.UseOpenGLOnWin32 = false;
				break;
			case Win32RenderingBackend.Default:
			default:
				break;
		}
	}
}
