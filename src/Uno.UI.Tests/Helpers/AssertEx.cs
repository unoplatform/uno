using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Uno.UI.Tests.Helpers
{
	public static class AssertEx
	{
		public static void AssertContainsColorBrushResource(ResourceDictionary resources, string key, Color expected)
		{
			Assert.IsNotNull(resources);

			AssertHasColor(resources[key] as Brush, expected);
		}

		public static void AssertHasColor(Brush brush, Color expected)
		{
			var colorBrush = brush as SolidColorBrush;
			Assert.IsNotNull(colorBrush);
			Assert.AreEqual(expected, colorBrush.Color);
		}
	}
}
