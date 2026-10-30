#nullable enable

using Silk.NET.WebGPU;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// A WebGPU color target for the <see cref="GraphicsContextKind.WebGpu"/> kind. It carries only the
/// <b>resolve</b> color attachment (a single-sample color the host owns, presents from, and can read back); the
/// backend allocates its own MSAA color + depth/stencil to match and resolves into <see cref="ColorView"/>.
/// </summary>
public unsafe interface IWebGpuRenderTarget : IRenderTarget
{
	/// <summary>The single-sample resolve destination the backend resolves the frame into. The host owns it.</summary>
	TextureView* ColorView { get; }
}
