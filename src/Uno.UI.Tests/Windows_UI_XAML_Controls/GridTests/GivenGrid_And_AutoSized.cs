using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno;
using Uno.Extensions;
using Uno.Logging;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Uno.Disposables;
using System.Text;
using System.Threading.Tasks;
using View = Windows.UI.Xaml.FrameworkElement;


namespace Uno.UI.Tests.GridTests
{
	[TestClass]
	public class GivenGrid_And_AutoSized : Context
	{
		[TestMethod]
		public void When_One_Auto_Columns_and_one_star_and_two_children()
		{
			var SUT = new Grid() { Name = "test" };

			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "auto" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });

			var c1 = SUT.AddChild(
				new View { Name = "Child01", RequestedDesiredSize = new Windows.Foundation.Size(5, 5) }
				.GridPosition(0, 0)
			);
			var c2 = SUT.AddChild(
				new View { Name = "Child02", RequestedDesiredSize = new Windows.Foundation.Size(10, 10) }
				.GridPosition(0, 1)
			);

			SUT.Measure(new Windows.Foundation.Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Windows.Foundation.Size(15, 10), measuredSize);
			Assert.AreEqual(new Windows.Foundation.Size(5, 5), c1.RequestedDesiredSize);
			Assert.AreEqual(new Windows.Foundation.Size(10, 10), c2.RequestedDesiredSize);

			SUT.Arrange(new Windows.Foundation.Rect(0, 0, 20, 20));

			Assert.AreEqual(new Windows.Foundation.Rect(0, 0, 5, 20), c1.Arranged);
			Assert.AreEqual(new Windows.Foundation.Rect(5, 0, 15, 20), c2.Arranged);
			
			Assert.AreEqual(2, SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_Two_Auto_Columns_two_children()
		{
			var SUT = new Grid() { Name = "test" };

			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "auto" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "auto" });

			var c1 = SUT.AddChild(
				new View { Name = "Child01", RequestedDesiredSize = new Windows.Foundation.Size(5, 5) }
				.GridPosition(0, 0)
			);
			var c2 = SUT.AddChild(
				new View { Name = "Child02", RequestedDesiredSize = new Windows.Foundation.Size(10, 10) }
				.GridPosition(0, 1)
			);

			SUT.Measure(new Windows.Foundation.Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Windows.Foundation.Size(15, 10), measuredSize);
			Assert.AreEqual(new Windows.Foundation.Size(5, 5), c1.RequestedDesiredSize);
			Assert.AreEqual(new Windows.Foundation.Size(10, 10), c2.RequestedDesiredSize);

			SUT.Arrange(new Windows.Foundation.Rect(0, 0, 20, 20));

			Assert.AreEqual(new Windows.Foundation.Rect(0, 0, 5, 20), c1.Arranged);
			Assert.AreEqual(new Windows.Foundation.Rect(5, 0, 10, 20), c2.Arranged);

			//Assert.AreEqual(new Windows.Foundation.Rect(0, 0, 20, 20), Windows.Foundation.default(Size));
			Assert.AreEqual(2, SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_One_Auto_and_one_abs_and_one_star_and_three_children()
		{
			var SUT = new Grid() { Name = "test" };

			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "auto" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "6" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });

			var c1 = SUT.AddChild(
				new View { Name = "Child01", RequestedDesiredSize = new Windows.Foundation.Size(5, 5) }
				.GridPosition(0, 0)
			);
			var c2 = SUT.AddChild(
				new View { Name = "Child02", RequestedDesiredSize = new Windows.Foundation.Size(10, 10) }
				.GridPosition(0, 1)
			);
			var c3 = SUT.AddChild(
				new View { Name = "Child03", RequestedDesiredSize = new Windows.Foundation.Size(10, 7) }
				.GridPosition(0, 2)
			);

			SUT.Measure(new Windows.Foundation.Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Windows.Foundation.Size(20, 10), measuredSize);
			Assert.AreEqual(new Windows.Foundation.Size(5, 5), c1.RequestedDesiredSize);
			Assert.AreEqual(new Windows.Foundation.Size(10, 10), c2.RequestedDesiredSize);
			Assert.AreEqual(new Windows.Foundation.Size(10, 7), c3.RequestedDesiredSize);

			SUT.Arrange(new Windows.Foundation.Rect(0, 0, 20, 20));

			Assert.AreEqual(new Windows.Foundation.Rect(0, 0, 5, 20), c1.Arranged);
			Assert.AreEqual(new Windows.Foundation.Rect(5, 0, 6, 20), c2.Arranged);
			Assert.AreEqual(new Windows.Foundation.Rect(11, 0, 9, 20), c3.Arranged);

			//Assert.AreEqual(new Windows.Foundation.Rect(0, 0, 20, 20), Windows.Foundation.default(Size));
			Assert.AreEqual(3, SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_Nine_grid_and_one_auto_cell_and_three_children()
		{
			var SUT = new Grid() { Name = "test" };

			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "auto" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });

