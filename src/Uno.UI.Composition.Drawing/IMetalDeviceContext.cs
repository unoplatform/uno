#nullable enable

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// The device face of a Metal graphics context: the stable <c>MTLDevice</c> and <c>MTLCommandQueue</c> a backend
/// builds its Metal rendering state from. A backend reads these from the context at
/// <see cref="IGraphicsProvider{TContext}.CreateGraphics"/> (the context <em>is</em> the device); the per-frame
/// <c>MTLTexture</c> is a separate <see cref="IMetalRenderTarget"/> concern. Neutral: opaque native pointers.
/// </summary>
public interface IMetalDeviceContext : IGraphicsContext
{
	nint Device { get; }

	nint Queue { get; }
}
