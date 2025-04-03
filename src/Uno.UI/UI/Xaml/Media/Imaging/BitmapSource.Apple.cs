using System;
using System.IO;
using Foundation;
using Uno.UI.Extensions;
using _UIImage = UIKit.UIImage;

namespace Microsoft.UI.Xaml.Media.Imaging;

public partial class BitmapSource
{
	partial void UpdatePixelWidthAndHeightPartial(Stream stream)
	{
		var image = UIKit.UIImage.LoadFromData(NSData.FromStream(stream));
		PixelWidth = (int)Math.Round(image.Size.Width * image.CurrentScale);
		PixelHeight = (int)Math.Round(image.Size.Height * image.CurrentScale);
	}
}
