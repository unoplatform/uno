using CoreGraphics;
using UIKit;

namespace Uno.UI.Extensions;

internal static partial class NSUIImageExtensions
{
	internal static UIImage FromCGImage(CGImage cgImage)
	{
		return UIImage.FromImage(cgImage);
	}

	/// <summary>
	/// Fixes orientation issues caused by taking an image straight from the camera
	/// </summary>
	/// <param name="image">UI Image</param>
	/// <returns>UI image</returns>
	internal static UIImage FixOrientation(this UIImage image)
	{
		if (image.Orientation == UIImageOrientation.Up)
		{
			return image;
		}

		// http://stackoverflow.com/questions/5427656/ios-uiimagepickercontroller-result-image-orientation-after-upload
		UIGraphics.BeginImageContextWithOptions(image.Size, false, image.CurrentScale);

		image.Draw(new CGRect(0, 0, image.Size.Width, image.Size.Height));
		var normalizedImage = UIGraphics.GetImageFromCurrentImageContext();

		UIGraphics.EndImageContext();

		return normalizedImage;
	}
}
