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
	}
}
