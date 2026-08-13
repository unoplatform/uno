#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.Foundation.Logging;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// The result of a successful <see cref="GraphicsRegistry.Initialize"/>: the winning provider, the context that
/// was created for it, and the matched <see cref="Graphics"/> (factory + renderer) bound to that context.
/// </summary>
public readonly struct GraphicsInitialization
{
	public GraphicsInitialization(IGraphicsProvider provider, IGraphicsContext context, Graphics graphics)
	{
		Provider = provider;
		Context = context;
		Graphics = graphics;
	}

	public IGraphicsProvider Provider { get; }
	public IGraphicsContext Context { get; }
	public Graphics Graphics { get; }

	/// <summary>The renderer of the matched pair (convenience for <see cref="Graphics"/>.Renderer).</summary>
	public IRenderer Renderer => Graphics.Renderer;
}

/// <summary>
/// Process-wide registry and negotiator for pluggable graphics backends. The app registers its ordered
/// backend preference and the available per-kind context providers (both user-side; there is no default
/// backend), then the host calls <see cref="Initialize"/> once its window exists.
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
	// One (uniformly async) per-kind context factory store; a synchronous factory is held as an already-completed task.
	private static readonly Dictionary<GraphicsContextKind, Func<INativeWindow, Task<IGraphicsContext?>>> _contextFactories = new();

	/// <summary>
	/// The concrete context factory (set once by the Uno graphics layer). Core stays free of GPU-API libraries;
	/// this single seam reaches the graphics layer that concretely creates each known context kind.
	/// </summary>
	public static GraphicsContextFactory? ContextFactory { get; set; }

	/// <summary>
	/// Registers a stand-alone factory that builds the on-window context (surface + device) for a context
	/// <paramref name="kind"/> from the neutral <see cref="INativeWindow"/>. This is the "GPU-API" half — separate
	/// from any render backend — so WebGPU context/window creation can be referenced on its own and consumed by
	/// <em>any</em> registered <see cref="IGraphicsProvider"/> (Uno's WebGPU renderer, or a user's own). Takes
	/// precedence over <see cref="ContextFactory"/> for that kind.
	/// </summary>
	internal static void RegisterContextFactory(GraphicsContextKind kind, Func<INativeWindow, IGraphicsContext?> factory)
	{
		ArgumentNullException.ThrowIfNull(factory);
		lock (_gate)
		{
			_contextFactories[kind] = window => Task.FromResult(factory(window));
		}
	}

	/// <summary>
	/// Like <see cref="RegisterContextFactory"/> but asynchronous — for a context whose device bring-up can't run
	/// synchronously (WASM/WebGPU: the device is imported from browser JS and the JS thread must not be blocked).
	/// Such a kind must be negotiated with <see cref="InitializeAsync"/>, not the synchronous <see cref="Initialize"/>.
	/// </summary>
	internal static void RegisterAsyncContextFactory(GraphicsContextKind kind, Func<INativeWindow, Task<IGraphicsContext?>> factory)
	{
		ArgumentNullException.ThrowIfNull(factory);
		lock (_gate)
		{
			_contextFactories[kind] = factory;
		}
	}

	/// <summary>
	/// Registers the app's backend preference, most-preferred first. Uniform across every platform (there is
	/// no fluent-builder dependency and no default backend). Replaces any previous registration.
	/// </summary>
	internal static void Register(IReadOnlyList<IGraphicsProvider> backendsInPreferenceOrder)
	{
		ArgumentNullException.ThrowIfNull(backendsInPreferenceOrder);
		lock (_gate)
		{
			_backends = backendsInPreferenceOrder;
		}
	}

	/// <summary>
	/// Registers <paramref name="backendsInPreferenceOrder"/> only if no backend preference is set yet, so a
	/// backend's implicit default (the Skia fallback) never clobbers a preference an app declared explicitly.
	/// </summary>
	internal static void RegisterDefault(IReadOnlyList<IGraphicsProvider> backendsInPreferenceOrder)
	{
		ArgumentNullException.ThrowIfNull(backendsInPreferenceOrder);
		lock (_gate)
		{
			if (_backends.Count == 0)
			{
				_backends = backendsInPreferenceOrder;
			}
		}
	}

	/// <summary>
	/// True once the app has declared an explicit backend preference via <see cref="Register"/>. That declaration
	/// owns backend selection even while the chosen backend is still initializing (e.g. the WASM/WebGPU async device
	/// import), so the implicit Skia auto-registration (<see cref="DrawingBackendFallback"/>) must stay out — it must
	/// not fill the pre-init window with SkiaSharp and clobber the app's choice.
	/// </summary>
	internal static bool HasRegisteredBackends
	{
		get
		{
			lock (_gate)
			{
				return _backends.Count > 0;
			}
		}
	}

	/// <summary>
	/// Walks the registered backends in preference order and, for each, its preferred context kinds in order,
	/// creating a context on demand until one succeeds. The first success wins: its factory is installed as
	/// <see cref="DrawingFactory.Current"/> and a <see cref="GraphicsInitialization"/> is returned. Nothing is
	/// created speculatively. Throws if no registered backend can initialize on <paramref name="window"/>.
	/// </summary>
	/// <param name="preferredKinds">
	/// Optional host override of the context-kind order, expressed purely in neutral <see cref="GraphicsContextKind"/>
	/// terms (the host names no backend type). When supplied, each backend is tried against these kinds, in this
	/// order, intersected with the kinds the backend actually supports — letting a host with one window type steer
	/// the outcome (e.g. <c>[Software]</c> to force the CPU path, or <c>[OpenGLES, Software]</c> to prefer GLES).
	/// When null, each backend's own <see cref="IGraphicsProvider.PreferredContexts"/> order is used.
	/// </param>
	/// <summary>
	/// Synchronous entry for hosts whose context factories all complete synchronously (every backend except the
	/// WASM/WebGPU async device import). The negotiation runs the same async core; because those factories return
	/// already-completed tasks it finishes inline, so this neither blocks nor deadlocks. A kind registered via
	/// <see cref="RegisterAsyncContextFactory"/> must be negotiated with <see cref="InitializeAsync"/> instead.
	/// </summary>
	public static GraphicsInitialization Initialize(INativeWindow window, IReadOnlyList<GraphicsContextKind>? preferredKinds = null)
		=> InitializeAsync(window, preferredKinds).GetAwaiter().GetResult();

	/// <summary>
	/// Negotiates a backend + context for the window: for each backend's preferred kinds (optionally filtered by
	/// <paramref name="preferredKinds"/>), builds the context via the registered per-kind factory (else the host
	/// <see cref="ContextFactory"/>) and, on success, mints and installs the matched <see cref="Graphics"/> pair.
	/// </summary>
	public static async Task<GraphicsInitialization> InitializeAsync(INativeWindow window, IReadOnlyList<GraphicsContextKind>? preferredKinds = null)
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
				"No graphics backend registered. Call GraphicsRegistry.Register(...) during app initialization.");
		}

		var factory = ContextFactory;
		var attempts = new StringBuilder();
		foreach (var backend in backends)
		{
			var kinds = preferredKinds is null
				? backend.PreferredContexts
				: preferredKinds.Where(backend.PreferredContexts.Contains).ToArray();

			foreach (var kind in kinds)
			{
				Func<INativeWindow, Task<IGraphicsContext?>>? kindFactory;
				lock (_gate)
				{
					_contextFactories.TryGetValue(kind, out kindFactory);
				}

				IGraphicsContext? context = null;
				try
				{
					// A registered per-kind factory (e.g. the WebGPU context/window factory, independent of any
					// renderer) wins; otherwise the host's factory (Skia's host-specific GL/software contexts).
					context = kindFactory is not null
						? await kindFactory(window).ConfigureAwait(true)
						: factory is null ? null : factory(kind, window, backend.Requirements);
				}
				catch (Exception e)
				{
					attempts.Append($"\n  - {backend.GetType().Name}/{kind}: context factory threw ({e.GetType().Name})");
					if (typeof(GraphicsRegistry).Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
					{
						typeof(GraphicsRegistry).Log().Debug($"Graphics negotiation: {backend.GetType().Name}/{kind} context factory threw: {e}");
					}
					continue;
				}

				if (context is null)
				{
					attempts.Append($"\n  - {backend.GetType().Name}/{kind}: context unavailable or requirements unmet");
					continue;
				}

				try
				{
					var graphics = backend.CreateGraphics(context);
					DrawingFactory.Register(graphics.DrawingFactory);
					return new GraphicsInitialization(backend, context, graphics);
				}
				catch (Exception e)
				{
					attempts.Append($"\n  - {backend.GetType().Name}/{kind}: CreateGraphics threw ({e.GetType().Name})");
					context.Dispose();
				}
			}
		}

		throw new InvalidOperationException(
			$"No registered backend could initialize on this host. Attempts:{attempts}");
	}
}
