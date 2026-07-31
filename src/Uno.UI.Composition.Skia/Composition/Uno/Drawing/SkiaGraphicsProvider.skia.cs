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
		=> _preferred = preferred.Length > 0
			? preferred
			: new[] { GraphicsContextKind.OpenGL, GraphicsContextKind.OpenGLES, GraphicsContextKind.Software };

	public IReadOnlyList<GraphicsContextKind> PreferredContexts => _preferred;

	public GraphicsRequirements Requirements => new() { MinStencilBits = 8, PreferredColor = GraphicsColorFormat.Bgra8888 };

	private readonly SkiaDrawingFactory _drawing = new();

	public Graphics CreateGraphics(IGraphicsContext context) => new(_drawing, new SkiaRenderer());
}
