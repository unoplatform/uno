using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using CoreGraphics;
using Windows.UI;
using AppKit;

namespace Uno.UI.Extensions
{
	internal static partial class NSUIImageExtensions
	{
		internal static NSImage FromCGImage(CGImage cgImage)
		{
			return new NSImage(cgImage, new CGSize(cgImage.Width, cgImage.Height));
		}
	}
}
