using System;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using AwesomeAssertions.Execution;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public partial class Given_GridLayouting
{
	private partial class View : FrameworkElement
	{
		public Size SizePassedToMeasureOverride { get; private set; }
		public Size SizePassedToArrangeOverride { get; private set; }
		public Size? RequestedDesiredSize { get; set; }
		internal Func<Size, Size> DesiredSizeSelector { get; set; }
		internal int MeasureCallCount { get; private set; }
		internal int ArrangeCallCount { get; private set; }

		protected override Size MeasureOverride(Size availableSize)
		{
			MeasureCallCount++;
			SizePassedToMeasureOverride = availableSize;
			if (DesiredSizeSelector != null)
			{
				return DesiredSizeSelector(availableSize);
			}
			else if (RequestedDesiredSize != null)
			{
				return RequestedDesiredSize.Value;
			}

			return base.MeasureOverride(availableSize);
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			ArrangeCallCount++;
			SizePassedToArrangeOverride = finalSize;
			return base.ArrangeOverride(finalSize);
		}
	}

	[TestMethod]
	public void When_Empty_And_MeasuredEmpty()
	{
		using var _ = new AssertionScope();

		var SUT = new Grid { Name = "test" };

		SUT.Measure(default);
		var size = SUT.DesiredSize;
		SUT.Arrange(default);

		size.Should().Be(default);
		SUT.Children.Should().BeEmpty();
	}

	[TestMethod]
	public void When_Empty_And_Measured_Non_Empty()
	{
		using var _ = new AssertionScope();

		var SUT = new Grid { Name = "test" };

		SUT.Measure(new Size(10, 10));
		var size = SUT.DesiredSize;
		SUT.Arrange(default);

		size.Should().Be(default);
		SUT.Children.Should().BeEmpty();
	}

	[TestMethod]
#if __APPLE_UIKIT__
	[Ignore("Fails on iOS.")]
#endif
	public void When_Grid_Has_One_Element()
	{
		using var _ = new AssertionScope();

		var SUT = new Grid { Name = "test" };

		SUT.Children.Add(new View { Name = "Child01", RequestedDesiredSize = new Size(10, 10) });

		SUT.Measure(new Size(20, 20));
		var measuredSize = SUT.DesiredSize;
		SUT.Arrange(new Rect(0, 0, 20, 20));


		var firstChild = (View)SUT.Children.First();
		firstChild.SizePassedToArrangeOverride.Should().Be(new Size(20, 20));
		LayoutInformation.GetLayoutSlot(firstChild).Should().Be(new Rect(0, 0, 20, 20));

		measuredSize.Should().Be(new Size(10, 10));
		SUT.Children.Should().HaveCount(1);
	}

	[TestMethod]
#if __APPLE_UIKIT__
	[Ignore("Fails on iOS.")]
#endif
	public void When_Grid_Has_One_Element_And_MinWidth_MinHeight_VerticalAlignment_Top_HorizontalAlignment_Center()
	{
		using var _ = new AssertionScope();

		var SUT = new Grid { Name = "test" };

		SUT.MinWidth = 40;
		SUT.MinHeight = 40;
		SUT.VerticalAlignment = VerticalAlignment.Top;
		SUT.HorizontalAlignment = HorizontalAlignment.Center;

		SUT.Children.Add(new View
		{
			Name = "Child01",
			Width = 20,
			Height = 20
		});

		SUT.Measure(new Size(60, 60));
		var measuredSize = SUT.DesiredSize;
		SUT.Arrange(new Rect(0, 0, 60, 60));

		var firstChild = (View)SUT.Children.First();
		firstChild.SizePassedToArrangeOverride.Should().Be(new Size(20, 20));
		LayoutInformation.GetLayoutSlot(firstChild).Should().Be(new Rect(0, 0, 40, 40));
		firstChild.ActualOffset.Should().Be(new Vector3(10, 10, 0));

		measuredSize.Should().Be(new Size(40, 40));
		SUT.Children.Should().HaveCount(1);
	}

	[TestMethod]
#if __APPLE_UIKIT__
	[Ignore("Fails on iOS.")]
#endif
	public void When_Grid_Has_Two_Elements_And_MinWidth_MinHeight_VerticalAlignment_Top_HorizontalAlignment_Center_And_Child_Stretched_And_Centered()
	{
		using var _ = new AssertionScope();

		var SUT = new Grid { Name = "test" };

		SUT.MinWidth = 40;
		SUT.MinHeight = 40;
		SUT.VerticalAlignment = VerticalAlignment.Top;
		SUT.HorizontalAlignment = HorizontalAlignment.Center;

		var c1 = new View
		{
			Name = "Child01",
			RequestedDesiredSize = new Size(20, 20),
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch
		};
		SUT.Children.Add(c1);
		var c2 = new View
		{
			Name = "Child02",
			RequestedDesiredSize = new Size(20, 20),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		SUT.Children.Add(c2);

		SUT.Measure(new Size(60, 60));
		var measuredSize = SUT.DesiredSize;
		SUT.Arrange(new Rect(0, 0, 60, 60));

		c1.SizePassedToArrangeOverride.Should().Be(new Size(40, 40));
		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 40, 40));
		c1.ActualOffset.Should().Be(default(Vector3));

		c2.SizePassedToArrangeOverride.Should().Be(new Size(20, 20));
		LayoutInformation.GetLayoutSlot(c2).Should().Be(new Rect(0, 0, 40, 40));
		c2.ActualOffset.Should().Be(new Vector3(10, 10, 0));

		measuredSize.Should().Be(new Size(40, 40));
		SUT.Children.Should().HaveCount(2);
	}

	[TestMethod]
#if __APPLE_UIKIT__
	[Ignore("Fails on iOS.")]
#endif
	public void When_Grid_Has_One_Colums_And_One_Row_And_No_Size_Spec()
	{
		using var _ = new AssertionScope();

		var SUT = new Grid { Name = "test" };

		var c1 = new View
		{
			Name = "Child01",
			RequestedDesiredSize = new Size(10, 10)
		};
		SUT.Children.Add(c1);
		var c2 = new View
		{
			Name = "Child02",
			RequestedDesiredSize = new Size(10, 10)
		};
		SUT.Children.Add(c2);

		SUT.Measure(new Size(20, 20));
		var measuredSize = SUT.DesiredSize;
		SUT.Arrange(new Rect(0, 0, 20, 20));

		c1.SizePassedToArrangeOverride.Should().Be(new Size(20, 20));
		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 20, 20));

		c2.SizePassedToArrangeOverride.Should().Be(new Size(20, 20));
		LayoutInformation.GetLayoutSlot(c2).Should().Be(new Rect(0, 0, 20, 20));

		measuredSize.Should().Be(new Size(10, 10));
		SUT.Children.Should().HaveCount(2);
	}

	[TestMethod]
#if __APPLE_UIKIT__
	[Ignore("Fails on iOS.")]
#endif
	public void When_Grid_Has_Two_Colums_And_One_Row_And_No_Size_Spec()
	{
		using var _ = new AssertionScope();

		var SUT = new Grid { Name = "test" };

		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

		var c1 = new View
		{
			Name = "Child01",
			RequestedDesiredSize = new Size(10, 10)
		};
		SUT.Children.Add(c1);
		var c2 = new View
		{
			Name = "Child02",
			RequestedDesiredSize = new Size(10, 10)
		};
		SUT.Children.Add(c2);

		Grid.SetColumn(c1, 0);
		Grid.SetColumn(c2, 1);

		SUT.Measure(new Size(20, 20));
		var measuredSize = SUT.DesiredSize;
		SUT.Arrange(new Rect(0, 0, 20, 20));

		c1.SizePassedToArrangeOverride.Should().Be(new Size(10, 20));
		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 10, 20));

		c2.SizePassedToArrangeOverride.Should().Be(new Size(10, 20));
		LayoutInformation.GetLayoutSlot(c2).Should().Be(new Rect(10, 0, 10, 20));

		measuredSize.Should().Be(new Size(20, 10));
		SUT.Children.Should().HaveCount(2);
	}

	[TestMethod]
#if __APPLE_UIKIT__
	[Ignore("Fails on iOS.")]
#endif
	public void When_Grid_Has_Two_Colums_And_One_Row_And_MinWidth_MinHeight_VerticalAlignment_Top_HorizontalAlignment_Center_And_Child_Stretched_And_Centered()
	{
		using var _ = new AssertionScope();

		var SUT = new Grid { Name = "test" };

		SUT.MinWidth = 80;
		SUT.MinHeight = 80;
		SUT.VerticalAlignment = VerticalAlignment.Top;
		SUT.HorizontalAlignment = HorizontalAlignment.Center;

		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

		var c1 = new View
		{
			Name = "Child01",
			Width = 20,
			Height = 20,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch
		};
		SUT.Children.Add(c1);
		var c2 = new View
		{
			Name = "Child02",
			Width = 20,
			Height = 20,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		SUT.Children.Add(c2);

		Grid.SetColumn(c1, 0);
		Grid.SetColumn(c2, 1);

		SUT.Measure(new Size(100, 100));
		var measuredSize = SUT.DesiredSize;
		SUT.Arrange(new Rect(0, 0, 100, 100));

		c1.SizePassedToArrangeOverride.Should().Be(new Size(20, 20));
		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 40, 80));
		c1.ActualOffset.Should().Be(new Vector3(10, 30, 0));

		c2.SizePassedToArrangeOverride.Should().Be(new Size(20, 20));
		LayoutInformation.GetLayoutSlot(c2).Should().Be(new Rect(40, 0, 40, 80));
		c2.ActualOffset.Should().Be(new Vector3(50, 30, 0));

		measuredSize.Should().Be(new Size(80, 80));
		SUT.Children.Should().HaveCount(2);
	}

	[TestMethod]
#if __APPLE_UIKIT__
	[Ignore("Fails on iOS.")]
#endif
	public void When_Grid_Has_Two_Colums_And_One_Row_And_MinWidth_MinHeight_VerticalAlignment_Top_HorizontalAlignment_Stretch_And_Padding()
	{
		using var _ = new AssertionScope();

		var SUT = new Grid { Name = "test" };

		SUT.MinWidth = 80;
		SUT.MinHeight = 80;
		SUT.Padding = new Thickness(10, 20, 10, 20);
		SUT.VerticalAlignment = VerticalAlignment.Top;
		SUT.HorizontalAlignment = HorizontalAlignment.Stretch;

		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var c1 = new View
		{
			Name = "Child01",
			RequestedDesiredSize = new Size(20, 20),
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch
		};
		SUT.Children.Add(c1);
		var c2 = new View
		{
			Name = "Child02",
			RequestedDesiredSize = new Size(20, 20),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		SUT.Children.Add(c2);

		Grid.SetColumn(c1, 0);
		Grid.SetColumn(c2, 1);

		SUT.Measure(new Size(160, 160));
		var measuredSize = SUT.DesiredSize;
		SUT.Arrange(new Rect(0, 0, 160, 160));

		c1.SizePassedToArrangeOverride.Should().Be(new Size(120, 40));
		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(10, 20, 120, 40));

		c2.SizePassedToArrangeOverride.Should().Be(new Size(20, 20));
		LayoutInformation.GetLayoutSlot(c2).Should().Be(new Rect(130, 20, 20, 40));

		measuredSize.Should().Be(new Size(80, 80));
		SUT.Children.Should().HaveCount(2);
	}

	[TestMethod]
#if __APPLE_UIKIT__
	[Ignore("Fails on iOS.")]
#endif
	public void When_Grid_Has_Two_Colums_And_One_Row_And_MinWidth_MinHeight_VerticalAlignment_Top_HorizontalAlignment_Center_And_Child_Stretched_With_ColumnSpan_And_Centered()
	{
		using var _ = new AssertionScope();

		var SUT = new Grid { Name = "test" };

		SUT.MinWidth = 80;
		SUT.MinHeight = 80;
		SUT.VerticalAlignment = VerticalAlignment.Top;
		SUT.HorizontalAlignment = HorizontalAlignment.Center;

		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

		var c1 = new View
		{
			Name = "Child01",
			Width = 20,
			Height = 20,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch
		};
		Grid.SetColumnSpan(c1, 2);
		SUT.Children.Add(c1);

		var c2 = new View
		{
			Name = "Child02",
			Width = 20,
			Height = 20,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		SUT.Children.Add(c2);

		Grid.SetColumn(c1, 0);
		Grid.SetColumn(c2, 1);

		SUT.Measure(new Size(100, 100));
		var measuredSize = SUT.DesiredSize;
		SUT.Arrange(new Rect(0, 0, 100, 100));

		c1.SizePassedToArrangeOverride.Should().Be(new Size(20, 20));
		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 80, 80));

		c2.SizePassedToArrangeOverride.Should().Be(new Size(20, 20));
		LayoutInformation.GetLayoutSlot(c2).Should().Be(new Rect(40, 0, 40, 80));

		measuredSize.Should().Be(new Size(80, 80));
		SUT.Children.Should().HaveCount(2);
	}

	[TestMethod]
#if __APPLE_UIKIT__
	[Ignore("Fails on iOS.")]
