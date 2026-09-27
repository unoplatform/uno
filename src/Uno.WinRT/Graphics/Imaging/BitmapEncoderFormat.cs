#nullable enable

namespace Uno.UI.Composition.Drawing;

/// <summary>Backend-neutral output formats a <c>BitmapEncoder</c> can target (part of the codec seam; kept out of the
/// WinUI <c>Windows.Graphics.Imaging</c> namespace to avoid colliding with a future WinUI type).</summary>
public enum BitmapEncoderFormat
{
	Bmp,
	Gif,
	Jpeg,
	Png,
	Heif,
}
