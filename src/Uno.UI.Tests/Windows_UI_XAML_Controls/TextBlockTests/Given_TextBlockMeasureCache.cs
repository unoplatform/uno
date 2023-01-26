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

			var tb = new TextBlock { Text = "42" }; // Used as key, never measured

			Assert.AreEqual(TextWrapping.NoWrap, tb.TextWrapping);

			SUT.CacheMeasure(tb, new Size(5, 25), new Size(5, 25));
			SUT.CacheMeasure(tb, new Size(12, 20), new Size(12, 15));
			SUT.CacheMeasure(tb, new Size(100, 100), new Size(20, 10));

			var size20x10 = new Size(20, 10);

			var resultFor100x100 = SUT.FindMeasuredSize(tb, new Size(100, 100));
			Assert.AreEqual(size20x10, resultFor100x100);

			var resultFor50x100 = SUT.FindMeasuredSize(tb, new Size(50, 100));
			Assert.AreEqual(size20x10, resultFor50x100);

			var resultFor20x10 = SUT.FindMeasuredSize(tb, size20x10);
			Assert.AreEqual(size20x10, resultFor20x10);

			var resultFor10x100 = SUT.FindMeasuredSize(tb, new Size(10, 100));
			Assert.AreEqual(size20x10, resultFor10x100);

			var resultFor5x20 = SUT.FindMeasuredSize(tb, new Size(5, 20));
			Assert.AreEqual(new Size(5, 25), resultFor5x20);

			var resultFor5x35 = SUT.FindMeasuredSize(tb, new Size(5, 35));
			Assert.AreEqual(resultFor10x100, resultFor5x35);
		}

		[TestMethod]
		[DataRow(TextTrimming.Clip, DisplayName = "TextTrimming.Clip")]
		[DataRow(TextTrimming.CharacterEllipsis, DisplayName = "TextTrimming.CharacterEllipsis")]
		[DataRow(TextTrimming.WordEllipsis, DisplayName = "TextTrimming.WordEllipsis")]
		public void When_DefaultTextBlock_Clip(TextTrimming trimmingMode)
		{
			var SUT = new TextBlockMeasureCache();

			var tb = new TextBlock { Text = "42", TextTrimming = trimmingMode }; // Used as key, never measured

			SUT.CacheMeasure(tb, new Size(200, 100), new Size(125, 25));
			SUT.CacheMeasure(tb, new Size(100, 100), new Size(100, 50));
			SUT.CacheMeasure(tb, new Size(75, 100), new Size(75, 100));
			SUT.CacheMeasure(tb, new Size(50, 100), new Size(50, 100));

			Assert.AreEqual(
				new Size(125, 25),
				SUT.FindMeasuredSize(tb, new Size(125, 100))
			);

			Assert.AreEqual(
				new Size(50, 100),
				SUT.FindMeasuredSize(tb, new Size(50, 70))
			);

			Assert.AreEqual(
				null,
				SUT.FindMeasuredSize(tb, new Size(52, 70))
			);

			Assert.AreEqual(
				null,
				SUT.FindMeasuredSize(tb, new Size(500, 500))
			);
		}

		[TestMethod]
		[DataRow(TextWrapping.Wrap, DisplayName = "TextWrapping.Wrap")]
		[DataRow(TextWrapping.WrapWholeWords, DisplayName = "TextWrapping.WrapWholeWords")]
		public void When_DefaultTextBlock_Wrap(TextWrapping wrappingMode)
		{
			var SUT = new TextBlockMeasureCache();

			var tb = new TextBlock { Text = "42", TextWrapping = wrappingMode }; // Used as key, never measured

			SUT.CacheMeasure(tb, new Size(200, 100), new Size(125, 25));
			SUT.CacheMeasure(tb, new Size(100, 100), new Size(100, 50));
			SUT.CacheMeasure(tb, new Size(75, 100), new Size(75, 100));
			SUT.CacheMeasure(tb, new Size(50, 100), new Size(50, 100));

			Assert.AreEqual(
				new Size(125, 25),
				SUT.FindMeasuredSize(tb, new Size(125, 100))
			);

			Assert.AreEqual(
				new Size(50, 100),
				SUT.FindMeasuredSize(tb, new Size(50, 70))
			);

			Assert.AreEqual(
				null,
				SUT.FindMeasuredSize(tb, new Size(52, 70))
			);

			Assert.AreEqual(
				null,
				SUT.FindMeasuredSize(tb, new Size(500, 500))
			);
		}

		[TestMethod]
		public void When_MaxMeasureSizeKeyEntries_Reached()
		{
			var SUT = new TextBlockMeasureCache();

			TextBlockMeasureCache.MaxMeasureKeyEntries = 2;
			TextBlockMeasureCache.MaxMeasureSizeKeyEntries = 2;

			var tb = new TextBlock { Text = "42", TextWrapping = TextWrapping.Wrap }; // Used as key, never measured

			SUT.CacheMeasure(tb, new Size(11, 11), new Size(11, 11));
			Assert.AreEqual(
				new Size(11, 11),
				SUT.FindMeasuredSize(tb, new Size(11, 11))
			);

			SUT.CacheMeasure(tb, new Size(22, 22), new Size(22, 22));
			Assert.AreEqual(
				new Size(11, 11),
				SUT.FindMeasuredSize(tb, new Size(11, 11))
			);
			Assert.AreEqual(
				new Size(22, 22),
				SUT.FindMeasuredSize(tb, new Size(22, 22))
			);

			SUT.CacheMeasure(tb, new Size(33, 33), new Size(33, 33));
			Assert.IsNull(
				SUT.FindMeasuredSize(tb, new Size(11, 11))
			);
			Assert.AreEqual(
				new Size(22, 22),
				SUT.FindMeasuredSize(tb, new Size(22, 22))
			);
			Assert.AreEqual(
				new Size(33, 33),
				SUT.FindMeasuredSize(tb, new Size(33, 33))
			);
		}

		[TestMethod]
		public void When_MaxMeasureKeyEntries_Reached()
		{
			var SUT = new TextBlockMeasureCache();

			TextBlockMeasureCache.MaxMeasureKeyEntries = 2;
			TextBlockMeasureCache.MaxMeasureSizeKeyEntries = 2;

			var tb1 = new TextBlock { Text = "42", TextWrapping = TextWrapping.Wrap }; // Used as key, never measured

			SUT.CacheMeasure(tb1, new Size(11, 11), new Size(1, 1));
			Assert.AreEqual(
				new Size(1, 1),
				SUT.FindMeasuredSize(tb1, new Size(11, 11))
			);

			var tb2 = new TextBlock { Text = "43", TextWrapping = TextWrapping.Wrap }; // Used as key, never measured

			SUT.CacheMeasure(tb2, new Size(11, 11), new Size(1, 1));
			Assert.AreEqual(
				new Size(1, 1),
				SUT.FindMeasuredSize(tb2, new Size(11, 11))
			);
			Assert.AreEqual(
				new Size(1, 1),
				SUT.FindMeasuredSize(tb1, new Size(11, 11))
			);

			var tb3 = new TextBlock { Text = "44", TextWrapping = TextWrapping.Wrap }; // Used as key, never measured

			SUT.CacheMeasure(tb3, new Size(11, 11), new Size(1, 1));
			Assert.AreEqual(
				new Size(1, 1),
				SUT.FindMeasuredSize(tb3, new Size(11, 11))
			);
			Assert.AreEqual(
				new Size(1, 1),
				SUT.FindMeasuredSize(tb2, new Size(11, 11))
			);
			Assert.IsNull(
				SUT.FindMeasuredSize(tb1, new Size(11, 11))
			);
		}

		[TestMethod]
		public void When_SmallerAvailableSize()
		{
			var SUT = new TextBlockMeasureCache();
			var tb = new TextBlock { Text = "42" }; // Used as key, never measured

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

		[TestMethod]
		[DataRow(0, .5, 50, 100, 10, .5)]
		[DataRow(0, .49, 50, 100, 10, .49)]
		[DataRow(0.1, .5, 50, 100, 10, .5)]
		[DataRow(0.49, .5, 50, 100, 10, .5)]
		[DataRow(0.5, .5, 50, 100, 10, .5)]
		[DataRow(0.51, .5, 50, 100, 10, .5)]
		[DataRow(1.0, .5, 50, 100, 10, .5)]
		[DataRow(1.0, 1.5, 50, 100, 10, 10)]
		[DataRow(1.5, 1.5, 50, 100, 10, 10)]
		public void When_SameSize(double availableWidth1, double measuredWidth1, double findWidth1, double availableWidth2, double measuredWidth2, double measuredWidth3)
		{
			var SUT = new TextBlockMeasureCache();
			var tb = new TextBlock { Text = "42" };

			Assert.AreEqual(TextWrapping.NoWrap, tb.TextWrapping);

			SUT.CacheMeasure(tb, new Size(availableWidth1, 10), new Size(measuredWidth1, 10));

			Assert.IsNull(
				SUT.FindMeasuredSize(tb, new Size(findWidth1, 10))
			);

			SUT.CacheMeasure(tb, new Size(availableWidth2, 10), new Size(measuredWidth2, 10));

			Assert.AreEqual(
				new Size(measuredWidth2, 10),
				SUT.FindMeasuredSize(tb, new Size(availableWidth2, 10)).Value
			);

			Assert.AreEqual(
				new Size(measuredWidth3, 10),
				SUT.FindMeasuredSize(tb, new Size(0, 0)).Value
			);
		}
	}
}
