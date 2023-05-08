using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml.Controls;
using System.Linq;
using Windows.Foundation;
using FluentAssertions;
using FluentAssertions.Execution;
using Windows.UI.Xaml;
using Uno.UI.Controls.Legacy;

namespace Uno.UI.Tests.GridTests
{
	[TestClass]
	public partial class GivenGrid_And_AutoSized : Context
	{
		private partial class View : FrameworkElement
		{
		}

		[TestMethod]
		public void When_One_Auto_Columns_and_one_star_and_two_children()
		{
			var SUT = new Grid() { Name = "test" };

			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "auto" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });

			var c1 = SUT.AddChild(
				new View { Name = "Child01", RequestedDesiredSize = new Size(5, 5) }
				.GridPosition(0, 0)
			);
			var c2 = SUT.AddChild(
				new View { Name = "Child02", RequestedDesiredSize = new Size(10, 10) }
				.GridPosition(0, 1)
			);

			SUT.Measure(new Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Size(15, 10), measuredSize);
			Assert.AreEqual(new Size(5, 5), c1.RequestedDesiredSize);
			Assert.AreEqual(new Size(10, 10), c2.RequestedDesiredSize);

			SUT.Arrange(new Rect(0, 0, 20, 20));

			Assert.AreEqual(new Rect(0, 0, 5, 20), c1.Arranged);
			Assert.AreEqual(new Rect(5, 0, 15, 20), c2.Arranged);

			Assert.AreEqual(2, SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_Two_Auto_Columns_two_children()
		{
			var SUT = new Grid() { Name = "test" };

			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "auto" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "auto" });

			var c1 = SUT.AddChild(
				new View { Name = "Child01", RequestedDesiredSize = new Size(5, 5) }
				.GridPosition(0, 0)
			);
			var c2 = SUT.AddChild(
				new View { Name = "Child02", RequestedDesiredSize = new Size(10, 10) }
				.GridPosition(0, 1)
			);

			SUT.Measure(new Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Size(15, 10), measuredSize);
			Assert.AreEqual(new Size(5, 5), c1.RequestedDesiredSize);
			Assert.AreEqual(new Size(10, 10), c2.RequestedDesiredSize);

			SUT.Arrange(new Rect(0, 0, 20, 20));

			Assert.AreEqual(new Rect(0, 0, 5, 20), c1.Arranged);
			Assert.AreEqual(new Rect(5, 0, 10, 20), c2.Arranged);

			//Assert.AreEqual(new Windows.Foundation.Rect(0, 0, 20, 20), Windows.Foundation.default(Size));
			Assert.AreEqual(2, SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_One_Auto_and_one_abs_and_one_star_and_three_children()
		{
			using var _ = new AssertionScope();

			var SUT = new Grid() { Name = "test" };

			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "auto" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "6" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });

			var c1 = SUT.AddChild(
				new View { Name = "Child01", RequestedDesiredSize = new Size(5, 5) }
				.GridPosition(0, 0)
			);
			var c2 = SUT.AddChild(
				new View { Name = "Child02", RequestedDesiredSize = new Size(10, 10) }
				.GridPosition(0, 1)
			);
			var c3 = SUT.AddChild(
				new View { Name = "Child03", RequestedDesiredSize = new Size(10, 7) }
				.GridPosition(0, 2)
			);

			SUT.Measure(new Size(20, 20));
			var measuredSize = SUT.DesiredSize;

			measuredSize.Should().Be(new Size(20, 10));
			c1.DesiredSize.Should().Be(new Size(5, 5));
			c2.DesiredSize.Should().Be(new Size(10, 10));
			c3.DesiredSize.Should().Be(new Size(10, 7));

			SUT.Arrange(new Rect(0, 0, 20, 20));

			c1.Arranged.Should().Be(new Rect(0, 0, 5, 20));
			c2.Arranged.Should().Be(new Rect(5, 0, 10, 20));
			c3.Arranged.Should().Be(new Rect(15, 0, 10, 20));

