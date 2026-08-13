#nullable enable

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// A Metal color target: the per-frame <c>MTLTexture</c> the backend renders into, plus the <c>MTLDevice</c> and
/// <c>MTLCommandQueue</c> the host created it with. The Skia backend builds (and caches) a <c>GRContext</c>-Metal
/// from the device/queue and wraps the texture as its surface; the host's own draw callback commits/presents the
/// drawable afterwards. Mirrors <see cref="IGLRenderTarget"/> / <see cref="ISoftwareRenderTarget"/> so the host
/// (e.g. macOS) stays free of any Skia/GPU-library type. All handles are opaque native pointers.
/// </summary>
public interface IMetalRenderTarget : IRenderTarget
{
	nint Texture { get; }

	nint Device { get; }

	nint Queue { get; }
}
