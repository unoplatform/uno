using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Xaml;

#if __ANDROID__
using Android.Views;
#endif  // __ANDROID__

namespace Microsoft.UI.Xaml
{
	internal static class TextAlignmentExtensions
	{
#if __APPLE_UIKIT__

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

#elif __ANDROID__

		internal static GravityFlags ToGravity(this TextAlignment textAlignment)
		{
			switch (textAlignment)
			{
				case TextAlignment.Center:
					return GravityFlags.Center;
				case TextAlignment.Right:
					return GravityFlags.Right;
				case TextAlignment.Justify:
					return GravityFlags.FillHorizontal;
				case TextAlignment.Left:
				default:
					return GravityFlags.Left;
			}
		}

#endif
	}
}
