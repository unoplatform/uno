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
		private int _originalMaxMeasureKeyEntries;
		private int _originalMaxMeasureSizeKeyEntries;

		[TestInitialize]
		public void TestInitialize()
		{
			_originalMaxMeasureKeyEntries = TextBlockMeasureCache.MaxMeasureKeyEntries;
			_originalMaxMeasureSizeKeyEntries = TextBlockMeasureCache.MaxMeasureSizeKeyEntries;
		}

		[TestCleanup]
		public void TestCleanup()
		{
			TextBlockMeasureCache.MaxMeasureKeyEntries = _originalMaxMeasureKeyEntries;
			TextBlockMeasureCache.MaxMeasureSizeKeyEntries = _originalMaxMeasureSizeKeyEntries;
		}

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

		[TestMethod]
		public void When_MaxMeasureSizeKeyEntries_Reached()
		{
			var SUT = new TextBlockMeasureCache();

			TextBlockMeasureCache.MaxMeasureKeyEntries = 2;
			TextBlockMeasureCache.MaxMeasureSizeKeyEntries = 2;

			var tb = new TextBlock() { Text = "42", TextWrapping = TextWrapping.Wrap };

			SUT.CacheMeasure(tb, new Size(11, 11), new Size(1, 1));
			Assert.AreEqual(
				new Size(1, 1),
				SUT.FindMeasuredSize(tb, new Size(11, 11)).Value
			);

			SUT.CacheMeasure(tb, new Size(22, 22), new Size(2, 2));
			Assert.AreEqual(
				new Size(1, 1),
				SUT.FindMeasuredSize(tb, new Size(11, 11)).Value
			);
			Assert.AreEqual(
				new Size(2, 2),
				SUT.FindMeasuredSize(tb, new Size(22, 22)).Value
			);

			SUT.CacheMeasure(tb, new Size(33, 33), new Size(3, 3));
			Assert.IsNull(
				SUT.FindMeasuredSize(tb, new Size(11, 11))
			);
			Assert.AreEqual(
				new Size(2, 2),
				SUT.FindMeasuredSize(tb, new Size(22, 22)).Value
			);
			Assert.AreEqual(
				new Size(3, 3),
				SUT.FindMeasuredSize(tb, new Size(33, 33)).Value
			);
		}

		[TestMethod]
		public void When_MaxMeasureKeyEntries_Reached()
		{
			var SUT = new TextBlockMeasureCache();

			TextBlockMeasureCache.MaxMeasureKeyEntries = 2;
			TextBlockMeasureCache.MaxMeasureSizeKeyEntries = 2;

			var tb1 = new TextBlock() { Text = "42", TextWrapping = TextWrapping.Wrap };

			SUT.CacheMeasure(tb1, new Size(11, 11), new Size(1, 1));
			Assert.AreEqual(
				new Size(1, 1),
				SUT.FindMeasuredSize(tb1, new Size(11, 11)).Value
			);

			var tb2 = new TextBlock() { Text = "43", TextWrapping = TextWrapping.Wrap };

			SUT.CacheMeasure(tb2, new Size(11, 11), new Size(1, 1));
			Assert.AreEqual(
				new Size(1, 1),
				SUT.FindMeasuredSize(tb2, new Size(11, 11)).Value
			);
			Assert.AreEqual(
				new Size(1, 1),
				SUT.FindMeasuredSize(tb1, new Size(11, 11)).Value
			);

			var tb3 = new TextBlock() { Text = "44", TextWrapping = TextWrapping.Wrap };

			SUT.CacheMeasure(tb3, new Size(11, 11), new Size(1, 1));
			Assert.AreEqual(
				new Size(1, 1),
				SUT.FindMeasuredSize(tb3, new Size(11, 11)).Value
			);
			Assert.AreEqual(
				new Size(1, 1),
				SUT.FindMeasuredSize(tb2, new Size(11, 11)).Value
			);
			Assert.IsNull(
				SUT.FindMeasuredSize(tb1, new Size(11, 11))
			);
		}

		[TestMethod]
		public void When_SmallerAvailableSize()
		{
			var SUT = new TextBlockMeasureCache();

			var tb = new TextBlock() { Text = "42" };

			Assert.AreEqual(TextWrapping.NoWrap, tb.TextWrapping);

			SUT.CacheMeasure(tb, new Size(0, 0), new Size(0, 0));

			Assert.IsNull(
				SUT.FindMeasuredSize(tb, new Size(50, 50))
			);

			SUT.CacheMeasure(tb, new Size(100, 100), new Size(50, 10));

			Assert.AreEqual(
				new Size(50, 10),
				SUT.FindMeasuredSize(tb, new Size(100, 100)).Value
			);

			Assert.AreEqual(
				new Size(0, 0),
				SUT.FindMeasuredSize(tb, new Size(0, 0)).Value
			);
		}
	}
}
