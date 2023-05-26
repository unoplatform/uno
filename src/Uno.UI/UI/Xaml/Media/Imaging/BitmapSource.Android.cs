using System.IO;
using Android.Graphics;

namespace Windows.UI.Xaml.Media.Imaging;

public partial class BitmapSource
{
	partial void UpdatePixelWidthAndHeightPartial(Stream stream)
	{
		// TODO: Handle EXIF here after https://github.com/unoplatform/uno/pull/12407 is merged.
		var bitmap = BitmapFactory.DecodeStream(stream);
		PixelWidth = bitmap.Width;
		PixelHeight = bitmap.Height;
	}
}
