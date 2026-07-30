#nullable enable

using System.Collections.Generic;

namespace Uno.UI.Composition.Drawing;

/// <summary>The registerable Skia backend pair (the built-in default choice; registered like any other backend).</summary>
public sealed class SkiaGraphicsBackend : IGraphicsBackend
{
	private static readonly GraphicsContextKind[] _preferred = { GraphicsContextKind.Software };

	public IReadOnlyList<GraphicsContextKind> PreferredContexts => _preferred;

	public GraphicsRequirements Requirements => new() { MinStencilBits = 8, PreferredColor = GraphicsColorFormat.Bgra8888 };

	public IDrawingBackend Drawing { get; } = new SkiaDrawingBackend();

	public IRenderBackend CreateRenderBackend(IGraphicsContext context) => new SkiaRenderBackend();
}
