using System;
using System.Collections.Generic;
using System.Text;
using Color = AppKit.NSColor;
using Uno.Extensions;
using AppKit;
using CoreGraphics;

namespace Windows.UI.Xaml.Media
{
	// iOS partial for SolidColorBrush
	public partial class SolidColorBrush
	{
		/// <summary>
		/// Blends the Color set on the SolidColorBrush with its Opacity. Should generally be used for rendering rather than the Color property itself.
		/// </summary>
		internal Color ColorWithOpacity
		{
			get; set;
		}

		/// <remarks>
		/// This method is required for performance. Creating a native Color 
		/// requires a round-trip with Objective-C, so updating this value only when opacity
		/// and color changes is more efficient.
		/// </remarks>
		partial void UpdateColorWithOpacity(Color newColor, double opacity)
		{
            newColor.A = (byte)(newColor.A * opacity);

			ColorWithOpacity = newColor;
		}
	}
}
