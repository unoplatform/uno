using Android.Graphics;
using Android.Media;

namespace Windows.UI.Xaml.Media.Imaging;

public partial class BitmapSource
{
	partial void UpdatePixelWidthAndHeightPartial(global::System.IO.Stream stream)
	{
		// InJustDecodeBounds will let DecodeStream to return null and not allocate for image pixels, but
		// allows reading some info like width and height.
		var options = new BitmapFactory.Options() { InJustDecodeBounds = true };
		_ = BitmapFactory.DecodeStream(stream, outPadding: null, options);
		var width = options.OutWidth;
		var height = options.OutHeight;

		if (stream.CanSeek)
		{
			stream.Position = 0;
			var orientation = new ExifInterface(stream).GetAttributeInt(ExifInterface.TagOrientation, (int)Orientation.Normal);
			if (orientation is (int)Orientation.Rotate90 or (int)Orientation.Rotate270)
			{
				(width, height) = (height, width);
			}
		}

		PixelWidth = width;
		PixelHeight = height;
	}
}