#endif
	public void When_Grid_Has_Two_Rows_And_One_Column_And_MinWidth_MinHeight_VerticalAlignment_Top_HorizontalAlignment_Center_And_Child_Stretched_With_RowSpan_And_Centered()
	{
		using var _ = new AssertionScope();

		var SUT = new Grid { Name = "test" };

		SUT.MinWidth = 80;
		SUT.MinHeight = 80;
		SUT.VerticalAlignment = VerticalAlignment.Top;
		SUT.HorizontalAlignment = HorizontalAlignment.Center;

		SUT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
		SUT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

		var c1 = new View
		{
			Name = "Child01",
			Width = 20,
			Height = 20,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch
		};
		Grid.SetRowSpan(c1, 2);
		SUT.Children.Add(c1);
		var c2 = new View
		{
			Name = "Child02",
			Width = 20,
			Height = 20,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		SUT.Children.Add(c2);

		Grid.SetRow(c1, 0);
		Grid.SetRow(c2, 1);

		SUT.Measure(new Size(100, 100));
		var measuredSize = SUT.DesiredSize;
		SUT.Arrange(new Rect(0, 0, 100, 100));

		c1.SizePassedToArrangeOverride.Should().Be(new Size(20, 20));
		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 80, 80));

		c2.SizePassedToArrangeOverride.Should().Be(new Size(20, 20));
		LayoutInformation.GetLayoutSlot(c2).Should().Be(new Rect(0, 40, 80, 40));

		measuredSize.Should().Be(new Size(80, 80));
		SUT.Children.Should().HaveCount(2);
	}

	[TestMethod]
#if __APPLE_UIKIT__
	[Ignore("Fails on iOS.")]
#endif
	public void When_Grid_Has_Two_Rows_And_One_Column_And_MinWidth_MinHeight_VerticalAlignment_Top_HorizontalAlignment_Center_And_Child_Stretched_And_Centered()
	{
		using var _ = new AssertionScope();

		var SUT = new Grid { Name = "test" };

		SUT.MinWidth = 80;
		SUT.MinHeight = 80;
		SUT.VerticalAlignment = VerticalAlignment.Top;
		SUT.HorizontalAlignment = HorizontalAlignment.Center;

		SUT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
		SUT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

		var c1 = new View
		{
			Name = "Child01",
			Width = 20,
			Height = 20,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch
		};
		SUT.Children.Add(c1);
		var c2 = new View
		{
			Name = "Child02",
			Width = 20,
			Height = 20,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		SUT.Children.Add(c2);

		Grid.SetRow(c1, 0);
		Grid.SetRow(c2, 1);

		SUT.Measure(new Size(100, 100));
		var measuredSize = SUT.DesiredSize;
		SUT.Arrange(new Rect(0, 0, 100, 100));

		c1.SizePassedToArrangeOverride.Should().Be(new Size(20, 20));
		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 80, 40));

		c2.SizePassedToArrangeOverride.Should().Be(new Size(20, 20));
		LayoutInformation.GetLayoutSlot(c2).Should().Be(new Rect(0, 40, 80, 40));

		measuredSize.Should().Be(new Size(80, 80));
		SUT.Children.Should().HaveCount(2);
	}

	[TestMethod]
#if __APPLE_UIKIT__
	[Ignore("Fails on iOS.")]
#endif
	public void When_Grid_Has_Two_Rows_And_One_Column_And_MinWidth_MinHeight_VerticalAlignment_Top_HorizontalAlignment_Stretch_And_Padding()
	{
		using var _ = new AssertionScope();

		var SUT = new Grid { Name = "test" };

		SUT.MinWidth = 80;
		SUT.MinHeight = 80;
		SUT.Padding = new Thickness(10, 20, 10, 20);
		SUT.VerticalAlignment = VerticalAlignment.Stretch;
		SUT.HorizontalAlignment = HorizontalAlignment.Left;

		SUT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
		SUT.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

		var c1 = new View
		{
			Name = "Child01",
			Width = 20,
			Height = 20,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch
		};
		SUT.Children.Add(c1);
		var c2 = new View
		{
			Name = "Child02",
			Width = 20,
			Height = 20,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		SUT.Children.Add(c2);

		Grid.SetRow(c1, 0);
		Grid.SetRow(c2, 1);

		SUT.Measure(new Size(160, 160));
		var measuredSize = SUT.DesiredSize;
		SUT.Arrange(new Rect(0, 0, 160, 160));

		c1.SizePassedToArrangeOverride.Should().Be(new Size(20, 20));
		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(10, 20, 60, 100));

		c2.SizePassedToArrangeOverride.Should().Be(new Size(20, 20));
		LayoutInformation.GetLayoutSlot(c2).Should().Be(new Rect(10, 120, 60, 20));

		measuredSize.Should().Be(new Size(80, 80));
		SUT.Children.Should().HaveCount(2);
	}

	[TestMethod]
#if __APPLE_UIKIT__
	[Ignore("Fails on iOS.")]
#endif
	public void When_Grid_Has_Two_Colums_And_Two_Rows_And_No_Size_Spec()
	{
		using var _ = new AssertionScope();

		var SUT = new Grid { Name = "test" };

		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

		SUT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
		SUT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

		var c1 = new View
		{
			Name = "Child01",
			RequestedDesiredSize = new Size(10, 10)
		};
		Grid.SetRow(c1, 0);
		Grid.SetColumn(c1, 0);
		SUT.Children.Add(c1);

		var c2 = new View
		{
			Name = "Child02",
			RequestedDesiredSize = new Size(10, 10)
		};
		Grid.SetRow(c2, 0);
		Grid.SetColumn(c2, 1);
		SUT.Children.Add(c2);

		var c3 = new View
		{
			Name = "Child03",
			RequestedDesiredSize = new Size(10, 10)
		};
		Grid.SetRow(c3, 1);
		Grid.SetColumn(c3, 0);
		SUT.Children.Add(c3);

		var c4 = new View
		{
			Name = "Child04",
			RequestedDesiredSize = new Size(10, 10)
		};
		Grid.SetRow(c4, 1);
		Grid.SetColumn(c4, 1);
		SUT.Children.Add(c4);

		SUT.Measure(new Size(20, 20));
		var measuredSize = SUT.DesiredSize;
		SUT.Arrange(new Rect(0, 0, 20, 20));

		c1.SizePassedToArrangeOverride.Should().Be(new Size(10, 10));
		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 10, 10));

		c2.SizePassedToArrangeOverride.Should().Be(new Size(10, 10));
		LayoutInformation.GetLayoutSlot(c2).Should().Be(new Rect(10, 0, 10, 10));

		c3.SizePassedToArrangeOverride.Should().Be(new Size(10, 10));
		LayoutInformation.GetLayoutSlot(c3).Should().Be(new Rect(0, 10, 10, 10));

		c4.SizePassedToArrangeOverride.Should().Be(new Size(10, 10));
		LayoutInformation.GetLayoutSlot(c4).Should().Be(new Rect(10, 10, 10, 10));

		measuredSize.Should().Be(new Size(20, 20));
		SUT.Children.Should().HaveCount(4);
	}

	[TestMethod]
#if __APPLE_UIKIT__
	[Ignore("Fails on iOS.")]
#endif
	public void When_Grid_Has_Two_Colums_And_Two_Rows_And_MinWidth_MinHeight_VerticalAlignment_Top_HorizontalAlignment_Center_And_Child_Stretched_And_Centered()
	{
		using var _ = new AssertionScope();

		var SUT = new Grid { Name = "test" };

		SUT.MinWidth = 80;
		SUT.MinHeight = 80;
		SUT.VerticalAlignment = VerticalAlignment.Top;
		SUT.HorizontalAlignment = HorizontalAlignment.Center;

		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

		SUT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
		SUT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

		var c1 = new View
		{
			Name = "Child01",
			MinWidth = 20,
			MinHeight = 20,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch
		};
		Grid.SetRow(c1, 0);
		Grid.SetColumn(c1, 0);
		SUT.Children.Add(c1);

		var c2 = new View
		{
			Name = "Child02",
			MinWidth = 20,
			MinHeight = 20,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		Grid.SetRow(c2, 0);
		Grid.SetColumn(c2, 1);
		SUT.Children.Add(c2);

		var c3 = new View
		{
			Name = "Child03",
			MinWidth = 20,
			MinHeight = 20,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		Grid.SetRow(c3, 1);
		Grid.SetColumn(c3, 0);
		SUT.Children.Add(c3);

		var c4 = new View
		{
			Name = "Child04",
			MinWidth = 20,
			MinHeight = 20,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch
		};
		Grid.SetRow(c4, 1);
		Grid.SetColumn(c4, 1);
		SUT.Children.Add(c4);

		SUT.Measure(new Size(100, 100));
		var measuredSize = SUT.DesiredSize;
		SUT.Arrange(new Rect(0, 0, 100, 100));

		c1.SizePassedToArrangeOverride.Should().Be(new Size(40, 40));
		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 40, 40));

		c2.SizePassedToArrangeOverride.Should().Be(new Size(20, 20));
		LayoutInformation.GetLayoutSlot(c2).Should().Be(new Rect(40, 0, 40, 40));

		c3.SizePassedToArrangeOverride.Should().Be(new Size(20, 20));
		LayoutInformation.GetLayoutSlot(c3).Should().Be(new Rect(0, 40, 40, 40));

		c4.SizePassedToArrangeOverride.Should().Be(new Size(40, 40));
		LayoutInformation.GetLayoutSlot(c4).Should().Be(new Rect(40, 40, 40, 40));

		measuredSize.Should().Be(new Size(80, 80));
		SUT.Children.Should().HaveCount(4);
	}

	[TestMethod]
#if __APPLE_UIKIT__
	[Ignore("Fails on iOS.")]
#elif __ANDROID__
	[Ignore("Fails on Android in CI, but passes locally.")]
#endif
	[RequiresScaling(1f)]
	public void When_Grid_Has_Two_Star_Uneven_Colums_And_One_Row()
	{
		using var _ = new AssertionScope();

		var SUT = new Grid { Name = "test" };

		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

		var c1 = new View
		{
			Name = "Child01",
			MinWidth = 10,
			MinHeight = 10
		};
		Grid.SetRow(c1, 0);
		Grid.SetColumn(c1, 0);
		SUT.Children.Add(c1);

		var c2 = new View
		{
			Name = "Child02",
			MinWidth = 10,
			MinHeight = 10
		};
		Grid.SetRow(c2, 0);
		Grid.SetColumn(c2, 1);
		SUT.Children.Add(c2);

		SUT.Measure(new Size(20, 20));
		var measuredSize = SUT.DesiredSize;
		SUT.Arrange(new Rect(0, 0, 20, 20));

#if __ANDROID__ || __APPLE_UIKIT__ || __WASM__
		c1.SizePassedToArrangeOverride.Should().Be(new Size(40.0 / 3.0, 20));
		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 40.0 / 3.0, 20));

		c2.SizePassedToArrangeOverride.Should().Be(new Size(10, 20));
		LayoutInformation.GetLayoutSlot(c2).Should().Be(new Rect(40.0 / 3.0, 0, 20.0 / 3.0, 20));

		measuredSize.Should().Be(new Size(50.0 / 3.0, 10));
#else
		c1.SizePassedToArrangeOverride.Should().Be(new Size(13, 20));
		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 13, 20));

		c2.SizePassedToArrangeOverride.Should().Be(new Size(10, 20));
		LayoutInformation.GetLayoutSlot(c2).Should().Be(new Rect(13, 0, 7, 20));

		measuredSize.Should().Be(new Size(17, 10));
#endif

		SUT.Children.Should().HaveCount(2);
	}

	[TestMethod]
#if __APPLE_UIKIT__
	[Ignore("Fails on iOS.")]
#elif __ANDROID__
	[Ignore("Fails on Android in CI, but passes locally.")]
#endif
	public void When_Grid_Has_One_Absolute_Column_And_One_Star_Column_And_One_Row()
	{
		using var _ = new AssertionScope();

		var SUT = new Grid { Name = "test" };

		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5, GridUnitType.Pixel) });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

		var c1 = new View
		{
			Name = "Child01",
			Width = 10,
			Height = 10
		};
		Grid.SetRow(c1, 0);
		Grid.SetColumn(c1, 0);
		SUT.Children.Add(c1);

		var c2 = new View
		{
			Name = "Child02",
			Width = 10,
			Height = 10
		};
		Grid.SetRow(c2, 0);
		Grid.SetColumn(c2, 1);
		SUT.Children.Add(c2);

		SUT.Measure(new Size(20, 20));
		var measuredSize = SUT.DesiredSize;
		SUT.Arrange(new Rect(0, 0, 20, 20));

#if __ANDROID__ || __APPLE_UIKIT__
		c1.DesiredSize.Should().Be(new Size(16.0 / 3.0, 10));
#else
		c1.DesiredSize.Should().Be(new Size(5, 10));
#endif
		c2.DesiredSize.Should().Be(new Size(10, 10));

#if __ANDROID__ || __APPLE_UIKIT__
		c1.SizePassedToArrangeOverride.Should().Be(new Size(16.0 / 3.0, 10));
		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 16.0 / 3.0, 20));

		c2.SizePassedToArrangeOverride.Should().Be(new Size(10, 10));
		LayoutInformation.GetLayoutSlot(c2).Should().Be(new Rect(16.0 / 3.0, 0, 44.0 / 3.0, 20));

		measuredSize.Should().Be(new Size(46.0 / 3.0, 10));
