using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using ContentLink = Windows.UI.Xaml.Documents.ContentLink;

namespace Uno.UI.Helpers.WinUI
{
	internal static class CppWinRTHelpers
	{
		public static bool SetFocus(DependencyObject obj, FocusState focusState)
		{
			if (obj != null)
			{
				// Use TryFocusAsync if it's available.
				if (false) //TODO:MZ: always true
				{
					//var result = FocusManager.TryFocusAsync(obj, focusState); //TODO:MZ:TryFocusAsync is not available in Uno!
					//if (result.Status == AsyncStatus.Completed)
					//{
					//	return result.GetResults().Succeeded;
					//}
					//// Operation was async, let's assume it worked.
					//return true;
				}

				//Unreachable as TryFocusAsync is available
				if (obj is Control control)
				{
					return control.Focus(focusState);
				}
				else if (obj is Hyperlink hyperlink)
				{
					return hyperlink.Focus(focusState);
				}
				else if (obj is ContentLink contentlink)
				{
					return contentlink.Focus(focusState);
				}
				else if (obj is WebView webview)
				{
					return webview.Focus(focusState);
				}
			}

			return false;
		}
	}
}
