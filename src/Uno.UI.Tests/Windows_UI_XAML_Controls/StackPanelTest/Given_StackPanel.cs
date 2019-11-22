using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno;
using Uno.Extensions;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Uno.Disposables;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using View = Windows.UI.Xaml.FrameworkElement;

namespace Uno.UI.Tests.StackPanelTest
{
	[TestClass]
	public class Given_StackPanel : Context
	{
		[TestMethod]
		public void When_Vertical_And_SimpleLayout()
		{
			var SUT = new StackPanel() { Name = "test", Orientation = Orientation.Vertical };

			var c1 = SUT.AddChild(
				new View { Name = "Child01", RequestedDesiredSize = new Windows.Foundation.Size(10, 8) }
			);

			var c2 = SUT.AddChild(
				new View { Name = "Child02", RequestedDesiredSize = new Windows.Foundation.Size(10, 7) }
			);

			SUT.Measure(new Windows.Foundation.Size(20, 20));
			Assert.AreEqual(new Windows.Foundation.Size(10, 15), SUT.DesiredSize, "measuredSize");

			Assert.AreEqual(new Windows.Foundation.Size(10, 8), c1.RequestedDesiredSize);
			Assert.AreEqual(new Windows.Foundation.Size(10, 7), c2.RequestedDesiredSize);

			SUT.Arrange(new Windows.Foundation.Rect(0, 0,20, 20));
			Assert.AreEqual(new Windows.Foundation.Rect(0, 0, 20, 8), c1.Arranged);
			Assert.AreEqual(new Windows.Foundation.Rect(0, 8, 20, 7), c2.Arranged);

			Assert.AreEqual(2, SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_Horizontal_And_SimpleLayout()
		{
			var SUT = new StackPanel() { Name = "test", Orientation = Orientation.Horizontal };

			var c1 = SUT.AddChild(
				new View { Name = "Child01", RequestedDesiredSize = new Windows.Foundation.Size(5, 8) }
			);

			var c2 = SUT.AddChild(
				new View { Name = "Child02", RequestedDesiredSize = new Windows.Foundation.Size(12, 7) }
			);

			SUT.Measure(new Windows.Foundation.Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Windows.Foundation.Size(17, 8), measuredSize, "measuredSize");

			Assert.AreEqual(new Windows.Foundation.Size(5, 8), c1.RequestedDesiredSize);
			Assert.AreEqual(new Windows.Foundation.Size(12, 7), c2.RequestedDesiredSize);

			SUT.Arrange(new Windows.Foundation.Rect(0, 0,20, 20));
			Assert.AreEqual(new Windows.Foundation.Rect(0, 0, 5, 20), c1.Arranged);
			Assert.AreEqual(new Windows.Foundation.Rect(5, 0, 12, 20), c2.Arranged);

			Assert.AreEqual(2, SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_Vertical_And_SimpleLayout_With_Spacing()
		{
			var SUT = new StackPanel() { Name = "test", Orientation = Orientation.Vertical, Spacing = 5 };

			var c1 = SUT.AddChild(
				new View { Name = "Child01", RequestedDesiredSize = new Windows.Foundation.Size(10, 8) }
			);

			var c2 = SUT.AddChild(
				new View { Name = "Child02", RequestedDesiredSize = new Windows.Foundation.Size(10, 7) }
			);

			SUT.Measure(new Windows.Foundation.Size(20, 20));
			Assert.AreEqual(new Windows.Foundation.Size(10, 20), SUT.DesiredSize, "measuredSize");

			Assert.AreEqual(new Windows.Foundation.Size(10, 8), c1.RequestedDesiredSize);
			Assert.AreEqual(new Windows.Foundation.Size(10, 7), c2.RequestedDesiredSize);

			SUT.Arrange(new Windows.Foundation.Rect(0, 0, 20, 20));
			Assert.AreEqual(new Windows.Foundation.Rect(0, 0, 20, 8), c1.Arranged);
			Assert.AreEqual(new Windows.Foundation.Rect(0, 13, 20, 7), c2.Arranged);

			Assert.AreEqual(2, SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_Vertical_And_Three_With_Spacing()
		{
			var SUT = new StackPanel() { Name = "test", Orientation = Orientation.Vertical, Spacing = 5 };

			var c1 = SUT.AddChild(
				new View { Name = "Child01", RequestedDesiredSize = new Windows.Foundation.Size(10, 8) }
			);

			var c2 = SUT.AddChild(
				new View { Name = "Child02", RequestedDesiredSize = new Windows.Foundation.Size(10, 7) }
			);

			var c3 = SUT.AddChild(
				new View { Name = "Child03", RequestedDesiredSize = new Windows.Foundation.Size(10, 11) }
			);

			SUT.Measure(new Windows.Foundation.Size(20, 20));
			Assert.AreEqual(new Windows.Foundation.Size(10, 36), SUT.DesiredSize, "measuredSize");

			Assert.AreEqual(new Windows.Foundation.Size(10, 8), c1.RequestedDesiredSize);
			Assert.AreEqual(new Windows.Foundation.Size(10, 7), c2.RequestedDesiredSize);
			Assert.AreEqual(new Windows.Foundation.Size(10, 11), c3.RequestedDesiredSize);

			SUT.Arrange(new Windows.Foundation.Rect(0, 0, 20, 20));
			Assert.AreEqual(new Windows.Foundation.Rect(0, 0, 20, 8), c1.Arranged);
			Assert.AreEqual(new Windows.Foundation.Rect(0, 13, 20, 7), c2.Arranged);
			Assert.AreEqual(new Windows.Foundation.Rect(0, 25, 20, 11), c3.Arranged);

			Assert.AreEqual(3, SUT.GetChildren().Count());
		}


		[TestMethod]
		public void When_Horizontal_And_SimpleLayout_With_Spacing()
		{
			var SUT = new StackPanel() { Name = "test", Orientation = Orientation.Horizontal, Spacing = 5 };

			var c1 = SUT.AddChild(
				new View { Name = "Child01", RequestedDesiredSize = new Windows.Foundation.Size(5, 8) }
			);

			var c2 = SUT.AddChild(
				new View { Name = "Child02", RequestedDesiredSize = new Windows.Foundation.Size(12, 7) }
			);

			SUT.Measure(new Windows.Foundation.Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Windows.Foundation.Size(22, 8), measuredSize, "measuredSize");

			Assert.AreEqual(new Windows.Foundation.Size(5, 8), c1.RequestedDesiredSize);
			Assert.AreEqual(new Windows.Foundation.Size(12, 7), c2.RequestedDesiredSize);

			SUT.Arrange(new Windows.Foundation.Rect(0, 0, 20, 20));
			Assert.AreEqual(new Windows.Foundation.Rect(0, 0, 5, 20), c1.Arranged);
			Assert.AreEqual(new Windows.Foundation.Rect(10, 0, 12, 20), c2.Arranged);

			Assert.AreEqual(2, SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_Horizontal_And_Three_With_Spacing()
		{
			var SUT = new StackPanel() { Name = "test", Orientation = Orientation.Horizontal, Spacing = 5 };

			var c1 = SUT.AddChild(
				new View { Name = "Child01", RequestedDesiredSize = new Windows.Foundation.Size(5, 8) }
			);

			var c2 = SUT.AddChild(
				new View { Name = "Child02", RequestedDesiredSize = new Windows.Foundation.Size(12, 7) }
			);

			var c3 = SUT.AddChild(
				new View { Name = "Child02", RequestedDesiredSize = new Windows.Foundation.Size(12, 5) }
			);

			SUT.Measure(new Windows.Foundation.Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Windows.Foundation.Size(39, 8), measuredSize, "measuredSize");

			Assert.AreEqual(new Windows.Foundation.Size(5, 8), c1.RequestedDesiredSize);
			Assert.AreEqual(new Windows.Foundation.Size(12, 7), c2.RequestedDesiredSize);
			Assert.AreEqual(new Windows.Foundation.Size(12, 5), c3.RequestedDesiredSize);

			SUT.Arrange(new Windows.Foundation.Rect(0, 0, 20, 20));
			Assert.AreEqual(new Windows.Foundation.Rect(0, 0, 5, 20), c1.Arranged);
			Assert.AreEqual(new Windows.Foundation.Rect(10, 0, 12, 20), c2.Arranged);
			Assert.AreEqual(new Windows.Foundation.Rect(27, 0, 12, 20), c3.Arranged);

			Assert.AreEqual(3, SUT.GetChildren().Count());
		}


		[TestMethod]
		public void When_Vertical_And_ArrangeIsBiggerThanMeasure()
		{
			var SUT = new StackPanel() { Name = "test", Orientation = Orientation.Vertical };

			var c1 = SUT.AddChild(
				new View { Name = "Child01", RequestedDesiredSize = new Windows.Foundation.Size(10, 10) }
			);

			SUT.Measure(new Windows.Foundation.Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Windows.Foundation.Size(10, 10), measuredSize, "measuredSize");

			SUT.Arrange(new Windows.Foundation.Rect(0, 0,20, 20));
			Assert.AreEqual(new Windows.Foundation.Rect(0, 0, 20, 10), c1.Arranged);

			Assert.AreEqual(1, SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_Horizontal_And_ArrangeIsBiggerThanMeasure()
		{
			var SUT = new StackPanel() { Name = "test", Orientation = Orientation.Horizontal };

			var c1 = SUT.AddChild(
				new View { Name = "Child01", RequestedDesiredSize = new Windows.Foundation.Size(10, 10) }
			);

			SUT.Measure(new Windows.Foundation.Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Windows.Foundation.Size(10, 10), measuredSize, "measuredSize");

			SUT.Arrange(new Windows.Foundation.Rect(0, 0,20, 20));
			Assert.AreEqual(new Windows.Foundation.Rect(0, 0, 10, 20), c1.Arranged);

			Assert.AreEqual(1, SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_Vertical_And_Fixed_Height_Item_With_Margin()
		{
			var SUT = new StackPanel() { Name = "test", Orientation = Orientation.Vertical };

			var c1 = SUT.AddChild(
				new View
				{
					Name = "Child01",
					RequestedDesiredSize = new Windows.Foundation.Size(10, 10),
				}
			);


			var c2 = SUT.AddChild(
				new View
				{
					Name = "Child02",
					DesiredSizeSelector = s =>
					{
						s.Width.Should().Be(40.0d);
						s.Height.Should().Be(double.PositiveInfinity);
						return new Windows.Foundation.Size(10, 10);
					},
					Height = 10,
					Margin = new Thickness(10)
				}
			);

			SUT.Measure(new Windows.Foundation.Size(40, float.PositiveInfinity));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Windows.Foundation.Size(30, 40), measuredSize, "measuredSize");

			Assert.AreEqual(new Windows.Foundation.Size(10, 10), c1.DesiredSize);
			Assert.AreEqual(new Windows.Foundation.Size(30, 30), c2.DesiredSize);

			SUT.Arrange(new Windows.Foundation.Rect(0, 0,30, 40));
			Assert.AreEqual(new Windows.Foundation.Rect(0, 0, 30, 10), c1.Arranged);
			Assert.AreEqual(new Windows.Foundation.Rect(10, 20, 10, 10), c2.Arranged);

			Assert.AreEqual(2, SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_Vertical_And_Fixed_Height_And_Width_Item_With_Margin()
		{
			var SUT = new StackPanel() { Name = "test", Orientation = Orientation.Vertical };

			var c1 = SUT.AddChild(
				new View
				{
					Name = "Child02",
					DesiredSizeSelector = s =>
					{
						s.Width.Should().Be(30.0d);
						s.Height.Should().Be(double.PositiveInfinity);
						return new Windows.Foundation.Size(10, 10);
					},
					Height = 10,
					Margin = new Thickness(10)
				}
			);

			SUT.Measure(new Windows.Foundation.Size(30, 30));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Windows.Foundation.Size(30, 30), measuredSize, "measuredSize");

			Assert.AreEqual(new Windows.Foundation.Size(30, 30), c1.DesiredSize);

			SUT.Arrange(new Windows.Foundation.Rect(0, 0,30, 30));
			Assert.AreEqual(new Windows.Foundation.Rect(10, 10, 10, 10), c1.Arranged);

			Assert.AreEqual(1, SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_Vertical_And_Fixed_Width_Item()
		{
			var SUT = new StackPanel() { Name = "test", Orientation = Orientation.Vertical };

			var c1 = SUT.AddChild(
				new View
				{
					Name = "Child01",
					RequestedDesiredSize = new Windows.Foundation.Size(10, 8),
					Width = 10
				}
			);

			SUT.Measure(new Windows.Foundation.Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Windows.Foundation.Size(10, 8), measuredSize, "measuredSize");

			Assert.AreEqual(new Windows.Foundation.Size(10, 8), c1.DesiredSize);

			SUT.Arrange(new Windows.Foundation.Rect(0, 0,20, 20));
			Assert.AreEqual(new Windows.Foundation.Rect(5, 0, 10, 8), c1.Arranged);

			Assert.AreEqual(1, SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_Vertical_And_Fixed_MaxWidth_UnderSized()
		{
			var SUT = new StackPanel() { Name = "test", Orientation = Orientation.Vertical };

			var c1 = SUT.AddChild(
				new View
				{
					Name = "Child01",
					RequestedDesiredSize = new Windows.Foundation.Size(10, 8),
					MaxWidth = 20
				}
			);

			SUT.Measure(new Windows.Foundation.Size(10, 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Windows.Foundation.Size(10, 8), measuredSize, "measuredSize");

			Assert.AreEqual(new Windows.Foundation.Size(10, 8), c1.DesiredSize);

			SUT.Arrange(new Windows.Foundation.Rect(0, 0,30, 20));
			Assert.AreEqual(new Windows.Foundation.Rect(5, 0, 20, 8), c1.Arranged);

			Assert.AreEqual(1, SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_Vertical_And_Fixed_MaxWidth_Oversized()
		{
			var SUT = new StackPanel() { Name = "test", Orientation = Orientation.Vertical };

			var c1 = SUT.AddChild(
				new View
				{
					Name = "Child01",
					RequestedDesiredSize = new Windows.Foundation.Size(10, 8),
					MaxWidth = 20
				}
			);

			SUT.Measure(new Windows.Foundation.Size(25, 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Windows.Foundation.Size(10, 8), measuredSize, "measuredSize");
			Assert.AreEqual(new Windows.Foundation.Size(25, float.PositiveInfinity), c1.AvailableMeasureSize, "AvailableMeasureSize");

			Assert.AreEqual(new Windows.Foundation.Size(10, 8), c1.DesiredSize);

			SUT.Arrange(new Windows.Foundation.Rect(0, 0,30, 20));
			Assert.AreEqual(new Windows.Foundation.Rect(5, 0, 20, 8), c1.Arranged);

			Assert.AreEqual(1, SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_Horizontal_And_Fixed_Height_Item()
		{
			var SUT = new StackPanel() { Name = "test", Orientation = Orientation.Horizontal };

			var c1 = SUT.AddChild(
				new View
				{
					Name = "Child01",
					RequestedDesiredSize = new Windows.Foundation.Size(8, 10),
					Height = 10
				}
			);

			SUT.Measure(new Windows.Foundation.Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Windows.Foundation.Size(8, 10), measuredSize, "measuredSize");

			Assert.AreEqual(new Windows.Foundation.Size(8, 10), c1.DesiredSize);

			SUT.Arrange(new Windows.Foundation.Rect(0, 0,20, 20));
			Assert.AreEqual(new Windows.Foundation.Rect(0, 5, 8, 10), c1.Arranged);

			Assert.AreEqual(1, SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_Horizontal_And_Fixed_Width_Item_And_Measured_Height_is_Valid()
		{
			var SUT = new StackPanel() { Name = "test", Orientation = Orientation.Horizontal };

			var c1 = SUT.AddChild(
				new View
				{
					Name = "Child01",
					DesiredSizeSelector = s => {

						s.Height.Should().Be(20.0d);

						return new Windows.Foundation.Size(8, 10); 
					},
					Height = 10
				}
			);

			SUT.Measure(new Windows.Foundation.Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Windows.Foundation.Size(8, 10), measuredSize, "measuredSize");

			Assert.AreEqual(new Windows.Foundation.Size(8, 10), c1.DesiredSize);

			SUT.Arrange(new Windows.Foundation.Rect(0, 0, 20, 20));
			Assert.AreEqual(new Windows.Foundation.Rect(0, 5, 8, 10), c1.Arranged);

			Assert.AreEqual(1, SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_Vertical_And_Fixed_Height_Item_ArrangeOverride()
		{
			var SUT = new MyStackPanel();

			SUT.AddChild(new Border { Height = 47, Width = 112 });

			var size1 = new Windows.Foundation.Size(1000, 1000);
			var arrange1 = SUT.ArrangeOverridePublic(size1);
			Assert.AreEqual(size1, arrange1);
			var size2 = new Windows.Foundation.Size(2000, 2000);
			var arrange2 = SUT.ArrangeOverridePublic(size2);
			Assert.AreEqual(size2, arrange2);
		}

		public partial class MyStackPanel : StackPanel
		{
			public Windows.Foundation.Size ArrangeOverridePublic(Windows.Foundation.Size finalSize) => ArrangeOverride(finalSize);
		}
	}
}
