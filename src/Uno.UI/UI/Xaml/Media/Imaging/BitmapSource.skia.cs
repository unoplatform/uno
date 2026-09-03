using System.IO;
using Uno.UI.Composition.Drawing;

namespace Microsoft.UI.Xaml.Media.Imaging;

public partial class BitmapSource
{
	partial void UpdatePixelWidthAndHeightPartial(Stream stream)
	{
		// Read the source dimensions through the neutral backend decoder (no Skia codec here).
		if (ImageEncoderDecoder.Current.TryDecode(stream, null, null, out var frames))
		{
			using (frames)
			{
				PixelWidth = frames.Frames[0].PixelWidth;
				PixelHeight = frames.Frames[0].PixelHeight;
			}
		}
	}
}
