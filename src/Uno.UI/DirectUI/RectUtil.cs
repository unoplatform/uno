using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace DirectUI;

internal static class RectUtil
{
	internal static bool AreEqual(Rect rect1, Rect rect2) => rect1.Equals(rect2);

	// MUX Reference RectUtil.cpp: only the Empty rectangle has a negative width; a zero-sized rect is not empty.
	internal static bool GetIsEmpty(Rect rect) => rect.Width < 0;

	internal static Rect CreateEmptyRect() => Rect.Empty;

	internal static Rect CreateRect(Point location, Size size) => new Rect(location, size);

	// Returns true if the rect2 is within rect, inclusive of
	// the edges.  Returns false otherwise.
	internal static bool ContainsFully(Rect rect, Rect rect2)
		=> rect2.X >= rect.X &&
			rect2.X + rect2.Width <= rect.X + rect.Width &&
			rect2.Y >= rect.Y &&
			rect2.Y + rect2.Height <= rect.Y + rect.Height;

	// Returns TRUE if either rect is empty or the rects
	// have an empty intersection.
	internal static bool AreDisjoint(Rect rect, Rect rect2)
	{
		var doIntersect =
			!(rect.Width < 0 || rect2.Width < 0) &&
			(rect2.X <= rect.X + rect.Width) &&
			(rect2.X + rect2.Width >= rect.X) &&
			(rect2.Y <= rect.Y + rect.Height) &&
			(rect2.Y + rect2.Height >= rect.Y);
		return !doIntersect;
	}
}