#else
		c1.SizePassedToArrangeOverride.Should().Be(new Size(10, 10));
		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 5, 20));

		c2.SizePassedToArrangeOverride.Should().Be(new Size(10, 10));
		LayoutInformation.GetLayoutSlot(c2).Should().Be(new Rect(5, 0, 15, 20));

		measuredSize.Should().Be(new Size(15, 10));
#endif

		SUT.Children.Should().HaveCount(2);
	}

	[TestMethod]
#if __APPLE_UIKIT__
	[Ignore("Fails on iOS.")]
#endif
	public void When_Grid_Has_One_Variable_Sized_Element_With_ColSpan_and_Three_Columns()
	{
		using var _ = new AssertionScope();

		var SUT = new Grid { Name = "test" };

		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

		var c1 = new View
		{
			Name = "Child01",
			DesiredSizeSelector = s => s.Width > 10
				? new Size(20, 5)
				: new Size(10, 10)
		};
		Grid.SetColumnSpan(c1, 2);

		SUT.Children.Add(c1);

		SUT.Measure(new Size(30, 30));
		SUT.DesiredSize.Should().Be(new Size(20, 5));
		//SUT.UnclippedDesiredSize.Should().Be(new Size(20, 5));
		c1.DesiredSize.Should().Be(new Size(20, 5));
		//c1.UnclippedDesiredSize.Should().Be(new Size(0, 0));

		SUT.Arrange(new Rect(0, 0, 30, 30));

		LayoutInformation.GetLayoutSlot(SUT).Should().Be(new Rect(0, 0, 30, 30));

		c1.SizePassedToArrangeOverride.Should().Be(new Size(20, 30));
		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 20, 30));

		SUT.Children.Should().HaveCount(1);
	}

	[TestMethod]
#if __APPLE_UIKIT__
	[Ignore("Fails on iOS.")]
#elif __ANDROID__
	[Ignore("Fails on Android in CI, but passes locally.")]
#endif
	[RequiresScaling(1f)]
	public void When_Grid_Has_Two_Variable_Sized_Element_With_ColSpan_and_One_Auto_Columns()
	{
		using var _ = new AssertionScope();

		var SUT = new Grid { Name = "test" };

		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

		var c1
			= new View
			{
				Name = "Child01",
				DesiredSizeSelector = s => s.Width > 10
					? new Size(20, 5)
					: new Size(10, 10)
			};
		Grid.SetColumnSpan(c1, 2);
		var c2 = new View
		{
			Name = "Child02",
			DesiredSizeSelector = s => new Size(5, 5)
		};
		Grid.SetRow(c2, 0);
		Grid.SetColumn(c2, 1);

		SUT.Children.Add(c1);
		SUT.Children.Add(c2);

		SUT.Measure(new Size(30, 30));
#if __ANDROID__ || __APPLE_UIKIT__
		SUT.DesiredSize.Should().Be(new Size(52.0 / 3.0, 5));
#elif __WASM__
		SUT.DesiredSize.Should().Be(new Size(17.5, 5));
#else
		SUT.DesiredSize.Should().Be(new Size(17, 5));
#endif
		//SUT.UnclippedDesiredSize.Should().Be(new Size(20, 5));

#if __ANDROID__ || __APPLE_UIKIT__
		c1.DesiredSize.Should().Be(new Size(52.0 / 3.0, 5));
#elif __WASM__
		c1.DesiredSize.Should().Be(new Size(17.5, 5));
#else
		c1.DesiredSize.Should().Be(new Size(17, 5));
#endif
		//c1.UnclippedDesiredSize.Should().Be(new Size(0, 0));

		c2.DesiredSize.Should().Be(new Size(5, 5));
		//c2.UnclippedDesiredSize.Should().Be(new Size(0, 0));

		SUT.Arrange(new Rect(0, 0, 30, 30));

		LayoutInformation.GetLayoutSlot(SUT).Should().Be(new Rect(0, 0, 30, 30));

#if __ANDROID__ || __APPLE_UIKIT__
		c1.SizePassedToArrangeOverride.Should().Be(new Size(17.5, 30));
		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 17.5, 30));

		c2.SizePassedToArrangeOverride.Should().Be(new Size(5, 30));
		LayoutInformation.GetLayoutSlot(c2).Should().Be(new Rect(12.5, 0, 5, 30));
#elif __WASM__
		c1.SizePassedToArrangeOverride.Should().Be(new Size(20, 30));
		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 17.5, 30));

		c2.SizePassedToArrangeOverride.Should().Be(new Size(5, 30));
		LayoutInformation.GetLayoutSlot(c2).Should().Be(new Rect(12.5, 0, 5, 30));
#else
		c1.SizePassedToArrangeOverride.Should().Be(new Size(20, 30));
		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 17, 30));

		c2.SizePassedToArrangeOverride.Should().Be(new Size(5, 30));
		LayoutInformation.GetLayoutSlot(c2).Should().Be(new Rect(12, 0, 5, 30));
#endif
		SUT.Children.Should().HaveCount(2);
	}

	[TestMethod]
#if __APPLE_UIKIT__
	[Ignore("Fails on iOS.")]
#endif
	[RequiresScaling(1f)]
	public void When_Grid_Has_One_Element_With_ColSpan_and_Three_Columns()
	{
		using var _ = new AssertionScope();

		var SUT = new Grid { Name = "test" };

		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

		var c1 =
			new View
			{
				Name = "Child01",
				RequestedDesiredSize = new Size(10, 10)
			};
		Grid.SetColumnSpan(c1, 2);
		SUT.Children.Add(c1);

		SUT.Measure(new Size(20, 20));
		var measuredSize = SUT.DesiredSize;
		SUT.Arrange(new Rect(0, 0, 20, 20));

#if __ANDROID__ || __APPLE_UIKIT__ || __WASM__
		c1.SizePassedToArrangeOverride.Should().Be(new Size(40.0 / 3.0, 20));
		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 40.0 / 3.0, 20));
#else
		c1.SizePassedToArrangeOverride.Should().Be(new Size(13, 20));
		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 13, 20));
#endif

		measuredSize.Should().Be(new Size(10, 10));
		SUT.Children.Should().HaveCount(1);
	}

	[TestMethod]
#if __APPLE_UIKIT__
	[Ignore("Fails on iOS.")]
#endif
	public void When_Grid_Has_Three_Element_With_ColSpan_and_Four_Progressing_Columns()
	{
		using var _ = new AssertionScope();

		var SUT = new Grid { Name = "test" };

		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4, GridUnitType.Star) });

		SUT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(10, GridUnitType.Pixel) });
		SUT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(11, GridUnitType.Pixel) });
		SUT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(12, GridUnitType.Pixel) });

		var c1 = new View
		{
			Name = "Child01",
			RequestedDesiredSize = new Size(10, 10)
		};
		Grid.SetColumnSpan(c1, 2);
		SUT.Children.Add(c1);

		var c2 = new View
		{
			Name = "Child02",
			RequestedDesiredSize = new Size(10, 10)
		};
		Grid.SetRow(c2, 1);
		Grid.SetColumn(c2, 1);
		Grid.SetColumnSpan(c2, 2);
		SUT.Children.Add(c2);

		var c3 = new View
		{
			Name = "Child03",
			RequestedDesiredSize = new Size(10, 10)
		};
		Grid.SetRow(c3, 2);
		Grid.SetColumn(c3, 2);
		Grid.SetColumnSpan(c3, 2);
		SUT.Children.Add(c3);

		SUT.Measure(new Size(20, 20));
		SUT.DesiredSize.Should().Be(new Size(17, 20));
		//SUT.UnclippedDesiredSize.Should().Be(new Size(20, 33));

		SUT.Arrange(new Rect(0, 0, 20, 20));

#if __ANDROID__ || __APPLE_UIKIT__
		c1.SizePassedToArrangeOverride.Should().Be(new Size(6, 10));
#else
		c1.SizePassedToArrangeOverride.Should().Be(new Size(10, 10));
#endif
		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 6, 10));

		c2.SizePassedToArrangeOverride.Should().Be(new Size(10, 11));
		LayoutInformation.GetLayoutSlot(c2).Should().Be(new Rect(2, 10, 10, 11));

		c3.SizePassedToArrangeOverride.Should().Be(new Size(14, 12));
		LayoutInformation.GetLayoutSlot(c3).Should().Be(new Rect(6, 21, 14, 12));

		SUT.Children.Should().HaveCount(3);
	}

	[TestMethod]
#if __APPLE_UIKIT__
	[Ignore("Fails on iOS.")]
#endif
	[RequiresScaling(1f)]
	public void When_Grid_Has_One_Element_With_ColSpan_and_RowSpan_and_Three_Columns()
	{
		using var _ = new AssertionScope();

		var SUT = new Grid { Name = "test" };

		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

		SUT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
		SUT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
		SUT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

		var c1 = new View
		{
			Name = "Child01",
			RequestedDesiredSize = new Size(10, 10)
		};

		Grid.SetColumnSpan(c1, 2);
		Grid.SetRowSpan(c1, 2);
		SUT.Children.Add(c1);

		SUT.Measure(new Size(20, 20));
		var measuredSize = SUT.DesiredSize;
		SUT.Arrange(new Rect(0, 0, 20, 20));

#if __ANDROID__ || __APPLE_UIKIT__ || __WASM__
		c1.SizePassedToArrangeOverride.Should().Be(new Size(40.0 / 3.0, 40.0 / 3.0));
		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 40.0 / 3.0, 40.0 / 3.0));
#else
		c1.SizePassedToArrangeOverride.Should().Be(new Size(13, 13));
		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 13, 13));
#endif

		measuredSize.Should().Be(new Size(10, 10));
		SUT.Children.Should().HaveCount(1);
	}

	[TestMethod]
#if __APPLE_UIKIT__
	[Ignore("Fails on iOS.")]
#endif
	[RequiresScaling(1f)]
	public void When_Grid_Has_One_Element_With_ColSpan_and_RowSpan_and_Three_Columns_And_Middle()
	{
		using var _ = new AssertionScope();

		var SUT = new Grid { Name = "test" };

		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

		SUT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
		SUT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
		SUT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

		var c1 = new View
		{
			Name = "Child01",
			RequestedDesiredSize = new Size(10, 10)
		};
		Grid.SetColumnSpan(c1, 2);
		Grid.SetRowSpan(c1, 2);
		Grid.SetRow(c1, 1);
		Grid.SetColumn(c1, 1);
		SUT.Children.Add(c1);

		SUT.Measure(new Size(20, 20));
		var measuredSize = SUT.DesiredSize;
		SUT.Arrange(new Rect(0, 0, 20, 20));
#if __ANDROID__ || __APPLE_UIKIT__ || __WASM__
		c1.SizePassedToArrangeOverride.Should().Be(new Size(40.0 / 3.0, 40.0 / 3.0));
		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(20.0 / 3.0, 20.0 / 3.0, 40.0 / 3.0, 40.0 / 3.0));
#else
		c1.SizePassedToArrangeOverride.Should().Be(new Size(13, 13));
		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(7, 7, 13, 13));
#endif
		measuredSize.Should().Be(new Size(10, 10));
		SUT.Children.Should().HaveCount(1);
	}

	[TestMethod]
#if __APPLE_UIKIT__
	[Ignore("Fails on iOS.")]
#endif
	[RequiresScaling(1f)]
	public void When_Grid_Has_One_Element_With_ColSpan_Overflow_and_Three_Columns()
	{
		using var _ = new AssertionScope();

		var SUT = new Grid { Name = "test" };

		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

		var c1 =
			new View
			{
				Name = "Child01",
				RequestedDesiredSize = new Size(10, 10)
			};
		Grid.SetColumnSpan(c1, 4);
		SUT.Children.Add(c1);

		SUT.Measure(new Size(20, 20));
		var measuredSize = SUT.DesiredSize;
		SUT.Arrange(new Rect(0, 0, 20, 20));

		c1.SizePassedToArrangeOverride.Should().Be(new Size(20, 20));
		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 20, 20));

		measuredSize.Should().Be(new Size(10, 10));
		SUT.Children.Should().HaveCount(1);
	}

	[TestMethod]
#if __APPLE_UIKIT__
	[Ignore("Fails on iOS for child.SizePassedToArrangeOverride https://github.com/unoplatform/uno/issues/9080")]
#endif
	public async Task When_Grid_RowCollection_Changes()
	{
		var SUT = new Grid();

		TestServices.WindowHelper.WindowContent = SUT;
		await TestServices.WindowHelper.WaitFor(() => SUT.IsLoaded);

		var child = new View
		{
			Name = "Child01",
			RequestedDesiredSize = new Size(10, 10)
		};

		Grid.SetRow(child, 1);
		SUT.Children.Add(child);

		var measureAvailableSize = new Size(20, 20);
		var arrangeFinalRect = new Rect(default, measureAvailableSize);

		SUT.Measure(measureAvailableSize);
		SUT.Arrange(arrangeFinalRect);

		child.SizePassedToArrangeOverride.Should().Be(new Size(arrangeFinalRect.Width, arrangeFinalRect.Height));
		LayoutInformation.GetLayoutSlot(child).Should().Be(arrangeFinalRect);

		var row1 = new RowDefinition { Height = new GridLength(5) };
		SUT.RowDefinitions.Add(row1);
		SUT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

		SUT.Measure(measureAvailableSize);
		SUT.Arrange(arrangeFinalRect);

		child.SizePassedToArrangeOverride.Should().Be(new Size(20, 10));
		LayoutInformation.GetLayoutSlot(child).Should().Be(new Rect(0, 5, 20, 10));

#if HAS_UNO && !(__ANDROID__ || __APPLE_UIKIT__)
		SUT.IsMeasureDirty.Should().BeFalse();
		SUT.IsMeasureDirtyOrMeasureDirtyPath.Should().BeFalse();
#endif
		row1.Height = new GridLength(10);
#if HAS_UNO
		SUT.IsMeasureDirty.Should().BeTrue();
		SUT.IsMeasureDirtyOrMeasureDirtyPath.Should().BeTrue();
#endif
	}

	[TestMethod]
