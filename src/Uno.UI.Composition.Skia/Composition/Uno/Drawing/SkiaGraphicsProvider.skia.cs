#nullable enable

using System.Collections.Generic;

namespace Uno.UI.Composition.Drawing;

/// <summary>The registerable Skia backend pair (the built-in default choice; registered like any other backend).</summary>
public sealed class SkiaGraphicsProvider : IGraphicsProvider
{
	// Skia can render on GL/GLES or the CPU framebuffer; the host's context factory vetoes a kind (returns null)
	// when it can't provide that window, so negotiation falls to the next. Default prefers desktop GL, then GLES,
	// then software; a host can narrow/reorder this per window through GraphicsRegistry.Initialize's neutral
	// kind-preference override (e.g. { Software } to force software, or { OpenGLES, Software } to prefer GLES).
	private readonly GraphicsContextKind[] _preferred;

	public SkiaGraphicsProvider(params GraphicsContextKind[] preferred)
		: this(null, preferred)
	{
	}

	/// <summary>
	/// Creates the Skia backend over a specific drawing factory (e.g. <c>SkiaManagedGeometryDrawingFactory</c> for
	/// managed geometry on Skia pixels). Defaults to a plain <see cref="SkiaDrawingFactory"/>. The provider owns its
	/// drawing factory — it is not sourced from a global registration.
	/// </summary>
	public SkiaGraphicsProvider(IDrawingFactory? drawingFactory, params GraphicsContextKind[] preferred)
	{
		_preferred = preferred.Length > 0
			? preferred
			: new[] { GraphicsContextKind.OpenGL, GraphicsContextKind.OpenGLES, GraphicsContextKind.Vulkan, GraphicsContextKind.Software };
		_drawing = drawingFactory ?? new SkiaDrawingFactory();
	}

	public IReadOnlyList<GraphicsContextKind> PreferredContexts => _preferred;

	public GraphicsRequirements Requirements => new() { MinStencilBits = 8, PreferredColor = GraphicsColorFormat.Bgra8888 };

	private readonly IDrawingFactory _drawing;

	public Graphics CreateGraphics(IGraphicsContext context) => new(_drawing, new SkiaRenderer());
}
