using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AppKit;
using Uno.UI.Extensions;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Input
{
	public partial class FocusManager
	{
		private static void FocusNative(UIElement control)
		{
			if (control?.AcceptsFirstResponder() == true)
			{
				Window.Current?.NativeWindow?.MakeFirstResponder(control);
			}
		}
	}
}
