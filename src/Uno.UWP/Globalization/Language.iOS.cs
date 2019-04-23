#if __IOS__
using UIKit;

namespace Windows.Globalization
{
	public partial class Language
	{
		public static string CurrentInputMethodLanguageTag
		{
			get
			{
				UIView responder = null;
				if (UIApplication.SharedApplication?.KeyWindow?.RootViewController?.View != null)
				{
					responder = FindFirstResponder(UIApplication.SharedApplication.KeyWindow.RootViewController.View);
				}
				var inputMode = responder?.TextInputMode;
				return inputMode?.PrimaryLanguage ?? "";
			}
		}

		private static UIView FindFirstResponder(UIView view)
		{
			if (view.IsFirstResponder)
			{
				return view;
			}
			foreach (var subView in view.Subviews)
			{
				var firstResponder = FindFirstResponder(subView);
				if (firstResponder != null)
				{
					return firstResponder;
				}
			}
			return null;
		}

		public static bool TrySetInputMethodLanguageTag(string languageTag) => false;
	}
}
#endif