#if __APPLE_UIKIT__
	[Ignore("Fails on iOS.")]
#endif
	public async Task When_Grid_ColumnCollection_Changes()
	{
		using var _ = new AssertionScope();

		var SUT = new Grid();

		TestServices.WindowHelper.WindowContent = SUT;
		await TestServices.WindowHelper.WaitFor(() => SUT.IsLoaded);

		var child = new View
		{
			Name = "Child01",
			RequestedDesiredSize = new Size(10, 10)
		};

		Grid.SetColumn(child, 1);
		SUT.Children.Add(child);

		SUT.Measure(new Size(20, 20));
		SUT.Arrange(new Rect(0, 0, 20, 20));

		child.SizePassedToArrangeOverride.Should().Be(new Size(20, 20));
		LayoutInformation.GetLayoutSlot(child).Should().Be(new Rect(0, 0, 20, 20));

		var col1 = new ColumnDefinition { Width = new GridLength(5) };
		SUT.ColumnDefinitions.Add(col1);
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

		SUT.Measure(new Size(20, 20));
		SUT.Arrange(new Rect(0, 0, 20, 20));

		child.SizePassedToArrangeOverride.Should().Be(new Size(10, 20));
		LayoutInformation.GetLayoutSlot(child).Should().Be(new Rect(5, 0, 10, 20));

#if false
		SUT.IsMeasureDirty.Should().BeFalse();
		SUT.IsMeasureDirtyOrMeasureDirtyPath.Should().BeFalse();
#endif
		col1.Width = new GridLength(10);
#if false
		SUT.IsMeasureDirty.Should().BeTrue();
		SUT.IsMeasureDirtyOrMeasureDirtyPath.Should().BeTrue();
#endif
	}

	[TestMethod]
	public async Task When_Grid_Column_Min_MaxWidth_Changes()
	{
		using var _ = new AssertionScope();

		var SUT = new Grid();

		TestServices.WindowHelper.WindowContent = SUT;
		await TestServices.WindowHelper.WaitFor(() => SUT.IsLoaded);

		SUT.Measure(new Size(20, 20));
		SUT.Arrange(new Rect(0, 0, 20, 20));

		ColumnDefinition ColumnDefinition1;

		SUT.ColumnDefinitions.Add(ColumnDefinition1 = new ColumnDefinition { Width = new GridLength(5) });
		//SUT.InvalidateMeasureCallCount.Should().Be(1);

		SUT.Measure(new Size(20, 20)); // need to remeasure for the invalidation to be called again

		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
		//SUT.InvalidateMeasureCallCount.Should().Be(2);

		ColumnDefinition1.MaxWidth = 22;
		//SUT.InvalidateMeasureCallCount.Should().Be(2); // Already invalidated, no new invalidations should be done

		SUT.Measure(new Size(20, 20)); // need to remeasure for the invalidation to be called again

		ColumnDefinition1.MaxWidth = 23;
		//SUT.InvalidateMeasureCallCount.Should().Be(3);

		ColumnDefinition1.MinWidth = 5;
		//SUT.InvalidateMeasureCallCount.Should().Be(3); // Already invalidated, no new invalidations should be done

		SUT.Measure(new Size(20, 20)); // need to remeasure for the invalidation to be called again

		ColumnDefinition1.MinWidth = 6;
		//SUT.InvalidateMeasureCallCount.Should().Be(4);
	}

	[TestMethod]
#if __APPLE_UIKIT__
	[Ignore("Fails on iOS.")]
#endif
	public void When_Grid_Has_Two_Columns_And_VerticalAlignment_Top()
	{
		using var _ = new AssertionScope();

		var SUT = new Grid
		{
			Name = "test",
			VerticalAlignment = VerticalAlignment.Top
		};


		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

		var child = new View
		{
			Name = "Child01",
			RequestedDesiredSize = new Size(10, 10)
		};
		SUT.Children.Add(child);

		SUT.Measure(new Size(20, 20));
		var measuredSize = SUT.DesiredSize;
		SUT.Arrange(new Rect(0, 0, 20, 20));

		measuredSize.Should().Be(new Size(10, 10));

		child.SizePassedToArrangeOverride.Should().Be(new Size(10, 10));
		LayoutInformation.GetLayoutSlot(child).Should().Be(new Rect(0, 0, 10, 10));

		SUT.Children.Should().HaveCount(1);
	}

	[TestMethod]
#if __APPLE_UIKIT__
	[Ignore("Fails on iOS.")]
#endif
	public void When_Grid_Has_Two_StarColums_One_Variable_Column_And_Two_StarRows_One_Variable_Row_And_MinWidth_MinHeight_VerticalAlignment_Top_HorizontalAlignment_Center_And_Child_Stretched_And_Centered()
	{
		using var _ = new AssertionScope();

		var SUT = new Grid
		{
			Name = "test",
			MinWidth = 80,
			MinHeight = 80,
			VerticalAlignment = VerticalAlignment.Top,
			HorizontalAlignment = HorizontalAlignment.Center
		};

		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

		SUT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
		SUT.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		SUT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

		var c1 = new View { Name = "Child01", RequestedDesiredSize = new Size(10, 10), HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
		Grid.SetRow(c1, 0);
		Grid.SetColumn(c1, 0);
		SUT.Children.Add(c1);

		var c2 = new View { Name = "Child02", RequestedDesiredSize = new Size(10, 10), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
		Grid.SetRow(c2, 1);
		Grid.SetColumn(c2, 1);
		SUT.Children.Add(c2);

		var c3 = new View { Name = "Child03", RequestedDesiredSize = new Size(10, 10), HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
		Grid.SetRow(c3, 2);
		Grid.SetColumn(c3, 1);
		SUT.Children.Add(c3);

		var c4 = new View { Name = "Child04", RequestedDesiredSize = new Size(10, 10), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
		Grid.SetRow(c4, 2);
		Grid.SetColumn(c4, 2);
		SUT.Children.Add(c4);

		SUT.Measure(new Size(100, 100));
		var measuredSize = SUT.DesiredSize;
		SUT.Arrange(new Rect(0, 0, 100, 100));

		c1.SizePassedToArrangeOverride.Should().Be(new Size(35, 35));
		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 35, 35));

		c2.SizePassedToArrangeOverride.Should().Be(new Size(10, 10));
		LayoutInformation.GetLayoutSlot(c2).Should().Be(new Rect(35, 35, 10, 10));

		c3.SizePassedToArrangeOverride.Should().Be(new Size(10, 35));
		LayoutInformation.GetLayoutSlot(c3).Should().Be(new Rect(35, 45, 10, 35));

		c4.SizePassedToArrangeOverride.Should().Be(new Size(10, 10));
		LayoutInformation.GetLayoutSlot(c4).Should().Be(new Rect(45, 45, 35, 35));

		measuredSize.Should().Be(new Size(80, 80));
		SUT.Children.Should().HaveCount(4);
	}

	[TestMethod]
#if __APPLE_UIKIT__
	[Ignore("Fails on iOS.")]
#endif
	public void When_Grid_Has_Two_StarColums_One_Variable_Column_And_Two_StarRows_One_Variable_Row_And_MinWidth_MinHeight_VerticalAlignment_Top_HorizontalAlignment_Center_And_Child_Stretched_And_Centered_With_RowSpan_And_ColumnSpan()
	{
		using var _ = new AssertionScope();

		var SUT = new Grid
		{
			Name = "test",
			MinWidth = 80,
			MinHeight = 80,
			VerticalAlignment = VerticalAlignment.Top,
			HorizontalAlignment = HorizontalAlignment.Center
		};

		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

		SUT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
		SUT.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		SUT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

		var c1 = new View { Name = "Child01", RequestedDesiredSize = new Size(10, 10), HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
		Grid.SetRow(c1, 0);
		Grid.SetColumn(c1, 0);
		Grid.SetColumnSpan(c1, 2);
		SUT.Children.Add(c1);

		var c2 = new View { Name = "Child02", RequestedDesiredSize = new Size(10, 10), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
		Grid.SetRow(c2, 0);
		Grid.SetColumn(c2, 2);
		SUT.Children.Add(c2);

		var c3 = new View { Name = "Child03", RequestedDesiredSize = new Size(10, 10), HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
		Grid.SetRow(c3, 1);
		Grid.SetColumn(c3, 0);
		Grid.SetRowSpan(c3, 2);
		SUT.Children.Add(c3);

		var c4 = new View { Name = "Child04", RequestedDesiredSize = new Size(10, 10), HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
		Grid.SetRow(c4, 1);
		Grid.SetColumn(c4, 1);
		Grid.SetColumnSpan(c4, 2);
		SUT.Children.Add(c4);

		var c5 = new View { Name = "Child05", RequestedDesiredSize = new Size(10, 10), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
		Grid.SetRow(c5, 2);
		Grid.SetColumn(c5, 1);
		Grid.SetColumnSpan(c5, 2);
		SUT.Children.Add(c5);

		var c6 = new View { Name = "Child06", RequestedDesiredSize = new Size(10, 10), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
		Grid.SetRow(c6, 1);
		Grid.SetColumn(c6, 1);
		SUT.Children.Add(c6);

		SUT.Measure(new Size(100, 100));
		var measuredSize = SUT.DesiredSize;
		SUT.Arrange(new Rect(0, 0, 100, 100));

		c1.SizePassedToArrangeOverride.Should().Be(new Size(45, 35));
		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 45, 35));

		c2.SizePassedToArrangeOverride.Should().Be(new Size(10, 10));
		LayoutInformation.GetLayoutSlot(c2).Should().Be(new Rect(45, 0, 35, 35));

		c3.SizePassedToArrangeOverride.Should().Be(new Size(35, 45));
		LayoutInformation.GetLayoutSlot(c3).Should().Be(new Rect(0, 35, 35, 45));

		c4.SizePassedToArrangeOverride.Should().Be(new Size(45, 10));
		LayoutInformation.GetLayoutSlot(c4).Should().Be(new Rect(35, 35, 45, 10));

		c5.SizePassedToArrangeOverride.Should().Be(new Size(10, 10));
		LayoutInformation.GetLayoutSlot(c5).Should().Be(new Rect(35, 45, 45, 35));

		c6.SizePassedToArrangeOverride.Should().Be(new Size(10, 10));
		LayoutInformation.GetLayoutSlot(c6).Should().Be(new Rect(35, 35, 10, 10));

		measuredSize.Should().Be(new Size(80, 80));
		SUT.Children.Should().HaveCount(6);
	}

	[TestMethod]
#if __APPLE_UIKIT__
	[Ignore("Fails on iOS.")]
#elif __ANDROID__
	[Ignore("Fails on Android in CI, but passes locally.")]
#endif
	public void When_Row_Out_Of_Range()
	{
		using var _ = new AssertionScope();

		var SUT = new Grid();

		SUT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(5, GridUnitType.Pixel) });
		SUT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(5, GridUnitType.Pixel) });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5, GridUnitType.Pixel) });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5, GridUnitType.Pixel) });

		var c1 = new View { RequestedDesiredSize = new Size(100, 100) };
		SUT.Children.Add(c1);

		var c2 = new View { RequestedDesiredSize = new Size(100, 100) };
		Grid.SetRow(c2, 3);
		SUT.Children.Add(c2);

		SUT.Measure(new Size(100, 1000));
#if __ANDROID__ || __APPLE_UIKIT__
		SUT.DesiredSize.Should().Be(new Size(31.0 / 3.0, 32.0 / 3.0));
#else
		SUT.DesiredSize.Should().Be(new Size(10, 10));
