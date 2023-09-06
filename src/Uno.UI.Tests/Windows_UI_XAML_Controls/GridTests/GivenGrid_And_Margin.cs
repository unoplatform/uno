using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Foundation;
using FluentAssertions;
using FluentAssertions.Execution;
using Uno.UI.Controls.Legacy;

namespace Uno.UI.Tests.GridTests
{
	[TestClass]
	public partial class GivenGrid_And_Margin : Context
	{
		private partial class View : FrameworkElement
		{
		}

		[TestMethod]
		public void When_One_Child_With_Margin_5()
		{
			var SUT = new Grid() { Name = "test" };

			var c1 = new View
			{
				Name = "Child01",
				Margin = new Thickness(5)
			}
				.GridPosition(0, 0);

			SUT.AddChild(c1);

			SUT.Measure(new Size(20, 20));

			SUT.DesiredSize.Should().Be(new Size(10, 10));
			SUT.UnclippedDesiredSize.Should().Be(new Size(10, 10));
			c1.DesiredSize.Should().Be(new Size(10, 10));
			c1.UnclippedDesiredSize.Should().Be(new Size(0, 0)); // UnclippedDesiredSize excludes margins

			SUT.Arrange(new Rect(0, 0, 20, 20));

			SUT.Arranged.Should().Be((Rect)"0,0,20,20");
			SUT.ClippedFrame.Should().Be((Rect)"0,0,20,20");
			c1.Arranged.Should().Be((Rect)"5,5,10,10");
			c1.ClippedFrame.Should().Be((Rect)"0,0,10,10");

			SUT.GetChildren().Should().HaveCount(1);
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
			}
				.GridPosition(0, 0);

			SUT.AddChild(c1);

			SUT.Measure(new Size(20, 20));

			SUT.DesiredSize.Should().Be(new Size(8, 10));
			SUT.UnclippedDesiredSize.Should().Be(new Size(8, 10));
			c1.DesiredSize.Should().Be(new Size(4, 6));
			c1.UnclippedDesiredSize.Should().Be(new Size(0, 0)); // UnclippedDesiredSize excludes margins

			SUT.Arrange(new Rect(0, 0, 20, 20));

			SUT.Arranged.Should().Be((Rect)"0,0,20,20");
			SUT.ClippedFrame.Should().Be((Rect)"0,0,20,20");
			c1.Arranged.Should().Be((Rect)"3,4,12,10");
			c1.ClippedFrame.Should().Be((Rect)"0,0,12,10");

			SUT.GetChildren().Should().HaveCount(1);
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
			}
				.GridPosition(0, 0);

			SUT.AddChild(c1);

			SUT.Measure(new Size(8, 8));

			SUT.DesiredSize.Should().Be(new Size(8, 8));
			SUT.UnclippedDesiredSize.Should().Be(new Size(8, 10)); // This should be 8, 8
			c1.DesiredSize.Should().Be(new Size(4, 6)); // This should be 4, 4
			c1.UnclippedDesiredSize.Should().Be(new Size(0, 0)); // UnclippedDesiredSize excludes margins

			SUT.Arrange(new Rect(0, 0, 8, 8));

			SUT.Arranged.Should().Be((Rect)"0,0,8,8");
			SUT.ClippedFrame.Should().Be((Rect)"0,0,8,8");
			c1.Arranged.Should().Be((Rect)"3,4,0,0");
			c1.ClippedFrame.Should().Be((Rect)"0,0,0,0");

			SUT.GetChildren().Should().HaveCount(1);
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
			}
				.GridPosition(0, 0);

			SUT.AddChild(c1);

			SUT.Measure(new Size(20, 20));

			SUT.DesiredSize.Should().Be(new Size(10, 20));
			SUT.UnclippedDesiredSize.Should().Be(new Size(10, 30)); // This should be 10, 20
			c1.DesiredSize.Should().Be(new Size(10, 30)); // This should be 10, 20
			c1.UnclippedDesiredSize.Should().Be(new Size(10, 10)); // UnclippedDesiredSize excludes margins

			SUT.Arrange(new Rect(0, 0, 50, 50));

			c1.Arranged.Should().Be((Rect)"20,5,10,10");
			c1.ClippedFrame.Should().Be((Rect)"0,0,10,10");

			SUT.GetChildren().Should().HaveCount(1);
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
			}
				.GridPosition(0, 0);

			SUT.AddChild(c1);

			SUT.Measure(new Size(20, 20));

			SUT.DesiredSize.Should().Be(new Size(10, 20));
			SUT.UnclippedDesiredSize.Should().Be(new Size(10, 30)); // This should be 10, 20
			c1.DesiredSize.Should().Be(new Size(10, 30)); // This should be 10, 20
			c1.UnclippedDesiredSize.Should().Be(new Size(10, 10)); // UnclippedDesiredSize excludes margins

			SUT.Arrange(new Rect(0, 0, 50, 50));

			SUT.Arranged.Should().Be((Rect)"0,0,50,50");
			SUT.ClippedFrame.Should().Be((Rect)"0,0,50,50");
			c1.Arranged.Should().Be((Rect)"20,10,10,10");
			c1.ClippedFrame.Should().Be((Rect)"0,0,10,10");

			SUT.GetChildren().Should().HaveCount(1);
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
			}
				.GridPosition(0, 0);

			SUT.AddChild(c1);

			SUT.Measure(new Size(20, 20));

			SUT.DesiredSize.Should().Be(new Size(10, 20));
			SUT.UnclippedDesiredSize.Should().Be(new Size(10, 30)); // This should be 10, 20
			c1.DesiredSize.Should().Be(new Size(10, 30)); // This should be 10, 20
			c1.UnclippedDesiredSize.Should().Be(new Size(10, 10)); // UnclippedDesiredSize excludes margins

			SUT.Arrange(new Rect(0, 0, 50, 50));
			c1.Arranged.Should().Be((Rect)"20,0,10,10");

			SUT.GetChildren().Should().HaveCount(1);
		}


		[TestMethod]
		public void When_One_Fixed_Size_Child_With_Margin_Right_And_Stretch()
		{
			var SUT = new Grid() { Name = "test" };


			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "auto" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });

			var c1 = new View
			{
				Name = "Child01",
				Margin = new Thickness(0, 0, 50, 0),
				HorizontalAlignment = HorizontalAlignment.Stretch,
				VerticalAlignment = VerticalAlignment.Stretch,
				Height = 50,
				Width = 50,
			}
				.GridPosition(0, 1);
			SUT.AddChild(c1);

			SUT.Measure(new Size(300, 300));
			SUT.DesiredSize.Should().Be(new Size(100, 50));
			c1.DesiredSize.Should().Be(new Size(100, 50));
			SUT.UnclippedDesiredSize.Should().Be(new Size(100, 50));

			SUT.Arrange(new Rect(0, 0, 300, 300));

			c1.Arranged.Should().Be(new Rect(100, 125, 50, 50));
			c1.ClippedFrame.Should().Be(new Rect(0, 0, 50, 50));
			SUT.GetChildren().Should().HaveCount(1);
		}
	}
}
