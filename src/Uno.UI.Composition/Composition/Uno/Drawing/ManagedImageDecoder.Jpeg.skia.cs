#nullable enable

using System.Diagnostics.CodeAnalysis;

namespace Uno.UI.Composition.Drawing;

internal static partial class ManagedImageDecoder
{
	private static bool TryDecodeJpeg(byte[] d, [NotNullWhen(true)] out DecodedImage? decoded)
	{
		// Implemented in a follow-up; falls back to the Skia codec for now.
		decoded = null;
		return false;
	}
}
