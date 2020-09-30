using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;

namespace Uno.UI.RuntimeTests.Helpers
{
	internal static class RectAssert
	{
		public static void AreEqual(Rect expected, Rect actual, double delta = 1)
		{
			Assert.AreEqual(expected.Left, actual.Left, delta);
			Assert.AreEqual(expected.Top, actual.Top, delta);
			Assert.AreEqual(expected.Width, actual.Width, delta);
			Assert.AreEqual(expected.Height, actual.Height, delta);
		}

		public static void AreNotEqual(Rect rect1, Rect rect2)
		{
			if (rect1.Left == rect2.Left && rect1.Top == rect2.Top && rect1.Width == rect2.Width && rect1.Height == rect2.Height)
			{
				throw new AssertFailedException("Expected unequal rects, but they were equal.");
			}
		}

		public static void Contains(Rect outer, Rect inner)
		{
			if (inner.Left < outer.Left || inner.Top < outer.Top || inner.Right > outer.Right || inner.Bottom > outer.Bottom)
			{
				throw new AssertFailedException($"Rect {inner} does not lie fully inside rect {outer}");
			}
		}
	}
}
