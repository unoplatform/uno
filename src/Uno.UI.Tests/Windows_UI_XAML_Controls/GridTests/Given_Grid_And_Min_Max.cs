using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Tests.Windows_UI_XAML_Controls.GridTests
{
	[TestClass]
#if !IS_UNIT_TESTS && !WINAPPSDK
	[RuntimeTests.RunsOnUIThread]
#endif
	public class Given_Grid_And_Min_Max
	{
		[TestMethod]
		public void When_Max_Width_Star()
		{
			ConstructAndTestSingleRowGrid(400,
				(60, GridUnitType.Star, 1, null, 60),
				(100, GridUnitType.Star, 1, null, 100),
				(40, GridUnitType.Auto, 40, null, null)
			);
		}

		[TestMethod]
		public void When_Max_Width_Star_2()
		{
			ConstructAndTestSingleRowGrid(400,
				(100, GridUnitType.Star, 1, null, 100),
				(60, GridUnitType.Star, 1, null, 60),
				(40, GridUnitType.Auto, 40, null, null)
			);
		}

		[TestMethod]
		public void When_Max_Width_Star_3()
		{
			ConstructAndTestSingleRowGrid(400,
				(100, GridUnitType.Star, 1, null, 100),
				(60, GridUnitType.Star, 1, null, 60),
				(240, GridUnitType.Star, 1, null, null)
			);
		}

		[TestMethod]
		public void When_Min_Width_Star()
		{
			ConstructAndTestSingleRowGrid(300,
				(150, GridUnitType.Star, 1, 150, null),
				(90, GridUnitType.Star, 1, 90, null),
				(60, GridUnitType.Star, 1, null, null)
			);
		}

		[TestMethod]
		public void When_Min_And_Max_Width_Star()
		{
			ConstructAndTestSingleRowGrid(350,
				(100, GridUnitType.Star, 1, null, 100),
				(210, GridUnitType.Star, 1, 60, null),
				(40, GridUnitType.Auto, 40, null, null)
			);
		}

		[TestMethod]
		public void When_Min_And_Max_Width_Star_2()
		{
			ConstructAndTestSingleRowGrid(350,
				(210, GridUnitType.Star, 1, 60, null),
				(100, GridUnitType.Star, 1, null, 100),
				(40, GridUnitType.Auto, 40, null, null)
			);
		}

		[TestMethod]
		public void When_All_Max_Width_Star()
		{
			ConstructAndTestSingleRowGrid(400,
				(30, GridUnitType.Star, 1, null, 30),
				(20, GridUnitType.Star, 1, null, 20),
				(30, GridUnitType.Star, 1, null, 30),
				(20, GridUnitType.Star, 1, null, 20),
				(30, GridUnitType.Star, 1, null, 30),
				(20, GridUnitType.Star, 1, null, 20)
			);
		}

		[TestMethod]
		public void When_Max_Width_Auto()
		{
			ConstructAndTestSingleRowGrid(350,
				(70, GridUnitType.Auto, 70, null, 30), //Auto measured width overrides MaxWidth
				(60, GridUnitType.Auto, 60, null, null)
			);
		}

		[TestMethod]
		public void When_Min_Width_Auto()
		{
			ConstructAndTestSingleRowGrid(350,
				(60, GridUnitType.Auto, 60, 100, null),
				(180, GridUnitType.Star, 1, null, null), // The auto column takes its MinWidth (even though the child is smaller), so the star column is reduced accordingly
				(70, GridUnitType.Auto, 70, null, null)
			);
		}

		[TestMethod]
		public void When_Max_Height_Star()
		{
			ConstructAndTestSingleColumnGrid(400,
				(60, GridUnitType.Star, 1, null, 60),
				(100, GridUnitType.Star, 1, null, 100),
				(40, GridUnitType.Auto, 40, null, null)
			);
		}

		[TestMethod]
		public void When_Max_Height_Star_2()
		{
			ConstructAndTestSingleColumnGrid(400,
				(100, GridUnitType.Star, 1, null, 100),
				(60, GridUnitType.Star, 1, null, 60),
				(40, GridUnitType.Auto, 40, null, null)
			);
		}

		[TestMethod]
		public void When_Max_Height_Star_3()
		{
			ConstructAndTestSingleColumnGrid(400,
				(100, GridUnitType.Star, 1, null, 100),
				(60, GridUnitType.Star, 1, null, 60),
				(240, GridUnitType.Star, 1, null, null)
			);
		}

		[TestMethod]
		public void When_Min_Height_Star()
		{
			ConstructAndTestSingleColumnGrid(300,
				(150, GridUnitType.Star, 1, 150, null),
				(90, GridUnitType.Star, 1, 90, null),
				(60, GridUnitType.Star, 1, null, null)
			);
		}

		[TestMethod]
		public void When_Min_And_Max_Height_Star()
		{
			ConstructAndTestSingleColumnGrid(350,
				(100, GridUnitType.Star, 1, null, 100),
				(210, GridUnitType.Star, 1, 60, null),
				(40, GridUnitType.Auto, 40, null, null)
			);
		}

		[TestMethod]
		public void When_Min_And_Max_Height_Star_2()
		{
			ConstructAndTestSingleColumnGrid(350,
				(210, GridUnitType.Star, 1, 60, null),
				(100, GridUnitType.Star, 1, null, 100),
				(40, GridUnitType.Auto, 40, null, null)
			);
		}

		[TestMethod]
		public void When_All_Max_Height_Star()
		{
			ConstructAndTestSingleColumnGrid(400,
				(30, GridUnitType.Star, 1, null, 30),
				(20, GridUnitType.Star, 1, null, 20),
				(30, GridUnitType.Star, 1, null, 30),
				(20, GridUnitType.Star, 1, null, 20),
				(30, GridUnitType.Star, 1, null, 30),
				(20, GridUnitType.Star, 1, null, 20)
			);
		}

		[TestMethod]
		public void When_Max_Height_Auto()
		{
			ConstructAndTestSingleColumnGrid(350,
				(70, GridUnitType.Auto, 70, null, 30), //Auto measured height overrides MaxHeight
				(60, GridUnitType.Auto, 60, null, null)
			);
		}

		[TestMethod]
		public void When_Min_Height_Auto()
		{
			ConstructAndTestSingleColumnGrid(350,
				(60, GridUnitType.Auto, 60, 100, null),
				(180, GridUnitType.Star, 1, null, null), // The auto row takes its MinHeight (even though the child is smaller), so the star row is reduced accordingly
				(70, GridUnitType.Auto, 70, null, null)
			);
		}

		[TestMethod]
		public void When_SingleRow_And_MinWidth_And_Same_Width()
		{
			var sut = new Grid();
			sut.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLengthHelper.Auto });
			sut.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star), MinWidth = 48 });

			var inner = new Border() { Width = 343, Height = 0 };

			sut.Children.Add(inner);

			sut.Measure(new Size(343, 979));

			Assert.AreEqual(new Size(343, 0), sut.DesiredSize);
		}

		[TestMethod]
		public void When_SingleColumn_And_MinHeight_And_Same_Height()
		{
			var sut = new Grid();
			sut.RowDefinitions.Add(new RowDefinition() { Height = GridLengthHelper.Auto });
			sut.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star), MinHeight = 48 });

			var inner = new Border() { Width = 0, Height = 343 };

			sut.Children.Add(inner);

			sut.Measure(new Size(979, 343));

			Assert.AreEqual(new Size(0, 343), sut.DesiredSize);
		}

		private void ConstructAndTestSingleRowGrid(double gridWidth, params (double ExpectedWidth, GridUnitType UnitType, double Size, double? MinWidth, double? MaxWidth)[] columns)
		{
			var grid = ConstructSingleRowGrid(columns.Select(c => (c.UnitType, c.Size, c.MinWidth, c.MaxWidth)).ToArray());

			grid.Width = gridWidth;
			grid.Height = 100;

			Assert.HasCount(columns.Length, grid.Children);

			grid.Measure(new Size(10000, 10000));
			var desired = grid.DesiredSize;
			grid.Arrange(new Rect(0, 0, desired.Width, desired.Height));

			for (int i = 0; i < columns.Length; i++)
			{
				var child = grid.Children[i] as FrameworkElement;

				Assert.AreEqual(columns[i].ExpectedWidth, child.ActualWidth, 1e-9);
			}
		}

		private Grid ConstructSingleRowGrid(params (GridUnitType UnitType, double Size, double? MinWidth, double? MaxWidth)[] columns)
		{
			return ConstructSingleRowGrid(
				columns.Select(col =>
				{
					var colDef = new ColumnDefinition
					{
						Width = new GridLength(col.Size, col.UnitType),
						MinWidth = col.MinWidth ?? 0,
						MaxWidth = col.MaxWidth ?? double.PositiveInfinity
					};

					var childWidth = col.UnitType == GridUnitType.Auto ? col.Size : (double?)null;

					return (colDef, childWidth);
				}).ToArray()
			);
		}

		private Grid ConstructSingleRowGrid(params (ColumnDefinition Column, double? ChildWidth)[] columns)
		{
			var childList = new List<FrameworkElement>();

			var grid = new Grid();
			var colNo = 0;
			foreach (var col in columns)
			{
				grid.ColumnDefinitions.Add(col.Column);

				var child = new Border { VerticalAlignment = VerticalAlignment.Stretch };
				if (col.ChildWidth.HasValue)
				{
					child.Width = col.ChildWidth.Value;
				}
				else
				{
					child.HorizontalAlignment = HorizontalAlignment.Stretch;
				}

				Grid.SetColumn(child, colNo);

				grid.Children.Add(child);

				colNo++;
			}

			grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50) });

			return grid;
		}

		private void ConstructAndTestSingleColumnGrid(double gridHeight, params (double ExpectedHeight, GridUnitType UnitType, double Size, double? MinHeight, double? MaxHeight)[] rows)
		{
			var grid = ConstructSingleColumnGrid(rows.Select(c => (c.UnitType, c.Size, c.MinHeight, c.MaxHeight)).ToArray());

			grid.Height = gridHeight;
			grid.Width = 100;

			Assert.HasCount(rows.Length, grid.Children);

			grid.Measure(new Size(10000, 10000));
			var desired = grid.DesiredSize;
			grid.Arrange(new Rect(0, 0, desired.Width, desired.Height));

			for (int i = 0; i < rows.Length; i++)
			{
				var child = grid.Children[i] as FrameworkElement;

				Assert.AreEqual(rows[i].ExpectedHeight, child.ActualHeight, 1e-9);
			}
		}

		private Grid ConstructSingleColumnGrid(params (GridUnitType UnitType, double Size, double? MinHeight, double? MaxHeight)[] rows)
		{
			return ConstructSingleColumnGrid(
				rows.Select(col =>
				{
					var colDef = new RowDefinition
					{
						Height = new GridLength(col.Size, col.UnitType),
						MinHeight = col.MinHeight ?? 0,
						MaxHeight = col.MaxHeight ?? double.PositiveInfinity
					};

					var childHeight = col.UnitType == GridUnitType.Auto ? col.Size : (double?)null;

					return (colDef, childHeight);
				}).ToArray()
			);
		}

		private Grid ConstructSingleColumnGrid(params (RowDefinition Row, double? ChildHeight)[] rows)
		{
			var childList = new List<FrameworkElement>();

			var grid = new Grid();
			var colNo = 0;
			foreach (var col in rows)
			{
				grid.RowDefinitions.Add(col.Row);

				var child = new Border { VerticalAlignment = VerticalAlignment.Stretch };
				if (col.ChildHeight.HasValue)
				{
					child.Height = col.ChildHeight.Value;
				}
				else
				{
					child.HorizontalAlignment = HorizontalAlignment.Stretch;
				}

				Grid.SetRow(child, colNo);

				grid.Children.Add(child);

				colNo++;
			}

			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });

			return grid;
		}
	}
}
