#nullable enable

using System;

namespace Uno.UI.Composition.Drawing;

internal static partial class ManagedImageDecoder
{
	/// <summary>
	/// Reads the size <see cref="TryDecode"/> produces with no target size (EXIF orientation applied) without decoding the
	/// pixels, through the decode's own header readers, so that it refuses every file the decode refuses from the headers
	/// those read: for a JPEG, the markers before its first scan and that scan's header. It can still be true for a file
	/// whose compressed data, or a JPEG marker after its first scan, then fails the decode.
	/// </summary>
	public static bool TryGetSize(ImageBytes data, out int width, out int height)
	{
		try
		{
			if (TryGetSizeCore(data, out width, out height))
			{
				return true;
			}
		}
		catch
		{
			// Malformed input is simply "not decoded", as in TryDecode.
		}

		width = 0;
		height = 0;
		return false;
	}

	private static bool TryGetSizeCore(ImageBytes d, out int width, out int height)
	{
		width = 0;
		height = 0;
		switch (DetectFormat(d))
		{
			case ImageFormat.Png:
				if (!TryReadPngHeader(d, null, out var png))
				{
					return false;
				}

				(width, height) = (png.Width, png.Height);
				return true;

			case ImageFormat.Gif:
				return TryReadGif(d, null, out width, out height);

			case ImageFormat.Bmp:
				if (!TryReadBmpHeader(d, out var bmp))
				{
					return false;
				}

				(width, height) = (bmp.Width, bmp.Height);
				return true;

			case ImageFormat.Jpeg:
				return new Jpeg(d, null).TryReadSize(out width, out height);

			case ImageFormat.Webp:
				return TryReadWebpSize(d, out width, out height);

			default:
				return false;
		}
	}
}
