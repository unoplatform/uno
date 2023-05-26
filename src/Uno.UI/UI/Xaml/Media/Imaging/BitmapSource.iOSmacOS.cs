using System;
using System.IO;
using Foundation;
using Uno.UI.Extensions;

#if __IOS__
using _UIImage = UIKit.UIImage;
#elif __MACOS__
using _UIImage = AppKit.NSImage;
#endif

namespace Windows.UI.Xaml.Media.Imaging;

public partial class BitmapSource
{
	partial void UpdatePixelWidthAndHeightPartial(Stream stream)
	{
#if __IOS__
		var image = UIKit.UIImage.LoadFromData(NSData.FromStream(stream)).FixOrientation();
		PixelWidth = (int)Math.Round(image.Size.Width * image.CurrentScale);
		PixelHeight = (int)Math.Round(image.Size.Height * image.CurrentScale);
#elif __MACOS__
		var image = AppKit.NSImage.FromStream(stream);
		PixelWidth = (int)Math.Round(image.Size.Width);
		PixelHeight = (int)Math.Round(image.Size.Height);
#endif
	}
}
