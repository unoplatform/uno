using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using CoreGraphics;
using UIKit;
using Windows.UI;

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
