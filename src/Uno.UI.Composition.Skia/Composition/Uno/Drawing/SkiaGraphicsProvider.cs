#nullable enable

using System.Collections.Generic;

namespace Uno.UI.Composition.Drawing;

/// <summary>The registerable Skia backend pair (the built-in default choice; registered like any other backend).</summary>
public sealed class SkiaGraphicsProvider : IGraphicsProvider
{
	// Skia can render on GL/GLES, Metal (Apple), or the CPU framebuffer; the host's context factory vetoes a kind
	// (returns null) when it can't provide that window, so negotiation falls to the next. This backend owns the kind
	// ORDER (negotiation walks it as-is; the host never reorders). Two knobs express a preference: the app passes an
	// explicit order to the constructor (e.g. `new SkiaGraphicsProvider(GraphicsContextKind.Software)` to force
	// software, or `{ OpenGLES, Software }` to prefer GLES); and a host declines kinds per its own config (Win32
	// returns null for OpenGL when UseOpenGLOnWin32 is false, X11 for OpenGL when PreferGLESOverGLOnX11, etc.).
	// The default order below prefers desktop GL, then GLES, Vulkan, Metal, then software.
	private readonly GraphicsContextKind[] _preferred;

	public SkiaGraphicsProvider(params GraphicsContextKind[] preferred)
	{
		_preferred = preferred.Length > 0
			? preferred
			: new[] { GraphicsContextKind.OpenGL, GraphicsContextKind.OpenGLES, GraphicsContextKind.Vulkan, GraphicsContextKind.Metal, GraphicsContextKind.Software };
	}

	public IReadOnlyList<GraphicsContextKind> PreferredContexts => _preferred;

	// Geometry is a separate seam (GeometryFactory) — swap the geometry engine there, not on the graphics backend.
	public Graphics CreateGraphics(IGraphicsContext context) => new(new SkiaDrawingFactory(), new SkiaRenderer());
}
