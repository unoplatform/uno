#nullable enable

using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// The result of a successful <see cref="GraphicsRegistry.Activate"/>: the winning backend, the context that
/// was created for it, and the render backend bound to that context.
/// </summary>
public readonly struct GraphicsActivation
{
	public GraphicsActivation(IGraphicsProvider backend, IGraphicsContext context, IRenderer renderBackend)
	{
		Backend = backend;
		Context = context;
		Renderer = renderBackend;
	}

	public IGraphicsProvider Backend { get; }
	public IGraphicsContext Context { get; }
	public IRenderer Renderer { get; }
}

/// <summary>
/// Process-wide registry and negotiator for pluggable graphics backends. The app registers its ordered
/// backend preference and the available per-kind context providers (both user-side; there is no default
/// backend), then the host calls <see cref="Activate"/> once its window exists.
/// </summary>
/// <summary>
/// Creates a context of a known kind for a window, or <see langword="null"/> if that API is unavailable or
/// the requirements can't be met (fully cleaning up so it's "as if never attempted"). The context kinds are a
/// closed, Uno-owned set — third parties plug in <see cref="IGraphicsProvider"/>s, not new kinds — so this is a
/// single concrete factory (implemented over a switch in the Uno graphics layer), not a per-kind plugin.
/// </summary>
public delegate IGraphicsContext? GraphicsContextFactory(GraphicsContextKind kind, INativeWindow window, GraphicsRequirements requirements);

public static class GraphicsRegistry
{
	private static readonly object _gate = new();
	private static IReadOnlyList<IGraphicsProvider> _backends = Array.Empty<IGraphicsProvider>();

	/// <summary>
	/// The concrete context factory (set once by the Uno graphics layer). Core stays free of GPU-API libraries;
	/// this single seam reaches the graphics layer that concretely creates each known context kind.
	/// </summary>
	public static GraphicsContextFactory? ContextFactory { get; set; }

	/// <summary>
	/// Registers the app's backend preference, most-preferred first. Uniform across every platform (there is
	/// no fluent-builder dependency and no default backend). Replaces any previous registration.
	/// </summary>
	public static void Register(IReadOnlyList<IGraphicsProvider> backendsInPreferenceOrder)
	{
		ArgumentNullException.ThrowIfNull(backendsInPreferenceOrder);
		lock (_gate)
		{
			_backends = backendsInPreferenceOrder;
		}
	}

	/// <summary>
	/// Walks the registered backends in preference order and, for each, its preferred context kinds in order,
	/// creating a context on demand until one succeeds. The first success wins: its factory is installed as
	/// <see cref="DrawingFactory.Current"/> and a <see cref="GraphicsActivation"/> is returned. Nothing is
	/// created speculatively. Throws if no registered backend can initialize on <paramref name="window"/>.
	/// </summary>
	public static GraphicsActivation Activate(INativeWindow window)
	{
		ArgumentNullException.ThrowIfNull(window);

		IReadOnlyList<IGraphicsProvider> backends;
		lock (_gate)
		{
			backends = _backends;
		}

		if (backends.Count == 0)
		{
			throw new InvalidOperationException(
				"No graphics backend registered. Call GraphicsRegistry.Register(...) with an ordered backend " +
				"list (e.g. new[] { new SkiaGraphicsProvider() }) during app initialization.");
		}

		var factory = ContextFactory
			?? throw new InvalidOperationException(
				"No GraphicsRegistry.ContextFactory set. The Uno graphics layer must install the concrete " +
				"context factory before Activate is called.");

		var attempts = new StringBuilder();
		foreach (var backend in backends)
		{
			foreach (var kind in backend.PreferredContexts)
			{
				IGraphicsContext? context = null;
				try
				{
					context = factory(kind, window, backend.Requirements);
				}
				catch (Exception e)
				{
					attempts.Append($"\n  - {backend.GetType().Name}/{kind}: context factory threw ({e.GetType().Name})");
					continue;
				}

				if (context is null)
				{
					attempts.Append($"\n  - {backend.GetType().Name}/{kind}: context unavailable or requirements unmet");
					continue;
				}

				try
				{
					var renderBackend = backend.CreateRenderBackend(context);
					DrawingFactory.Register(backend.Drawing);
					return new GraphicsActivation(backend, context, renderBackend);
				}
				catch (Exception e)
				{
					// A backend that can't stand up on a created context is treated like a context failure:
					// dispose and continue the walk rather than hard-failing.
					attempts.Append($"\n  - {backend.GetType().Name}/{kind}: CreateRenderBackend threw ({e.GetType().Name})");
					context.Dispose();
				}
			}
		}

		throw new InvalidOperationException(
			$"No registered backend could initialize on this host. Attempts:{attempts}");
	}
}
