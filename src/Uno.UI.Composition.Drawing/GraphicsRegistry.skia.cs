#nullable enable

using System;
using System.Collections.Generic;
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
/// Creates a window+context for a well-known <see cref="GraphicsContextKind"/>, or <see langword="null"/> to
/// decline (the kind is unavailable, or the host's config opts out — negotiation then tries the next kind). The
/// host owns both the native window (created freshly when the kind requires it, e.g. an X11 GLX visual, or its
/// existing kind-agnostic window reused, e.g. a Win32 HWND) and the GPU-API context/device. The host switches
/// purely on <paramref name="kind"/> and never sees the render backend (Skia vs WebGPU). It is asynchronous
/// because one kind (WASM WebGpu) imports its device from JS and must not block the JS thread; every native host
/// returns an already-completed task.
/// </summary>
public delegate Task<IGraphicsContext?> GraphicsContextFactory(GraphicsContextKind kind);

/// <summary>
/// Process-wide registry and negotiator for pluggable graphics backends. The app registers its ordered backend
/// preference (each backend declares the context kinds it accepts, in its own order); the host sets a single
/// <see cref="ContextFactory"/> that turns a kind into a window+context. The host calls <see cref="Initialize"/>
/// once and negotiation binds the first kind the host can serve to the backend that accepts it.
/// </summary>
public static class GraphicsRegistry
{
	private static readonly object _gate = new();
	private static IReadOnlyList<IGraphicsProvider> _backends = Array.Empty<IGraphicsProvider>();

	/// <summary>
	/// The host's window+context creator (set once by the host). This is the <em>only</em> platform-specific,
	/// GPU-agnostic seam: the host maps a neutral context kind to a window+context and never references a render
	/// backend. WGL/GLX/EGL/DIB live in the host's implementations; portable kinds (WebGpu, Vulkan) redirect to a
	/// shared GPU-API helper.
	/// </summary>
	public static GraphicsContextFactory? ContextFactory { get; set; }

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
	/// True once the app has declared a graphics backend through the host builder (<see cref="Register"/>). The
	/// implicit Skia auto-registration (<see cref="DrawingBackendFallback"/>) stays out of the seams a declared
	/// backend owns — even while that backend is still initializing (e.g. the WASM/WebGPU async device import).
	/// </summary>
	public static bool HasRegisteredBackends
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
	/// Whether any registered backend accepts <paramref name="kind"/>. A neutral pre-negotiation signal for a host
	/// that must choose a native window/view type up front where that choice is tied to the kind (e.g. Android
	/// picks a plain SurfaceView for the WebGpu swapchain vs a GLSurfaceView for the GLES path) — the host reads the
	/// kind, never a backend type or an env var.
	/// </summary>
	public static bool HasBackendPreferring(GraphicsContextKind kind)
	{
		lock (_gate)
		{
			foreach (var backend in _backends)
			{
				foreach (var supported in backend.PreferredContexts)
				{
					if (supported == kind)
					{
						return true;
					}
				}
			}

			return false;
		}
	}

	/// <summary>
	/// Synchronous entry for hosts whose context creation completes synchronously (every kind except WASM/WebGpu).
	/// The negotiation runs the same async core; because those creations return already-completed tasks it finishes
	/// inline, so this neither blocks nor deadlocks. A host that can create the WASM/WebGpu context must negotiate
	/// with <see cref="InitializeAsync"/> instead.
	/// </summary>
	public static GraphicsInitialization Initialize()
		=> InitializeAsync().GetAwaiter().GetResult();

	/// <summary>
	/// Negotiates a backend + context: for each registered backend (registration order) and each context kind it
	/// accepts (the backend's own order), asks the host <see cref="ContextFactory"/> to create a window+context for
	/// that kind. The first non-null context wins: its <see cref="Graphics"/> pair is minted and its drawing factory
	/// installed as <see cref="DrawingFactory.Current"/>. The host names no backend and no kind order.
	/// </summary>
	public static async Task<GraphicsInitialization> InitializeAsync()
	{
		IReadOnlyList<IGraphicsProvider> backends;
		lock (_gate)
		{
			backends = _backends;
		}

		if (backends.Count == 0)
		{
			// No backend was declared by the app: light up the implicit Skia default (if the Skia backend assembly
			// is present) so a host stays fully backend-agnostic — it always negotiates, whether the app registered
			// a backend explicitly or relies on the built-in Skia fallback. A SkiaSharp-free head that declares no
			// backend finds nothing and still throws below.
			DrawingBackendFallback.EnsureGraphicsBackend();
			lock (_gate)
			{
				backends = _backends;
			}
		}

		if (backends.Count == 0)
		{
			throw new InvalidOperationException(
				"No graphics backend registered. Call GraphicsRegistry.Register(...) during app initialization.");
		}

		var factory = ContextFactory
			?? throw new InvalidOperationException(
				"No host graphics context factory set. The host must set GraphicsRegistry.ContextFactory before initializing.");

		var attempts = new StringBuilder();
		foreach (var backend in backends)
		{
			foreach (var kind in backend.PreferredContexts)
			{
				IGraphicsContext? context;
				try
				{
					// The host creates the window+context for this kind (or null to decline). WebGpu/Vulkan redirect
					// to a shared GPU-API helper inside the host; GL/software are the host's own.
					context = await factory(kind).ConfigureAwait(true);
				}
				catch (Exception e)
				{
					attempts.Append($"\n  - {backend.GetType().Name}/{kind}: context factory threw ({e.GetType().Name})");
					if (typeof(GraphicsRegistry).Log().IsEnabled(LogLevel.Debug))
					{
						typeof(GraphicsRegistry).Log().Debug($"Graphics negotiation: {backend.GetType().Name}/{kind} context factory threw: {e}");
					}
					continue;
				}

				if (context is null)
				{
					attempts.Append($"\n  - {backend.GetType().Name}/{kind}: host declined (kind unavailable or opted out)");
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
