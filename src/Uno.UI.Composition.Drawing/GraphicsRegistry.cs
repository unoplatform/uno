#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Uno.Foundation.Logging;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// The result of a successful <see cref="GraphicsRegistry.Initialize"/>: the winning provider, the context that
/// was created for it, and the single device-bound <see cref="IDrawingFactory"/> backend bound to that context
/// (installed as <see cref="DrawingFactory.Current"/>).
/// </summary>
internal readonly struct GraphicsInitialization
{
	public GraphicsInitialization(IGraphicsProvider provider, ISwapChain context, IDrawingFactory backend)
	{
		Provider = provider;
		Context = context;
		Renderer = backend;
	}

	public IGraphicsProvider Provider { get; }
	public ISwapChain Context { get; }

	/// <summary>The backend — the host sets it as <c>CompositionTarget.Renderer</c>.</summary>
	public IDrawingFactory Renderer { get; }

	/// <summary>Alias for the backend (the drawing factory); the two are one object since the merge.</summary>
	public IDrawingFactory DrawingFactory => Renderer;
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
internal delegate Task<ISwapChain?> GraphicsContextFactory(GraphicsContextKind kind);

/// <summary>
/// Process-wide registry and negotiator for pluggable graphics backends. The app registers its ordered backend
/// preference (each backend declares the context kinds it accepts, in its own order); the host sets a single
/// <see cref="ContextFactory"/> that turns a kind into a window+context. The host calls <see cref="Initialize"/>
/// once and negotiation binds the first kind the host can serve to the backend that accepts it.
/// </summary>
internal static class GraphicsRegistry
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
				ISwapChain? context;
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
					// Narrow the context to the device face this kind implies and hand it to the matching typed
					// provider (Uno-side, keyed on the closed kind — the backend reads its device details without
					// casting a neutral context). Null when the backend doesn't implement that instantiation → decline.
					var backendFactory = CreateGraphics(kind, backend, context);
					if (backendFactory is null)
					{
						attempts.Append($"\n  - {backend.GetType().Name}/{kind}: backend does not implement IGraphicsProvider<T> for this kind");
						context.Dispose();
						continue;
					}

					// Capability gate: the backend must also implement the typed IDrawingFactory<TTarget> matching
					// this kind, else it could win a kind it can't present (crashing at the first frame).
					if (!CanPresent(kind, backendFactory))
					{
						attempts.Append($"\n  - {backend.GetType().Name}/{kind}: backend does not implement IDrawingFactory<T> for this kind");
						(backendFactory as IDisposable)?.Dispose();
						context.Dispose();
						continue;
					}

					DrawingFactory.Register(backendFactory);
					return new GraphicsInitialization(backend, context, backendFactory);
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

	// The closed kind → device-context narrowing: hand the typed provider the context's device face. GL/Metal use
	// neutral device contexts (nameable here); Software + WebGpu use the base IGraphicsContext (WebGpu self-casts
	// to its own device context inside its provider). Null when the provider lacks the instantiation. The `?.`
	// short-circuits before the (device-context) cast, so a declining provider never triggers an InvalidCast.
	private static IDrawingFactory? CreateGraphics(GraphicsContextKind kind, IGraphicsProvider provider, ISwapChain context) => kind switch
	{
		GraphicsContextKind.OpenGL or GraphicsContextKind.OpenGLES
			=> (provider as IGraphicsProvider<IGLDeviceContext>)?.CreateGraphics((IGLDeviceContext)context),
		GraphicsContextKind.Metal
			=> (provider as IGraphicsProvider<IMetalDeviceContext>)?.CreateGraphics((IMetalDeviceContext)context),
		_ => (provider as IGraphicsProvider<IGraphicsContext>)?.CreateGraphics(context),
	};

	// The closed kind → typed-present capability mapping (kind ⇒ the IDrawingFactory<TTarget> a backend must
	// implement). Vulkan is intentionally unmapped (no present path wired on this seam yet) → declined.
	private static bool CanPresent(GraphicsContextKind kind, IDrawingFactory backend) => kind switch
	{
		GraphicsContextKind.OpenGL or GraphicsContextKind.OpenGLES => backend is IDrawingFactory<IGLRenderTarget>,
		GraphicsContextKind.Metal => backend is IDrawingFactory<IMetalRenderTarget>,
		GraphicsContextKind.Software => backend is IDrawingFactory<ISoftwareRenderTarget>,
		GraphicsContextKind.WebGpu => backend is IDrawingFactory<IWebGpuRenderTarget>,
		_ => false,
	};
}
