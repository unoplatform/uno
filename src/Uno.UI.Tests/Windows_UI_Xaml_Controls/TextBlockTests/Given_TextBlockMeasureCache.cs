using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Tests.Windows_UI_Xaml_Controls.TextBlockTests
{
	[TestClass]
	public class Given_TextBlockMeasureCache
	{
		[TestMethod]
		public void When_DefaultTextBlock()
		{
			var SUT = new TextBlockMeasureCache();

			var tb = new TextBlock() { Text = "42" };

			Assert.AreEqual(TextWrapping.NoWrap, tb.TextWrapping);

			SUT.CacheMeasure(tb, new Size(100, 100), new Size(20, 10));

			Assert.AreEqual(
				new Size(20, 10),
				SUT.FindMeasuredSize(tb, new Size(100, 100)).Value
			);

			Assert.AreEqual(
				new Size(20, 10),
				SUT.FindMeasuredSize(tb, new Size(50, 100)).Value
			);

			Assert.IsNull(
				SUT.FindMeasuredSize(tb, new Size(10, 100))
			);
		}

		[TestMethod]
		public void When_DefaultTextBlock_Wrap()
		{
			var SUT = new TextBlockMeasureCache();

			var tb = new TextBlock() { Text = "42", TextWrapping = TextWrapping.Wrap };

			SUT.CacheMeasure(tb, new Size(50, 100), new Size(50, 70));

			Assert.AreEqual(
				new Size(50, 70),
				SUT.FindMeasuredSize(tb, new Size(50, 100)).Value
			);

			Assert.AreEqual(
				new Size(50, 70),
				SUT.FindMeasuredSize(tb, new Size(50, 70)).Value
			);

			Assert.IsNull(
				SUT.FindMeasuredSize(tb, new Size(50, 69))
			);

			Assert.IsNull(
				SUT.FindMeasuredSize(tb, new Size(49, 100))
			);

			Assert.IsNull(
				SUT.FindMeasuredSize(tb, new Size(50.1, 100))
			);
		}
	}
}