			SUT.RowDefinitions.Add(new RowDefinition { Height = "*" });
			SUT.RowDefinitions.Add(new RowDefinition { Height = "auto" });
			SUT.RowDefinitions.Add(new RowDefinition { Height = "*" });

			var c1 = SUT.AddChild(
				new View { Name = "Child01", RequestedDesiredSize = new Windows.Foundation.Size(5, 5) }
				.GridPosition(0, 0)
			);
			var c2 = SUT.AddChild(
				new View { Name = "Child02", RequestedDesiredSize = new Windows.Foundation.Size(3, 4) }
				.GridPosition(1, 1)
			);
			var c3 = SUT.AddChild(
				new View { Name = "Child03", RequestedDesiredSize = new Windows.Foundation.Size(8, 9) }
				.GridPosition(2, 2)
			);

			SUT.Measure(new Windows.Foundation.Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Windows.Foundation.Size(19, 20), measuredSize);

			Assert.AreEqual(new Windows.Foundation.Size(5, 5), c1.RequestedDesiredSize);
			Assert.AreEqual(new Windows.Foundation.Size(3, 4), c2.RequestedDesiredSize);
			Assert.AreEqual(new Windows.Foundation.Size(8, 9), c3.RequestedDesiredSize);

			SUT.Arrange(new Windows.Foundation.Rect(0, 0, 20, 20));

			Assert.AreEqual(new Windows.Foundation.Rect(0, 0, 8.5f, 8), c1.Arranged);
			Assert.AreEqual(new Windows.Foundation.Rect(8.5f, 8, 3, 4), c2.Arranged);
			Assert.AreEqual(new Windows.Foundation.Rect(11.5f, 12, 8.5f, 8), c3.Arranged);

