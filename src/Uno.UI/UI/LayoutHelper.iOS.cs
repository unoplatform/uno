using System;
using System.Runtime.CompilerServices;
using UIKit;
using Windows.Foundation;
using Windows.UI.Xaml;

namespace Uno.UI
{
	partial class LayoutHelper
	{
		/// <summary>
		/// Get relative bounds for native views.
		/// </summary>
		internal static Rect GetBoundsRectRelativeTo(this UIView element, UIView relativeTo)
			=> relativeTo.ConvertRectFromView(element.Bounds, element);
	}
}
