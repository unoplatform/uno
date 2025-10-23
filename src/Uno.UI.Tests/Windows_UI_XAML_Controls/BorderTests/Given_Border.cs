#if !WINAPPSDK
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno;
using Uno.Extensions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Disposables;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.Foundation;
using AwesomeAssertions.Execution;

namespace Uno.UI.Tests.BorderTests
{
	[TestClass]
#if !IS_UNIT_TESTS
	[RuntimeTests.RunsOnUIThread]
#endif
	public partial class Given_Border : Context
	{
		private partial class View : FrameworkElement
		{
		}

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
			var margin = new Thickness(10, 20, 30, 40);
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
#if __APPLE_UIKIT__
		[Ignore("Layout engine on ios needs actual layout pass")]
#endif
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
			SUT.Arrange(new Windows.Foundation.Rect(0, 0, 100, 100));

			Assert.AreEqual(parentSize, measuredSize, $"(parentSize:{parentSize}) != (measuredSize:{measuredSize})");
			var expectedArrange = new Windows.Foundation.Rect(0, 0, parentSize.Width, parentSize.Height);
			var layoutSlot = LayoutInformation.GetLayoutSlot(child);
			Assert.AreEqual(expectedArrange, layoutSlot, $"(expectedArrange:{expectedArrange}) != (layoutSlot: {layoutSlot})");
		}

		[TestMethod]
#if __APPLE_UIKIT__
		[Ignore("Layout engine on ios needs actual layout pass")]
#endif
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
			SUT.Arrange(new Windows.Foundation.Rect(0, 0, 500, 500));

			Assert.AreEqual(parentSize, measuredSize);
			Assert.AreEqual(new Windows.Foundation.Rect(0, 0, parentSize.Width, parentSize.Height), LayoutInformation.GetLayoutSlot(child));
		}

		[TestMethod]
#if IS_UNIT_TESTS || __APPLE_UIKIT__ || __ANDROID__ // Broken on Android for now
		[Ignore("Layout engine is incomplete on IS_UNIT_TESTS for arrange, ios & macOS needs actual layout pass")]
#endif
		public void When_Top_Align_Nested_With_Margin()
		{
			var border1 = new Border()
			{
				Height = 34,
				Name = "border1"
			};

			var border2 = new Border()
			{
				VerticalAlignment = VerticalAlignment.Top,
				Margin = new Thickness(15, 5, 0, 7),
				Name = "border2"
			};

			var border3 = new Border()
			{
				Width = 50,
				Height = 30,
				Name = "border3"
			};

			border1.Child = border2;
			border2.Child = border3;

			border1.Measure(new Windows.Foundation.Size(500, 500));
			var measuredSize = border1.DesiredSize;
			border1.Arrange(new Rect(default, measuredSize));

			var slot1 = LayoutInformation.GetLayoutSlot(border1);
			var slot2 = LayoutInformation.GetLayoutSlot(border2);
			var slot3 = LayoutInformation.GetLayoutSlot(border3);

			using (new AssertionScope("Checking layout slots"))
			{
				measuredSize.Should().Be(new Size(65, 34), 0.5, "measuredSize");
				slot1.Should().Be(new Rect(0, 0, 65, 34), 0.5, "slot1");
				slot2.Should().Be(new Rect(0, 0, 65, 34), 0.5, "slot2");
				slot3.Should().Be(new Rect(0, 0, 50, 22), 0.5, "slot3");
			}
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
			SUT.Arrange(new Windows.Foundation.Rect(0, 0, 500, 500));

			Assert.AreEqual(parentSize, measuredSize);
			Assert.AreEqual(new Windows.Foundation.Rect((SUT.Width - maxSize.Width) / 2, (SUT.Height - maxSize.Height) / 2, maxSize.Width, maxSize.Height), child.LayoutSlotWithMarginsAndAlignments);
		}

		[TestMethod]
#if __ANDROID__
		[Ignore] // Broken on Android for now
#endif
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
			SUT.Arrange(new Windows.Foundation.Rect(0, 0, 500, 500));

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
			var measuredSize1 = SUT.DesiredSize;
			SUT.Arrange(new Windows.Foundation.Rect(0, 0, 500, 500));

			child.LayoutSlotWithMarginsAndAlignments.Should().Be(new Rect((SUT.Width - maxSize.Width) / 2, (SUT.Height - maxSize.Height) / 2, maxSize.Width, maxSize.Height), 0.5);

			child.HorizontalAlignment = HorizontalAlignment.Left;
			child.VerticalAlignment = VerticalAlignment.Top;

			SUT.Measure(new Windows.Foundation.Size(500, 500));
			var measuredSize2 = SUT.DesiredSize;
			SUT.Arrange(new Windows.Foundation.Rect(0, 0, 500, 500));

			using (new AssertionScope())
			{
				measuredSize1.Should().Be(parentSize, 0.5, "measuredSize1");
				measuredSize2.Should().Be(parentSize, 0.5, "measuredSize2");
#if !__ANDROID__ // Broken for tests on Android for now
				child.LayoutSlotWithMarginsAndAlignments.Should().HaveSize(minSize, 0.5);
#endif
			}
		}
	}
}
#endif
