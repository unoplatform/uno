#nullable enable

using System.Collections.Generic;

namespace Uno.UI.Composition.Drawing;

/// <summary>The registerable Skia backend pair (the built-in default choice; registered like any other backend).</summary>
public sealed class SkiaGraphicsProvider :
	IGraphicsProvider<IGLDeviceContext>,
	IGraphicsProvider<IMetalDeviceContext>,
	IGraphicsProvider<IVulkanDeviceContext>,
	IGraphicsProvider<IGraphicsContext>
{
	// Negotiation walks this kind order as-is; the host vetoes a kind it can't serve (returns null) and the next
	// is tried. The app can override the order via the constructor. Default is Vulkan first, then desktop GL,
	// GLES, Metal, then software — GPU hosts that don't serve Vulkan (macOS→Metal, LinuxFB/WASM→GLES) decline it
	// and fall through, so the single order is safe for every host.
	private readonly GraphicsContextKind[] _preferred;

	public SkiaGraphicsProvider(params GraphicsContextKind[] preferred)
	{
		_preferred = preferred.Length > 0
			? preferred
			: new[] { GraphicsContextKind.Vulkan, GraphicsContextKind.OpenGL, GraphicsContextKind.OpenGLES, GraphicsContextKind.Metal, GraphicsContextKind.Software };
	}

	public IReadOnlyList<GraphicsContextKind> PreferredContexts => _preferred;

	// One typed CreateGraphics per device face Skia serves — it reads the device details off the typed context
	// (no cast). Software needs no device. Geometry is a separate seam (GeometryFactory).
	public IDrawingFactory CreateGraphics(IGLDeviceContext context) => new SkiaDrawingFactory(glDevice: context);

	public IDrawingFactory CreateGraphics(IMetalDeviceContext context) => new SkiaDrawingFactory(metalDevice: context);

	public IDrawingFactory CreateGraphics(IVulkanDeviceContext context) => new SkiaDrawingFactory(vulkanDevice: context);

	public IDrawingFactory CreateGraphics(IGraphicsContext context) => new SkiaDrawingFactory();
}
