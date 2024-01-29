using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using Android.Views;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using static System.Double;

namespace Uno.UI
{
	partial class LayoutHelper
	{
		/// <summary>
		/// Get relative bounds for native views.
		/// </summary>
		/// <remarks>This should only be used for non-FrameworkElements, prefer the more accurate overload that takes <see cref="FrameworkElement"/>s where possible.</remarks>
		[Pure]
		internal static Rect GetBoundsRectRelativeTo(this View element, View relativeTo)
		{
			var elementToTarget = UIElement.TransformToVisual(element, relativeTo);
			var elementRect = new Rect(0, 0, ViewHelper.PhysicalToLogicalPixels(element.Width), ViewHelper.PhysicalToLogicalPixels(element.Height));
			var elementRectRelToTarget = elementToTarget.TransformBounds(elementRect);

			return elementRectRelToTarget;
		}
	}
}
