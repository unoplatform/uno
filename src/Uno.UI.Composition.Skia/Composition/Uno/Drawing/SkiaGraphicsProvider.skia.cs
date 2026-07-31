#nullable enable

using System.Collections.Generic;

namespace Uno.UI.Composition.Drawing;

/// <summary>The registerable Skia backend pair (the built-in default choice; registered like any other backend).</summary>
public sealed class SkiaGraphicsProvider : IGraphicsProvider
{
	// Skia can render on GL or the CPU framebuffer; prefer GL, letting the host's context factory veto it
	// (return null) when it can't provide a GL window — negotiation then falls to software.
	private static readonly GraphicsContextKind[] _preferred = { GraphicsContextKind.OpenGL, GraphicsContextKind.Software };

	public IReadOnlyList<GraphicsContextKind> PreferredContexts => _preferred;

	public GraphicsRequirements Requirements => new() { MinStencilBits = 8, PreferredColor = GraphicsColorFormat.Bgra8888 };

	private readonly SkiaDrawingFactory _drawing = new();

	public Graphics CreateGraphics(IGraphicsContext context) => new(_drawing, new SkiaRenderer());
}
