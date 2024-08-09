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
}
