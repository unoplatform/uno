#nullable enable

using Uno.UI.Composition.Drawing;

namespace Uno.UI.Composition.WebGpu;

/// <summary>
/// Entry point to the WebGPU drawing backend, for an app to register on the host builder
/// (<c>builder.GraphicsBackend(WebGpuBackend.CreateGraphicsProvider())</c>). Geometry is a separate seam: WebGPU
/// flattens everything, so a SkiaSharp-free app registers the managed engine there
/// (<c>Uno.UI.Composition.Managed.ManagedBackend.CreateGeometryFactory()</c>).
/// </summary>
public static class WebGpuBackend
{
	public static IGraphicsProvider CreateGraphicsProvider() => new WebGpuGraphicsProvider();
}
