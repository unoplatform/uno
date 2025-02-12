using System.IO;
using SkiaSharp;

namespace Windows.UI.Xaml.Media.Imaging;

public partial class BitmapSource
{
	partial void UpdatePixelWidthAndHeightPartial(Stream stream)
	{
		using var codec = SKCodec.Create(stream);
		var info = codec.Info;
		PixelWidth = info.Width;
		PixelHeight = info.Height;
	}
}
