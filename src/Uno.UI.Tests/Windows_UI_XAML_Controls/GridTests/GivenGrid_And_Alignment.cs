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
	public class GivenGrid_And_Alignment : Context
	{
		[TestMethod]
		public void When_One_Child_With_VerticalTopAlignment_and_Fixed_Height()
		{
			var SUT = new Grid() { Name = "test" };

			var c1 = SUT.AddChild(
				new View
				{
					Name = "Child01",
					RequestedDesiredSize = new Windows.Foundation.Size(10, 10),
					Height = 10, 
					VerticalAlignment = VerticalAlignment.Top,
				}
				.GridPosition(0, 0)
			);

			SUT.Measure(new Windows.Foundation.Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Windows.Foundation.Size(10, 10), measuredSize);
			Assert.AreEqual(new Windows.Foundation.Size(10, 10), c1.RequestedDesiredSize);

			SUT.Arrange(new Windows.Foundation.Rect(0, 0,20, 20));

			Assert.AreEqual(new Windows.Foundation.Rect(0, 0, 20, 10), c1.Arranged);

			Assert.AreEqual(1, SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_One_Child_With_VerticalBottomAlignment_and_Fixed_Height()
		{
			var SUT = new Grid() { Name = "test" };
			

			var c1 = SUT.AddChild(
				new View
				{
					Name = "Child01",
					RequestedDesiredSize = new Windows.Foundation.Size(10, 10),
					Height = 10,
					VerticalAlignment = VerticalAlignment.Bottom,
				}
				.GridPosition(0, 0)
			);

			SUT.Measure(new Windows.Foundation.Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Windows.Foundation.Size(10, 10), measuredSize);
			Assert.AreEqual(new Windows.Foundation.Size(10, 10), c1.RequestedDesiredSize);

			SUT.Arrange(new Windows.Foundation.Rect(0, 0,20, 20));

			Assert.AreEqual(new Windows.Foundation.Rect(0, 10, 20, 10), c1.Arranged);

			Assert.AreEqual(1, SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_One_Child_With_VerticalCenterAlignment_and_Fixed_Height()
		{
			var SUT = new Grid() { Name = "test" };
			

			var c1 = SUT.AddChild(
				new View
				{
					Name = "Child01",
					RequestedDesiredSize = new Windows.Foundation.Size(10, 10),
					Height = 10,
					VerticalAlignment = VerticalAlignment.Center,
				}
				.GridPosition(0, 0)
			);

			SUT.Measure(new Windows.Foundation.Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Windows.Foundation.Size(10, 10), measuredSize);
			Assert.AreEqual(new Windows.Foundation.Size(10, 10), c1.RequestedDesiredSize);

			SUT.Arrange(new Windows.Foundation.Rect(0, 0,20, 20));

			Assert.AreEqual(new Windows.Foundation.Rect(0, 5, 20, 10), c1.Arranged);

			Assert.AreEqual(1, SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_One_Child_With_HorizontalLeftAlignment_and_Fixed_Width()
		{
			var SUT = new Grid() { Name = "test" };

			var c1 = SUT.AddChild(
				new View
				{
					Name = "Child01",
					RequestedDesiredSize = new Windows.Foundation.Size(10, 10),
					Width = 10,
					HorizontalAlignment = HorizontalAlignment.Left,
				}
				.GridPosition(0, 0)
			);

			SUT.Measure(new Windows.Foundation.Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Windows.Foundation.Size(10, 10), measuredSize);
			Assert.AreEqual(new Windows.Foundation.Size(10, 10), c1.RequestedDesiredSize);

			SUT.Arrange(new Windows.Foundation.Rect(0, 0,20, 20));

			Assert.AreEqual(new Windows.Foundation.Rect(0, 0, 10, 20), c1.Arranged);

			Assert.AreEqual(1, SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_One_Child_With_HorizontalRightAlignment_and_Fixed_Width()
		{
			var SUT = new Grid() { Name = "test" };
			

			var c1 = SUT.AddChild(
				new View
				{
					Name = "Child01",
					RequestedDesiredSize = new Windows.Foundation.Size(10, 10),
					Width = 10,
					HorizontalAlignment = HorizontalAlignment.Right,
				}
				.GridPosition(0, 0)
			);

			SUT.Measure(new Windows.Foundation.Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Windows.Foundation.Size(10, 10), measuredSize);
			Assert.AreEqual(new Windows.Foundation.Size(10, 10), c1.RequestedDesiredSize);

			SUT.Arrange(new Windows.Foundation.Rect(0, 0,20, 20));

			Assert.AreEqual(new Windows.Foundation.Rect(10, 0, 10, 20), c1.Arranged);

			Assert.AreEqual(1, SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_One_Child_With_HorizontalCenterAlignment_and_Fixed_Width()
		{
			var SUT = new Grid() { Name = "test" };
			

			var c1 = SUT.AddChild(
				new View
				{
					Name = "Child01",
					RequestedDesiredSize = new Windows.Foundation.Size(10, 10),
					Width = 10,
					HorizontalAlignment = HorizontalAlignment.Center,
				}
				.GridPosition(0, 0)
			);

			SUT.Measure(new Windows.Foundation.Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Windows.Foundation.Size(10, 10), measuredSize);
			Assert.AreEqual(new Windows.Foundation.Size(10, 10), c1.RequestedDesiredSize);

			SUT.Arrange(new Windows.Foundation.Rect(0, 0,20, 20));

			Assert.AreEqual(new Windows.Foundation.Rect(5, 0, 10, 20), c1.Arranged);

			Assert.AreEqual(1, SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_One_Child_With_VerticalTopAlignment_and_Variable_Height()
		{
			var SUT = new Grid() { Name = "test" };
			

			var c1 = SUT.AddChild(
				new View
				{
					Name = "Child01",
					RequestedDesiredSize = new Windows.Foundation.Size(10, 10),
					VerticalAlignment = VerticalAlignment.Top,
				}
				.GridPosition(0, 0)
			);

			SUT.Measure(new Windows.Foundation.Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Windows.Foundation.Size(10, 10), measuredSize);
			Assert.AreEqual(new Windows.Foundation.Size(10, 10), c1.RequestedDesiredSize);

			SUT.Arrange(new Windows.Foundation.Rect(0, 0,20, 20));

			Assert.AreEqual(new Windows.Foundation.Rect(0, 0, 20, 10), c1.Arranged);

			Assert.AreEqual(1, SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_One_Child_With_VerticalCenterAlignment_and_Variable_Height()
		{
			var SUT = new Grid() { Name = "test" };
			

			var c1 = SUT.AddChild(
				new View
				{
					Name = "Child01",
					RequestedDesiredSize = new Windows.Foundation.Size(10, 10),
					VerticalAlignment = VerticalAlignment.Center,
				}
				.GridPosition(0, 0)
			);

			SUT.Measure(new Windows.Foundation.Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Windows.Foundation.Size(10, 10), measuredSize);
			Assert.AreEqual(new Windows.Foundation.Size(10, 10), c1.RequestedDesiredSize);

			SUT.Arrange(new Windows.Foundation.Rect(0, 0,20, 20));

			Assert.AreEqual(new Windows.Foundation.Rect(0, 5, 20, 10), c1.Arranged);

			Assert.AreEqual(1, SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_One_Child_With_HorizontalStretchAlignment_and_MaxWidth()
		{
			var SUT = new Grid() { Name = "test" };
			

			var c1 = SUT.AddChild(
				new View
				{
					Name = "Child01",
					RequestedDesiredSize = new Windows.Foundation.Size(10, 10),
					MaxWidth = 10,
				}
				.GridPosition(0, 0)
			);

			SUT.Measure(new Windows.Foundation.Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Windows.Foundation.Size(10, 10), measuredSize);
			Assert.AreEqual(new Windows.Foundation.Size(10, 10), c1.RequestedDesiredSize);

			SUT.Arrange(new Windows.Foundation.Rect(0, 0,20, 20));

			Assert.AreEqual(new Windows.Foundation.Rect(5, 0, 10, 20), c1.Arranged);

			Assert.AreEqual(1, SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_One_Child_With_VerticalStretchAlignment_and_MaxHeight()
		{
			var SUT = new Grid() { Name = "test" };
			

			var c1 = SUT.AddChild(
				new View
				{
					Name = "Child01",
					RequestedDesiredSize = new Windows.Foundation.Size(10, 10),
					MaxHeight = 10,
				}
				.GridPosition(0, 0)
			);

			SUT.Measure(new Windows.Foundation.Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Windows.Foundation.Size(10, 10), measuredSize);
			Assert.AreEqual(new Windows.Foundation.Size(10, 10), c1.RequestedDesiredSize);

			SUT.Arrange(new Windows.Foundation.Rect(0, 0,20, 20));

			Assert.AreEqual(new Windows.Foundation.Rect(0, 5, 20, 10), c1.Arranged);

			Assert.AreEqual(1, SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_One_Child_With_VerticalCenterAlignment_HorizontalCenterAlignment_and_Variable_Height_and_Variable_Width()
		{
			var SUT = new Grid() { Name = "test" };
			

			var c1 = SUT.AddChild(
				new View
				{
					Name = "Child01",
					RequestedDesiredSize = new Windows.Foundation.Size(10, 10),
					VerticalAlignment = VerticalAlignment.Center,
					HorizontalAlignment = HorizontalAlignment.Center,
				}
				.GridPosition(0, 0)
			);

			SUT.Measure(new Windows.Foundation.Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Windows.Foundation.Size(10, 10), measuredSize);
			Assert.AreEqual(new Windows.Foundation.Size(10, 10), c1.RequestedDesiredSize);

			SUT.Arrange(new Windows.Foundation.Rect(0, 0,20, 20));

			Assert.AreEqual(new Windows.Foundation.Rect(5, 5, 10, 10), c1.Arranged);

			Assert.AreEqual(1, SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_One_Child_Centered_And_Auto_row_And_Fixed_Column()
		{
			var SUT = new Grid() { Name = "test" };
			

			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "20" });
			SUT.RowDefinitions.Add(new RowDefinition { Height = "auto" });

			var c1 = SUT.AddChild(
				new View
				{
					Name = "Child01",

					// The selector will return a different size if the measured size if greater than the column size.
					// This should not happen, as the child should not be measured with a Width greater
					// than 20, as the column is of size 20.
					DesiredSizeSelector = s => s.Width > 20 ? new Windows.Foundation.Size(20, 5) : new Windows.Foundation.Size(10, 10),

					VerticalAlignment = VerticalAlignment.Center,
					HorizontalAlignment = HorizontalAlignment.Center,
				}
				.GridPosition(0, 0)
			);

			SUT.Measure(new Windows.Foundation.Size(100, 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Windows.Foundation.Size(20, 10), measuredSize);
			Assert.AreEqual(new Windows.Foundation.Size(10, 10), c1.RequestedDesiredSize);

			SUT.Arrange(new Windows.Foundation.Rect(0, 0,100, 20));

			Assert.AreEqual(new Windows.Foundation.Size(10, 10), c1.RequestedDesiredSize);
			Assert.AreEqual(new Windows.Foundation.Rect(5, 0, 10, 10), c1.Arranged);

			Assert.AreEqual(1, SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_One_Child_Centered_And_Auto_row_And_Star_Column()
		{
			var SUT = new Grid() { Name = "test" };
			

			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "auto" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });
			SUT.RowDefinitions.Add(new RowDefinition { Height = "auto" });

			var c1 = SUT.AddChild(
				new View
				{
					Name = "Child01",

					// The selector will return a different size if the measured size if greater than the column size.
					// This should not happen, as the child should not be measured with a Width greater
					// than 20, as the column is of size 20.
					DesiredSizeSelector = s => s.Width > 20 ? new Windows.Foundation.Size(20, 5) : new Windows.Foundation.Size(10, 10),

					VerticalAlignment = VerticalAlignment.Center,
					HorizontalAlignment = HorizontalAlignment.Center,
				}
				.GridPosition(0, 1)
			);

			var c2 = SUT.AddChild(
				new View
				{
					Name = "Child02",
					RequestedDesiredSize = new Windows.Foundation.Size(11, 11),
				}
				.GridPosition(0, 0)
			);

			SUT.Measure(new Windows.Foundation.Size(100, 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Windows.Foundation.Size(31, 11), measuredSize);
			Assert.AreEqual(new Windows.Foundation.Size(20, 5), c1.RequestedDesiredSize);
			Assert.AreEqual(new Windows.Foundation.Size(11, 11), c2.RequestedDesiredSize);
			Assert.AreEqual(1, c1.MeasureCallCount);
			Assert.AreEqual(1, c2.MeasureCallCount); // The measure count is 1 beceause the grid has a recognized pattern (Nx1). It would be 2 otherwise.
			Assert.AreEqual(0, c1.ArrangeCallCount);
			Assert.AreEqual(0, c2.ArrangeCallCount);

			SUT.Arrange(new Windows.Foundation.Rect(0, 0,100, 20));

			Assert.AreEqual(new Windows.Foundation.Size(20, 5), c1.RequestedDesiredSize);
			Assert.AreEqual(new Windows.Foundation.Size(11, 11), c2.RequestedDesiredSize);
			Assert.AreEqual(new Windows.Foundation.Rect(45.5f, 3, 20, 5), c1.Arranged);
			Assert.AreEqual(new Windows.Foundation.Rect(0, 0, 11, 11), c2.Arranged);
			Assert.AreEqual(1, c1.MeasureCallCount);
			Assert.AreEqual(1, c2.MeasureCallCount); // The measure count is 1 beceause the grid has a recognized pattern (Nx1). It would be 2 otherwise.
			Assert.AreEqual(1, c1.ArrangeCallCount);
			Assert.AreEqual(1, c2.ArrangeCallCount);

			Assert.AreEqual(2, SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_One_Child_Centered_and_Margin()
		{
			var SUT = new Grid() { Name = "test" };
			

			var c1 = SUT.AddChild(
				new View
				{
					Name = "Child01",
					RequestedDesiredSize = new Windows.Foundation.Size(10, 10),
					VerticalAlignment = VerticalAlignment.Center,
					HorizontalAlignment = HorizontalAlignment.Center,
					Margin = new Thickness(2, 2, 2, 2),
				}
			);

			SUT.Measure(new Windows.Foundation.Size(40, 40));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Windows.Foundation.Size(14, 14), measuredSize);
			Assert.AreEqual(new Windows.Foundation.Size(10, 10), c1.RequestedDesiredSize);

			SUT.Arrange(new Windows.Foundation.Rect(0, 0,40, 40));

			Assert.AreEqual(new Windows.Foundation.Size(10, 10), c1.RequestedDesiredSize);
			Assert.AreEqual(new Windows.Foundation.Rect(15, 15, 10, 10), c1.Arranged);

			Assert.AreEqual(1, SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_One_Child_and_Margin_and_Vertical_Bottom_and_Horizontal_Center()
		{
			var SUT = new Grid() { Name = "test" };
			

			var c1 = SUT.AddChild(
				new View
				{
					Name = "Child01",
					RequestedDesiredSize = new Windows.Foundation.Size(10, 10),
					VerticalAlignment = VerticalAlignment.Bottom,
					HorizontalAlignment = HorizontalAlignment.Center,
					Margin = new Thickness(2, 2, 2, 2),
				}
			);

			SUT.Measure(new Windows.Foundation.Size(40, 40));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Windows.Foundation.Size(14, 14), measuredSize);
			Assert.AreEqual(new Windows.Foundation.Size(10, 10), c1.RequestedDesiredSize);

			SUT.Arrange(new Windows.Foundation.Rect(0, 0,40, 40));

			Assert.AreEqual(new Windows.Foundation.Size(10, 10), c1.RequestedDesiredSize);
			Assert.AreEqual(new Windows.Foundation.Rect(15, 28, 10, 10), c1.Arranged);

			Assert.AreEqual(1, SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_One_Child_and_Margin_and_Vertical_Top_and_Horizontal_Center()
		{
			var SUT = new Grid() { Name = "test" };
			

			var c1 = SUT.AddChild(
				new View
				{
					Name = "Child01",
					RequestedDesiredSize = new Windows.Foundation.Size(10, 10),
					VerticalAlignment = VerticalAlignment.Top,
					HorizontalAlignment = HorizontalAlignment.Center,
					Margin = new Thickness(2, 2, 2, 2),
				}
			);

			SUT.Measure(new Windows.Foundation.Size(40, 40));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Windows.Foundation.Size(14, 14), measuredSize);
			Assert.AreEqual(new Windows.Foundation.Size(10, 10), c1.RequestedDesiredSize);

			SUT.Arrange(new Windows.Foundation.Rect(0, 0,40, 40));

			Assert.AreEqual(new Windows.Foundation.Size(10, 10), c1.RequestedDesiredSize);
			Assert.AreEqual(new Windows.Foundation.Rect(15, 2, 10, 10), c1.Arranged);

			Assert.AreEqual(1, SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_One_Child_and_Margin_and_Vertical_Botton_and_Horizontal_Right()
		{
			var SUT = new Grid() { Name = "test" };
			

			var c1 = SUT.AddChild(
				new View
				{
					Name = "Child01",
					RequestedDesiredSize = new Windows.Foundation.Size(10, 10),
					VerticalAlignment = VerticalAlignment.Bottom,
					HorizontalAlignment = HorizontalAlignment.Right,
					Margin = new Thickness(2, 2, 2, 2),
				}
			);

			SUT.Measure(new Windows.Foundation.Size(40, 40));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Windows.Foundation.Size(14, 14), measuredSize);
			Assert.AreEqual(new Windows.Foundation.Size(10, 10), c1.RequestedDesiredSize);

			SUT.Arrange(new Windows.Foundation.Rect(0, 0,20, 20));

			Assert.AreEqual(new Windows.Foundation.Size(10, 10), c1.RequestedDesiredSize);
			Assert.AreEqual(new Windows.Foundation.Rect(8, 8, 10, 10), c1.Arranged);

			Assert.AreEqual(1, SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_One_Child_and_Measure_Bigger_than_arrange()
		{
			var SUT = new Grid() { Name = "test" };
			

			var c1 = SUT.AddChild(
				new View
				{
					Name = "Child01",
					DesiredSizeSelector = s => s,
				}
			);

			SUT.Measure(new Windows.Foundation.Size(100, 100));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Windows.Foundation.Size(100, 100), measuredSize);
			Assert.AreEqual(new Windows.Foundation.Size(100, 100), c1.DesiredSize);

			SUT.Arrange(new Windows.Foundation.Rect(0, 0,20, 20));
			
			Assert.AreEqual(new Windows.Foundation.Rect(0, 0, 20, 20), c1.Arranged);

			Assert.AreEqual(1, SUT.GetChildren().Count());
		}
	}
}
