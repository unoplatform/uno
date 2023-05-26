using System.IO;
using SkiaSharp;

namespace Windows.UI.Xaml.Media.Imaging;

public partial class BitmapSource
{
	protected void UpdatePixelWidthAndHeight(Stream stream)
	{
		using var codec = SKCodec.Create(stream);
		var info = codec.Info;
		PixelWidth = info.Width;
		PixelHeight = info.Height;
	}
}
