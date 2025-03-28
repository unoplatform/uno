using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Tests.GridTests
{

	[TestClass]
	public class Given_Grid_Def_ActualSize
	{
		[TestMethod]
		public void When_Single_Row_All_Auto()
		{
			ConstructAndTestGrid(
				new Size(1000, 1000),
				false,
				Enumerable.Empty<(GridUnitType, double)>().ToArray(),
				Enumerable.Empty<double>().ToArray(),
				new[] { (GridUnitType.Auto, 1d), (GridUnitType.Auto, 1d), (GridUnitType.Auto, 1d), },
				new[] { 100d, 30d, 222d },
				(0, 0, new Border { Width = 100, Height = 20 }),
				(0, 1, new Border { Width = 30, Height = 20 }),
				(0, 2, new Border { Width = 222, Height = 20 })
			);

			ConstructAndTestGrid(
				new Size(1000, 1000),
				false,
				Enumerable.Empty<(GridUnitType, double)>().ToArray(),
				Enumerable.Empty<double>().ToArray(),
				new[] { (GridUnitType.Auto, 1d), (GridUnitType.Auto, 1d), (GridUnitType.Auto, 1d), },
				new[] { 222d, 30d, 0 },
				(0, 0, new Border { Width = 100, Height = 20 }),
				(0, 1, new Border { Width = 30, Height = 20 }),
				(0, 0, new Border { Width = 222, Height = 20 })
			);

		}

		[TestMethod]
		public void When_Single_Row_Star_And_Auto()
		{
			ConstructAndTestGrid(
				new Size(700, 1000),
				true,
				Enumerable.Empty<(GridUnitType, double)>().ToArray(),
				Enumerable.Empty<double>().ToArray(),
				new[] { (GridUnitType.Star, 1d), (GridUnitType.Auto, 1d), (GridUnitType.Star, 2d), },
				new[] { 200d, 100d, 400d },
				(0, 0, new Border { Width = 10, Height = 20 }),
				(0, 1, new Border { Width = 100, Height = 20 }),
				(0, 2, new Border { Width = 10, Height = 20 })
			);

			ConstructAndTestGrid(
				new Size(700, 1000),
				false,
				Enumerable.Empty<(GridUnitType, double)>().ToArray(),
				Enumerable.Empty<double>().ToArray(),
				new[] { (GridUnitType.Star, 1d), (GridUnitType.Auto, 1d), (GridUnitType.Star, 1d), },
				new[] { 14d, 100d, 14 },
				(0, 0, new Border { Width = 14, Height = 20 }),
				(0, 1, new Border { Width = 100, Height = 20 }),
				(0, 2, new Border { Width = 14, Height = 20 })
			);
		}

		[TestMethod]
		[Ignore("Uno's implementation diverges from UWP's for this case")]
		public void When_Single_Row_Multiple_Star_And_Auto()
		{

			ConstructAndTestGrid(
				new Size(700, 1000),
				false,
				Enumerable.Empty<(GridUnitType, double)>().ToArray(),
				Enumerable.Empty<double>().ToArray(),
				new[] { (GridUnitType.Star, 1d), (GridUnitType.Auto, 1d), (GridUnitType.Star, 2d), },
				new[] { 14d, 100d, 22d },
				(0, 0, new Border { Width = 14, Height = 20 }),
				(0, 1, new Border { Width = 100, Height = 20 }),
				(0, 2, new Border { Width = 22, Height = 20 }) //Uno gives an ActualWidth of 2x* = 28 for this column
			);
		}

		[TestMethod]
		public void When_Single_Column_All_Auto()
		{
			ConstructAndTestGrid(
				new Size(1000, 1000),
				false,
				new[] { (GridUnitType.Auto, 1d), (GridUnitType.Auto, 1d), (GridUnitType.Auto, 1d), },
				new[] { 100d, 30d, 222d },
				Enumerable.Empty<(GridUnitType, double)>().ToArray(),
				Enumerable.Empty<double>().ToArray(),
				(0, 0, new Border { Width = 20, Height = 100 }),
				(1, 0, new Border { Width = 20, Height = 30 }),
				(2, 0, new Border { Width = 20, Height = 222 })
			);

			ConstructAndTestGrid(
				new Size(1000, 1000),
				false,
				new[] { (GridUnitType.Auto, 1d), (GridUnitType.Auto, 1d), (GridUnitType.Auto, 1d), },
				new[] { 222d, 30d, 0 },
				Enumerable.Empty<(GridUnitType, double)>().ToArray(),
				Enumerable.Empty<double>().ToArray(),
				(0, 0, new Border { Width = 20, Height = 100 }),
				(1, 0, new Border { Width = 20, Height = 30 }),
				(0, 0, new Border { Width = 20, Height = 222 })
			);

		}

		private void ConstructAndTestGrid(
			Size availableSize,
			bool shouldStretch,
			(GridUnitType UnitType, double Size)[] rowDefinitions,
			double[] expectedRowHeights,
			(GridUnitType UnitType, double Size)[] columnDefinitions,
			double[] expectedColumnWidths,
			params (int Row, int Column, FrameworkElement Element)[] cells
		)
		{
			Assert.AreEqual(rowDefinitions.Length, expectedRowHeights.Length);
			Assert.AreEqual(columnDefinitions.Length, expectedColumnWidths.Length);

			var grid = ConstructGrid(rowDefinitions, columnDefinitions, cells);

			grid.Measure(availableSize);
			foreach (var def in grid.RowDefinitions)
			{
				// TODO: for now we make no guarantees to match UWP's value prior to Arrange pass
			}
			foreach (var def in grid.ColumnDefinitions)
			{
				// TODO: for now we make no guarantees to match UWP's value prior to Arrange pass
			}

			var finalSize = shouldStretch ? availableSize : grid.DesiredSize;
			grid.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));

			Assert.AreEqual(expectedRowHeights.Length, grid.RowDefinitions.Count);
			for (int i = 0; i < expectedRowHeights.Length; i++)
			{
				Assert.AreEqual(expectedRowHeights[i], grid.RowDefinitions[i].ActualHeight);
			}

			Assert.AreEqual(expectedColumnWidths.Length, grid.ColumnDefinitions.Count);
			for (int i = 0; i < expectedColumnWidths.Length; i++)
			{
				Assert.AreEqual(expectedColumnWidths[i], grid.ColumnDefinitions[i].ActualWidth);
			}
		}

		private Grid ConstructGrid(
			(GridUnitType UnitType, double Size)[] rowDefinitions,
			(GridUnitType UnitType, double Size)[] columnDefinitions,
			params (int Row, int Column, FrameworkElement Element)[] cells
		)
		{
			var grid = new Grid();
			foreach (var rd in rowDefinitions)
			{
				grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(rd.Size, rd.UnitType) });
			}
			foreach (var rd in columnDefinitions)
			{
				grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(rd.Size, rd.UnitType) });
			}

			foreach (var cell in cells)
			{
				var child = cell.Element;
				Grid.SetRow(child, cell.Row);
				Grid.SetColumn(child, cell.Column);

				grid.Children.Add(child);
			}

			return grid;
		}
	}
}
