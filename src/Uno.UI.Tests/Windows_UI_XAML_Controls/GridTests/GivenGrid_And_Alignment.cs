using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System.Linq;
using Windows.Foundation;
using FluentAssertions;
using Uno.UI.Controls.Legacy;

namespace Uno.UI.Tests.GridTests
{
	[TestClass]
	public partial class GivenGrid_And_Alignment : Context
	{
		private partial class View : FrameworkElement
		{
		}

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

			SUT.Arrange(new Rect(0, 0, 20, 20));

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

			SUT.Arrange(new Rect(0, 0, 20, 20));

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

			SUT.Arrange(new Rect(0, 0, 20, 20));

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

			SUT.Arrange(new Rect(0, 0, 20, 20));

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

			SUT.Arrange(new Rect(0, 0, 20, 20));

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

			SUT.Arrange(new Rect(0, 0, 20, 20));

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

			SUT.Arrange(new Rect(0, 0, 20, 20));

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

			SUT.Arrange(new Rect(0, 0, 20, 20));

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

			SUT.Arrange(new Rect(0, 0, 20, 20));

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

			SUT.Arrange(new Rect(0, 0, 20, 20));

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

			SUT.Arrange(new Rect(0, 0, 20, 20));

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

			SUT.Arrange(new Rect(0, 0, 100, 20));

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

			SUT.Arrange(new Rect(0, 0, 100, 20));

			Assert.AreEqual(new Size(20, 5), c1.RequestedDesiredSize);
			Assert.AreEqual(new Size(11, 11), c2.RequestedDesiredSize);
			Assert.AreEqual(new Rect(45.5f, 3, 20, 5), c1.Arranged);
			Assert.AreEqual(new Rect(0, 0, 11, 11), c2.Arranged);
			Assert.AreEqual(1, c1.MeasureCallCount);
			Assert.AreEqual(1, c2.MeasureCallCount); // The measure count is 1 because the grid has a recognized pattern (Nx1). It would be 2 otherwise.
			Assert.AreEqual(1, c1.ArrangeCallCount);
			Assert.AreEqual(1, c2.ArrangeCallCount);

			Assert.AreEqual(2, SUT.GetChildren().Count());
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
				return s == "empty" ? Rect.Empty : (Rect)s;
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

			SUT.AddChild(c1);


			SUT.GetChildren().Should().HaveCount(1);

			var availableSize = new Size(40, 40);
			var unclippedExpectedSize = new Size(6d + margin * 2, 6d + margin * 2);
			var expectedDesiredSize = unclippedExpectedSize.AtMost(availableSize);

			SUT.Measure(availableSize);


			SUT.DesiredSize.Should().Be(expectedDesiredSize);
			SUT.UnclippedDesiredSize.Should().Be(expectedDesiredSize);
			c1.DesiredSize.Should().Be(expectedDesiredSize.AtLeast(new Size(margin * 2, margin * 2)));
			c1.UnclippedDesiredSize.Should().Be(new Size(6d, 6d)); // Unclipped doesn't include margins!

			SUT.Arrange(new Rect(default, availableSize));

			c1.Arranged.Should().Be(GetRect(expected));
			var expectedClippedFrameRect = expectedClippedFrame == null
				? new Rect(default, (GetRect(expected)).Size)
				: GetRect(expectedClippedFrame);
			c1.ClippedFrame.Should().Be(expectedClippedFrameRect);
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

			SUT.Arrange(new Rect(0, 0, 20, 20));

			Assert.AreEqual(new Rect(0, 0, 100, 100), c1.Arranged);

			Assert.AreEqual(1, SUT.GetChildren().Count());
		}
	}
}
