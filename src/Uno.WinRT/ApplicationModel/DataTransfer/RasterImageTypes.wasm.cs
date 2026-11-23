#nullable enable

using System;
using System.Collections.Generic;

namespace Windows.ApplicationModel.DataTransfer;

// The Bitmap format holds a raster image. Browsers hand copied or dragged image data over as a single image file,
// so a paste or a drop made of a single file of one of these types is also offered as the Bitmap; a file of any
// other type, SVG included, is offered as a file only.
internal static class RasterImageTypes
{
	private static readonly HashSet<string> _types = new(StringComparer.OrdinalIgnoreCase)
	{
		"image/png",
		"image/jpeg",
		"image/gif",
		"image/bmp",
		"image/webp",
	};

	public static bool Contains(string? mimeType) => mimeType is not null && _types.Contains(mimeType);
}
