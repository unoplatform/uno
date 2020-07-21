using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Display;

namespace Uno.UI.Toolkit.Extensions
{
    internal static class RectExtensions
	{
#if !XAMARIN && !NETSTANDARD2_0
		/// <summary>
		/// Returns the orientation of the rectangle.
		/// </summary>
		/// <param name="rect">A rectangle.</param>
		/// <returns>Portrait, Landscape, or None (if the rectangle has an exact 1:1 ratio)</returns>
		public static DisplayOrientations GetOrientation(this Rect rect)
		{
			if (rect.Height > rect.Width)
			{
				return DisplayOrientations.Portrait;
			}
			else if (rect.Width > rect.Height)
			{
				return DisplayOrientations.Landscape;
			}
			else
			{
				return DisplayOrientations.None;
			}
		} 
#endif
	}
}
