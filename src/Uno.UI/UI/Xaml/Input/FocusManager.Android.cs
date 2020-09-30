using System;
using Android.App;
using Android.Views;
using Android.Widget;
using Uno.UI;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml.Controls;
using Android.Graphics;
using Windows.UI.ViewManagement;
using Uno.UI.Xaml.Core;

namespace Windows.UI.Xaml.Input
{
	public partial class FocusManager
	{
		private static void FocusNative(UIElement element)
		{
			// TODO Uno: Handle Hyperlink focus
			if (element is Control control)
			{
				var focusManager = VisualTree.GetFocusManagerForElement(control);
				if (element is Android.Views.View androidView &&
					androidView.Focusable &&
					focusManager?.InitialFocus == false) // Do not focus natively on initial focus so the soft keyboard is not opened
				{
					control.RequestFocus();
				}

				// Forcefully try to bring the control into view when keyboard is open to accommodate adjust nothing mode
				if (InputPane.GetForCurrentView().Visible)
				{
					control.StartBringIntoView();
				}
			}
		}
	}
}
