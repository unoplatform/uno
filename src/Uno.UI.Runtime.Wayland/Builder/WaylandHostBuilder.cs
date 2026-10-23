#nullable enable

using System;
using System.Collections.Generic;
using Uno.UI.Composition.Drawing;
using Uno.UI.Runtime.Wayland;

namespace Uno.UI.Hosting;

public sealed class WaylandHostBuilder : IPlatformHostBuilder
{
	// Every GPU API the Wayland host can serve; forcing one excludes all the others.
	private static readonly GraphicsContextKind[] _allKinds =
		{ GraphicsContextKind.OpenGLES, GraphicsContextKind.Software };

	private readonly HashSet<GraphicsContextKind> _disabledKinds = new();
	private bool _useSystemHarfBuzz;

	internal WaylandHostBuilder()
	{
	}

	/// <summary>
	/// Forces a single rendering backend, excluding every other from negotiation. If the forced backend cannot be
	/// created, no other is tried. Mutually exclusive with <see cref="DisableRenderingBackends"/>.
	/// </summary>
	public WaylandHostBuilder ForceRenderingBackend(WaylandRenderingBackend backend)
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
	public WaylandHostBuilder DisableRenderingBackends(params WaylandRenderingBackend[] backends)
	{
		foreach (var backend in backends)
		{
			_disabledKinds.Add(ToKind(backend));
		}
		return this;
	}

	private static GraphicsContextKind ToKind(WaylandRenderingBackend backend) => backend switch
	{
		WaylandRenderingBackend.OpenGLES => GraphicsContextKind.OpenGLES,
		WaylandRenderingBackend.Software => GraphicsContextKind.Software,
		_ => throw new ArgumentOutOfRangeException(nameof(backend), backend, null),
	};

	/// <summary>
	/// Makes HarfBuzzSharp, the font provider's dependency, use the system libharfbuzz instead of its bundled
	/// libHarfBuzzSharp, avoiding symbol clashes with other libraries (such as GTK) that load the system one.
	/// </summary>
	public WaylandHostBuilder UseSystemHarfBuzz(bool value)
	{
		_useSystemHarfBuzz = value;
		return this;
	}

	bool IPlatformHostBuilder.IsSupported => WaylandApplicationHost.IsSupported();

	UnoPlatformHost IPlatformHostBuilder.Create(Func<Microsoft.UI.Xaml.Application> appBuilder, Type appType)
	{
		GraphicsRegistry.DisabledContextKinds = _disabledKinds;
		return new WaylandApplicationHost(appBuilder, _useSystemHarfBuzz);
	}
}
