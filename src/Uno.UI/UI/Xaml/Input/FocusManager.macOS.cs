#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AppKit;
using Uno.UI.Extensions;
using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Input
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