#endif

		SUT.Arrange(new Rect(0, 0, 100, 1000));

		LayoutInformation.GetLayoutSlot(SUT).Should().Be(new Rect(0, 0, 100, 1000));
	}

	[TestMethod]
	public void When_Column_Out_Of_Range()
	{
		using var _ = new AssertionScope();

		var SUT = new Grid();

		SUT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(5, GridUnitType.Pixel) });
		SUT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(5, GridUnitType.Pixel) });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5, GridUnitType.Pixel) });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5, GridUnitType.Pixel) });

		var c1 = new View { RequestedDesiredSize = new Size(100, 100) };
		SUT.Children.Add(c1);

		var c2 = new View { RequestedDesiredSize = new Size(100, 100) };
		Grid.SetColumn(c2, 3);
		SUT.Children.Add(c2);

		SUT.Measure(new Size(10, 10));
		SUT.DesiredSize.Should().Be(new Size(10, 10));
		//SUT.UnclippedDesiredSize.Should().Be(new Size(200, 105));

		SUT.Arrange(new Rect(0, 0, 10, 10));
	}

	[TestMethod]
	public void When_RowSpan_Out_Of_Range()
	{
		using var _ = new AssertionScope();

		var SUT = new Grid();

		SUT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
		SUT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

		var c1 = new View { RequestedDesiredSize = new Size(100, 100) };
		SUT.Children.Add(c1);

		var c2 = new View { RequestedDesiredSize = new Size(100, 100) };
		Grid.SetRow(c2, 1);
		Grid.SetColumn(c2, 0);
		Grid.SetRowSpan(c2, 3);
		SUT.Children.Add(c2);

		SUT.Measure(new Size(100, 1000));
		SUT.DesiredSize.Should().Be(new Size(100, 200));
		//SUT.UnclippedDesiredSize.Should().Be(new Size(100, 200));

		SUT.Arrange(new Rect(0, 0, 100, 1000));
	}

	[TestMethod]
	public void When_ColumnSpan_Out_Of_Range()
	{
		using var _ = new AssertionScope();

		var SUT = new Grid();

		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

		var c1 = new View { RequestedDesiredSize = new Size(100, 100) };
		SUT.Children.Add(c1);

		var c2 = new View { RequestedDesiredSize = new Size(100, 100) };
		Grid.SetRow(c2, 0);
		Grid.SetColumn(c2, 1);
		Grid.SetColumnSpan(c2, 3);
		SUT.Children.Add(c2);

		SUT.Measure(new Size(1000, 100));
		SUT.DesiredSize.Should().Be(new Size(200, 100));
		//SUT.UnclippedDesiredSize.Should().Be(new Size(200, 100));

		SUT.Arrange(new Rect(0, 0, 1000, 100));
	}

	[TestMethod]
	public void When_Clear_ColumnDefinitions()
	{
		var SUT = new Grid();

		SUT.ColumnDefinitions.Clear();
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		SUT.ColumnDefinitions.Clear();

		SUT.ColumnDefinitions.Should().HaveCount(0);
	}

	[TestMethod]
	public async Task When_Clear_RowDefinitions()
	{
		var SUT = new Grid();

		TestServices.WindowHelper.WindowContent = SUT;
		await TestServices.WindowHelper.WaitFor(() => SUT.IsLoaded);

		SUT.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		SUT.RowDefinitions.Clear();

		SUT.RowDefinitions.Should().HaveCount(0);
	}

	[TestMethod]
	public void When_Zero_Star_Size()
	{
		var SUT = new Grid();
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0, GridUnitType.Star) });
		SUT.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		SUT.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

		SetChild(0, 0);
		SetChild(0, 1);
		SetChild(1, 0);
		SetChild(1, 1);

		SUT.Measure(new Size(800, 800));
		SUT.DesiredSize.Should().Be(new Size(50, 100));

		void SetChild(int col, int row)
		{
			var border = new Border { Height = 50, Width = 50 };
			Grid.SetColumn(border, col);
			Grid.SetRow(border, row);
			SUT.Children.Add(border);
		}
	}

	[TestMethod]
	public void When_RowSpan_Reuse()
	{
		// This sample is taken from the ToggleSwitch template

		var SUT = new StackPanel();

		var topLevel = new Grid();
		SUT.Children.Add(topLevel);

		topLevel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		topLevel.RowDefinitions.Add(new RowDefinition { Height = GridLengthHelper2.FromValueAndType(10, GridUnitType.Pixel) });
		topLevel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		topLevel.RowDefinitions.Add(new RowDefinition { Height = GridLengthHelper2.FromValueAndType(10, GridUnitType.Pixel) });

		topLevel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		topLevel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLengthHelper2.FromValueAndType(12, GridUnitType.Pixel), MaxWidth = 12 });
		topLevel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLengthHelper2.FromValueAndType(12, GridUnitType.Pixel) });
		topLevel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		topLevel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLengthHelper2.FromValueAndType(1, GridUnitType.Star) });

		var spacer = new Grid() { Margin = new Thickness(0, 5, 0, 5) };
		topLevel.Children.Add(spacer);
		Grid.SetRow(spacer, 1);
		Grid.SetRowSpan(spacer, 3);
		Grid.SetColumnSpan(spacer, 3);

		var knob = new Grid()
		{
			HorizontalAlignment = HorizontalAlignment.Left,
			Width = 20,
			Height = 20
		};

		Grid.SetRow(spacer, 2);
		topLevel.Children.Add(knob);

		SUT.Measure(new Size(800, 800));
		Assert.AreEqual(new Size(20, 20), knob.DesiredSize);
	}

	[TestMethod]
	public void When_One_Child_With_Margin_5()
	{
		var SUT = new Grid() { Name = "test" };

		var c1 = new View
		{
			Name = "Child01",
			Margin = new Thickness(5)
		};

		SUT.Children.Add(c1);

		SUT.Measure(new Size(20, 20));

		SUT.DesiredSize.Should().Be(new Size(10, 10), because: "SUT.DesiredSize");
#if !WINAPPSDK
		GetUnclippedDesiredSize(SUT).Should().Be(new Size(10, 10), because: "SUT UnclippedDesiredSize");
#endif

		c1.DesiredSize.Should().Be(new Size(10, 10), because: "c1.DesiredSize");

#if !WINAPPSDK
		GetUnclippedDesiredSize(c1).Should().Be(new Size(0, 0), because: "c1 UnclippedDesiredSize"); // UnclippedDesiredSize excludes margins
#endif

		SUT.Arrange(new Rect(0, 0, 20, 20));

		LayoutInformation.GetAvailableSize(SUT).Should().Be(new Size(20, 20), because: "SUT AvailableSize");
		LayoutInformation.GetLayoutSlot(SUT).Should().Be(new Rect(0, 0, 20, 20), because: "SUT LayoutSlot");

#if __APPLE_UIKIT__
		c1.SizePassedToArrangeOverride.Should().Be(new Size(0, 0), because: "c1.SizePassedToArrangeOverride");
#else
		c1.SizePassedToArrangeOverride.Should().Be(new Size(10, 10), because: "c1.SizePassedToArrangeOverride");
#endif
		c1.SizePassedToMeasureOverride.Should().Be(new Size(10, 10), because: "c1.SizePassedToMeasureOverride");
		LayoutInformation.GetAvailableSize(c1).Should().Be(new Size(20, 20), because: "c1 AvailableSize");
		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 20, 20), because: "c1 LayoutSlot");

		SUT.Children.Should().HaveCount(1);
	}

	[TestMethod]
	public void When_One_Child_With_Margin_1234()
	{
		var SUT = new Grid
		{
			Name = "test",
			Padding = new Thickness(2)
		};

		var c1 = new View
		{
			Name = "Child01",
			Margin = new Thickness(1, 2, 3, 4)
		};

		SUT.Children.Add(c1);

		SUT.Measure(new Size(20, 20));

		SUT.DesiredSize.Should().Be(new Size(8, 10), because: "SUT.DesiredSize");
#if !WINAPPSDK
		GetUnclippedDesiredSize(SUT).Should().Be(new Size(8, 10), because: "SUT UnclippedDesiredSize");
#endif

		c1.DesiredSize.Should().Be(new Size(4, 6), because: "c1.DesiredSize");
#if !WINAPPSDK
		GetUnclippedDesiredSize(c1).Should().Be(new Size(0, 0), because: "c1 UnclippedDesiredSize"); // UnclippedDesiredSize excludes margins
#endif

		SUT.Arrange(new Rect(0, 0, 20, 20));

		LayoutInformation.GetAvailableSize(SUT).Should().Be(new Size(20, 20), because: "SUT AvailableSize");
		LayoutInformation.GetLayoutSlot(SUT).Should().Be(new Rect(0, 0, 20, 20), because: "SUT LayoutSlot");

#if __APPLE_UIKIT__
		c1.SizePassedToArrangeOverride.Should().Be(new Size(0, 0), because: "c1.SizePassedToArrangeOverride");
#else
		c1.SizePassedToArrangeOverride.Should().Be(new Size(12, 10), because: "c1.SizePassedToArrangeOverride");
#endif

		c1.SizePassedToMeasureOverride.Should().Be(new Size(12, 10), because: "c1.SizePassedToMeasureOverride");
		LayoutInformation.GetAvailableSize(c1).Should().Be(new Size(16, 16), because: "c1 AvailableSize");
		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(2, 2, 16, 16), because: "c1 LayoutSlot");

		SUT.Children.Should().HaveCount(1);
	}

	[TestMethod]
	public void When_One_Child_With_Margin_1234_Size8()
	{
		using var _ = new AssertionScope();

		var SUT = new Grid
		{
			Name = "test",
			Padding = new Thickness(2)
		};

		var c1 = new View
		{
			Name = "Child01",
			Margin = new Thickness(1, 2, 3, 4)
		};

		SUT.Children.Add(c1);

		SUT.Measure(new Size(8, 8));

		SUT.DesiredSize.Should().Be(new Size(8, 8));
#if UNO_REFERENCE_API
		GetUnclippedDesiredSize(SUT).Should().Be(new Size(8, 8));
#elif !WINAPPSDK
		GetUnclippedDesiredSize(SUT).Should().Be(new Size(8, 10));
#endif

#if UNO_REFERENCE_API
		c1.DesiredSize.Should().Be(new Size(4, 4));
#else
		c1.DesiredSize.Should().Be(new Size(4, 6));
#endif
#if !WINAPPSDK
		GetUnclippedDesiredSize(c1).Should().Be(new Size(0, 0)); // UnclippedDesiredSize excludes margins
#endif

		SUT.Arrange(new Rect(0, 0, 8, 8));

		LayoutInformation.GetLayoutSlot(SUT).Should().Be(new Rect(0, 0, 8, 8));
		LayoutInformation.GetAvailableSize(SUT).Should().Be(new Size(8, 8));

		c1.SizePassedToArrangeOverride.Should().Be(default);
		c1.SizePassedToMeasureOverride.Should().Be(default);
		LayoutInformation.GetAvailableSize(c1).Should().Be(new Size(4, 4));
#if UNO_REFERENCE_API || WINAPPSDK
		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(2, 2, 4, 4));
#else
		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(2, 2, 4, 6));
