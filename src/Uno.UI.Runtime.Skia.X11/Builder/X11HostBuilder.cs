using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Uno.UI.Composition.Drawing;
using Uno.WinUI.Runtime.Skia.X11;

namespace Uno.UI.Hosting;

public partial class X11HostBuilder : IPlatformHostBuilder
{
	// [hostname]:display[.screen], e.g. 127.0.0.1:0.0 or most likely just :0
	[GeneratedRegex(@"^(?:(?<hostname>[\w\.-]+))?:(?<displaynumber>\d+)(?:\.(?<screennumber>\d+))?$")]
	private static partial Regex DisplayRegex();

	// Every GPU API the X11 host can serve; forcing one excludes all the others.
	private static readonly GraphicsContextKind[] _allKinds =
		{ GraphicsContextKind.Vulkan, GraphicsContextKind.OpenGL, GraphicsContextKind.OpenGLES, GraphicsContextKind.Software };

	private int _renderFrameRate = 60;
	private bool _preloadMediaPlayer;
	private bool _useSystemHarfBuzz;
	private readonly HashSet<GraphicsContextKind> _disabledKinds = new();

	internal X11HostBuilder()
	{
	}

	/// <summary>
	/// Forces a single rendering backend, excluding every other from negotiation. If the forced backend cannot be
	/// created, no other is tried. Mutually exclusive with <see cref="DisableRenderingBackends"/>.
	/// </summary>
	public X11HostBuilder ForceRenderingBackend(X11RenderingBackend backend)
	{
		var forced = ToKind(backend);
		_disabledKinds.Clear();
		foreach (var kind in _allKinds)
		{
			if (kind != forced)
			{
				_disabledKinds.Add(kind);
			}
		}
		return this;
	}

	/// <summary>
	/// Disables specific rendering backends, leaving every other available to negotiation (in the backend's
	/// preference order). Call repeatedly or pass several to disable more than one.
	/// </summary>
	public X11HostBuilder DisableRenderingBackends(params X11RenderingBackend[] backends)
	{
		foreach (var backend in backends)
		{
			_disabledKinds.Add(ToKind(backend));
		}
		return this;
	}

	private static GraphicsContextKind ToKind(X11RenderingBackend backend) => backend switch
	{
		X11RenderingBackend.Vulkan => GraphicsContextKind.Vulkan,
		X11RenderingBackend.OpenGL => GraphicsContextKind.OpenGL,
		X11RenderingBackend.OpenGLES => GraphicsContextKind.OpenGLES,
		X11RenderingBackend.Software => GraphicsContextKind.Software,
		_ => throw new ArgumentOutOfRangeException(nameof(backend), backend, null),
	};

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
		// The negotiation reads the excluded kinds; nothing host-side stores render policy any more.
		GraphicsRegistry.DisabledContextKinds = _disabledKinds;
		return new X11ApplicationHost(appBuilder, _renderFrameRate, _preloadMediaPlayer, _useSystemHarfBuzz);
	}
}
