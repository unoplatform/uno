using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System.Linq;
using Windows.Foundation;
using FluentAssertions;
using View = Windows.UI.Xaml.FrameworkElement;
using System;

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
					RequestedDesiredSize = new Size(10, 10),
					Height = 10, 
					VerticalAlignment = VerticalAlignment.Top,
				}
				.GridPosition(0, 0)
			);

			SUT.Measure(new Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Size(10, 10), measuredSize);
			Assert.AreEqual(new Size(10, 10), c1.RequestedDesiredSize);

			SUT.Arrange(new Rect(0, 0,20, 20));

			Assert.AreEqual(new Rect(0, 0, 20, 10), c1.Arranged);

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
					RequestedDesiredSize = new Size(10, 10),
					Height = 10,
					VerticalAlignment = VerticalAlignment.Bottom,
				}
				.GridPosition(0, 0)
			);

			SUT.Measure(new Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Size(10, 10), measuredSize);
			Assert.AreEqual(new Size(10, 10), c1.RequestedDesiredSize);

			SUT.Arrange(new Rect(0, 0,20, 20));

			Assert.AreEqual(new Rect(0, 10, 20, 10), c1.Arranged);

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
					RequestedDesiredSize = new Size(10, 10),
					Height = 10,
					VerticalAlignment = VerticalAlignment.Center,
				}
				.GridPosition(0, 0)
			);

			SUT.Measure(new Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Size(10, 10), measuredSize);
			Assert.AreEqual(new Size(10, 10), c1.RequestedDesiredSize);

			SUT.Arrange(new Rect(0, 0,20, 20));

			Assert.AreEqual(new Rect(0, 5, 20, 10), c1.Arranged);

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
					RequestedDesiredSize = new Size(10, 10),
					Width = 10,
					HorizontalAlignment = HorizontalAlignment.Left,
				}
				.GridPosition(0, 0)
			);

			SUT.Measure(new Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Size(10, 10), measuredSize);
			Assert.AreEqual(new Size(10, 10), c1.RequestedDesiredSize);

			SUT.Arrange(new Rect(0, 0,20, 20));

			Assert.AreEqual(new Rect(0, 0, 10, 20), c1.Arranged);

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
					RequestedDesiredSize = new Size(10, 10),
					Width = 10,
					HorizontalAlignment = HorizontalAlignment.Right,
				}
				.GridPosition(0, 0)
			);

			SUT.Measure(new Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Size(10, 10), measuredSize);
			Assert.AreEqual(new Size(10, 10), c1.RequestedDesiredSize);

			SUT.Arrange(new Rect(0, 0,20, 20));

			Assert.AreEqual(new Rect(10, 0, 10, 20), c1.Arranged);

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
					RequestedDesiredSize = new Size(10, 10),
					Width = 10,
					HorizontalAlignment = HorizontalAlignment.Center,
				}
				.GridPosition(0, 0)
			);

			SUT.Measure(new Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Size(10, 10), measuredSize);
			Assert.AreEqual(new Size(10, 10), c1.RequestedDesiredSize);

			SUT.Arrange(new Rect(0, 0,20, 20));

			Assert.AreEqual(new Rect(5, 0, 10, 20), c1.Arranged);

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
					RequestedDesiredSize = new Size(10, 10),
					VerticalAlignment = VerticalAlignment.Top,
				}
				.GridPosition(0, 0)
			);

			SUT.Measure(new Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Size(10, 10), measuredSize);
			Assert.AreEqual(new Size(10, 10), c1.RequestedDesiredSize);

			SUT.Arrange(new Rect(0, 0,20, 20));

			Assert.AreEqual(new Rect(0, 0, 20, 10), c1.Arranged);

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
					RequestedDesiredSize = new Size(10, 10),
					VerticalAlignment = VerticalAlignment.Center,
				}
				.GridPosition(0, 0)
			);

			SUT.Measure(new Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Size(10, 10), measuredSize);
			Assert.AreEqual(new Size(10, 10), c1.RequestedDesiredSize);

			SUT.Arrange(new Rect(0, 0,20, 20));

			Assert.AreEqual(new Rect(0, 5, 20, 10), c1.Arranged);

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
					RequestedDesiredSize = new Size(10, 10),
					MaxWidth = 10,
				}
				.GridPosition(0, 0)
			);

			SUT.Measure(new Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Size(10, 10), measuredSize);
			Assert.AreEqual(new Size(10, 10), c1.RequestedDesiredSize);

			SUT.Arrange(new Rect(0, 0,20, 20));

			Assert.AreEqual(new Rect(5, 0, 10, 20), c1.Arranged);

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
					RequestedDesiredSize = new Size(10, 10),
					MaxHeight = 10,
				}
				.GridPosition(0, 0)
			);

			SUT.Measure(new Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Size(10, 10), measuredSize);
			Assert.AreEqual(new Size(10, 10), c1.RequestedDesiredSize);

			SUT.Arrange(new Rect(0, 0,20, 20));

			Assert.AreEqual(new Rect(0, 5, 20, 10), c1.Arranged);

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
					RequestedDesiredSize = new Size(10, 10),
					VerticalAlignment = VerticalAlignment.Center,
					HorizontalAlignment = HorizontalAlignment.Center,
				}
				.GridPosition(0, 0)
			);

			SUT.Measure(new Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Size(10, 10), measuredSize);
			Assert.AreEqual(new Size(10, 10), c1.RequestedDesiredSize);

			SUT.Arrange(new Rect(0, 0,20, 20));

			Assert.AreEqual(new Rect(5, 5, 10, 10), c1.Arranged);

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
					DesiredSizeSelector = s => s.Width > 20 ? new Size(20, 5) : new Size(10, 10),

					VerticalAlignment = VerticalAlignment.Center,
					HorizontalAlignment = HorizontalAlignment.Center,
				}
				.GridPosition(0, 0)
			);

			SUT.Measure(new Size(100, 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Size(20, 10), measuredSize);
			Assert.AreEqual(new Size(10, 10), c1.RequestedDesiredSize);

			SUT.Arrange(new Rect(0, 0,100, 20));

			Assert.AreEqual(new Size(10, 10), c1.RequestedDesiredSize);
			Assert.AreEqual(new Rect(5, 0, 10, 10), c1.Arranged);

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
					DesiredSizeSelector = s => s.Width > 20 ? new Size(20, 5) : new Size(10, 10),

					VerticalAlignment = VerticalAlignment.Center,
					HorizontalAlignment = HorizontalAlignment.Center,
				}
				.GridPosition(0, 1)
			);

			var c2 = SUT.AddChild(
				new View
				{
					Name = "Child02",
					RequestedDesiredSize = new Size(11, 11),
				}
				.GridPosition(0, 0)
			);

			SUT.Measure(new Size(100, 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Size(31, 11), measuredSize);
			Assert.AreEqual(new Size(20, 5), c1.RequestedDesiredSize);
			Assert.AreEqual(new Size(11, 11), c2.RequestedDesiredSize);
			Assert.AreEqual(1, c1.MeasureCallCount);
			Assert.AreEqual(1, c2.MeasureCallCount); // The measure count is 1 beceause the grid has a recognized pattern (Nx1). It would be 2 otherwise.
			Assert.AreEqual(0, c1.ArrangeCallCount);
			Assert.AreEqual(0, c2.ArrangeCallCount);

			SUT.Arrange(new Rect(0, 0,100, 20));

			Assert.AreEqual(new Size(20, 5), c1.RequestedDesiredSize);
			Assert.AreEqual(new Size(11, 11), c2.RequestedDesiredSize);
			Assert.AreEqual(new Rect(45.5f, 3, 20, 5), c1.Arranged);
			Assert.AreEqual(new Rect(0, 0, 11, 11), c2.Arranged);
			Assert.AreEqual(1, c1.MeasureCallCount);
			Assert.AreEqual(1, c2.MeasureCallCount); // The measure count is 1 beceause the grid has a recognized pattern (Nx1). It would be 2 otherwise.
			Assert.AreEqual(1, c1.ArrangeCallCount);
			Assert.AreEqual(1, c2.ArrangeCallCount);

			Assert.AreEqual(2, SUT.GetChildren().Count());
		}

		[DataRow("cc", 0d, "15,15,10,10")]
		[DataRow("cc", 2d, "15,15,10,10")]
		[DataRow("cc", 15d, "15,15,10,10")]
		[DataRow("cc", 16d, "16,16,8,8")]
		[DataRow("cc", 18d, "17,17,6,6")]
		[DataRow("cb", 2d, "15,28,10,10")]
		[DataRow("ct", 2d, "15,2,10,10")]
		[DataRow("ss", 0d, "0,0,40,40")]
		[DataRow("ss", 2d, "2,2,36,36")]
		[DataRow("ss", 16d, "16,16,8,8")]
		[DataRow("ss", 18d, "17,17,6,6")]
		[DataRow("lt", 2d, "2,2,10,10")]
		[DataRow("rt", 2d, "28,2,10,10")]
		[DataRow("rb", 2d, "28,28,10,10")]
		[DataRow("lb", 2d, "2,28,10,10")]
