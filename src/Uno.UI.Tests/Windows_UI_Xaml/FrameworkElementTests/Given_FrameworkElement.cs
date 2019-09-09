using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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

			SUT.LayoutUpdated += delegate {
				sutLayoutUpdatedCount ++;
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
	}
}
