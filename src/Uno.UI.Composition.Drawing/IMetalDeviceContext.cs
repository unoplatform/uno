#nullable enable

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// The device face of a Metal graphics context: the <c>MTLDevice</c> and <c>MTLCommandQueue</c> (as opaque
/// pointers) a backend builds its Metal rendering state from. The per-frame <c>MTLTexture</c> is a separate
/// <see cref="IMetalRenderTarget"/> concern.
/// </summary>
public interface IMetalDeviceContext : IGraphicsContext
{
	nint Device { get; }

	nint Queue { get; }
}
