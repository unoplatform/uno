using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
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
	public class Given_Grid : Context
	{
		[TestMethod]
		public void When_Empty_And_MeasuredEmpty()
		{
			var SUT = new Grid() { Name = "test" };

			SUT.Measure(default(Windows.Foundation.Size));
			var size = SUT.DesiredSize;
			SUT.Arrange(Windows.Foundation.Rect.Empty);

			Assert.AreEqual(default(Windows.Foundation.Size), size);
			Assert.IsTrue(SUT.GetChildren().None());
		}

		[TestMethod]
		public void When_Empty_And_Measured_Non_Empty()
		{
			var SUT = new Grid() { Name = "test" };

			SUT.Measure(new Windows.Foundation.Size(10, 10));
			var size = SUT.DesiredSize;
			SUT.Arrange(Windows.Foundation.Rect.Empty);

			Assert.AreEqual(size, default(Windows.Foundation.Size));
			Assert.IsTrue(SUT.GetChildren().None());
		}

		[TestMethod]
		public void When_Grid_Has_One_Element()
		{
			var SUT = new Grid() { Name = "test" };

			SUT.AddChild(new View { Name = "Child01", RequestedDesiredSize = new Windows.Foundation.Size(10, 10) });

			SUT.Measure(new Windows.Foundation.Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			SUT.Arrange(new Windows.Foundation.Rect(0, 0, 20, 20));

			Assert.AreEqual(new Windows.Foundation.Rect(0, 0, 20, 20), SUT.GetChildren().First().Arranged);

			Assert.AreEqual(measuredSize, new Windows.Foundation.Size(10, 10));
			Assert.AreEqual(1, SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_Grid_Has_One_Colums_And_One_Row_And_No_Size_Spec()
		{
			var SUT = new Grid() { Name = "test" };

			var c1 = SUT.AddChild(new View { Name = "Child01", RequestedDesiredSize = new Windows.Foundation.Size(10, 10) });
			var c2 = SUT.AddChild(new View { Name = "Child02", RequestedDesiredSize = new Windows.Foundation.Size(10, 10) });

			SUT.Measure(new Windows.Foundation.Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			SUT.Arrange(new Windows.Foundation.Rect(0, 0, 20, 20));

			Assert.AreEqual(new Windows.Foundation.Rect(0, 0, 20, 20), c1.Arranged);
			Assert.AreEqual(new Windows.Foundation.Rect(0, 0, 20, 20), c2.Arranged);

			//Assert.AreEqual(new Windows.Foundation.Rect(0, 0, 20, 20), Windows.Foundation.default(Size));
			Assert.AreEqual(measuredSize, new Windows.Foundation.Size(10, 10));
			Assert.AreEqual(2, SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_Grid_Has_Two_Colums_And_One_Row_And_No_Size_Spec()
		{
			var SUT = new Grid() { Name = "test" };

			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });

			var c1 = SUT.AddChild(new View { Name = "Child01", RequestedDesiredSize = new Windows.Foundation.Size(10, 10) });
			var c2 = SUT.AddChild(new View { Name = "Child02", RequestedDesiredSize = new Windows.Foundation.Size(10, 10) });

			Grid.SetColumn(c1, 0);
			Grid.SetColumn(c2, 1);

			SUT.Measure(new Windows.Foundation.Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			SUT.Arrange(new Windows.Foundation.Rect(0, 0, 20, 20));

			Assert.AreEqual(new Windows.Foundation.Rect(0, 0, 10, 20), c1.Arranged);
			Assert.AreEqual(new Windows.Foundation.Rect(10, 0, 10, 20), c2.Arranged);

			//Assert.AreEqual(new Windows.Foundation.Rect(0, 0, 20, 20), Windows.Foundation.default(Size));
			Assert.AreEqual(measuredSize, new Windows.Foundation.Size(20, 10));
			Assert.AreEqual(2, SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_Grid_Has_Two_Colums_And_Two_Rows_And_No_Size_Spec()
		{
			var SUT = new Grid() { Name = "test" };

			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });

			SUT.RowDefinitions.Add(new RowDefinition { Height = "*" });
			SUT.RowDefinitions.Add(new RowDefinition { Height = "*" });

			var c1 = SUT.AddChild(
				new View { Name = "Child01", RequestedDesiredSize = new Windows.Foundation.Size(10, 10) }
				.GridPosition(0, 0)
			);

			var c2 = SUT.AddChild(
				new View { Name = "Child02", RequestedDesiredSize = new Windows.Foundation.Size(10, 10) }
				.GridPosition(0, 1)
			);

			var c3 = SUT.AddChild(
				new View { Name = "Child03", RequestedDesiredSize = new Windows.Foundation.Size(10, 10) }
				.GridPosition(1, 0)
			);

			var c4 = SUT.AddChild(
				new View { Name = "Child04", RequestedDesiredSize = new Windows.Foundation.Size(10, 10) }
				.GridPosition(1, 1)
			);

			SUT.Measure(new Windows.Foundation.Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			SUT.Arrange(new Windows.Foundation.Rect(0, 0, 20, 20));

			Assert.AreEqual(new Windows.Foundation.Rect(0, 0, 10, 10), c1.Arranged);
			Assert.AreEqual(new Windows.Foundation.Rect(10, 0, 10, 10), c2.Arranged);
			Assert.AreEqual(new Windows.Foundation.Rect(0, 10, 10, 10), c3.Arranged);
			Assert.AreEqual(new Windows.Foundation.Rect(10, 10, 10, 10), c4.Arranged);

			//Assert.AreEqual(new Windows.Foundation.Rect(0, 0, 20, 20), Windows.Foundation.default(Size));
			Assert.AreEqual(measuredSize, new Windows.Foundation.Size(20, 20));
			Assert.AreEqual(4, SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_Grid_Has_Two_Star_Uneven_Colums_And_One_Row()
		{
			var SUT = new Grid() { Name = "test" };

			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "2*" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });

			var c1 = SUT.AddChild(
				new View { Name = "Child01", RequestedDesiredSize = new Windows.Foundation.Size(10, 10) }
				.GridPosition(0, 0)
			);

			var c2 = SUT.AddChild(
				new View { Name = "Child02", RequestedDesiredSize = new Windows.Foundation.Size(10, 10) }
				.GridPosition(0, 1)
			);

			SUT.Measure(new Windows.Foundation.Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			SUT.Arrange(new Windows.Foundation.Rect(0, 0, 20, 20));

			Assert.AreEqual(new Windows.Foundation.Rect(0, 0, (20 / 3.0) * 2.0, 20), c1.Arranged);
			Assert.AreEqual(new Windows.Foundation.Rect((20 / 3.0) * 2.0, 0, (20 / 3.0) * 1.0, 20), c2.Arranged);

			//Assert.AreEqual(new Windows.Foundation.Rect(0, 0, 20, 20), Windows.Foundation.default(Size));
			Assert.AreEqual(measuredSize, new Windows.Foundation.Size(20, 10));
			Assert.AreEqual(2, SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_Grid_Has_One_Absolute_Column_And_One_Star_Column_And_One_Row()
		{
			var SUT = new Grid() { Name = "test" };

			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "5" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });

			var c1 = SUT.AddChild(
				new View { Name = "Child01", RequestedDesiredSize = new Windows.Foundation.Size(10, 10) }
				.GridPosition(0, 0)
			);

			var c2 = SUT.AddChild(
				new View { Name = "Child02", RequestedDesiredSize = new Windows.Foundation.Size(10, 10) }
				.GridPosition(0, 1)
			);

			SUT.Measure(new Windows.Foundation.Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			SUT.Arrange(new Windows.Foundation.Rect(0, 0, 20, 20));

			Assert.AreEqual(new Windows.Foundation.Rect(0, 0, 5, 20), c1.Arranged);
			Assert.AreEqual(new Windows.Foundation.Rect(5, 0, 15, 20), c2.Arranged);
			
			Assert.AreEqual(new Windows.Foundation.Size(15, 10), measuredSize);
			Assert.AreEqual(2, SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_Grid_Has_One_Variable_Sized_Element_With_ColSpan_and_Three_Columns()
		{
			var SUT = new Grid() { Name = "test" };

			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });

			var c1 = SUT.AddChild(
				new View
				{
					Name = "Child01",
					DesiredSizeSelector = s => s.Width > 10 ? new Windows.Foundation.Size(20, 5) : new Windows.Foundation.Size(10, 10)
				}
				.GridColumnSpan(2)
			);

			SUT.Measure(new Windows.Foundation.Size(30, 30));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Windows.Foundation.Size(20, 5), c1.DesiredSize);

			SUT.Arrange(new Windows.Foundation.Rect(0, 0, 30, 30));
			Assert.AreEqual(new Windows.Foundation.Size(20, 5), c1.DesiredSize);

			Assert.AreEqual(new Windows.Foundation.Rect(0, 0, 20, 30), c1.Arranged);
			Assert.AreEqual(1, SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_Grid_Has_Two_Variable_Sized_Element_With_ColSpan_and_One_Auto_Columns()
		{
			var SUT = new Grid() { Name = "test" };

			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "auto" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });

			var c1 = SUT.AddChild(
				new View
				{
					Name = "Child01",
					DesiredSizeSelector = s => s.Width > 10 ? new Windows.Foundation.Size(20, 5) : new Windows.Foundation.Size(10, 10)
				}
				.GridColumnSpan(2)
			);

			var c2 = SUT.AddChild(
				new View
				{
					Name = "Child02",
					DesiredSizeSelector = s => new Windows.Foundation.Size(5, 5)
				}
				.GridPosition(0, 1)
			);

			SUT.Measure(new Windows.Foundation.Size(30, 30));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(new Windows.Foundation.Size(20, 5), c1.DesiredSize);
			Assert.AreEqual(new Windows.Foundation.Size(5, 5), c2.DesiredSize);

			SUT.Arrange(new Windows.Foundation.Rect(0, 0, 30, 30));
			Assert.AreEqual(new Windows.Foundation.Size(20, 5), c1.DesiredSize);
			Assert.AreEqual(new Windows.Foundation.Size(5, 5), c2.DesiredSize);

			Assert.AreEqual(new Windows.Foundation.Rect(0, 0, 17.5f, 30), c1.Arranged);
			Assert.AreEqual(new Windows.Foundation.Rect(12.5f, 0, 5, 30), c2.Arranged);
			Assert.AreEqual(2, SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_Grid_Has_One_Element_With_ColSpan_and_Three_Columns()
		{
			var SUT = new Grid() { Name = "test" };

			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });

			SUT.AddChild(
				new View { Name = "Child01", RequestedDesiredSize = new Windows.Foundation.Size(10, 10) }
				.GridColumnSpan(2)
			);

			SUT.Measure(new Windows.Foundation.Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			SUT.Arrange(new Windows.Foundation.Rect(0, 0, 20, 20));

			Assert.AreEqual(new Windows.Foundation.Rect(0, 0, (20.0 / 3.0) * 2.0, 20), SUT.GetChildren().First().Arranged);

			Assert.AreEqual(new Windows.Foundation.Size(15, 10), measuredSize);
			Assert.AreEqual(1, SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_Grid_Has_Three_Element_With_ColSpan_and_Four_Progressing_Columns()
		{
			var SUT = new Grid() { Name = "test" };
			

			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "2*" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "3*" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "4*" });

			SUT.RowDefinitions.Add(new RowDefinition { Height = "10" });
			SUT.RowDefinitions.Add(new RowDefinition { Height = "11" });
			SUT.RowDefinitions.Add(new RowDefinition { Height = "12" });

			var c1 = SUT.AddChild(
				new View { Name = "Child01", RequestedDesiredSize = new Windows.Foundation.Size(10, 10) }
				.GridColumnSpan(2)
			);

			var c2 = SUT.AddChild(
				new View { Name = "Child02", RequestedDesiredSize = new Windows.Foundation.Size(10, 10) }
				.GridPosition(1, 1)
				.GridColumnSpan(2)
			);

			var c3 = SUT.AddChild(
				new View { Name = "Child03", RequestedDesiredSize = new Windows.Foundation.Size(10, 10) }
				.GridPosition(2, 2)
				.GridColumnSpan(2)
			);

			SUT.Measure(new Windows.Foundation.Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			SUT.Arrange(new Windows.Foundation.Rect(0, 0, 20, 20));

			Assert.AreEqual(new Windows.Foundation.Rect(0, 0, 6, 10), c1.Arranged);
			Assert.AreEqual(new Windows.Foundation.Rect(2, 10, 10, 11), c2.Arranged);
			Assert.AreEqual(new Windows.Foundation.Rect(6, 21, 14, 12), c3.Arranged);

			Assert.AreEqual(new Windows.Foundation.Size(20, 33), measuredSize);
			Assert.AreEqual(3, SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_Grid_Has_One_Element_With_ColSpan_and_RowSpan_and_Three_Columns()
		{
			var SUT = new Grid() { Name = "test" };
			

			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });

			SUT.RowDefinitions.Add(new RowDefinition { Height = "*" });
			SUT.RowDefinitions.Add(new RowDefinition { Height = "*" });
			SUT.RowDefinitions.Add(new RowDefinition { Height = "*" });

			SUT.AddChild(
				new View { Name = "Child01", RequestedDesiredSize = new Windows.Foundation.Size(10, 10) }
				.GridColumnSpan(2)
				.GridRowSpan(2)
			);

			SUT.Measure(new Windows.Foundation.Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			SUT.Arrange(new Windows.Foundation.Rect(0, 0, 20, 20));

			Assert.AreEqual(
				new Windows.Foundation.Rect(0, 0, (20.0 / 3.0) * 2.0, (20.0 / 3.0) * 2.0),
				SUT.GetChildren().First().Arranged
			);
			
			Assert.AreEqual(new Windows.Foundation.Size(15, 15), measuredSize);
			Assert.AreEqual(1, SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_Grid_Has_One_Element_With_ColSpan_and_RowSpan_and_Three_Columns_And_Middle()
		{
			var SUT = new Grid() { Name = "test" };
			

			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });

			SUT.RowDefinitions.Add(new RowDefinition { Height = "*" });
			SUT.RowDefinitions.Add(new RowDefinition { Height = "*" });
			SUT.RowDefinitions.Add(new RowDefinition { Height = "*" });

			SUT.AddChild(
				new View { Name = "Child01", RequestedDesiredSize = new Windows.Foundation.Size(10, 10) }
				.GridColumnSpan(2)
				.GridRowSpan(2)
				.GridPosition(1, 1)
			);

			SUT.Measure(new Windows.Foundation.Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			SUT.Arrange(new Windows.Foundation.Rect(0, 0, 20, 20));

			Assert.AreEqual(
				new Windows.Foundation.Rect(
					20.0 / 3.0,
					20.0 / 3.0,
					(20.0 / 3.0) * 2.0,
					(20.0 / 3.0) * 2.0
				),
				SUT.GetChildren().First().Arranged
			);
			
			Assert.AreEqual(new Windows.Foundation.Size(15, 15), measuredSize);
			Assert.AreEqual(1, SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_Grid_Has_One_Element_With_ColSpan_Overflow_and_Three_Columns()
		{
			var SUT = new Grid() { Name = "test" };
			

			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });
			SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = "*" });

			SUT.AddChild(
				new View { Name = "Child01", RequestedDesiredSize = new Windows.Foundation.Size(10, 10) }
				.GridColumnSpan(4)
			);

			SUT.Measure(new Windows.Foundation.Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			SUT.Arrange(new Windows.Foundation.Rect(0, 0, 20, 20));

			Assert.AreEqual(new Windows.Foundation.Rect(0, 0, 20, 20), SUT.GetChildren().First().Arranged);
			
			Assert.AreEqual(new Windows.Foundation.Size(10, 10), measuredSize);
			Assert.AreEqual(1, SUT.GetChildren().Count());
		}

		[TestMethod]
		public void When_Grid_RowCollection_Changes()
		{
			var SUT = new Grid();

			SUT.ForceLoaded();

			var child = SUT.AddChild(
				new View { Name = "Child01", RequestedDesiredSize = new Windows.Foundation.Size(10, 10) }
				.GridRow(1)
			);

			SUT.Measure(new Windows.Foundation.Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			SUT.Arrange(new Windows.Foundation.Rect(0, 0, 20, 20));

			Assert.AreEqual(new Windows.Foundation.Rect(0, 0, 20, 20), child.Arranged);

			RowDefinition rowDefinition1;
			SUT.RowDefinitions.Add(rowDefinition1 = new RowDefinition() { Height = new GridLength(5) });
			SUT.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });

			SUT.Measure(new Windows.Foundation.Size(20, 20));
			measuredSize = SUT.DesiredSize;
			SUT.Arrange(new Windows.Foundation.Rect(0, 0, 20, 20));

			Assert.AreEqual(new Windows.Foundation.Rect(0, 5, 20, 10), child.Arranged);

			rowDefinition1.Height = new GridLength(10);
			Assert.AreEqual(3, SUT.InvalidateMeasureCallCount);
		}

		[TestMethod]
		public void When_Grid_ColumnCollection_Changes()
		{
			var SUT = new Grid();

			SUT.ForceLoaded();

			var child = SUT.AddChild(
				new View { Name = "Child01", RequestedDesiredSize = new Windows.Foundation.Size(10, 10) }
				.GridColumn(1)
			);

			SUT.Measure(new Windows.Foundation.Size(20, 20));
			var measuredSize = SUT.DesiredSize;
			SUT.Arrange(new Windows.Foundation.Rect(0, 0, 20, 20));

			Assert.AreEqual(new Windows.Foundation.Rect(0, 0, 20, 20), child.Arranged);

			ColumnDefinition ColumnDefinition1;
			SUT.ColumnDefinitions.Add(ColumnDefinition1 = new ColumnDefinition() { Width = new GridLength(5) });
			SUT.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });

			SUT.Measure(new Windows.Foundation.Size(20, 20));
			measuredSize = SUT.DesiredSize;
			SUT.Arrange(new Windows.Foundation.Rect(0, 0, 20, 20));

			Assert.AreEqual(new Windows.Foundation.Rect(5, 0, 10, 20), child.Arranged);

			ColumnDefinition1.Width = new GridLength(10);
			Assert.AreEqual(3, SUT.InvalidateMeasureCallCount);
		}
	}
}
