using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;

namespace Windows.UI.Xaml
{
	internal static class TextAlignmentExtensions
	{
#if __IOS__

		internal static UIKit.UITextAlignment ToNativeTextAlignment(this TextAlignment textAlignment)
		{
			switch (textAlignment)
			{
				case TextAlignment.Center:
					return UIKit.UITextAlignment.Center;
				case TextAlignment.Right:
					return UIKit.UITextAlignment.Right;
				case TextAlignment.Justify:
					return UIKit.UITextAlignment.Justified;
				default:
				case TextAlignment.Left:
					return UIKit.UITextAlignment.Left;
			}
		}

#elif __MACOS__

		internal static AppKit.NSTextAlignment ToNativeTextAlignment(this TextAlignment textAlignment)
		{
			switch (textAlignment)
			{
				case TextAlignment.Center:
					return AppKit.NSTextAlignment.Center;
				case TextAlignment.Right:
					return AppKit.NSTextAlignment.Right;
				case TextAlignment.Justify:
					return AppKit.NSTextAlignment.Justified;
				case TextAlignment.Left:
					return AppKit.NSTextAlignment.Left;
				case TextAlignment.DetectFromContent:
					return AppKit.NSTextAlignment.Natural;
				default:
					throw new ArgumentException($"Unexpected value '{textAlignment}' for '{nameof(textAlignment)}'.");
			}
		}

#elif __ANDROID__

		internal static Android.Views.GravityFlags ToGravity(this TextAlignment textAlignment)
		{
			switch (textAlignment)
			{
				case TextAlignment.Center:
					return Android.Views.GravityFlags.Center;
				case TextAlignment.Right:
					return Android.Views.GravityFlags.Right;
				case TextAlignment.Justify:
					return Android.Views.GravityFlags.FillHorizontal;
				case TextAlignment.Left:
				default:
					return Android.Views.GravityFlags.Left;
			}
		}

#endif
	}
}
