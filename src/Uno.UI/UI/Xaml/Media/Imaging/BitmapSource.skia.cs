using System.IO;
using SkiaSharp;

namespace Microsoft.UI.Xaml.Media.Imaging;

public partial class BitmapSource
{
	partial void UpdatePixelWidthAndHeightPartial(Stream stream)
	{
		if (stream.CanSeek)
		{
			stream.Seek(0, 0);
		}
		using var codec = SKCodec.Create(stream);
		var info = codec.Info;
		PixelWidth = info.Width;
		PixelHeight = info.Height;
	}
}
