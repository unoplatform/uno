using System;
using System.Linq;
using AwesomeAssertions.Execution;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Windows.Foundation;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	public partial class Given_StackPanel
	{
		// Tests in this file were migrated from the legacy Uno.UI.Tests unit-test project,
		// which exercised an in-memory layout mock. They now run against the real Skia
		// layout pipeline by calling Measure/Arrange directly on detached elements,
		// matching the pattern used by Given_GridLayouting.

		private partial class LayoutTestView : FrameworkElement
		{
			public Size? RequestedDesiredSize { get; set; }

			public Func<Size, Size> DesiredSizeSelector { get; set; }

			public Size SizePassedToMeasureOverride { get; private set; }

			public Size SizePassedToArrangeOverride { get; private set; }

			protected override Size MeasureOverride(Size availableSize)
			{
				SizePassedToMeasureOverride = availableSize;

				if (DesiredSizeSelector != null)
				{
					return DesiredSizeSelector(availableSize);
				}

				if (RequestedDesiredSize.HasValue)
				{
					return RequestedDesiredSize.Value;
				}

				return base.MeasureOverride(availableSize);
			}

			protected override Size ArrangeOverride(Size finalSize)
			{
				SizePassedToArrangeOverride = finalSize;
				return base.ArrangeOverride(finalSize);
			}
		}

		private partial class MyStackPanel
		{
			public Size ArrangeOverridePublic(Size finalSize) => ArrangeOverride(finalSize);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_Vertical_And_SimpleLayout()
		{
			var SUT = new StackPanel { Name = "test", Orientation = Orientation.Vertical };

			var c1 = new LayoutTestView { Name = "Child01", RequestedDesiredSize = new Size(10, 8) };
			var c2 = new LayoutTestView { Name = "Child02", RequestedDesiredSize = new Size(10, 7) };
			SUT.Children.Add(c1);
			SUT.Children.Add(c2);

			SUT.Measure(new Size(20, 20));
			SUT.DesiredSize.Should().Be(new Size(10, 15), "measuredSize");

			SUT.Arrange(new Rect(0, 0, 20, 20));
			LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 20, 8));
			LayoutInformation.GetLayoutSlot(c2).Should().Be(new Rect(0, 8, 20, 7));
			SUT.Children.Count.Should().Be(2);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_Horizontal_And_SimpleLayout()
		{
			var SUT = new StackPanel { Name = "test", Orientation = Orientation.Horizontal };

			var c1 = new LayoutTestView { Name = "Child01", RequestedDesiredSize = new Size(5, 8) };
			var c2 = new LayoutTestView { Name = "Child02", RequestedDesiredSize = new Size(12, 7) };
			SUT.Children.Add(c1);
			SUT.Children.Add(c2);

			SUT.Measure(new Size(20, 20));
			SUT.DesiredSize.Should().Be(new Size(17, 8), "measuredSize");

			SUT.Arrange(new Rect(0, 0, 20, 20));
			LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 5, 20));
			LayoutInformation.GetLayoutSlot(c2).Should().Be(new Rect(5, 0, 12, 20));
			SUT.Children.Count.Should().Be(2);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_Vertical_And_SimpleLayout_With_Spacing()
		{
			var SUT = new StackPanel { Name = "test", Orientation = Orientation.Vertical, Spacing = 5 };

			var c1 = new LayoutTestView { Name = "Child01", RequestedDesiredSize = new Size(10, 8) };
			var c2 = new LayoutTestView { Name = "Child02", RequestedDesiredSize = new Size(10, 7) };
			SUT.Children.Add(c1);
			SUT.Children.Add(c2);

			SUT.Measure(new Size(20, 20));
			SUT.DesiredSize.Should().Be(new Size(10, 20), "measuredSize");

			SUT.Arrange(new Rect(0, 0, 20, 20));
			LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 20, 8));
			LayoutInformation.GetLayoutSlot(c2).Should().Be(new Rect(0, 13, 20, 7));
			SUT.Children.Count.Should().Be(2);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_Vertical_And_Three_With_Spacing()
		{
			var SUT = new StackPanel { Name = "test", Orientation = Orientation.Vertical, Spacing = 5 };

			var c1 = new LayoutTestView { Name = "Child01", RequestedDesiredSize = new Size(10, 8) };
			var c2 = new LayoutTestView { Name = "Child02", RequestedDesiredSize = new Size(10, 7) };
			var c3 = new LayoutTestView { Name = "Child03", RequestedDesiredSize = new Size(10, 11) };
			SUT.Children.Add(c1);
			SUT.Children.Add(c2);
			SUT.Children.Add(c3);

			SUT.Measure(new Size(20, 20));
			SUT.DesiredSize.Should().Be(new Size(10, 20));
			SUT.UnclippedDesiredSize.Should().Be(new Size(10, 36));

			SUT.Arrange(new Rect(0, 0, 20, 20));
			LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 20, 8));
			LayoutInformation.GetLayoutSlot(c2).Should().Be(new Rect(0, 13, 20, 7));
			LayoutInformation.GetLayoutSlot(c3).Should().Be(new Rect(0, 25, 20, 11));
			SUT.Children.Count.Should().Be(3);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_Horizontal_And_SimpleLayout_With_Spacing()
		{
			var SUT = new StackPanel { Name = "test", Orientation = Orientation.Horizontal, Spacing = 5 };

			var c1 = new LayoutTestView { Name = "Child01", RequestedDesiredSize = new Size(5, 8) };
			var c2 = new LayoutTestView { Name = "Child02", RequestedDesiredSize = new Size(12, 7) };
			SUT.Children.Add(c1);
			SUT.Children.Add(c2);

			SUT.Measure(new Size(20, 20));
			SUT.DesiredSize.Should().Be(new Size(20, 8));
			SUT.UnclippedDesiredSize.Should().Be(new Size(22, 8));

			SUT.Arrange(new Rect(0, 0, 20, 20));
			LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 5, 20));
			LayoutInformation.GetLayoutSlot(c2).Should().Be(new Rect(10, 0, 12, 20));
			SUT.Children.Count.Should().Be(2);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_Horizontal_And_Three_With_Spacing()
		{
			var SUT = new StackPanel { Name = "test", Orientation = Orientation.Horizontal, Spacing = 5 };

			var c1 = new LayoutTestView { Name = "Child01", RequestedDesiredSize = new Size(5, 8) };
			var c2 = new LayoutTestView { Name = "Child02", RequestedDesiredSize = new Size(12, 7) };
			var c3 = new LayoutTestView { Name = "Child03", RequestedDesiredSize = new Size(12, 5) };
			SUT.Children.Add(c1);
			SUT.Children.Add(c2);
			SUT.Children.Add(c3);

			SUT.Measure(new Size(20, 20));
			SUT.DesiredSize.Should().Be(new Size(20, 8));
			SUT.UnclippedDesiredSize.Should().Be(new Size(39, 8));

			SUT.Arrange(new Rect(0, 0, 20, 20));
			LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 5, 20));
			LayoutInformation.GetLayoutSlot(c2).Should().Be(new Rect(10, 0, 12, 20));
			LayoutInformation.GetLayoutSlot(c3).Should().Be(new Rect(27, 0, 12, 20));
			SUT.Children.Count.Should().Be(3);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_Vertical_And_ArrangeIsBiggerThanMeasure()
		{
			var SUT = new StackPanel { Name = "test", Orientation = Orientation.Vertical };

			var c1 = new LayoutTestView { Name = "Child01", RequestedDesiredSize = new Size(10, 10) };
			SUT.Children.Add(c1);

			SUT.Measure(new Size(20, 20));
			SUT.DesiredSize.Should().Be(new Size(10, 10), "measuredSize");

			SUT.Arrange(new Rect(0, 0, 20, 20));
			LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 20, 10));
			SUT.Children.Count.Should().Be(1);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_Horizontal_And_ArrangeIsBiggerThanMeasure()
		{
			var SUT = new StackPanel { Name = "test", Orientation = Orientation.Horizontal };

			var c1 = new LayoutTestView { Name = "Child01", RequestedDesiredSize = new Size(10, 10) };
			SUT.Children.Add(c1);

			SUT.Measure(new Size(20, 20));
			SUT.DesiredSize.Should().Be(new Size(10, 10), "measuredSize");

			SUT.Arrange(new Rect(0, 0, 20, 20));
			LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 10, 20));
			SUT.Children.Count.Should().Be(1);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_Vertical_And_Fixed_Height_Item_With_Margin()
		{
			var SUT = new StackPanel { Name = "test", Orientation = Orientation.Vertical };

			var c1 = new LayoutTestView { Name = "Child01", RequestedDesiredSize = new Size(10, 10) };
			var c2 = new Border { Name = "Child02", Height = 10, Margin = new Thickness(10) };
			SUT.Children.Add(c1);
			SUT.Children.Add(c2);

			SUT.Measure(new Size(40, double.PositiveInfinity));
			using (new AssertionScope("Desired Sizes"))
			{
				SUT.DesiredSize.Should().Be(new Size(20, 40));
				c1.DesiredSize.Should().Be(new Size(10, 10));
				c2.DesiredSize.Should().Be(new Size(20, 30));
			}

			SUT.Arrange(new Rect(0, 0, 30, 40));
			using (new AssertionScope("Arranged Sizes"))
			{
				LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 30, 10));
				LayoutInformation.GetLayoutSlot(c2).Should().Be(new Rect(10, 20, 10, 10));
				SUT.Children.Count.Should().Be(2);
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_Vertical_And_Fixed_Height_And_Width_Item_With_Margin()
		{
			var SUT = new StackPanel { Name = "test", Orientation = Orientation.Vertical };

			var c1 = new LayoutTestView
			{
				Name = "Child02",
				DesiredSizeSelector = s =>
				{
					s.Width.Should().Be(30.0d);
					s.Height.Should().Be(double.PositiveInfinity);
					return new Size(10, 10);
				},
				Height = 10,
				Margin = new Thickness(10),
			};
			SUT.Children.Add(c1);

			SUT.Measure(new Size(30, 30));
			SUT.DesiredSize.Should().Be(new Size(10, 10));
			c1.DesiredSize.Should().Be(new Size(10, 10));

			SUT.Arrange(new Rect(0, 0, 30, 30));
			// size is 10x0 because of margins (w = 30-(10+10), h = 10-(10+10))
			LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(10, 10, 10, 0));
			SUT.Children.Count.Should().Be(1);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_Vertical_And_Fixed_Width_Item()
		{
			var SUT = new StackPanel { Name = "test", Orientation = Orientation.Vertical };

			var c1 = new LayoutTestView { Name = "Child01", RequestedDesiredSize = new Size(10, 8), Width = 10 };
			SUT.Children.Add(c1);

			SUT.Measure(new Size(20, 20));
			SUT.DesiredSize.Should().Be(new Size(10, 8), "measuredSize");
			c1.DesiredSize.Should().Be(new Size(10, 8));

			SUT.Arrange(new Rect(0, 0, 20, 20));
			LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(5, 0, 10, 8));
			SUT.Children.Count.Should().Be(1);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_Vertical_And_Fixed_MaxWidth_UnderSized()
		{
			var SUT = new StackPanel { Name = "test", Orientation = Orientation.Vertical };

			var c1 = new LayoutTestView { Name = "Child01", RequestedDesiredSize = new Size(10, 8), MaxWidth = 20 };
			SUT.Children.Add(c1);

			SUT.Measure(new Size(10, 20));
			SUT.DesiredSize.Should().Be(new Size(10, 8), "measuredSize");
			c1.DesiredSize.Should().Be(new Size(10, 8));

			SUT.Arrange(new Rect(0, 0, 30, 20));
			LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(5, 0, 20, 8));
			SUT.Children.Count.Should().Be(1);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_Vertical_And_Fixed_MaxWidth_Oversized()
		{
			var SUT = new StackPanel { Name = "test", Orientation = Orientation.Vertical };

			var c1 = new LayoutTestView { Name = "Child01", RequestedDesiredSize = new Size(10, 8), MaxWidth = 20 };
			SUT.Children.Add(c1);

			SUT.Measure(new Size(25, 20));
			SUT.DesiredSize.Should().Be(new Size(10, 8), "measuredSize");
			c1.SizePassedToMeasureOverride.Should().Be(new Size(25, double.PositiveInfinity), "AvailableMeasureSize");
			c1.DesiredSize.Should().Be(new Size(10, 8));

			SUT.Arrange(new Rect(0, 0, 30, 20));
			LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(5, 0, 20, 8));
			SUT.Children.Count.Should().Be(1);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_Horizontal_And_Fixed_Height_Item()
		{
			var SUT = new StackPanel { Name = "test", Orientation = Orientation.Horizontal };

			var c1 = new LayoutTestView { Name = "Child01", RequestedDesiredSize = new Size(8, 10), Height = 10 };
			SUT.Children.Add(c1);

			SUT.Measure(new Size(20, 20));
			SUT.DesiredSize.Should().Be(new Size(8, 10), "measuredSize");
			c1.DesiredSize.Should().Be(new Size(8, 10));

			SUT.Arrange(new Rect(0, 0, 20, 20));
			LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 5, 8, 10));
			SUT.Children.Count.Should().Be(1);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_Horizontal_And_Fixed_Width_Item_And_Measured_Height_is_Valid()
		{
			var SUT = new StackPanel { Name = "test", Orientation = Orientation.Horizontal };

			var c1 = new LayoutTestView
			{
				Name = "Child01",
				DesiredSizeSelector = s =>
				{
					s.Height.Should().Be(20.0d);
					return new Size(8, 10);
				},
				Height = 10,
			};
			SUT.Children.Add(c1);

			SUT.Measure(new Size(20, 20));
			SUT.DesiredSize.Should().Be(new Size(8, 10), "measuredSize");
			c1.DesiredSize.Should().Be(new Size(8, 10));

			SUT.Arrange(new Rect(0, 0, 20, 20));
			LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 5, 8, 10));
			SUT.Children.Count.Should().Be(1);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_Vertical_And_Fixed_Height_Item_ArrangeOverride()
		{
			var SUT = new MyStackPanel();
			SUT.Children.Add(new Border { Height = 47, Width = 112 });

			SUT.Measure(new Size(1000, 1000));
			var size1 = new Size(1000, 1000);
			SUT.ArrangeOverridePublic(size1).Should().Be(size1);

			SUT.Measure(new Size(2000, 2000));
			var size2 = new Size(2000, 2000);
			SUT.ArrangeOverridePublic(size2).Should().Be(size2);
		}
	}
}
