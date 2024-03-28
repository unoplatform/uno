using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
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

		protected override Size MeasureOverride(Size availableSize)
		{
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
#if __IOS__
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
#if __IOS__
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
#if __IOS__
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
#if __IOS__
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
#if __IOS__
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
#if __IOS__
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
#if __IOS__
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
#if __IOS__
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
#if __IOS__
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
#if __IOS__
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
#if __IOS__
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
#if __IOS__
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
#if __IOS__
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
#if __IOS__
	[Ignore("Fails on iOS.")]
#elif __ANDROID__
	[Ignore("Fails on Android in CI, but passes locally.")]
#endif
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

#if __ANDROID__ || __IOS__ || __MACOS__ || __WASM__
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
#if __IOS__
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

#if __ANDROID__ || __IOS__ || __MACOS__
		c1.DesiredSize.Should().Be(new Size(16.0 / 3.0, 10));
#else
		c1.DesiredSize.Should().Be(new Size(5, 10));
#endif
		c2.DesiredSize.Should().Be(new Size(10, 10));

#if __ANDROID__ || __IOS__ || __MACOS__
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
#if __IOS__
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
#if __IOS__
	[Ignore("Fails on iOS.")]
#elif __ANDROID__
	[Ignore("Fails on Android in CI, but passes locally.")]
#endif
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
#if __ANDROID__ || __IOS__ || __MACOS__
		SUT.DesiredSize.Should().Be(new Size(52.0 / 3.0, 5));
#elif __WASM__
		SUT.DesiredSize.Should().Be(new Size(17.5, 5));
#else
		SUT.DesiredSize.Should().Be(new Size(17, 5));
#endif
		//SUT.UnclippedDesiredSize.Should().Be(new Size(20, 5));

#if __ANDROID__ || __IOS__ || __MACOS__
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

#if __ANDROID__ || __IOS__ || __MACOS__
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
#if __IOS__
	[Ignore("Fails on iOS.")]
#endif
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

#if __ANDROID__ || __IOS__ || __MACOS__ || __WASM__
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
#if __IOS__
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

#if __ANDROID__ || __IOS__ || __MACOS__
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
#if __IOS__
	[Ignore("Fails on iOS.")]
#endif
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

#if __ANDROID__ || __IOS__ || __MACOS__ || __WASM__
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
#if __IOS__
	[Ignore("Fails on iOS.")]
#endif
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
#if __ANDROID__ || __IOS__ || __MACOS__ || __WASM__
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
#if __IOS__
	[Ignore("Fails on iOS.")]
#endif
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
#if __IOS__
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

#if HAS_UNO && !(__ANDROID__ || __IOS__ || __MACOS__)
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
#if __IOS__
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
#if __IOS__
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
#if __IOS__
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
#if __IOS__
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
#if __IOS__
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
#if __ANDROID__ || __IOS__ || __MACOS__
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
}
