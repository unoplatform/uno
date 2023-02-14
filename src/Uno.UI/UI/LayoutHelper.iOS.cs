using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using UIKit;
using Windows.Foundation;
using Microsoft.UI.Xaml;

namespace Uno.UI
{
	partial class LayoutHelper
	{
		/// <summary>
		/// Get relative bounds for native views.
		/// </summary>
		[Pure]
		internal static Rect GetBoundsRectRelativeTo(this UIView element, UIView relativeTo)
			=> relativeTo.ConvertRectFromView(element.Bounds, element);
	}
}
