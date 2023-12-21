using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;

namespace Uno.UI.Helpers.WinUI
{
	internal static class CppWinRTHelpers
	{
		public static bool SetFocus(DependencyObject obj, FocusState focusState)
		{
			if (obj != null)
			{
				// Use TryFocusAsync if it's available.
				if (false) //TODO Uno specific: WinUI checks for TryFocusAsync method presence, which is not implemented yet in Uno (issue #4256)
				{
					//var result = FocusManager.TryFocusAsync(obj, focusState);
					//if (result.Status == AsyncStatus.Completed)
					//{
					//	return result.GetResults().Succeeded;
					//}
					//// Operation was async, let's assume it worked.
					//return true;
				}

				if (obj is Control control)
				{
					return control.Focus(focusState);
				}
				else if (obj is Hyperlink hyperlink)
				{
					return hyperlink.Focus(focusState);
				}
#if !HAS_UNO_WINUI // ContentLink is no longer available in WinUI
				else if (obj is ContentLink contentlink)
				{
					return contentlink.Focus(focusState);
				}
#endif
				else if (obj is WebView webview)
				{
					return webview.Focus(focusState);
				}
			}

			return false;
		}
	}
}
