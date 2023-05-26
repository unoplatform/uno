using System.IO;
using SkiaSharp;

namespace Windows.UI.Xaml.Media.Imaging;

public partial class BitmapSource
{
	partial void UpdatePixelWidthAndHeightPartial(Stream stream)
	{
		var image = SKImage.FromEncodedData(stream);
		PixelWidth = image.Width;
		PixelHeight = image.Height;
	}
}