			SUT.GetChildren().Should().HaveCount(3);
		}

		[TestMethod]
		public void When_Nine_grid_and_one_auto_cell_and_three_children()
		{
			using var _ = new AssertionScope();

			var SUT = new Grid() { Name = "test" };

			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "auto" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });

			SUT.RowDefinitions.Add(new RowDefinition { Height = "*" });
			SUT.RowDefinitions.Add(new RowDefinition { Height = "auto" });
			SUT.RowDefinitions.Add(new RowDefinition { Height = "*" });

			var c1 = SUT.AddChild(
				new View { Name = "Child01", RequestedDesiredSize = new Size(5, 5) }
				.GridPosition(0, 0)
			);
			var c2 = SUT.AddChild(
				new View { Name = "Child02", RequestedDesiredSize = new Size(3, 4) }
				.GridPosition(1, 1)
			);
			var c3 = SUT.AddChild(
				new View { Name = "Child03", RequestedDesiredSize = new Size(8, 9) }
				.GridPosition(2, 2)
			);

			SUT.Measure(new Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			measuredSize.Should().Be(new Size(16, 18));

			c1.DesiredSize.Should().Be(new Size(5, 5));
			c2.DesiredSize.Should().Be(new Size(3, 4));
			c3.DesiredSize.Should().Be(new Size(8, 9));

			SUT.Arrange(new Rect(0, 0, 20, 20));

			c1.Arranged.Should().Be(new Rect(0, 0, 8.5f, 7));
			c2.Arranged.Should().Be(new Rect(8.5f, 7, 3, 4));
			c3.Arranged.Should().Be(new Rect(11.5f, 11, 8.5f, 9));

			SUT.GetChildren().Should().HaveCount(3);
		}

		[TestMethod]
		public void When_Quad_two_auto_and_four_children()
		{
			var SUT = new Grid() { Name = "test" };

			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "auto" });

			SUT.RowDefinitions.Add(new RowDefinition { Height = "auto" });
			SUT.RowDefinitions.Add(new RowDefinition { Height = "*" });

			var c1 = SUT.AddChild(
				new View { Name = "Child01", RequestedDesiredSize = new Size(3, 4) }
				.GridPosition(0, 0)
			);
			var c2 = SUT.AddChild(
				new View { Name = "Child02", RequestedDesiredSize = new Size(4, 5) }
				.GridPosition(0, 1)
			);
			var c3 = SUT.AddChild(
				new View { Name = "Child03", RequestedDesiredSize = new Size(6, 7) }
				.GridPosition(1, 0)
			);
			var c4 = SUT.AddChild(
				new View { Name = "Child04", RequestedDesiredSize = new Size(8, 9) }
				.GridPosition(1, 1)
			);

			SUT.Measure(new Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Size(14, 14), measuredSize);

			Assert.AreEqual(new Size(3, 4), c1.RequestedDesiredSize);
			Assert.AreEqual(new Size(4, 5), c2.RequestedDesiredSize);
			Assert.AreEqual(new Size(6, 7), c3.RequestedDesiredSize);
			Assert.AreEqual(new Size(8, 9), c4.RequestedDesiredSize);

			SUT.Arrange(new Rect(0, 0, 20, 20));

			Assert.AreEqual(new Rect(0, 0, 12, 5), c1.Arranged);
			Assert.AreEqual(new Rect(12, 0, 8, 5), c2.Arranged);
			Assert.AreEqual(new Rect(0, 5, 12, 15), c3.Arranged);
			Assert.AreEqual(new Rect(12, 5, 8, 15), c4.Arranged);

			Assert.AreEqual(4, SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_Nine_grid_and_one_auto_cell_and_four_children()
		{
			using var _ = new AssertionScope();

			var SUT = new Grid() { Name = "test" };

			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "auto" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });

			SUT.RowDefinitions.Add(new RowDefinition { Height = "*" });
			SUT.RowDefinitions.Add(new RowDefinition { Height = "*" });
			SUT.RowDefinitions.Add(new RowDefinition { Height = "auto" });

			var c1 = SUT.AddChild(
				new View { Name = "Child01", RequestedDesiredSize = new Size(5, 5) }
				.GridPosition(0, 0)
			);
			var c2 = SUT.AddChild(
				new View { Name = "Child02", RequestedDesiredSize = new Size(3, 4) }
				.GridPosition(1, 1)
			);
			var c3 = SUT.AddChild(
				new View { Name = "Child03", RequestedDesiredSize = new Size(8, 9) }
				.GridPosition(2, 2)
			);
			var c4 = SUT.AddChild(
				new View { Name = "Child04", RequestedDesiredSize = new Size(5, 5) }
				.GridPosition(1, 0)
			);

			SUT.Measure(new Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			measuredSize.Should().Be(new Size(16, 19));

			c1.DesiredSize.Should().Be(new Size(5, 5));
			c2.DesiredSize.Should().Be(new Size(3, 4));
			c3.DesiredSize.Should().Be(new Size(8, 9));
			c4.DesiredSize.Should().Be(new Size(5, 5));

			SUT.Arrange(new Rect(0, 0, 20, 20));

			c1.Arranged.Should().Be(new Rect(0, 0, 8.5f, 5.5f));
			c2.Arranged.Should().Be(new Rect(8.5f, 5.5f, 3, 5.5f));
			c3.Arranged.Should().Be(new Rect(11.5f, 11, 8.5f, 9));
			c4.Arranged.Should().Be(new Rect(0, 5.5f, 8.5f, 5.5f));

			SUT.GetChildren().Should().HaveCount(4);
		}

		[TestMethod]
		public void When_Three_Rows_One_Auto_Two_Fixed_And_Row_Span_Full()
		{
			using var _ = new AssertionScope();

			var SUT = new Grid() { Name = "test", Height = 44 };

			SUT.RowDefinitions.Add(new RowDefinition { Height = "18" });
			SUT.RowDefinitions.Add(new RowDefinition { Height = "auto" });
			SUT.RowDefinitions.Add(new RowDefinition { Height = "18" });

			var c1 = SUT.AddChild(
				new View { Name = "Child01", RequestedDesiredSize = new Size(0, 10) }
				.GridPosition(1, 0)
			);
			var c2 = SUT.AddChild(
				new View { Name = "Child02", RequestedDesiredSize = new Size(30, 0) }
				.GridPosition(1, 0)
			);
			var c3 = SUT.AddChild(
				new View { Name = "Child03", RequestedDesiredSize = new Size(8, 24) }
				.GridPosition(0, 0)
				.GridRowSpan(3)
			);

			SUT.Measure(new Size(100, 44));
			var measuredSize = SUT.DesiredSize;
			measuredSize.Should().Be(new Size(30, 44));

			c1.DesiredSize.Should().Be(new Size(0, 10));
			c2.DesiredSize.Should().Be(new Size(30, 0));
			c3.DesiredSize.Should().Be(new Size(8, 24));

			SUT.Arrange(new Rect(0, 0, 20, 44));

			c1.Arranged.Should().Be(new Rect(0, 18, 30, 10));
			c2.Arranged.Should().Be(new Rect(0, 18, 30, 10));
			c3.Arranged.Should().Be(new Rect(0, 0, 30, 46));

			SUT.GetChildren().Should().HaveCount(3);
		}

		[TestMethod]
		public void When_Three_Colums_One_Auto_Two_Fixed_And_Column_Span_Full()
		{
			using var _ = new AssertionScope();

			var SUT = new Grid() { Name = "test", Width = 44 };

			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "18" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "auto" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "18" });

			var c1 = SUT.AddChild(
				new View { Name = "Child01", RequestedDesiredSize = new Size(10, 0) }
				.GridPosition(0, 1)
			);
			var c2 = SUT.AddChild(
				new View { Name = "Child02", RequestedDesiredSize = new Size(0, 30) }
				.GridPosition(0, 1)
			);
			var c3 = SUT.AddChild(
				new View { Name = "Child03", RequestedDesiredSize = new Size(24, 8) }
				.GridPosition(0, 0)
				.GridColumnSpan(3)
			);

			SUT.Measure(new Size(44, 100));
			var measuredSize = SUT.DesiredSize;
			measuredSize.Should().Be(new Size(44, 30));

			c1.DesiredSize.Should().Be(new Size(10, 0));
			c2.DesiredSize.Should().Be(new Size(0, 30));
			c3.DesiredSize.Should().Be(new Size(24, 8));

			SUT.Arrange(new Rect(0, 0, 44, 20));

			c1.Arranged.Should().Be(new Rect(18, 0, 10, 30));
			c2.Arranged.Should().Be(new Rect(18, 0, 10, 30));
			c3.Arranged.Should().Be(new Rect(0, 0, 46, 30));

			SUT.GetChildren().Should().HaveCount(3);
		}
	}
}
