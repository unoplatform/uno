#nullable enable

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// A Metal color target: the per-frame <c>MTLTexture</c> the backend renders into. Pure surface — the Metal
/// device details (<c>MTLDevice</c> + <c>MTLCommandQueue</c>) live on the context
/// (<see cref="IMetalDeviceContext"/>), not here. The Skia backend builds/caches a <c>GRContext</c>-Metal from
/// the context's device/queue and wraps this texture as its surface; the host commits/presents the drawable.
/// Mirrors <see cref="IGLRenderTarget"/> / <see cref="ISoftwareRenderTarget"/>. The handle is an opaque pointer.
/// </summary>
public interface IMetalRenderTarget : IRenderTarget
{
	nint Texture { get; }
}
