#if __IOS__
using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI;
using Windows.Foundation;
using UIKit;
using Foundation;

namespace Windows.UI.ViewManagement
{
	public partial class InputPane
	{
		partial void TryHidePartial()
		{
			UIApplication.SharedApplication.KeyWindow.EndEditing(true);
		}

		internal Uno.UI.Controls.Window Window { get; set; }

		partial void EnsureFocusedElementInViewPartial()
		{
			if (Visible)
			{
				Window?.MakeFocusedViewVisible(isOpeningKeyboard: false);
			}
			else
			{
				Window?.RestoreFocusedViewVisibility();
			}
		}
	}
}
#endif