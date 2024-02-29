using UIKit;
using Uno.Extensions;

namespace Windows.Globalization
{
	public partial class Language
	{
		public static string CurrentInputMethodLanguageTag
		{
			get
			{
				UIView? responder = null;
				if (UIApplication.SharedApplication?.KeyWindow?.RootViewController?.View != null)
				{
					responder = UIApplication.SharedApplication.KeyWindow.RootViewController.View.FindFirstResponder();
				}
				var inputMode = responder?.TextInputMode;
				return inputMode?.PrimaryLanguage ?? "";
			}
		}

		public static bool TrySetInputMethodLanguageTag(string languageTag) => false;
	}
}
