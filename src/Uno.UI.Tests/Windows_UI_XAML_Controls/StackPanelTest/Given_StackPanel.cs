using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Linq;
using Windows.Foundation;
using AwesomeAssertions.Execution;

namespace Uno.UI.Tests.StackPanelTest
{
	[TestClass]
	public partial class Given_StackPanel : Context
	{
		private partial class View : FrameworkElement
		{
		}

		[TestMethod]
		public void When_Vertical_And_SimpleLayout()
		{
			var SUT = new StackPanel { Name = "test", Orientation = Orientation.Vertical };

			var c1 = SUT.AddChild(
				child: new View { Name = "Child01", RequestedDesiredSize = new Size(width: 10, height: 8) }
			);

			var c2 = SUT.AddChild(
				child: new View { Name = "Child02", RequestedDesiredSize = new Size(width: 10, height: 7) }
			);

			SUT.Measure(availableSize: new Size(width: 20, height: 20));
			Assert.AreEqual(expected: new Size(width: 10, height: 15), actual: SUT.DesiredSize, message: "measuredSize");

			SUT.Arrange(finalRect: new Rect(x: 0, y: 0, width: 20, height: 20));
			Assert.AreEqual(expected: new Rect(x: 0, y: 0, width: 20, height: 8), actual: c1.Arranged);
			Assert.AreEqual(expected: new Rect(x: 0, y: 8, width: 20, height: 7), actual: c2.Arranged);

			Assert.AreEqual(expected: 2, actual: SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_Horizontal_And_SimpleLayout()
		{
			var SUT = new StackPanel { Name = "test", Orientation = Orientation.Horizontal };

			var c1 = SUT.AddChild(
				child: new View { Name = "Child01", RequestedDesiredSize = new Size(width: 5, height: 8) }
			);

			var c2 = SUT.AddChild(
				child: new View { Name = "Child02", RequestedDesiredSize = new Size(width: 12, height: 7) }
			);

			SUT.Measure(availableSize: new Size(width: 20, height: 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(expected: new Size(width: 17, height: 8), actual: measuredSize, message: "measuredSize");

			SUT.Arrange(finalRect: new Rect(x: 0, y: 0, width: 20, height: 20));
			Assert.AreEqual(expected: new Rect(x: 0, y: 0, width: 5, height: 20), actual: c1.Arranged);
			Assert.AreEqual(expected: new Rect(x: 5, y: 0, width: 12, height: 20), actual: c2.Arranged);

			Assert.AreEqual(expected: 2, actual: SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_Vertical_And_SimpleLayout_With_Spacing()
		{
			var SUT = new StackPanel { Name = "test", Orientation = Orientation.Vertical, Spacing = 5 };

			var c1 = SUT.AddChild(
				child: new View { Name = "Child01", RequestedDesiredSize = new Size(width: 10, height: 8) }
			);

			var c2 = SUT.AddChild(
				child: new View { Name = "Child02", RequestedDesiredSize = new Size(width: 10, height: 7) }
			);

			SUT.Measure(availableSize: new Size(width: 20, height: 20));
			Assert.AreEqual(expected: new Size(width: 10, height: 20), actual: SUT.DesiredSize, message: "measuredSize");

			SUT.Arrange(finalRect: new Rect(x: 0, y: 0, width: 20, height: 20));
			Assert.AreEqual(expected: new Rect(x: 0, y: 0, width: 20, height: 8), actual: c1.Arranged);
			Assert.AreEqual(expected: new Rect(x: 0, y: 13, width: 20, height: 7), actual: c2.Arranged);

			Assert.AreEqual(expected: 2, actual: SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_Vertical_And_Three_With_Spacing()
		{
			var SUT = new StackPanel { Name = "test", Orientation = Orientation.Vertical, Spacing = 5 };

			var c1 = SUT
				.AddChild(
					child: new View { Name = "Child01", RequestedDesiredSize = new Size(width: 10, height: 8) }
				);

			var c2 = SUT
				.AddChild(
					child: new View { Name = "Child02", RequestedDesiredSize = new Size(width: 10, height: 7) }
				);

			var c3 = SUT
				.AddChild(
					child: new View { Name = "Child03", RequestedDesiredSize = new Size(width: 10, height: 11) }
				);

			SUT.Measure(availableSize: new Size(width: 20, height: 20));
			SUT.DesiredSize.Should().Be(new Size(10, 20));
			SUT.UnclippedDesiredSize.Should().Be(new Size(10, 36));

			SUT.Arrange(finalRect: new Rect(x: 0, y: 0, width: 20, height: 20));
			Assert.AreEqual(expected: new Rect(x: 0, y: 0, width: 20, height: 8), actual: c1.Arranged);
			Assert.AreEqual(expected: new Rect(x: 0, y: 13, width: 20, height: 7), actual: c2.Arranged);
			Assert.AreEqual(expected: new Rect(x: 0, y: 25, width: 20, height: 11), actual: c3.Arranged);

			Assert.AreEqual(expected: 3, actual: SUT.GetChildren().Count());
		}


		[TestMethod]
		public void When_Horizontal_And_SimpleLayout_With_Spacing()
		{
			var SUT = new StackPanel { Name = "test", Orientation = Orientation.Horizontal, Spacing = 5 };

			var c1 = SUT
				.AddChild(
					child: new View { Name = "Child01", RequestedDesiredSize = new Size(width: 5, height: 8) }
				);

			var c2 = SUT
				.AddChild(
					child: new View { Name = "Child02", RequestedDesiredSize = new Size(width: 12, height: 7) }
				);

			SUT.Measure(availableSize: new Size(width: 20, height: 20));
			SUT.DesiredSize.Should().Be(new Size(20, 8));
			SUT.UnclippedDesiredSize.Should().Be(new Size(22, 8));

			SUT.Arrange(finalRect: new Rect(x: 0, y: 0, width: 20, height: 20));
			Assert.AreEqual(expected: new Rect(x: 0, y: 0, width: 5, height: 20), actual: c1.Arranged);
			Assert.AreEqual(expected: new Rect(x: 10, y: 0, width: 12, height: 20), actual: c2.Arranged);

			Assert.AreEqual(expected: 2, actual: SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_Horizontal_And_Three_With_Spacing()
		{
			var SUT = new StackPanel { Name = "test", Orientation = Orientation.Horizontal, Spacing = 5 };

			var c1 = SUT
				.AddChild(
					child: new View { Name = "Child01", RequestedDesiredSize = new Size(width: 5, height: 8) }
				);

			var c2 = SUT
				.AddChild(
					child: new View { Name = "Child02", RequestedDesiredSize = new Size(width: 12, height: 7) }
				);

			var c3 = SUT
				.AddChild(
					child: new View { Name = "Child02", RequestedDesiredSize = new Size(width: 12, height: 5) }
				);

			SUT.Measure(availableSize: new Size(width: 20, height: 20));
			SUT.DesiredSize.Should().Be(new Size(20, 8));
			SUT.UnclippedDesiredSize.Should().Be(new Size(39, 8));

			SUT.Arrange(finalRect: new Rect(x: 0, y: 0, width: 20, height: 20));
			Assert.AreEqual(expected: new Rect(x: 0, y: 0, width: 5, height: 20), actual: c1.Arranged);
			Assert.AreEqual(expected: new Rect(x: 10, y: 0, width: 12, height: 20), actual: c2.Arranged);
			Assert.AreEqual(expected: new Rect(x: 27, y: 0, width: 12, height: 20), actual: c3.Arranged);

			Assert.AreEqual(expected: 3, actual: SUT.GetChildren().Count());
		}


		[TestMethod]
		public void When_Vertical_And_ArrangeIsBiggerThanMeasure()
		{
			var SUT = new StackPanel { Name = "test", Orientation = Orientation.Vertical };

			var c1 = SUT.AddChild(
				child: new View { Name = "Child01", RequestedDesiredSize = new Size(width: 10, height: 10) }
			);

			SUT.Measure(availableSize: new Size(width: 20, height: 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(expected: new Size(width: 10, height: 10), actual: measuredSize, message: "measuredSize");

			SUT.Arrange(finalRect: new Rect(x: 0, y: 0, width: 20, height: 20));
			Assert.AreEqual(expected: new Rect(x: 0, y: 0, width: 20, height: 10), actual: c1.Arranged);

			Assert.AreEqual(expected: 1, actual: SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_Horizontal_And_ArrangeIsBiggerThanMeasure()
		{
			var SUT = new StackPanel { Name = "test", Orientation = Orientation.Horizontal };

			var c1 = SUT.AddChild(
				child: new View { Name = "Child01", RequestedDesiredSize = new Size(width: 10, height: 10) }
			);

			SUT.Measure(availableSize: new Size(width: 20, height: 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(expected: new Size(width: 10, height: 10), actual: measuredSize, message: "measuredSize");

			SUT.Arrange(finalRect: new Rect(x: 0, y: 0, width: 20, height: 20));
			Assert.AreEqual(expected: new Rect(x: 0, y: 0, width: 10, height: 20), actual: c1.Arranged);

			Assert.AreEqual(expected: 1, actual: SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_Vertical_And_Fixed_Height_Item_With_Margin()
		{
			var SUT = new StackPanel { Name = "test", Orientation = Orientation.Vertical };

			var c1 = SUT.AddChild(
				child: new View
				{
					Name = "Child01",
					RequestedDesiredSize = new Size(width: 10, height: 10),
				}
			);


			var c2 = SUT.AddChild(
				child: new View
				{
					Name = "Child02",
					Height = 10,
					Margin = new Thickness(uniformLength: 10)
				}
			);

			SUT.Measure(availableSize: new Size(width: 40, height: float.PositiveInfinity));
			using (new AssertionScope("Desired Sizes"))
			{
				SUT.DesiredSize.Should().Be(new Size(20, 40));
				c1.DesiredSize.Should().Be(new Size(10, 10));
				c2.DesiredSize.Should().Be(new Size(20, 30));
			}

			SUT.Arrange(finalRect: new Rect(x: 0, y: 0, width: 30, height: 40));

			using (new AssertionScope("Arranged Sizes"))
			{
				SUT.Arranged.Should().Be((Rect)"0,0,30,40");
				c1.Arranged.Should().Be((Rect)"0,0,30,10");
				c2.Arranged.Should().Be((Rect)"10,20,10,10");

				SUT.GetChildren().Should().HaveCount(2);
			}
		}

		[TestMethod]
		public void When_Vertical_And_Fixed_Height_And_Width_Item_With_Margin()
		{
			var SUT = new StackPanel { Name = "test", Orientation = Orientation.Vertical };

			var c1 = SUT.AddChild(
				child: new View
				{
					Name = "Child02",
					DesiredSizeSelector = s =>
					{
						s.Width.Should().Be(expected: 30.0d);
						s.Height.Should().Be(expected: double.PositiveInfinity);
						return new Size(width: 10, height: 10);
					},
					Height = 10,
					Margin = new Thickness(uniformLength: 10)
				}
			);

			SUT.Measure(availableSize: new Size(width: 30, height: 30));
			SUT.DesiredSize.Should().Be(new Size(10, 10));

			c1.DesiredSize.Should().Be(new Size(10, 10));

			SUT.Arrange(finalRect: new Rect(x: 0, y: 0, width: 30, height: 30));
			SUT.Arranged.Should().Be((Rect)"0,0,30,30");
			c1.Arranged.Should().Be((Rect)"10,10,10,0"); // size is 10x0 because of margins (w= 30-(10+10), h=10-(10+10))

			SUT.GetChildren().Should().HaveCount(1);
		}

		[TestMethod]
		public void When_Vertical_And_Fixed_Width_Item()
		{
			var SUT = new StackPanel { Name = "test", Orientation = Orientation.Vertical };

			var c1 = SUT.AddChild(
				child: new View
				{
					Name = "Child01",
					RequestedDesiredSize = new Size(width: 10, height: 8),
					Width = 10
				}
			);

			SUT.Measure(availableSize: new Size(width: 20, height: 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(expected: new Size(width: 10, height: 8), actual: measuredSize, message: "measuredSize");

			Assert.AreEqual(expected: new Size(width: 10, height: 8), actual: c1.DesiredSize);

			SUT.Arrange(finalRect: new Rect(x: 0, y: 0, width: 20, height: 20));
			Assert.AreEqual(expected: new Rect(x: 5, y: 0, width: 10, height: 8), actual: c1.Arranged);

			Assert.AreEqual(expected: 1, actual: SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_Vertical_And_Fixed_MaxWidth_UnderSized()
		{
			var SUT = new StackPanel { Name = "test", Orientation = Orientation.Vertical };

			var c1 = SUT.AddChild(
				child: new View
				{
					Name = "Child01",
					RequestedDesiredSize = new Size(width: 10, height: 8),
					MaxWidth = 20
				}
			);

			SUT.Measure(availableSize: new Size(width: 10, height: 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(expected: new Size(width: 10, height: 8), actual: measuredSize, message: "measuredSize");

			Assert.AreEqual(expected: new Size(width: 10, height: 8), actual: c1.DesiredSize);

			SUT.Arrange(finalRect: new Rect(x: 0, y: 0, width: 30, height: 20));
			Assert.AreEqual(expected: new Rect(x: 5, y: 0, width: 20, height: 8), actual: c1.Arranged);

			Assert.AreEqual(expected: 1, actual: SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_Vertical_And_Fixed_MaxWidth_Oversized()
		{
			var SUT = new StackPanel { Name = "test", Orientation = Orientation.Vertical };

			var c1 = SUT.AddChild(
				child: new View
				{
					Name = "Child01",
					RequestedDesiredSize = new Size(width: 10, height: 8),
					MaxWidth = 20
				}
			);

			SUT.Measure(availableSize: new Size(width: 25, height: 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(expected: new Size(width: 10, height: 8), actual: measuredSize, message: "measuredSize");
			Assert.AreEqual(expected: new Size(width: 25, height: float.PositiveInfinity), actual: c1.AvailableMeasureSize, message: "AvailableMeasureSize");

			Assert.AreEqual(expected: new Size(width: 10, height: 8), actual: c1.DesiredSize);

			SUT.Arrange(finalRect: new Rect(x: 0, y: 0, width: 30, height: 20));
			Assert.AreEqual(expected: new Rect(x: 5, y: 0, width: 20, height: 8), actual: c1.Arranged);

			Assert.AreEqual(expected: 1, actual: SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_Horizontal_And_Fixed_Height_Item()
		{
			var SUT = new StackPanel { Name = "test", Orientation = Orientation.Horizontal };

			var c1 = SUT.AddChild(
				child: new View
				{
					Name = "Child01",
					RequestedDesiredSize = new Size(width: 8, height: 10),
					Height = 10
				}
			);

			SUT.Measure(availableSize: new Size(width: 20, height: 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(expected: new Size(width: 8, height: 10), actual: measuredSize, message: "measuredSize");

			Assert.AreEqual(expected: new Size(width: 8, height: 10), actual: c1.DesiredSize);

			SUT.Arrange(finalRect: new Rect(x: 0, y: 0, width: 20, height: 20));
			Assert.AreEqual(expected: new Rect(x: 0, y: 5, width: 8, height: 10), actual: c1.Arranged);

			Assert.AreEqual(expected: 1, actual: SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_Horizontal_And_Fixed_Width_Item_And_Measured_Height_is_Valid()
		{
			var SUT = new StackPanel { Name = "test", Orientation = Orientation.Horizontal };

			var c1 = SUT.AddChild(
				child: new View
				{
					Name = "Child01",
					DesiredSizeSelector = s =>
					{

						s.Height.Should().Be(expected: 20.0d);

						return new Size(width: 8, height: 10);
					},
					Height = 10
				}
			);

			SUT.Measure(availableSize: new Size(width: 20, height: 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(expected: new Size(width: 8, height: 10), actual: measuredSize, message: "measuredSize");

			Assert.AreEqual(expected: new Size(width: 8, height: 10), actual: c1.DesiredSize);

			SUT.Arrange(finalRect: new Rect(x: 0, y: 0, width: 20, height: 20));
			Assert.AreEqual(expected: new Rect(x: 0, y: 5, width: 8, height: 10), actual: c1.Arranged);

			Assert.AreEqual(expected: 1, actual: SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_Vertical_And_Fixed_Height_Item_ArrangeOverride()
		{
			var SUT = new MyStackPanel();

			SUT.AddChild(child: new Border { Height = 47, Width = 112 });

			var size1 = new Size(width: 1000, height: 1000);
			var arrange1 = SUT.ArrangeOverridePublic(finalSize: size1);
			Assert.AreEqual(expected: size1, actual: arrange1);
			var size2 = new Size(width: 2000, height: 2000);
			var arrange2 = SUT.ArrangeOverridePublic(finalSize: size2);
			Assert.AreEqual(expected: size2, actual: arrange2);
		}

		public partial class MyStackPanel : StackPanel
		{
			public Size ArrangeOverridePublic(Size finalSize) => ArrangeOverride(arrangeSize: finalSize);
		}
	}
}
