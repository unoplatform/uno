#nullable enable

using System;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// The device face of an OpenGL graphics context: the proc-address loader a backend needs to build its GL
/// rendering state. The GL dialect (desktop GL / GLES / WebGL) is carried by <see cref="IGraphicsContext.Kind"/>;
/// the per-frame framebuffer is a separate <see cref="IGLRenderTarget"/> concern. Only neutral types cross the seam.
/// </summary>
public interface IGLDeviceContext : IGraphicsContext
{
	/// <summary>Host GL proc-address loader so any backend can resolve GL entry points itself.</summary>
	Func<string, nint> GetProcAddress { get; }
}
