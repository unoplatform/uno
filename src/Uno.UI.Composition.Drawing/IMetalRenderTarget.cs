#nullable enable

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// A Metal color target: the per-frame <c>MTLTexture</c> (an opaque pointer) the backend renders into. The Metal
/// device details live on <see cref="IMetalDeviceContext"/>, not here.
/// </summary>
public interface IMetalRenderTarget : IRenderTarget
{
	nint Texture { get; }
}