			Assert.AreEqual(3, SUT.GetChildren().Count());
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
				new View { Name = "Child01", RequestedDesiredSize = new Windows.Foundation.Size(3, 4) }
				.GridPosition(0, 0)
			);
			var c2 = SUT.AddChild(
				new View { Name = "Child02", RequestedDesiredSize = new Windows.Foundation.Size(4, 5) }
				.GridPosition(0, 1)
			);
			var c3 = SUT.AddChild(
				new View { Name = "Child03", RequestedDesiredSize = new Windows.Foundation.Size(6, 7) }
				.GridPosition(1, 0)
			);
			var c4 = SUT.AddChild(
				new View { Name = "Child04", RequestedDesiredSize = new Windows.Foundation.Size(8, 9) }
				.GridPosition(1, 1)
			);

			SUT.Measure(new Windows.Foundation.Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Windows.Foundation.Size(14, 14), measuredSize);

			Assert.AreEqual(new Windows.Foundation.Size(3, 4), c1.RequestedDesiredSize);
			Assert.AreEqual(new Windows.Foundation.Size(4, 5), c2.RequestedDesiredSize);
			Assert.AreEqual(new Windows.Foundation.Size(6, 7), c3.RequestedDesiredSize);
			Assert.AreEqual(new Windows.Foundation.Size(8, 9), c4.RequestedDesiredSize);

			SUT.Arrange(new Windows.Foundation.Rect(0, 0, 20, 20));

			Assert.AreEqual(new Windows.Foundation.Rect(0, 0, 12, 5), c1.Arranged);
			Assert.AreEqual(new Windows.Foundation.Rect(12, 0, 8, 5), c2.Arranged);
			Assert.AreEqual(new Windows.Foundation.Rect(0, 5, 12, 15), c3.Arranged);
			Assert.AreEqual(new Windows.Foundation.Rect(12, 5, 8, 15), c4.Arranged);

			Assert.AreEqual(4, SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_Nine_grid_and_one_auto_cell_and_four_children()
		{
			var SUT = new Grid() { Name = "test" };

			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "auto" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });

			SUT.RowDefinitions.Add(new RowDefinition { Height = "*" });
			SUT.RowDefinitions.Add(new RowDefinition { Height = "*" });
			SUT.RowDefinitions.Add(new RowDefinition { Height = "auto" });

			var c1 = SUT.AddChild(
				new View { Name = "Child01", RequestedDesiredSize = new Windows.Foundation.Size(5, 5) }
				.GridPosition(0, 0)
			);
			var c2 = SUT.AddChild(
				new View { Name = "Child02", RequestedDesiredSize = new Windows.Foundation.Size(3, 4) }
				.GridPosition(1, 1)
			);
			var c3 = SUT.AddChild(
				new View { Name = "Child03", RequestedDesiredSize = new Windows.Foundation.Size(8, 9) }
				.GridPosition(2, 2)
			);
			var c4 = SUT.AddChild(
				new View { Name = "Child04", RequestedDesiredSize = new Windows.Foundation.Size(5, 5) }
				.GridPosition(1, 0)
			);

			SUT.Measure(new Windows.Foundation.Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Windows.Foundation.Size(19, 19), measuredSize);

			Assert.AreEqual(new Windows.Foundation.Size(5, 5), c1.RequestedDesiredSize);
			Assert.AreEqual(new Windows.Foundation.Size(3, 4), c2.RequestedDesiredSize);
			Assert.AreEqual(new Windows.Foundation.Size(8, 9), c3.RequestedDesiredSize);
			Assert.AreEqual(new Windows.Foundation.Size(5, 5), c4.RequestedDesiredSize);

			SUT.Arrange(new Windows.Foundation.Rect(0, 0, 20, 20));

			Assert.AreEqual(new Windows.Foundation.Rect(0, 0, 8.5f, 5.5f), c1.Arranged);
			Assert.AreEqual(new Windows.Foundation.Rect(8.5f, 5.5f, 3, 5.5f), c2.Arranged);
			Assert.AreEqual(new Windows.Foundation.Rect(11.5f, 11, 8.5f, 9), c3.Arranged);
			Assert.AreEqual(new Windows.Foundation.Rect(0, 5.5f, 8.5f, 5.5f), c4.Arranged);

			Assert.AreEqual(4, SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_Three_Rows_One_Auto_Two_Fixed_And_Row_Span_Full()
		{
			var SUT = new Grid() { Name = "test", Height = 44 };

			SUT.RowDefinitions.Add(new RowDefinition { Height = "18" });
			SUT.RowDefinitions.Add(new RowDefinition { Height = "auto" });
			SUT.RowDefinitions.Add(new RowDefinition { Height = "18" });

			var c1 = SUT.AddChild(
				new View { Name = "Child01", RequestedDesiredSize = new Windows.Foundation.Size(0, 10) }
				.GridPosition(1, 0)
			);
			var c2 = SUT.AddChild(
				new View { Name = "Child02", RequestedDesiredSize = new Windows.Foundation.Size(30, 0) }
				.GridPosition(1, 0)
			);
			var c3 = SUT.AddChild(
				new View { Name = "Child03", RequestedDesiredSize = new Windows.Foundation.Size(8, 24) }
				.GridPosition(0, 0)
				.GridRowSpan(3)
			);

			SUT.Measure(new Windows.Foundation.Size(100, 44));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Windows.Foundation.Size(30, 44), measuredSize);

			Assert.AreEqual(new Windows.Foundation.Size(0, 10), c1.RequestedDesiredSize);
			Assert.AreEqual(new Windows.Foundation.Size(30, 0), c2.RequestedDesiredSize);
			Assert.AreEqual(new Windows.Foundation.Size(8, 24), c3.RequestedDesiredSize);

			SUT.Arrange(new Windows.Foundation.Rect(0, 0, 20, 44));

			Assert.AreEqual(new Windows.Foundation.Rect(0, 18, 20, 8), c1.Arranged);
			Assert.AreEqual(new Windows.Foundation.Rect(0, 18, 20, 8), c2.Arranged);
			Assert.AreEqual(new Windows.Foundation.Rect(0, 0, 20, 44), c3.Arranged);

			Assert.AreEqual(3, SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_Three_Colums_One_Auto_Two_Fixed_And_Column_Span_Full()
		{
			var SUT = new Grid() { Name = "test", Width = 44 };

			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "18" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "auto" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "18" });

			var c1 = SUT.AddChild(
				new View { Name = "Child01", RequestedDesiredSize = new Windows.Foundation.Size(10, 0) }
				.GridPosition(0, 1)
			);
			var c2 = SUT.AddChild(
				new View { Name = "Child02", RequestedDesiredSize = new Windows.Foundation.Size(0, 30) }
				.GridPosition(0, 1)
			);
			var c3 = SUT.AddChild(
				new View { Name = "Child03", RequestedDesiredSize = new Windows.Foundation.Size(24, 8) }
				.GridPosition(0, 0)
				.GridColumnSpan(3)
			);

			SUT.Measure(new Windows.Foundation.Size(44, 100));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Windows.Foundation.Size(44, 30), measuredSize);

			Assert.AreEqual(new Windows.Foundation.Size(10, 0), c1.RequestedDesiredSize);
			Assert.AreEqual(new Windows.Foundation.Size(0, 30), c2.RequestedDesiredSize);
			Assert.AreEqual(new Windows.Foundation.Size(24, 8), c3.RequestedDesiredSize);

			SUT.Arrange(new Windows.Foundation.Rect(0, 0, 44, 20));

			Assert.AreEqual(new Windows.Foundation.Rect(18, 0, 8, 20), c1.Arranged);
			Assert.AreEqual(new Windows.Foundation.Rect(18, 0, 8, 20), c2.Arranged);
			Assert.AreEqual(new Windows.Foundation.Rect(0, 0, 44, 20), c3.Arranged);

			Assert.AreEqual(3, SUT.GetChildren().Count());
		}
	}
}