#endif

		SUT.Children.Should().HaveCount(1);
	}

	[TestMethod]
	public void When_One_Child_With_Margin_Center_And_Center()
	{
		using var _ = new AssertionScope();

		var SUT = new Grid() { Name = "test" };

		var c1 = new View
		{
			Name = "Child01",
			Margin = new Thickness(0, 0, 0, 30),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Height = 10,
			Width = 10,
		};

		SUT.Children.Add(c1);

		SUT.Measure(new Size(20, 20));
		SUT.DesiredSize.Should().Be(new Size(10, 20), because: "SUT.DesiredSize");

#if UNO_REFERENCE_API
		GetUnclippedDesiredSize(SUT).Should().Be(new Size(10, 20), because: "SUT UnclippedDesiredSize");
#endif

#if UNO_REFERENCE_API || WINAPPSDK
		c1.DesiredSize.Should().Be(new Size(10, 20), because: "c1.DesiredSize");
#else
		GetUnclippedDesiredSize(SUT).Should().Be(new Size(10, 30), because: "SUT UnclippedDesiredSize");
		c1.DesiredSize.Should().Be(new Size(10, 30), because: "c1.DesiredSize");
#endif

#if !WINAPPSDK
		GetUnclippedDesiredSize(c1).Should().Be(new Size(10, 10), because: "c1 UnclippedDesiredSize"); // UnclippedDesiredSize excludes margins
#endif

		SUT.Arrange(new Rect(0, 0, 50, 50));

#if __APPLE_UIKIT__
		c1.SizePassedToArrangeOverride.Should().Be(new Size(0, 0), because: "c1.SizePassedToArrangeOverride");
#else
		c1.SizePassedToArrangeOverride.Should().Be(new Size(10, 10), because: "c1.SizePassedToArrangeOverride");
#endif

#if UNO_REFERENCE_API
		c1.SizePassedToMeasureOverride.Should().Be(new Size(10, 10), because: "c1.SizePassedToMeasureOverride");
#else
		c1.SizePassedToMeasureOverride.Should().Be(new Size(10, 0), because: "c1.SizePassedToMeasureOverride");
#endif
		LayoutInformation.GetAvailableSize(c1).Should().Be(new Size(20, 20), because: "c1 AvailableSize");
		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 50, 50), because: "c1 LayoutSlot");

		SUT.Children.Should().HaveCount(1);
	}

	[TestMethod]
	public void When_One_Child_With_Margin_Center_And_Bottom()
	{
		using var _ = new AssertionScope();

		var SUT = new Grid() { Name = "test" };

		var c1 = new View
		{
			Name = "Child01",
			Margin = new Thickness(0, 0, 0, 30),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Bottom,
			Height = 10,
			Width = 10,
		};

		SUT.Children.Add(c1);

		SUT.Measure(new Size(20, 20));

		SUT.DesiredSize.Should().Be(new Size(10, 20), because: "SUT.DesiredSize");
#if UNO_REFERENCE_API
		GetUnclippedDesiredSize(SUT).Should().Be(new Size(10, 20), because: "SUT UnclippedDesiredSize");
#elif !WINAPPSDK
		GetUnclippedDesiredSize(SUT).Should().Be(new Size(10, 30), because: "SUT.UnclippedDesiredSize");
#endif

#if UNO_REFERENCE_API || WINAPPSDK
		c1.DesiredSize.Should().Be(new Size(10, 20), because: "c1.DesiredSize");
#else
		c1.DesiredSize.Should().Be(new Size(10, 30), because: "c1.DesiredSize");
#endif

#if !WINAPPSDK
		GetUnclippedDesiredSize(c1).Should().Be(new Size(10, 10), because: "c1 UnclippedDesiredSize"); // UnclippedDesiredSize excludes margins
#endif

		SUT.Arrange(new Rect(0, 0, 50, 50));

		LayoutInformation.GetAvailableSize(SUT).Should().Be(new Size(20, 20), because: "SUT AvailableSize");
		LayoutInformation.GetLayoutSlot(SUT).Should().Be(new Rect(0, 0, 50, 50), because: "SUT LayoutSlot");

#if __APPLE_UIKIT__
		c1.SizePassedToArrangeOverride.Should().Be(new Size(0, 0), because: "c1.SizePassedToArrangeOverride");
#else
		c1.SizePassedToArrangeOverride.Should().Be(new Size(10, 10), because: "c1.SizePassedToArrangeOverride");
#endif

#if UNO_REFERENCE_API
		c1.SizePassedToMeasureOverride.Should().Be(new Size(10, 10), because: "c1.SizePassedToMeasureOverride");
#else
		c1.SizePassedToMeasureOverride.Should().Be(new Size(10, 0), because: "c1.SizePassedToMeasureOverride");
#endif
		LayoutInformation.GetAvailableSize(c1).Should().Be(new Size(20, 20), because: "c1 AvailableSize");
		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 50, 50), because: "c1 LayoutSlot");

		SUT.Children.Should().HaveCount(1);
	}

	[TestMethod]
	public void When_One_Child_With_Margin_Center_And_Top()
	{
		using var _ = new AssertionScope();

		var SUT = new Grid() { Name = "test" };

		var c1 = new View
		{
			Name = "Child01",
			Margin = new Thickness(0, 0, 0, 30),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Top,
			Height = 10,
			Width = 10,
		};

		SUT.Children.Add(c1);

		SUT.Measure(new Size(20, 20));

		SUT.DesiredSize.Should().Be(new Size(10, 20), because: "SUT.DesiredSize");
#if UNO_REFERENCE_API
		GetUnclippedDesiredSize(SUT).Should().Be(new Size(10, 20), because: "SUT UnclippedDesiredSize");
		c1.DesiredSize.Should().Be(new Size(10, 20), because: "c1.DesiredSize");
#elif !WINAPPSDK
		GetUnclippedDesiredSize(SUT).Should().Be(new Size(10, 30), because: "SUT UnclippedDesiredSize");
		c1.DesiredSize.Should().Be(new Size(10, 30), because: "c1.DesiredSize");
#endif

#if !WINAPPSDK
		GetUnclippedDesiredSize(c1).Should().Be(new Size(10, 10), because: "c1 UnclippedDesiredSize"); // UnclippedDesiredSize excludes margins
#endif

		SUT.Arrange(new Rect(0, 0, 50, 50));

#if __APPLE_UIKIT__
		c1.SizePassedToArrangeOverride.Should().Be(new Size(0, 0), because: "c1.SizePassedToArrangeOverride");
#else
		c1.SizePassedToArrangeOverride.Should().Be(new Size(10, 10), because: "c1.SizePassedToArrangeOverride");
#endif

#if UNO_REFERENCE_API
		c1.SizePassedToMeasureOverride.Should().Be(new Size(10, 10), because: "c1.SizePassedToMeasureOverride");
#else
		c1.SizePassedToMeasureOverride.Should().Be(new Size(10, 0), because: "c1.SizePassedToMeasureOverride");
#endif
		LayoutInformation.GetAvailableSize(c1).Should().Be(new Size(20, 20), because: "c1 AvailableSize");
		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 50, 50), because: "c1 LayoutSlot");

		SUT.Children.Should().HaveCount(1);
	}

	[TestMethod]
	public void When_One_Fixed_Size_Child_With_Margin_Right_And_Stretch()
	{
		var SUT = new Grid() { Name = "test" };


		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

		var c1 = new View
		{
			Name = "Child01",
			Margin = new Thickness(0, 0, 50, 0),
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch,
			Height = 50,
			Width = 50,
		};
		Grid.SetRow(c1, 0);
		Grid.SetColumn(c1, 1);

		SUT.Children.Add(c1);

		SUT.Measure(new Size(300, 300));
		SUT.DesiredSize.Should().Be(new Size(100, 50), because: "SUT.DesiredSize");
		c1.DesiredSize.Should().Be(new Size(100, 50), because: "c1.DesiredSize");
#if !WINAPPSDK
		GetUnclippedDesiredSize(SUT).Should().Be(new Size(100, 50), because: "SUT UnclippedDesiredSize");
#endif

		SUT.Arrange(new Rect(0, 0, 300, 300));

		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(100, 0, 100, 300), because: "c1 LayoutSlot");
		LayoutInformation.GetAvailableSize(c1).Should().Be(new Size(double.PositiveInfinity, 300), because: "c1 AvailableSize");
#if __APPLE_UIKIT__
		c1.SizePassedToArrangeOverride.Should().Be(new Size(0, 0), because: "c1.SizePassedToArrangeOverride");
#else
		c1.SizePassedToArrangeOverride.Should().Be(new Size(50, 50), because: "c1.SizePassedToArrangeOverride");
#endif
		c1.SizePassedToMeasureOverride.Should().Be(new Size(50, 50), because: "c1.SizePassedToMeasureOverride");
		SUT.Children.Should().HaveCount(1);
	}

#if !WINAPPSDK
	private static Size GetUnclippedDesiredSize(UIElement element)
	{
#if UNO_REFERENCE_API
		return element.m_unclippedDesiredSize;
#else
		var layouterElement = (ILayouterElement)element;
		var layouter = (Layouter)layouterElement.Layouter;
		return layouter._unclippedDesiredSize;
#endif
	}
#endif

	[TestMethod]
	public void When_One_Auto_Columns_and_one_star_and_two_children()
	{
		var SUT = new Grid() { Name = "test" };

		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

		var c1 = new View { Name = "Child01", RequestedDesiredSize = new Size(5, 5) };
		SUT.Children.Add(c1);

		var c2 = new View { Name = "Child02", RequestedDesiredSize = new Size(10, 10) };
		Grid.SetColumn(c2, 1);
		SUT.Children.Add(c2);

		SUT.Measure(new Size(20, 20));
		var measuredSize = SUT.DesiredSize;
		Assert.AreEqual(new Size(15, 10), measuredSize);
		Assert.AreEqual(new Size(5, 5), c1.RequestedDesiredSize);
		Assert.AreEqual(new Size(10, 10), c2.RequestedDesiredSize);

		SUT.Arrange(new Rect(0, 0, 20, 20));

		Assert.AreEqual(new Rect(0, 0, 5, 20), LayoutInformation.GetLayoutSlot(c1));
		Assert.AreEqual(new Rect(5, 0, 15, 20), LayoutInformation.GetLayoutSlot(c2));

		Assert.HasCount(2, SUT.Children);
	}

	[TestMethod]
	public void When_Two_Auto_Columns_two_children()
	{
		var SUT = new Grid() { Name = "test" };

		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var c1 = new View { Name = "Child01", RequestedDesiredSize = new Size(5, 5) };
		SUT.Children.Add(c1);

		var c2 = new View { Name = "Child02", RequestedDesiredSize = new Size(10, 10) };
		Grid.SetColumn(c2, 1);
		SUT.Children.Add(c2);

		SUT.Measure(new Size(20, 20));
		var measuredSize = SUT.DesiredSize;
		Assert.AreEqual(new Size(15, 10), measuredSize);
		Assert.AreEqual(new Size(5, 5), c1.RequestedDesiredSize);
		Assert.AreEqual(new Size(10, 10), c2.RequestedDesiredSize);

		SUT.Arrange(new Rect(0, 0, 20, 20));

		Assert.AreEqual(new Rect(0, 0, 5, 20), LayoutInformation.GetLayoutSlot(c1));
		Assert.AreEqual(new Rect(5, 0, 10, 20), LayoutInformation.GetLayoutSlot(c2));

		Assert.HasCount(2, SUT.Children);
	}

	[TestMethod]
	public void When_One_Auto_and_one_abs_and_one_star_and_three_children()
	{
		using var _ = new AssertionScope();

		var SUT = new Grid() { Name = "test" };

		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

		var c1 = new View { Name = "Child01", RequestedDesiredSize = new Size(5, 5) };
		SUT.Children.Add(c1);

		var c2 = new View { Name = "Child02", RequestedDesiredSize = new Size(10, 10) };
		Grid.SetColumn(c2, 1);
		SUT.Children.Add(c2);

		var c3 = new View { Name = "Child03", RequestedDesiredSize = new Size(10, 7) };
		Grid.SetColumn(c3, 2);
		SUT.Children.Add(c3);

		SUT.Measure(new Size(20, 20));
		var measuredSize = SUT.DesiredSize;

		measuredSize.Should().Be(new Size(20, 10));
		c1.DesiredSize.Should().Be(new Size(5, 5));
		c2.DesiredSize.Should().Be(new Size(6, 10));
		c3.DesiredSize.Should().Be(new Size(9, 7));

		SUT.Arrange(new Rect(0, 0, 20, 20));

		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 5, 20));
		LayoutInformation.GetLayoutSlot(c2).Should().Be(new Rect(5, 0, 6, 20));
		LayoutInformation.GetLayoutSlot(c3).Should().Be(new Rect(11, 0, 9, 20));

		SUT.Children.Should().HaveCount(3);
	}

	[TestMethod]
	[RequiresScaling(1f)]
	public void When_Nine_grid_and_one_auto_cell_and_three_children()
	{
		using var _ = new AssertionScope();

		var SUT = new Grid() { Name = "test" };

		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

		SUT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
		SUT.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		SUT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

		var c1 = new View { Name = "Child01", RequestedDesiredSize = new Size(5, 5) };
		SUT.Children.Add(c1);

		var c2 = new View { Name = "Child02", RequestedDesiredSize = new Size(3, 4) };
		Grid.SetRow(c2, 1);
		Grid.SetColumn(c2, 1);
		SUT.Children.Add(c2);

		var c3 = new View { Name = "Child03", RequestedDesiredSize = new Size(8, 9) };
		Grid.SetRow(c3, 2);
		Grid.SetColumn(c3, 2);
		SUT.Children.Add(c3);

		SUT.Measure(new Size(20, 20));
		var measuredSize = SUT.DesiredSize;
		measuredSize.Should().Be(new Size(16, 17));

		c1.DesiredSize.Should().Be(new Size(5, 5));
		c2.DesiredSize.Should().Be(new Size(3, 4));
		c3.DesiredSize.Should().Be(new Size(8, 8));

		SUT.Arrange(new Rect(0, 0, 20, 20));

#if __ANDROID__ || __APPLE_UIKIT__ || __WASM__
		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 8.5f, 8.0));
		LayoutInformation.GetLayoutSlot(c2).Should().Be(new Rect(8.5f, 8.0f, 3, 4));
		LayoutInformation.GetLayoutSlot(c3).Should().Be(new Rect(11.5f, 12.0f, 8.5f, 8.0f));
#else
		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 8.0f, 8.0f));
		LayoutInformation.GetLayoutSlot(c2).Should().Be(new Rect(8.0f, 8.0f, 3, 4));
		LayoutInformation.GetLayoutSlot(c3).Should().Be(new Rect(11.0f, 12.0f, 9.0f, 8.0f));
