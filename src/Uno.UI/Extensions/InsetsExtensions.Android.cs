using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Xaml;

namespace Uno.UI.Extensions
{
	internal static class InsetsExtensions
	{
		public static Thickness ToThickness(this Android.Graphics.Insets insets)
			=> new Thickness(insets.Left, insets.Top, insets.Right, insets.Bottom);
	}
}
