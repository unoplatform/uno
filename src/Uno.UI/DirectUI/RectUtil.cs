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

	internal static Rect CreateEmptyRect() => Rect.Empty;

	internal static bool GetIsEmpty(Rect rect) => rect.IsEmpty;

	internal static bool AreDisjoint(Rect rect1, Rect rect2) =>
		rect1.IsEmpty || rect2.IsEmpty ||
		rect1.X + rect1.Width <= rect2.X || rect2.X + rect2.Width <= rect1.X ||
		rect1.Y + rect1.Height <= rect2.Y || rect2.Y + rect2.Height <= rect1.Y;
}
