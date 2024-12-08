using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using Windows.UI.Xaml;

namespace Uno.UI.RuntimeTests.Extensions
{
	internal static class FrameworkElementExtensions
	{
		/// <summary>
		/// Get bounds of <paramref name="element"/> in screen coordinates.
		/// </summary>
		public static Rect GetOnScreenBounds(this FrameworkElement element)
			=> element.TransformToVisual(null).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));

		/// <summary>
		/// Get bounds of <paramref name="element"/> relative to <paramref name="relativeTo"/>.
		/// </summary>
		public static Rect GetRelativeBounds(this FrameworkElement element, FrameworkElement relativeTo)
			=> element.TransformToVisual(relativeTo).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));

		public static RelativeCoords GetRelativeCoords(this FrameworkElement parent, FrameworkElement child)
		{
			return RelativeCoords.From(parent, child);
		}
	}
}
