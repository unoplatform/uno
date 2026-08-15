using System;
using System.Text.RegularExpressions;
using Uno.UI.Xaml;
using Uno.WinUI.Runtime.Skia.X11;

namespace Uno.UI.Hosting;

public partial class X11HostBuilder : IPlatformHostBuilder
{
	// [hostname]:display[.screen], e.g. 127.0.0.1:0.0 or most likely just :0
	[GeneratedRegex(@"^(?:(?<hostname>[\w\.-]+))?:(?<displaynumber>\d+)(?:\.(?<screennumber>\d+))?$")]
	private static partial Regex DisplayRegex();

	private int _renderFrameRate = 60;
	private bool _preloadMediaPlayer;
	private bool _useSystemHarfBuzz;
	private X11RenderingBackend? _renderingBackend;

	internal X11HostBuilder()
	{
	}

	/// <summary>
	/// Sets the rendering backend for the X11 host.
	/// This takes precedence over <see cref="FeatureConfiguration.Rendering.UseVulkanOnX11"/>
	/// and <see cref="FeatureConfiguration.Rendering.UseOpenGLOnX11"/> if set.
	/// </summary>
	public X11HostBuilder RenderingBackend(X11RenderingBackend backend)
	{
		_renderingBackend = backend;
		return this;
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

	/// <summary>
	/// Uses the system HarfBuzz library for text shaping instead of libHarfBuzzSharp shipped with SkiaSharp.
	/// </summary>
	public X11HostBuilder UseSystemHarfBuzz(bool value)
	{
		_useSystemHarfBuzz = value;
		return this;
	}

	bool IPlatformHostBuilder.IsSupported
		=> OperatingSystem.IsLinux() &&
			Environment.GetEnvironmentVariable("DISPLAY") is { } displayString &&
			DisplayRegex().Match(displayString).Success;

	UnoPlatformHost IPlatformHostBuilder.Create(Func<Microsoft.UI.Xaml.Application> appBuilder, Type appType)
	{
		if (_renderingBackend is { } backend)
		{
			ApplyRenderingBackend(backend);
		}

		return new X11ApplicationHost(appBuilder, _renderFrameRate, _preloadMediaPlayer, _useSystemHarfBuzz);
	}

	private static void ApplyRenderingBackend(X11RenderingBackend backend)
	{
		switch (backend)
		{
			case X11RenderingBackend.Vulkan:
				FeatureConfiguration.Rendering.UseVulkanOnX11 = true;
				break;
			case X11RenderingBackend.OpenGL:
				FeatureConfiguration.Rendering.UseVulkanOnX11 = false;
				FeatureConfiguration.Rendering.UseOpenGLOnX11 = true;
				FeatureConfiguration.Rendering.PreferGLESOverGLOnX11 = false;
				break;
			case X11RenderingBackend.OpenGLES:
				FeatureConfiguration.Rendering.UseVulkanOnX11 = false;
				FeatureConfiguration.Rendering.UseOpenGLOnX11 = true;
				FeatureConfiguration.Rendering.PreferGLESOverGLOnX11 = true;
				break;
			case X11RenderingBackend.Software:
				FeatureConfiguration.Rendering.UseVulkanOnX11 = false;
				FeatureConfiguration.Rendering.UseOpenGLOnX11 = false;
				break;
			case X11RenderingBackend.Default:
			default:
				break;
		}
	}
}
