#nullable enable

namespace Uno.UI.Runtime.Skia;

/// <summary>
/// The pixel format of a headless render buffer. Kept independent from the underlying rendering
/// engine so the public API carries no engine-specific types.
/// </summary>
public enum HeadlessPixelFormat
{
	/// <summary>32-bit, 8 bits per channel, byte order B, G, R, A. Premultiplied alpha.</summary>
	Bgra8888 = 0,

	/// <summary>32-bit, 8 bits per channel, byte order R, G, B, A. Premultiplied alpha.</summary>
	Rgba8888,
}
