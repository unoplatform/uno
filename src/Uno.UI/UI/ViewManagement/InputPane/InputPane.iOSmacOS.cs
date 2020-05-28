using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI;
using Windows.Foundation;
using Foundation;

#if __IOS__
using UIKit;
#endif

namespace Windows.UI.ViewManagement
{
	public partial class InputPane
	{
		internal Uno.UI.Controls.Window Window { get; set; }

#if __IOS__
		partial void TryHidePartial()
		{
			UIKit.UIApplication.SharedApplication.KeyWindow.EndEditing(true);
		}


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
#endif
	}
}