#endif
		SUT.Children.Should().HaveCount(3);
	}

	[TestMethod]
	public void When_Quad_two_auto_and_four_children()
	{
		var SUT = new Grid() { Name = "test" };

		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		SUT.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		SUT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

		var c1 = new View { Name = "Child01", RequestedDesiredSize = new Size(3, 4) };
		SUT.Children.Add(c1);

		var c2 = new View { Name = "Child02", RequestedDesiredSize = new Size(4, 5) };
		Grid.SetColumn(c2, 1);
		SUT.Children.Add(c2);

		var c3 = new View { Name = "Child03", RequestedDesiredSize = new Size(6, 7) };
		Grid.SetRow(c3, 1);
		SUT.Children.Add(c3);

		var c4 = new View { Name = "Child04", RequestedDesiredSize = new Size(8, 9) };
		Grid.SetRow(c4, 1);
		Grid.SetColumn(c4, 1);
		SUT.Children.Add(c4);

		SUT.Measure(new Size(20, 20));
		var measuredSize = SUT.DesiredSize;
		Assert.AreEqual(new Size(14, 14), measuredSize);

		Assert.AreEqual(new Size(3, 4), c1.RequestedDesiredSize);
		Assert.AreEqual(new Size(4, 5), c2.RequestedDesiredSize);
		Assert.AreEqual(new Size(6, 7), c3.RequestedDesiredSize);
		Assert.AreEqual(new Size(8, 9), c4.RequestedDesiredSize);

		SUT.Arrange(new Rect(0, 0, 20, 20));

		Assert.AreEqual(new Rect(0, 0, 12, 5), LayoutInformation.GetLayoutSlot(c1));
		Assert.AreEqual(new Rect(12, 0, 8, 5), LayoutInformation.GetLayoutSlot(c2));
		Assert.AreEqual(new Rect(0, 5, 12, 15), LayoutInformation.GetLayoutSlot(c3));
		Assert.AreEqual(new Rect(12, 5, 8, 15), LayoutInformation.GetLayoutSlot(c4));

		Assert.HasCount(4, SUT.Children);
	}

	[TestMethod]
	[RequiresScaling(1f)]
	public void When_Nine_grid_and_one_auto_cell_and_four_children()
	{
		using var _ = new AssertionScope();

		var SUT = new Grid() { Name = "test" };

		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

		SUT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
		SUT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
		SUT.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

		var c1 = new View { Name = "Child01", RequestedDesiredSize = new Size(5, 5) };
		SUT.Children.Add(c1);

		var c2 = new View { Name = "Child02", RequestedDesiredSize = new Size(3, 4) };
		Grid.SetRow(c2, 1);
		Grid.SetColumn(c2, 1);
		SUT.Children.Add(c2);

		var c3 = new View { Name = "Child03", RequestedDesiredSize = new Size(8, 9) };
		Grid.SetRow(c3, 2);
		Grid.SetColumn(c3, 2);
		SUT.Children.Add(c3);

		var c4 = new View { Name = "Child04", RequestedDesiredSize = new Size(5, 5) };
		Grid.SetRow(c4, 1);
		SUT.Children.Add(c4);

		SUT.Measure(new Size(20, 20));
		var measuredSize = SUT.DesiredSize;
		measuredSize.Should().Be(new Size(16, 19));

		c1.DesiredSize.Should().Be(new Size(5, 5));
		c2.DesiredSize.Should().Be(new Size(3, 4));
		c3.DesiredSize.Should().Be(new Size(8, 9));
		c4.DesiredSize.Should().Be(new Size(5, 5));

		SUT.Arrange(new Rect(0, 0, 20, 20));

#if __ANDROID__ || __APPLE_UIKIT__ || __WASM__
		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 8.5f, 5.5f));
		LayoutInformation.GetLayoutSlot(c2).Should().Be(new Rect(8.5f, 5.5f, 3, 5.5f));
		LayoutInformation.GetLayoutSlot(c3).Should().Be(new Rect(11.5f, 11, 8.5f, 9));
		LayoutInformation.GetLayoutSlot(c4).Should().Be(new Rect(0, 5.5f, 8.5f, 5.5f));

#else
		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 8.0f, 6.0f));
		LayoutInformation.GetLayoutSlot(c2).Should().Be(new Rect(8.0f, 6.0f, 3, 5.0f));
		LayoutInformation.GetLayoutSlot(c3).Should().Be(new Rect(11.0f, 11, 9.0f, 9));
		LayoutInformation.GetLayoutSlot(c4).Should().Be(new Rect(0, 6.0f, 8.0f, 5.0f));
