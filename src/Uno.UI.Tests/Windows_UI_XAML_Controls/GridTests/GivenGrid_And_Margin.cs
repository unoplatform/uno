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
	public class GivenGrid_And_Margin : Context
	{
		[TestMethod]
		public void When_One_Child_With_Margin_5()
		{
			var SUT = new Grid() { Name = "test" };

			var c1 = SUT.AddChild(
				new View
				{
					Name = "Child01",
					RequestedDesiredSize = new Windows.Foundation.Size(10, 10),
					Margin = new Thickness(5)
				}
				.GridPosition(0, 0)
			);

			SUT.Measure(new Windows.Foundation.Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Windows.Foundation.Size(20, 20), measuredSize);
			Assert.AreEqual(new Windows.Foundation.Size(10, 10), c1.RequestedDesiredSize);

			SUT.Arrange(new Windows.Foundation.Rect(0, 0,20, 20));

			Assert.AreEqual(new Windows.Foundation.Rect(5, 5, 10, 10), c1.Arranged);

			Assert.AreEqual(1, SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_One_Child_With_Margin_1234()
		{
			var SUT = new Grid() { Name = "test" };
			

			var c1 = SUT.AddChild(
				new View
				{
					Name = "Child01",
					RequestedDesiredSize = new Windows.Foundation.Size(10, 10),
					Margin = new Thickness(1, 2, 3, 4)
				}
				.GridPosition(0, 0)
			);

			SUT.Measure(new Windows.Foundation.Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Windows.Foundation.Size(14, 16), measuredSize);
			Assert.AreEqual(new Windows.Foundation.Size(10, 10), c1.RequestedDesiredSize);

			SUT.Arrange(new Windows.Foundation.Rect(0, 0,20, 20));

			Assert.AreEqual(new Windows.Foundation.Rect(1, 2, 16, 14), c1.Arranged);

			Assert.AreEqual(1, SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_One_Child_With_Margin_Botton_And_Center()
		{
			var SUT = new Grid() { Name = "test" };
			

			var c1 = SUT.AddChild(
				new View
				{
					Name = "Child01",
					RequestedDesiredSize = new Windows.Foundation.Size(10, 10),
					Margin = new Thickness(0, 0, 0, 30),
					HorizontalAlignment = HorizontalAlignment.Center,
					VerticalAlignment = VerticalAlignment.Center,
					Height = 10,
					Width = 10,
				}
				.GridPosition(0, 0)
			);

			SUT.Measure(new Windows.Foundation.Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Windows.Foundation.Size(10, 20), measuredSize);
			Assert.AreEqual(new Windows.Foundation.Size(10, 10), c1.RequestedDesiredSize);

			SUT.Arrange(new Windows.Foundation.Rect(0, 0,50, 50));

			Assert.AreEqual(new Windows.Foundation.Rect(20, 5, 10, 10), c1.Arranged);

			Assert.AreEqual(1, SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_One_Child_With_Margin_Botton_And_Bottom()
		{
			var SUT = new Grid() { Name = "test" };
			

			var c1 = SUT.AddChild(
				new View
				{
					Name = "Child01",
					RequestedDesiredSize = new Windows.Foundation.Size(10, 10),
					Margin = new Thickness(0, 0, 0, 30),
					HorizontalAlignment = HorizontalAlignment.Center,
					VerticalAlignment = VerticalAlignment.Bottom,
					Height = 10,
					Width = 10,
				}
				.GridPosition(0, 0)
			);

			SUT.Measure(new Windows.Foundation.Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Windows.Foundation.Size(10, 20), measuredSize);
			Assert.AreEqual(new Windows.Foundation.Size(10, 10), c1.RequestedDesiredSize);

			SUT.Arrange(new Windows.Foundation.Rect(0, 0,50, 50));

			Assert.AreEqual(new Windows.Foundation.Rect(20, 10, 10, 10), c1.Arranged);

			Assert.AreEqual(1, SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_One_Child_With_Margin_Botton_And_Top()
		{
			var SUT = new Grid() { Name = "test" };
			

			var c1 = SUT.AddChild(
				new View
				{
					Name = "Child01",
					RequestedDesiredSize = new Windows.Foundation.Size(10, 10),
					Margin = new Thickness(0, 0, 0, 30),
					HorizontalAlignment = HorizontalAlignment.Center,
					VerticalAlignment = VerticalAlignment.Top,
					Height = 10,
					Width = 10,
				}
				.GridPosition(0, 0)
			);

			SUT.Measure(new Windows.Foundation.Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Windows.Foundation.Size(10, 20), measuredSize);
			Assert.AreEqual(new Windows.Foundation.Size(10, 10), c1.RequestedDesiredSize);

			SUT.Arrange(new Windows.Foundation.Rect(0, 0,50, 50));

			Assert.AreEqual(new Windows.Foundation.Rect(20, 0, 10, 10), c1.Arranged);

			Assert.AreEqual(1, SUT.GetChildren().Count());
		}


		[TestMethod]
		public void When_One_Fixed_Size_Child_With_Margin_Right_And_Stretch()
		{
			var SUT = new Grid() { Name = "test" };
			

			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "auto" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });

			var c1 = SUT.AddChild(
				new View
				{
					Name = "Child01",
					RequestedDesiredSize = new Windows.Foundation.Size(50, 50),
					Margin = new Thickness(0, 0, 50, 0),
					HorizontalAlignment = HorizontalAlignment.Stretch,
					VerticalAlignment = VerticalAlignment.Stretch,
					Height = 50,
					Width = 50,
				}
				.GridPosition(0, 1)
			);
			 
			SUT.Measure(new Windows.Foundation.Size(300, 300));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Windows.Foundation.Size(100, 50), measuredSize);
			Assert.AreEqual(new Windows.Foundation.Size(50, 50), c1.RequestedDesiredSize);

			SUT.Arrange(new Windows.Foundation.Rect(0, 0,300, 300));

			Assert.AreEqual(new Windows.Foundation.Rect(100, 125, 50, 50), c1.Arranged);

			Assert.AreEqual(1, SUT.GetChildren().Count());
		}
	}
}
