using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
	}
}
