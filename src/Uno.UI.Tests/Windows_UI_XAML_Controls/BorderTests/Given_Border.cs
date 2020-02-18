using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno;
using Uno.Extensions;
using Uno.Logging;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Disposables;
using System.Text;
using System.Threading.Tasks;
using View = Windows.UI.Xaml.FrameworkElement;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.Foundation;

namespace Uno.UI.Tests.BorderTests
{
	[TestClass]
	public class Given_Border : Context
	{
		[TestMethod]
		public void When_Border_Has_Fixed_Size()
		{
			var fixedSize = new Windows.Foundation.Size(100, 100);

			var SUT = new Border()
			{
				Width = fixedSize.Width,
				Height = fixedSize.Height
			};

			SUT.Measure(new Windows.Foundation.Size(500, 500));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(fixedSize, measuredSize);
		}

        [TestMethod]
        public void When_Border_Has_Margin()
        {
            var fixedSize = new Windows.Foundation.Size(100, 120);
            var margin = new Thickness(10,20,30,40);
            var totalSize = new Windows.Foundation.Size(fixedSize.Width + margin.Left + margin.Right, fixedSize.Height + margin.Top + margin.Bottom);

            var SUT = new Border()
            {
                Width = fixedSize.Width,
                Height = fixedSize.Height,
                Margin = margin,
            };

            SUT.Measure(new Windows.Foundation.Size(500, 500));
            var measuredSize = SUT.DesiredSize;
            Assert.AreEqual(totalSize, measuredSize);
        }

		[TestMethod]
		public void When_Child_Has_Fixed_Size_Smaller_than_Parent()
		{
			var parentSize = new Windows.Foundation.Size(100, 100);
			var childSize = new Windows.Foundation.Size(500, 500);

			var SUT = new Border()
			{
				Width = parentSize.Width,
				Height = parentSize.Height
			};

			var child = new View()
			{
				Width = childSize.Width,
				Height = childSize.Height
			};

			SUT.Child = child;

			SUT.Measure(new Windows.Foundation.Size(100, 100));
			var measuredSize = SUT.DesiredSize;
			SUT.Arrange(new Windows.Foundation.Rect(0, 0,100, 100));

			Assert.AreEqual(parentSize, measuredSize, $"(parentSize:{parentSize}) != (measuredSize:{measuredSize})");
			var expectedArrange = new Windows.Foundation.Rect(0, 0, parentSize.Width, parentSize.Height);
			var layoutSlot = LayoutInformation.GetLayoutSlot(child);
			Assert.AreEqual(expectedArrange, layoutSlot, $"(expectedArrange:{expectedArrange}) != (layoutSlot: {layoutSlot})");
		}

		[TestMethod]
		public void When_Child_Is_Stretch()
		{
			var parentSize = new Windows.Foundation.Size(500, 500);

			var SUT = new Border()
			{
				Width = parentSize.Width,
				Height = parentSize.Height
			};

			var child = new View();

			SUT.Child = child;

			SUT.Measure(new Windows.Foundation.Size(500, 500));
			var measuredSize = SUT.DesiredSize;
			SUT.Arrange(new Windows.Foundation.Rect(0, 0,500, 500));

			Assert.AreEqual(parentSize, measuredSize);
			Assert.AreEqual(new Windows.Foundation.Rect(0, 0, parentSize.Width, parentSize.Height), LayoutInformation.GetLayoutSlot(child));
		}

		[TestMethod]
#if NET461
		[Ignore("Layout engine is incomplete on net461 for arrange")]
#endif
		public void When_Top_Align_Nested_With_Margin()
		{
			var border1 = new Border()
			{
				Height = 34,
			};

			var border2 = new Border()
			{
				VerticalAlignment = VerticalAlignment.Top,
				Margin = new Thickness(15, 5, 0, 7)
			};

			var border3 = new Border()
			{
				Width = 50,
				Height = 30,
			};

			border1.Child = border2;
			border2.Child = border3;

			border1.Measure(new Windows.Foundation.Size(500, 500));
			var measuredSize = border1.DesiredSize;
			border1.Arrange(new Rect(0, 0, measuredSize.Width, measuredSize.Height));

			var slot1 = LayoutInformation.GetLayoutSlot(border1);
			var slot2 = LayoutInformation.GetLayoutSlot(border2);
			var slot3 = LayoutInformation.GetLayoutSlot(border3);

			Assert.AreEqual(new Rect(0, 0, 65, 34), slot1, "slot1");
			Assert.AreEqual(new Rect(0, 0, 65, 34), slot2, "slot2");
			Assert.AreEqual(new Rect(0, 0, 50, 22), slot3, "slot3");
		}

