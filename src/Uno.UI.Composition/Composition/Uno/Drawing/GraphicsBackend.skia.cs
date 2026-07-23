#nullable enable

using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// The result of a successful <see cref="GraphicsBackend.Activate"/>: the winning backend, the context that
/// was created for it, and the render backend bound to that context.
/// </summary>
public readonly struct GraphicsActivation
{
	public GraphicsActivation(IGraphicsBackend backend, IGraphicsContext context, IRenderBackend renderBackend)
	{
		Backend = backend;
		Context = context;
		RenderBackend = renderBackend;
	}

	public IGraphicsBackend Backend { get; }
	public IGraphicsContext Context { get; }
	public IRenderBackend RenderBackend { get; }
}

/// <summary>
/// Process-wide registry and negotiator for pluggable graphics backends. The app registers its ordered
/// backend preference and the available per-kind context providers (both user-side; there is no default
/// backend), then the host calls <see cref="Activate"/> once its window exists.
/// </summary>
public static class GraphicsBackend
{
	private static readonly object _gate = new();
	private static IReadOnlyList<IGraphicsBackend> _backends = Array.Empty<IGraphicsBackend>();
	private static readonly Dictionary<GraphicsContextKind, IGraphicsContextProvider> _providers = new();

	/// <summary>
	/// Registers the app's backend preference, most-preferred first. Uniform across every platform (there is
	/// no fluent-builder dependency and no default backend). Replaces any previous registration.
	/// </summary>
	public static void Register(IReadOnlyList<IGraphicsBackend> backendsInPreferenceOrder)
	{
		ArgumentNullException.ThrowIfNull(backendsInPreferenceOrder);
		lock (_gate)
		{
			_backends = backendsInPreferenceOrder;
		}
	}

	/// <summary>Registers a per-kind context provider (one per <c>Uno.Graphics.&lt;kind&gt;</c> package). Last registration per kind wins.</summary>
	public static void RegisterProvider(IGraphicsContextProvider provider)
	{
		ArgumentNullException.ThrowIfNull(provider);
		lock (_gate)
		{
			_providers[provider.Kind] = provider;
		}
	}

	/// <summary>
	/// Walks the registered backends in preference order and, for each, its preferred context kinds in order,
	/// creating a context on demand until one succeeds. The first success wins: its factory is installed as
	/// <see cref="DrawingBackend.Current"/> and a <see cref="GraphicsActivation"/> is returned. Nothing is
	/// created speculatively. Throws if no registered backend can initialize on <paramref name="window"/>.
	/// </summary>
	public static GraphicsActivation Activate(INativeWindow window)
	{
		ArgumentNullException.ThrowIfNull(window);

		IReadOnlyList<IGraphicsBackend> backends;
		lock (_gate)
		{
			backends = _backends;
		}

		if (backends.Count == 0)
		{
			throw new InvalidOperationException(
				"No graphics backend registered. Call GraphicsBackend.Register(...) with an ordered backend " +
				"list (e.g. new[] { new SkiaGraphicsBackend() }) during app initialization.");
		}

		var attempts = new StringBuilder();
		foreach (var backend in backends)
		{
			foreach (var kind in backend.PreferredContexts)
			{
				IGraphicsContextProvider? provider;
				lock (_gate)
				{
					_providers.TryGetValue(kind, out provider);
				}

				if (provider is null)
				{
					attempts.Append($"\n  - {backend.GetType().Name}/{kind}: no provider registered for that kind");
					continue;
				}

				IGraphicsContext? context = null;
				try
				{
					context = provider.TryCreate(window, backend.Requirements);
				}
				catch (Exception e)
				{
					attempts.Append($"\n  - {backend.GetType().Name}/{kind}: provider threw ({e.GetType().Name})");
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
					DrawingBackend.Register(backend.Drawing);
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
