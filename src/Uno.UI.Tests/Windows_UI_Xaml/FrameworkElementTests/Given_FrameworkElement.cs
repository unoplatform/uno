using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Tests.Windows_UI_Xaml.FrameworkElementTests
{
	[TestClass]
	public class Given_FrameworkElement
	{
		[TestMethod]
		public void When_LayoutUpdated()
		{
			var SUT = new Grid();

			var item1 = new Border();

			var sutLayoutUpdatedCount = 0;

			SUT.LayoutUpdated += delegate
			{
				sutLayoutUpdatedCount++;
			};

			var item1LayoutUpdatedCount = 0;
			item1.LayoutUpdated += delegate
			{
				item1LayoutUpdatedCount++;
			};

			SUT.Children.Add(item1);

			SUT.Measure(new Windows.Foundation.Size(1, 1));
			SUT.Arrange(new Windows.Foundation.Rect(0, 0, 1, 1));

			Assert.AreEqual(1, sutLayoutUpdatedCount);
			Assert.AreEqual(1, item1LayoutUpdatedCount);

			SUT.Measure(new Windows.Foundation.Size(2, 2));
			SUT.Arrange(new Windows.Foundation.Rect(0, 0, 2, 2));

			Assert.AreEqual(2, sutLayoutUpdatedCount);
			Assert.AreEqual(2, item1LayoutUpdatedCount);
		}

		[TestMethod]
		public void When_MaxWidth_NaN()
		{
			var SUT = new ContentControl
			{
				MaxWidth = double.NaN,
				MaxHeight = double.NaN,
				Content = new Border { Width = 10, Height = 15 }
			};

			var grid = new Grid
			{
				Width = 32,
				Height = 47
			};

			grid.Children.Add(SUT);

			grid.Measure(new Size(1000, 1000));
			grid.Arrange(new Rect(default(Point), grid.DesiredSize));

			Assert.AreEqual(32d, grid.ActualWidth);
			Assert.AreEqual(47d, grid.ActualHeight);
		}
	}
}
