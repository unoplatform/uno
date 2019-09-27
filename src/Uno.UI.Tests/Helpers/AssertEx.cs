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

			var brush = resources[key] as SolidColorBrush;
			Assert.IsNotNull(brush);
			Assert.AreEqual(expected, brush.Color);
		}
	}
}
