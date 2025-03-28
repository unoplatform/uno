using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UIKit;
using Uno.UI.Extensions;
using Windows.UI.Xaml.Controls;
using Windows.UI.ViewManagement;
using Uno.UI.Xaml.Core;

namespace Windows.UI.Xaml.Input
{
	public partial class FocusManager
	{
		private static void FocusNative(UIElement control)
		{
			var focusManager = VisualTree.GetFocusManagerForElement(control);
			if (control?.CanBecomeFirstResponder == true &&
				focusManager?.InitialFocus == false)  // Do not focus natively on initial focus so the soft keyboard is not opened
			{
				control.BecomeFirstResponder();
			}
		}
	}
}
