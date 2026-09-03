using System;
using System.Collections.Generic;
using Uno.UI.Composition.Drawing;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia.Win32;

namespace Uno.UI.Hosting;

public class Win32HostBuilder : IPlatformHostBuilder
{
	// Every GPU API the Win32 host can serve; forcing one excludes all the others.
	private static readonly GraphicsContextKind[] _allKinds =
		{ GraphicsContextKind.Vulkan, GraphicsContextKind.OpenGL, GraphicsContextKind.Software };

	private bool _preloadMediaPlayer;
	private readonly HashSet<GraphicsContextKind> _disabledKinds = new();

	internal Win32HostBuilder()
	{
	}

	public Win32HostBuilder PreloadMediaPlayer(bool preload)
	{
		_preloadMediaPlayer = preload;
		return this;
	}

	/// <summary>
	/// Forces a single rendering backend, excluding every other from negotiation. If the forced backend cannot be
	/// created, no other is tried. Mutually exclusive with <see cref="DisableRenderingBackends"/>.
	/// </summary>
	public Win32HostBuilder ForceRenderingBackend(Win32RenderingBackend backend)
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
	public Win32HostBuilder DisableRenderingBackends(params Win32RenderingBackend[] backends)
	{
		foreach (var backend in backends)
		{
			_disabledKinds.Add(ToKind(backend));
		}
		return this;
	}

	private static GraphicsContextKind ToKind(Win32RenderingBackend backend) => backend switch
	{
		Win32RenderingBackend.Vulkan => GraphicsContextKind.Vulkan,
		Win32RenderingBackend.OpenGL => GraphicsContextKind.OpenGL,
		Win32RenderingBackend.Software => GraphicsContextKind.Software,
		_ => throw new ArgumentOutOfRangeException(nameof(backend), backend, null),
	};

	bool IPlatformHostBuilder.IsSupported
		=> OperatingSystem.IsWindows();

	UnoPlatformHost IPlatformHostBuilder.Create(Func<Microsoft.UI.Xaml.Application> appBuilder, Type appType)
	{
		// The negotiation reads the excluded kinds; nothing host-side stores render policy any more.
		GraphicsRegistry.DisabledContextKinds = _disabledKinds;
		return new Win32Host(appBuilder, _preloadMediaPlayer);
	}
}
