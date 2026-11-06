using System.IO;
using SkiaSharp;

namespace Microsoft.UI.Xaml.Media.Imaging;

public partial class BitmapSource
{
	partial void UpdatePixelWidthAndHeightPartial(Stream stream)
	{
		// Null when Skia can't decode the data (HEIC, PDF, corrupt bytes). The size is left as is: the load then
		// raises ImageFailed, after the browser decoder had its chance on WebAssembly.
		using var codec = SKCodec.Create(stream);
		if (codec is null)
		{
			return;
		}

		var info = codec.Info;
		PixelWidth = info.Width;
		PixelHeight = info.Height;
	}
}
