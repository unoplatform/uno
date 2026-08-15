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
	// Skia can render on GL/GLES, Metal (Apple), or the CPU framebuffer; the host's context factory vetoes a kind
	// (returns null) when it can't provide that window, so negotiation falls to the next. This backend owns the kind
	// ORDER (negotiation walks it as-is; the host never reorders). Two knobs express a preference: the app passes an
	// explicit order to the constructor (e.g. `new SkiaGraphicsProvider(GraphicsContextKind.Software)` to force
	// software, or `{ OpenGLES, Software }` to prefer GLES); and a host declines kinds per its own config (Win32
	// returns null for OpenGL when UseOpenGLOnWin32 is false, X11 for OpenGL when PreferGLESOverGLOnX11, etc.).
	// Default order: Vulkan first, then desktop GL, GLES, Metal, then software. This matches the historical
	// per-host selection (feature/breakingchanges): X11/Win32 tried Vulkan first and fell back to GL, so Vulkan
	// is the default on the GPU hosts that serve it (they decline it — return null — when their UseVulkanOnX/
	// UseOpenGLOnX knobs say so, and negotiation falls through). Hosts that don't serve Vulkan (macOS→Metal,
	// LinuxFB/WASM→GLES) decline it and the next kind they serve wins, so the order is safe for them too.
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