#if false
		// On UWP, margin takes precedence over MinSize
		[DataRow("ss", 200d, "20,20,0,0")]
		[DataRow("cc", 200d, "20,20,0,0")]
		[DataRow("lt", 200d, "20,20,0,0")]
#endif
		[TestMethod]
		public void When_One_Child_Alignment(string alignment, double margin, string expected)
		{
			var SUT = new Grid() { Name = "test" };

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
				switch(alignment[1])
				{
					case 's': return VerticalAlignment.Stretch;
					case 't': return VerticalAlignment.Top;
					case 'c': return VerticalAlignment.Center;
					case 'b': return VerticalAlignment.Bottom;
				}
				return default;
			}

			Rect ExpectedArranged()
			{
				var parts = expected.Split(',').Select(double.Parse).ToArray();
				return new Rect(parts[0], parts[1], parts[2], parts[3]);
			}

			var c1 = SUT.AddChild(
				new View
				{
					Name = "Child01",
					RequestedDesiredSize = new Size(10, 10),
					HorizontalAlignment = GetH(),
					VerticalAlignment = GetV(),
					MinWidth = 6,
					MinHeight = 6,
					Margin = new Thickness(margin),
				}
			);


			SUT.GetChildren().Should().HaveCount(1);

			SUT.Measure(new Size(40, 40));
			SUT.DesiredSize.Should().Be(new Size(Math.Min(10d + margin * 2, 40d), Math.Min(10d + margin * 2, 40d)));
			c1.DesiredSize.Should().Be(new Size(10d + margin * 2d, 10d + margin * 2d));

			SUT.Arrange(new Rect(0, 0,40, 40));

			SUT.Arranged.Should().Be(new Rect(0, 0, 40, 40));
			c1.Arranged.Should().Be(ExpectedArranged());
		}

		[TestMethod]
		public void When_One_Child_and_Margin_and_Vertical_Botton_and_Horizontal_Right()
		{
			var SUT = new Grid() { Name = "test" };
			

			var c1 = SUT.AddChild(
				new View
				{
					Name = "Child01",
					RequestedDesiredSize = new Size(10, 10),
					VerticalAlignment = VerticalAlignment.Bottom,
					HorizontalAlignment = HorizontalAlignment.Right,
					Margin = new Thickness(2, 2, 2, 2),
				}
			);

			SUT.Measure(new Size(40, 40));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Size(14, 14), measuredSize);
			Assert.AreEqual(new Size(10, 10), c1.RequestedDesiredSize);

			SUT.Arrange(new Rect(0, 0,20, 20));

			Assert.AreEqual(new Size(10, 10), c1.RequestedDesiredSize);
			Assert.AreEqual(new Rect(8, 8, 10, 10), c1.Arranged);

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

			SUT.Measure(new Size(100, 100));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Size(100, 100), measuredSize);
			Assert.AreEqual(new Size(100, 100), c1.DesiredSize);

			SUT.Arrange(new Rect(0, 0,20, 20));
			
			Assert.AreEqual(new Rect(0, 0, 20, 20), c1.Arranged);

			Assert.AreEqual(1, SUT.GetChildren().Count());
		}
	}
}
