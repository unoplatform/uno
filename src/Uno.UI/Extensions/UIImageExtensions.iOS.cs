using CoreGraphics;
using UIKit;

namespace Uno.UI.Extensions
{
	internal static partial class NSUIImageExtensions
	{
		internal static UIImage FromCGImage(CGImage cgImage)
		{
			return UIImage.FromImage(cgImage);
		}
	}
}