#endif

		SUT.Children.Should().HaveCount(4);
	}

	[TestMethod]
	public void When_Three_Rows_One_Auto_Two_Fixed_And_Row_Span_Full()
	{
		using var _ = new AssertionScope();

		var SUT = new Grid() { Name = "test", Height = 44 };

		SUT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(18) });
		SUT.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		SUT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(18) });

		var c1 = new View { Name = "Child01", RequestedDesiredSize = new Size(0, 10) };
		Grid.SetRow(c1, 1);
		SUT.Children.Add(c1);

		var c2 = new View { Name = "Child02", RequestedDesiredSize = new Size(30, 0) };
		Grid.SetRow(c2, 1);
		SUT.Children.Add(c2);

		var c3 = new View { Name = "Child03", RequestedDesiredSize = new Size(8, 24) };
		Grid.SetRowSpan(c3, 3);
		SUT.Children.Add(c3);

		SUT.Measure(new Size(100, 44));
		var measuredSize = SUT.DesiredSize;
		measuredSize.Should().Be(new Size(30, 44));

		c1.DesiredSize.Should().Be(new Size(0, 10));
		c2.DesiredSize.Should().Be(new Size(30, 0));
		c3.DesiredSize.Should().Be(new Size(8, 24));

		SUT.Arrange(new Rect(0, 0, 20, 44));

		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 18, 30, 10));
		LayoutInformation.GetLayoutSlot(c2).Should().Be(new Rect(0, 18, 30, 10));
		LayoutInformation.GetLayoutSlot(c3).Should().Be(new Rect(0, 0, 30, 46));

		SUT.Children.Should().HaveCount(3);
	}

	[TestMethod]
	public void When_Three_Colums_One_Auto_Two_Fixed_And_Column_Span_Full()
	{
		using var _ = new AssertionScope();

		var SUT = new Grid() { Name = "test", Width = 44 };

		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) });

		var c1 = new View { Name = "Child01", RequestedDesiredSize = new Size(10, 0) };
		Grid.SetColumn(c1, 1);
		SUT.Children.Add(c1);

		var c2 = new View { Name = "Child02", RequestedDesiredSize = new Size(0, 30) };
		Grid.SetColumn(c2, 1);
		SUT.Children.Add(c2);

		var c3 = new View { Name = "Child03", RequestedDesiredSize = new Size(24, 8) };
		Grid.SetColumnSpan(c3, 3);
		SUT.Children.Add(c3);

		SUT.Measure(new Size(44, 100));
		var measuredSize = SUT.DesiredSize;
		measuredSize.Should().Be(new Size(44, 30));

		c1.DesiredSize.Should().Be(new Size(10, 0));
		c2.DesiredSize.Should().Be(new Size(0, 30));
		c3.DesiredSize.Should().Be(new Size(24, 8));

		SUT.Arrange(new Rect(0, 0, 44, 20));

		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(18, 0, 10, 30));
		LayoutInformation.GetLayoutSlot(c2).Should().Be(new Rect(18, 0, 10, 30));
		LayoutInformation.GetLayoutSlot(c3).Should().Be(new Rect(0, 0, 46, 30));

		SUT.Children.Should().HaveCount(3);
	}

	[TestMethod]
	public void When_One_Child_With_VerticalTopAlignment_and_Fixed_Height()
	{
		var SUT = new Grid() { Name = "test" };

		var c1 = new View
		{
			Name = "Child01",
			RequestedDesiredSize = new Size(10, 10),
			Height = 10,
			VerticalAlignment = VerticalAlignment.Top,
		};
		SUT.Children.Add(c1);

		SUT.Measure(new Size(20, 20));
		var measuredSize = SUT.DesiredSize;
		Assert.AreEqual(new Size(10, 10), measuredSize);
		Assert.AreEqual(new Size(10, 10), c1.RequestedDesiredSize);

		SUT.Arrange(new Rect(0, 0, 20, 20));

		Assert.AreEqual(new Rect(0, 0, 20, 20), LayoutInformation.GetLayoutSlot(c1));
		Assert.AreEqual(new Size(20, 10), c1.RenderSize);

		Assert.HasCount(1, SUT.Children);
	}

	[TestMethod]
	public void When_One_Child_With_VerticalBottomAlignment_and_Fixed_Height()
	{
		var SUT = new Grid() { Name = "test" };

		var c1 = new View
		{
			Name = "Child01",
			RequestedDesiredSize = new Size(10, 10),
			Height = 10,
			VerticalAlignment = VerticalAlignment.Bottom,
		};
		SUT.Children.Add(c1);

		SUT.Measure(new Size(20, 20));
		var measuredSize = SUT.DesiredSize;
		Assert.AreEqual(new Size(10, 10), measuredSize);
		Assert.AreEqual(new Size(10, 10), c1.RequestedDesiredSize);

		SUT.Arrange(new Rect(0, 0, 20, 20));

		Assert.AreEqual(new Rect(0, 0, 20, 20), LayoutInformation.GetLayoutSlot(c1));
		Assert.AreEqual(new Vector3(0, 10, 0), c1.ActualOffset);
		Assert.AreEqual(new Size(20, 10), c1.RenderSize);

		Assert.HasCount(1, SUT.Children);
	}

	[TestMethod]
	public void When_One_Child_With_VerticalCenterAlignment_and_Fixed_Height()
	{
		var SUT = new Grid() { Name = "test" };


		var c1 = new View
		{
			Name = "Child01",
			RequestedDesiredSize = new Size(10, 10),
			Height = 10,
			VerticalAlignment = VerticalAlignment.Center,
		};
		SUT.Children.Add(c1);

		SUT.Measure(new Size(20, 20));
		var measuredSize = SUT.DesiredSize;
		Assert.AreEqual(new Size(10, 10), measuredSize);
		Assert.AreEqual(new Size(10, 10), c1.RequestedDesiredSize);

		SUT.Arrange(new Rect(0, 0, 20, 20));

		Assert.AreEqual(new Rect(0, 0, 20, 20), LayoutInformation.GetLayoutSlot(c1));
		Assert.AreEqual(new Vector3(0, 5, 0), c1.ActualOffset);
		Assert.AreEqual(new Size(20, 10), c1.RenderSize);

		Assert.HasCount(1, SUT.Children);
	}

	[TestMethod]
	public void When_One_Child_With_HorizontalLeftAlignment_and_Fixed_Width()
	{
		var SUT = new Grid() { Name = "test" };

		var c1 = new View
		{
			Name = "Child01",
			RequestedDesiredSize = new Size(10, 10),
			Width = 10,
			HorizontalAlignment = HorizontalAlignment.Left,
		};
		SUT.Children.Add(c1);

		SUT.Measure(new Size(20, 20));
		var measuredSize = SUT.DesiredSize;
		Assert.AreEqual(new Size(10, 10), measuredSize);
		Assert.AreEqual(new Size(10, 10), c1.RequestedDesiredSize);

		SUT.Arrange(new Rect(0, 0, 20, 20));

		Assert.AreEqual(new Rect(0, 0, 20, 20), LayoutInformation.GetLayoutSlot(c1));
		Assert.AreEqual(Vector3.Zero, c1.ActualOffset);
		Assert.AreEqual(new Size(10, 20), c1.RenderSize);

		Assert.HasCount(1, SUT.Children);
	}

	[TestMethod]
	public void When_One_Child_With_HorizontalRightAlignment_and_Fixed_Width()
	{
		var SUT = new Grid() { Name = "test" };

		var c1 = new View
		{
			Name = "Child01",
			RequestedDesiredSize = new Size(10, 10),
			Width = 10,
			HorizontalAlignment = HorizontalAlignment.Right,
		};

		SUT.Children.Add(c1);

		SUT.Measure(new Size(20, 20));
		var measuredSize = SUT.DesiredSize;
		Assert.AreEqual(new Size(10, 10), measuredSize);
		Assert.AreEqual(new Size(10, 10), c1.RequestedDesiredSize);

		SUT.Arrange(new Rect(0, 0, 20, 20));

		Assert.AreEqual(new Rect(0, 0, 20, 20), LayoutInformation.GetLayoutSlot(c1));
		Assert.AreEqual(new Vector3(10, 0, 0), c1.ActualOffset);
		Assert.AreEqual(new Size(10, 20), c1.RenderSize);

		Assert.HasCount(1, SUT.Children);
	}

	[TestMethod]
	public void When_One_Child_With_HorizontalCenterAlignment_and_Fixed_Width()
	{
		var SUT = new Grid() { Name = "test" };

		var c1 = new View
		{
			Name = "Child01",
			RequestedDesiredSize = new Size(10, 10),
			Width = 10,
			HorizontalAlignment = HorizontalAlignment.Center,
		};

		SUT.Children.Add(c1);

		SUT.Measure(new Size(20, 20));
		var measuredSize = SUT.DesiredSize;
		Assert.AreEqual(new Size(10, 10), measuredSize);
		Assert.AreEqual(new Size(10, 10), c1.RequestedDesiredSize);

		SUT.Arrange(new Rect(0, 0, 20, 20));

		Assert.AreEqual(new Rect(0, 0, 20, 20), LayoutInformation.GetLayoutSlot(c1));
		Assert.AreEqual(new Vector3(5, 0, 0), c1.ActualOffset);
		Assert.AreEqual(new Size(10, 20), c1.RenderSize);


		Assert.HasCount(1, SUT.Children);
	}

	[TestMethod]
	public void When_One_Child_With_VerticalTopAlignment_and_Variable_Height()
	{
		var SUT = new Grid() { Name = "test" };


		var c1 = new View
		{
			Name = "Child01",
			RequestedDesiredSize = new Size(10, 10),
			VerticalAlignment = VerticalAlignment.Top,
		};
		SUT.Children.Add(c1);

		SUT.Measure(new Size(20, 20));
		var measuredSize = SUT.DesiredSize;
		Assert.AreEqual(new Size(10, 10), measuredSize);
		Assert.AreEqual(new Size(10, 10), c1.RequestedDesiredSize);

		SUT.Arrange(new Rect(0, 0, 20, 20));

		Assert.AreEqual(new Rect(0, 0, 20, 20), LayoutInformation.GetLayoutSlot(c1));
		Assert.AreEqual(new Size(20, 10), c1.RenderSize);

		Assert.HasCount(1, SUT.Children);
	}

	[TestMethod]
	public void When_One_Child_With_VerticalCenterAlignment_and_Variable_Height()
	{
		var SUT = new Grid() { Name = "test" };

		var c1 = new View
		{
			Name = "Child01",
			RequestedDesiredSize = new Size(10, 10),
			VerticalAlignment = VerticalAlignment.Center,
		};

		SUT.Children.Add(c1);

		SUT.Measure(new Size(20, 20));
		var measuredSize = SUT.DesiredSize;
		Assert.AreEqual(new Size(10, 10), measuredSize);
		Assert.AreEqual(new Size(10, 10), c1.RequestedDesiredSize);

		SUT.Arrange(new Rect(0, 0, 20, 20));

		Assert.AreEqual(new Rect(0, 0, 20, 20), LayoutInformation.GetLayoutSlot(c1));
		Assert.AreEqual(new Vector3(0, 5, 0), c1.ActualOffset);
		Assert.AreEqual(new Size(20, 10), c1.RenderSize);

		Assert.HasCount(1, SUT.Children);
	}

	[TestMethod]
	public void When_One_Child_With_HorizontalStretchAlignment_and_MaxWidth()
	{
		var SUT = new Grid() { Name = "test" };

		var c1 = new View
		{
			Name = "Child01",
			RequestedDesiredSize = new Size(10, 10),
			MaxWidth = 10,
		};
		SUT.Children.Add(c1);

		SUT.Measure(new Size(20, 20));
		var measuredSize = SUT.DesiredSize;
		Assert.AreEqual(new Size(10, 10), measuredSize);
		Assert.AreEqual(new Size(10, 10), c1.RequestedDesiredSize);

		SUT.Arrange(new Rect(0, 0, 20, 20));

		Assert.AreEqual(new Rect(0, 0, 20, 20), LayoutInformation.GetLayoutSlot(c1));
		Assert.AreEqual(new Vector3(5, 0, 0), c1.ActualOffset);
		Assert.AreEqual(new Size(10, 20), c1.RenderSize);

		Assert.HasCount(1, SUT.Children);
	}

	[TestMethod]
	public void When_One_Child_With_VerticalStretchAlignment_and_MaxHeight()
	{
		var SUT = new Grid() { Name = "test" };


		var c1 = new View
		{
			Name = "Child01",
			RequestedDesiredSize = new Size(10, 10),
			MaxHeight = 10,
		};

		SUT.Children.Add(c1);

		SUT.Measure(new Size(20, 20));
		var measuredSize = SUT.DesiredSize;
		Assert.AreEqual(new Size(10, 10), measuredSize);
		Assert.AreEqual(new Size(10, 10), c1.RequestedDesiredSize);

		SUT.Arrange(new Rect(0, 0, 20, 20));

		Assert.AreEqual(new Rect(0, 0, 20, 20), LayoutInformation.GetLayoutSlot(c1));
		Assert.AreEqual(new Vector3(0, 5, 0), c1.ActualOffset);
		Assert.AreEqual(new Size(20, 10), c1.RenderSize);

		Assert.HasCount(1, SUT.Children);
	}

	[TestMethod]
	public void When_One_Child_With_VerticalCenterAlignment_HorizontalCenterAlignment_and_Variable_Height_and_Variable_Width()
	{
		var SUT = new Grid() { Name = "test" };

		var c1 = new View
		{
			Name = "Child01",
			RequestedDesiredSize = new Size(10, 10),
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Center,
		};

		SUT.Children.Add(c1);

		SUT.Measure(new Size(20, 20));
		var measuredSize = SUT.DesiredSize;
		Assert.AreEqual(new Size(10, 10), measuredSize);
		Assert.AreEqual(new Size(10, 10), c1.RequestedDesiredSize);

		SUT.Arrange(new Rect(0, 0, 20, 20));

		Assert.AreEqual(new Rect(0, 0, 20, 20), LayoutInformation.GetLayoutSlot(c1));
		Assert.AreEqual(new Vector3(5, 5, 0), c1.ActualOffset);
		Assert.AreEqual(new Size(10, 10), c1.RenderSize);

		Assert.HasCount(1, SUT.Children);
	}

	[TestMethod]
	public void When_One_Child_Centered_And_Auto_row_And_Fixed_Column()
	{
		var SUT = new Grid() { Name = "test" };

		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
		SUT.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

		var c1 = new View
		{
			Name = "Child01",

			// The selector will return a different size if the measured size if greater than the column size.
			// This should not happen, as the child should not be measured with a Width greater
			// than 20, as the column is of size 20.
			DesiredSizeSelector = s => s.Width > 20 ? new Size(20, 5) : new Size(10, 10),

			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Center,
		};

		SUT.Children.Add(c1);

		SUT.Measure(new Size(100, 20));
		var measuredSize = SUT.DesiredSize;
		Assert.AreEqual(new Size(20, 10), measuredSize);
		Assert.AreEqual(new Size(10, 10), c1.DesiredSize);

		SUT.Arrange(new Rect(0, 0, 100, 20));

		Assert.AreEqual(new Size(10, 10), c1.DesiredSize);
		Assert.AreEqual(new Rect(0, 0, 20, 10), LayoutInformation.GetLayoutSlot(c1));
		Assert.AreEqual(new Vector3(5, 0, 0), c1.ActualOffset);
		Assert.AreEqual(new Size(10, 10), c1.RenderSize);

		Assert.HasCount(1, SUT.Children);
	}

	[TestMethod]
	[RequiresScaling(1f)]
	public void When_One_Child_Centered_And_Auto_row_And_Star_Column()
	{
		var SUT = new Grid() { Name = "test" };

		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		SUT.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

		var c1 = new View
		{
			Name = "Child01",

			// The selector will return a different size if the measured size if greater than the column size.
			// This should not happen, as the child should not be measured with a Width greater
			// than 20, as the column is of size 20.
			DesiredSizeSelector = s => s.Width > 20 ? new Size(20, 5) : new Size(10, 10),

			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Center,
		};

		Grid.SetColumn(c1, 1);
		SUT.Children.Add(c1);

		var c2 = new View
		{
			Name = "Child02",
			RequestedDesiredSize = new Size(11, 11),
		};

		SUT.Children.Add(c2);

		SUT.Measure(new Size(100, 20));
		var measuredSize = SUT.DesiredSize;
		Assert.AreEqual(new Size(31, 11), measuredSize);
		Assert.AreEqual(new Size(20, 5), c1.DesiredSize);
		Assert.AreEqual(new Size(11, 11), c2.RequestedDesiredSize);
		Assert.AreEqual(1, c1.MeasureCallCount);
		Assert.AreEqual(1, c2.MeasureCallCount); // The measure count is 1 beceause the grid has a recognized pattern (Nx1). It would be 2 otherwise.
		Assert.AreEqual(0, c1.ArrangeCallCount);
		Assert.AreEqual(0, c2.ArrangeCallCount);

		SUT.Arrange(new Rect(0, 0, 100, 20));

		Assert.AreEqual(new Size(20, 5), c1.DesiredSize);
		Assert.AreEqual(new Size(11, 11), c2.RequestedDesiredSize);
		Assert.AreEqual(new Rect(11, 0, 89, 11), LayoutInformation.GetLayoutSlot(c1));
#if __ANDROID__ || __APPLE_UIKIT__ || __WASM__
		Assert.AreEqual(new Vector3(45.5f, 3, 0), c1.ActualOffset);
#else
		Assert.AreEqual(new Vector3(46, 3, 0), c1.ActualOffset);
#endif
		Assert.AreEqual(new Size(20, 5), c1.RenderSize);
		Assert.AreEqual(new Rect(0, 0, 11, 11), LayoutInformation.GetLayoutSlot(c2));
		Assert.AreEqual(1, c1.MeasureCallCount, "c1.MeasureCallCount");
		Assert.AreEqual(1, c2.MeasureCallCount, "c2.MeasureCallCount"); // The measure count is 1 because the grid has a recognized pattern (Nx1). It would be 2 otherwise.
#if __APPLE_UIKIT__
		Assert.AreEqual(0, c1.ArrangeCallCount, "c1.ArrangeCallCount");
		Assert.AreEqual(0, c2.ArrangeCallCount, "c2.ArrangeCallCount");
#else
		Assert.AreEqual(1, c1.ArrangeCallCount, "c1.ArrangeCallCount");
		Assert.AreEqual(1, c2.ArrangeCallCount, "c2.ArrangeCallCount");
#endif

		Assert.HasCount(2, SUT.Children);
	}

	[DataRow("cc", 0d, "17,17,6,6", null)]
	[DataRow("cc", 2d, "15,15,10,10", null)]
	[DataRow("cc", 15d, "15,15,10,10", null)]
	[DataRow("cc", 16d, "16,16,8,8", null)]
	[DataRow("cc", 18d, "17,17,6,6", "1,1,4,4")]
	[DataRow("cc", 20d, "17,17,6,6", "3,3,0,0")]
	[DataRow("cc", 30d, "17,17,6,6", "empty")]
	[DataRow("cc", 50d, "17,17,6,6", "empty")]
	[DataRow("cb", 2d, "15,28,10,10", null)]
	[DataRow("ct", 2d, "15,2,10,10", null)]
	[DataRow("ss", 0d, "0,0,40,40", null)]
	[DataRow("ss", 2d, "2,2,36,36", null)]
	[DataRow("ss", 16d, "16,16,8,8", null)]
	[DataRow("ss", 18d, "18,18,6,6", "0,0,4,4")]
	[DataRow("ss", 20d, "20,20,6,6", "0,0,0,0")]
	[DataRow("ss", 30d, "30,30,6,6", "0,0,0,0")]
	[DataRow("ss", 50d, "50,50,6,6", "0,0,0,0")]
	[DataRow("lt", 2d, "2,2,10,10", null)]
	[DataRow("rt", 2d, "28,2,10,10", null)]
	[DataRow("rb", 2d, "28,28,10,10", null)]
	[DataRow("lb", 2d, "2,28,10,10", null)]
	[TestMethod]
	[Ignore("https://github.com/unoplatform/uno/issues/2733")]
	public void When_One_Child_Alignment(string alignment, double margin, string expected, string expectedClippedFrame)
	{
		var SUT = new Grid { Name = "test" };

		HorizontalAlignment GetH()
		{
			switch (alignment[0])
			{
				case 's': return HorizontalAlignment.Stretch;
				case 'l': return HorizontalAlignment.Left;
				case 'c': return HorizontalAlignment.Center;
				case 'r': return HorizontalAlignment.Right;
			}
			return default;
		}

		VerticalAlignment GetV()
		{
			switch (alignment[1])
			{
				case 's': return VerticalAlignment.Stretch;
				case 't': return VerticalAlignment.Top;
				case 'c': return VerticalAlignment.Center;
				case 'b': return VerticalAlignment.Bottom;
			}
			return default;
		}

		Rect GetRect(string s)
		{
			var parts = s.Split(',').Select(s => double.Parse(s, CultureInfo.InvariantCulture)).ToArray();
			return s == "empty" ? Rect.Empty : new Rect(parts[0], parts[1], parts[2], parts[3]);
		}

		var c1 = new View
		{
			Name = "Child01",
			HorizontalAlignment = GetH(),
			VerticalAlignment = GetV(),
			MinWidth = 6,
			MinHeight = 6,
			Margin = new Thickness(margin),
		};

		SUT.Children.Add(c1);


		SUT.Children.Should().HaveCount(1);

		var availableSize = new Size(40, 40);
		var unclippedExpectedSize = new Size(6d + margin * 2, 6d + margin * 2);
		var expectedDesiredSize = new Size(Math.Min(unclippedExpectedSize.Width, availableSize.Width), Math.Min(unclippedExpectedSize.Height, availableSize.Height));

		SUT.Measure(availableSize);

		SUT.DesiredSize.Should().Be(expectedDesiredSize);
#if !WINAPPSDK
		GetUnclippedDesiredSize(SUT).Should().Be(expectedDesiredSize);
#endif
		c1.DesiredSize.Should().Be(new Size(Math.Max(margin * 2, expectedDesiredSize.Width), Math.Max(margin * 2, expectedDesiredSize.Height)));
#if !WINAPPSDK
		GetUnclippedDesiredSize(c1).Should().Be(new Size(6d, 6d)); // Unclipped doesn't include margins!
#endif

		SUT.Arrange(new Rect(default, availableSize));

		new Rect(c1.ActualOffset.X, c1.ActualOffset.Y, c1.RenderSize.Width, c1.RenderSize.Height).Should().Be(GetRect(expected));
		var expectedClippedFrameRect = expectedClippedFrame == null
			? new Rect(default, new Size(GetRect(expected).Width, GetRect(expected).Height))
			: GetRect(expectedClippedFrame);
		LayoutInformation.GetLayoutSlot(c1).Should().Be(expectedClippedFrameRect);
	}

	[TestMethod]
	public void When_One_Child_and_Measure_Bigger_than_arrange()
	{
		var SUT = new Grid() { Name = "test" };

		var c1 = new View
		{
			Name = "Child01",
			DesiredSizeSelector = s => s,
		};

		SUT.Children.Add(c1);

		SUT.Measure(new Size(100, 100));
		var measuredSize = SUT.DesiredSize;
		Assert.AreEqual(new Size(100, 100), measuredSize);
		Assert.AreEqual(new Size(100, 100), c1.DesiredSize);

		SUT.Arrange(new Rect(0, 0, 20, 20));

		Assert.AreEqual(new Rect(0, 0, 100, 100), LayoutInformation.GetLayoutSlot(c1));

		Assert.HasCount(1, SUT.Children);
	}
}