		[TestMethod]
		public void When_Child_Is_Not_Stretch()
		{
			var parentSize = new Windows.Foundation.Size(500, 500);

			var SUT = new Border()
			{
				Width = parentSize.Width,
				Height = parentSize.Height
			};

			var child = new Border()
			{
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top
			};

			SUT.Child = child;

			SUT.Measure(new Windows.Foundation.Size(500, 500));
			var measuredSize = SUT.DesiredSize;
			SUT.Arrange(new Windows.Foundation.Rect(0, 0, 500, 500));

			Assert.AreEqual(parentSize, measuredSize);
			Assert.AreEqual(new Size(0, 0), new Size(child.LayoutSlotWithMarginsAndAlignments.Width, child.LayoutSlotWithMarginsAndAlignments.Height));
		}

		[TestMethod]
		public void When_Child_Is_Stretch_With_MaxSize()
		{
			var parentSize = new Windows.Foundation.Size(500, 500);
			var maxSize = new Windows.Foundation.Size(100, 100);

			var SUT = new Border()
			{
				Width = parentSize.Width,
				Height = parentSize.Height
			};

			var child = new Border()
			{
				MaxWidth = maxSize.Width,
				MaxHeight = maxSize.Height
			};

			SUT.Child = child;

			SUT.Measure(new Windows.Foundation.Size(500, 500));
			var measuredSize = SUT.DesiredSize;
			SUT.Arrange(new Windows.Foundation.Rect(0, 0,500, 500));

			Assert.AreEqual(parentSize, measuredSize);
			Assert.AreEqual(new Windows.Foundation.Rect((SUT.Width - maxSize.Width) / 2, (SUT.Height - maxSize.Height) / 2, maxSize.Width, maxSize.Height), child.LayoutSlotWithMarginsAndAlignments);
		}

		[TestMethod]
		public void When_Child_Is_Not_Stretch_With_MinSize()
		{
			var parentSize = new Windows.Foundation.Size(500, 500);
			var minSize = new Windows.Foundation.Size(100, 100);

			var SUT = new Border()
			{
				Width = parentSize.Width,
				Height = parentSize.Height
			};

			var child = new View()
			{
				MinWidth = minSize.Width,
				MinHeight = minSize.Height,
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top
			};

			SUT.Child = child;

			SUT.Measure(new Windows.Foundation.Size(500, 500));
			var measuredSize = SUT.DesiredSize;
			SUT.Arrange(new Windows.Foundation.Rect(0, 0,500, 500));

			Assert.AreEqual(parentSize, measuredSize);
			Assert.AreEqual(minSize, new Size(child.LayoutSlotWithMarginsAndAlignments.Width, child.LayoutSlotWithMarginsAndAlignments.Height));
		}

		[TestMethod]
		public void When_Child_Is_Stretch_With_MaxSize_And_MinSize()
		{
			var parentSize = new Windows.Foundation.Size(500, 500);
			var maxSize = new Windows.Foundation.Size(100, 100);
			var minSize = new Windows.Foundation.Size(50, 50);

			var SUT = new Border()
			{
				Width = parentSize.Width,
				Height = parentSize.Height
			};

			var child = new Border()
			{
				MaxWidth = maxSize.Width,
				MaxHeight = maxSize.Height,
				MinWidth = minSize.Width,
				MinHeight = minSize.Height
			};

			SUT.Child = child;

			SUT.Measure(new Windows.Foundation.Size(500, 500));
			var measuredSize = SUT.DesiredSize;
			SUT.Arrange(new Windows.Foundation.Rect(0, 0,500, 500));

			Assert.AreEqual(parentSize, measuredSize, "parentSize != measuredSize");
			Assert.AreEqual(new Windows.Foundation.Rect((SUT.Width - maxSize.Width) / 2, (SUT.Height - maxSize.Height) / 2, maxSize.Width, maxSize.Height), child.LayoutSlotWithMarginsAndAlignments);

			child.HorizontalAlignment = HorizontalAlignment.Left;
			child.VerticalAlignment = VerticalAlignment.Top;

			SUT.Measure(new Windows.Foundation.Size(500, 500));
			measuredSize = SUT.DesiredSize;
			SUT.Arrange(new Windows.Foundation.Rect(0, 0,500, 500));

			Assert.AreEqual(parentSize, measuredSize);
			Assert.AreEqual(minSize, new Size(child.LayoutSlotWithMarginsAndAlignments.Width, child.LayoutSlotWithMarginsAndAlignments.Height));
		}
	}
}
