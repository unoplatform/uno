#pragma warning disable CS0168 // Disable for unused TestCleanupWrapper

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Xaml.Tests.Common;
using Private.Infrastructure;
using Uno.UI.RuntimeTests;
using Windows.Foundation;
using Windows.UI;

namespace Windows.UI.Xaml.Tests.Controls.Grid_Tests
{
	[TestClass]
#if __MACOS__
	[Ignore("Currently fails on macOS, part of #9282! epic")]
#endif
	public class GridIntegrationTests
	{
		const double s_rectSize = 30.0;
		const double s_gridSize = 700.0;
		const double s_errorMargin = 0.0001;

		bool ClassSetup()
		{
			TestServices.EnsureInitialized();
			return true;
		}

		bool TestSetup()
		{
			// TestServices.WindowHelper.InitializeXaml(new MetadataProvider());
			return true;
		}

		bool TestCleanup()
		{
			TestServices.WindowHelper.ShutdownXaml();
			TestServices.WindowHelper.VerifyTestCleanup();
			return true;
		}

		[TestMethod]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task CanPerformLayout()
		{
			TestCleanupWrapper cleanup;
			TestServices.WindowHelper.SetWindowSizeOverride(new Size(400, 400));
			//WUCRenderingScopeGuard guard(DCompRendering.WUCCompleteSynchronousCompTree, false /*resizeWindow*/);

			Grid grid = null;

			// Verify that basic layout is performed correctly.
			await TestServices.RunOnUIThread(() =>

		{
			grid = (Grid)XamlReader.Load(
				"<Grid" +

				"  xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'" +

				"  xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'" +

				"  Background='Purple'>" +

				"  <Grid.RowDefinitions>" +

				"      <RowDefinition Height='Auto'/>" +

				"      <RowDefinition Height='*'/>" +

				"      <RowDefinition Height='50'/>" +

				"      <RowDefinition Height='50'/>" +

				"  </Grid.RowDefinitions>" +

				"  <Grid.ColumnDefinitions>" +

				"      <ColumnDefinition Width='Auto'/>" +

				"      <ColumnDefinition Width='*'/>" +

				"      <ColumnDefinition Width='50'/>" +

				"      <ColumnDefinition Width='50'/>" +

				"  </Grid.ColumnDefinitions>" +

				"  <Border Grid.Row='0' Grid.Column='0'" +

				"      Background='Red' Width='50' Height='50'" +

				"      HorizontalAlignment='Stretch' VerticalAlignment='Stretch'/>" +

				"  <Border Grid.Row='0' Grid.Column='1'" +

				"      Background='Green'" +

				"      HorizontalAlignment='Stretch' VerticalAlignment='Stretch'/>" +

				"  <Border Grid.Row='0' Grid.Column='2'" +

				"      Background='Blue' Width='50' Height='50'" +

				"      HorizontalAlignment='Stretch' VerticalAlignment='Stretch'/>" +

				"  <Border Grid.Row='1' Grid.Column='3' Grid.RowSpan='2'" +

				"      Background='Yellow'" +

				"      HorizontalAlignment='Stretch' VerticalAlignment='Stretch'/>" +

				"  <Border Grid.Row='3' Grid.Column='1' Grid.ColumnSpan='2'" +

				"      Background='Orange'" +

				"      HorizontalAlignment='Stretch' VerticalAlignment='Stretch'/>" +

				"</Grid>");

			TestServices.WindowHelper.WindowContent = grid;
		});

			await TestServices.WindowHelper.WaitForIdle();

			TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison, "1");

			// Verify that changing Grid.Row and Grid.Column triggers a layout
			// pass and that this is performed correctly.
			Console.WriteLine("Changing layout properties: Grid.Row and Grid.Column.");
			await TestServices.RunOnUIThread(() =>

		{
			Grid.SetRow((FrameworkElement)(grid.Children[0]), 0);
			Grid.SetColumn((FrameworkElement)(grid.Children[0]), 0);
			Grid.SetRow((FrameworkElement)(grid.Children[1]), 1);
			Grid.SetColumn((FrameworkElement)(grid.Children[1]), 0);
			Grid.SetRow((FrameworkElement)(grid.Children[2]), 2);
			Grid.SetColumn((FrameworkElement)(grid.Children[2]), 0);
		});
			await TestServices.WindowHelper.WaitForIdle();

			TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison, "2");

			// Verify that changing Grid.RowSpan and Grid.ColumnSpan triggers a layout
			// pass and that this is performed correctly.
			Console.WriteLine("Changing layout properties: Grid.RowSpan and Grid.ColumnSpan.");
			await TestServices.RunOnUIThread(() =>

		{
			Grid.SetRowSpan((FrameworkElement)(grid.Children[3]), 1);
			Grid.SetColumnSpan((FrameworkElement)(grid.Children[4]), 1);
		});
			await TestServices.WindowHelper.WaitForIdle();

			TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison, "3");

			// Verify that the desired size of the panel is correct based on the
			// size of its rows, columns and content.
			await TestServices.RunOnUIThread(() =>

		{
			TestServices.VERIFY_ARE_VERY_CLOSE(grid.DesiredSize.Width, 150.0f);
			TestServices.VERIFY_ARE_VERY_CLOSE(grid.DesiredSize.Height, 150.0f);
		});
		}

		[TestMethod]
		[Ignore] // Not yet fully implemented
		public async Task VerifyMinWidthAndMinHeight()
		{
			TestCleanupWrapper cleanup;
			Grid grid = null;

			await TestServices.RunOnUIThread(() =>
			{
				grid = (Grid)XamlReader.Load(
					"<Grid x:Name='root' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>" +
					"  <Grid.RowDefinitions>" +
					"      <RowDefinition Height='Auto' MinHeight='100'/>" +
					"  </Grid.RowDefinitions>" +
					"  <Grid.ColumnDefinitions>" +
					"      <ColumnDefinition Width='Auto' MinWidth='100'/>" +
					"  </Grid.ColumnDefinitions>" +
					"  <Rectangle Grid.Row='1' Grid.Column='1' Width='50' Height='50' Fill='Red'/>" +
					"</Grid>");

				TestServices.WindowHelper.WindowContent = grid;
			});

			await TestServices.WindowHelper.WaitForIdle();

			await TestServices.RunOnUIThread(() =>
			{
				TestServices.VERIFY_ARE_VERY_CLOSE(grid.DesiredSize.Width, 100.0f);
				TestServices.VERIFY_ARE_VERY_CLOSE(grid.DesiredSize.Height, 100.0f);
			});
		}

		[TestMethod]
		[Ignore] // Not yet fully implemented
		public async Task VerifyMaxWidthAndMaxHeight()
		{
			Grid grid = null;

			await TestServices.RunOnUIThread(() =>
			{
				grid = (Grid)XamlReader.Load(
					"<Grid x:Name='root' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>" +
					"  <Grid.RowDefinitions>" +
					"      <RowDefinition Height='Auto' MaxHeight='50'/>" +
					"  </Grid.RowDefinitions>" +
					"  <Grid.ColumnDefinitions>" +
					"      <ColumnDefinition Width='Auto' MaxWidth='50'/>" +
					"  </Grid.ColumnDefinitions>" +
					"  <Rectangle Grid.Row='1' Grid.Column='1' Width='100' Height='100' Fill='Red'/>" +
					"</Grid>");

				TestServices.WindowHelper.WindowContent = grid;
			});

			await TestServices.WindowHelper.WaitForIdle();

			await TestServices.RunOnUIThread(() =>
			{
				TestServices.VERIFY_ARE_VERY_CLOSE(grid.DesiredSize.Width, 50.0f);
				TestServices.VERIFY_ARE_VERY_CLOSE(grid.DesiredSize.Height, 50.0f);
			});
		}

		[TestMethod]
		public async Task VerifyGridLengthForRowsAndColumns()
		{
			TestCleanupWrapper cleanup;
			Grid grid = null;

			await TestServices.RunOnUIThread(() =>
			{
				// Create a 5x5 Grid to verify that cells using different
				// combinations of GridLengths are sized correctly.
				//
				// ----------------------------------
				// 25px | Auto | Auto | Star | 25px |
				// ----------------------------------
				// Auto |      |      |      |      |
				// ----------------------------------
				// Auto |      |      |      |      |
				// ----------------------------------
				// Star |      |      |      |      |
				// ----------------------------------
				// 25px |      |      |      |      |
				// ----------------------------------

				grid = (Grid)XamlReader.Load(
					"<Grid" +
					"  xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'" +
					"  xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'" +
					"  Width='250' Height='250' Background='Red'>" +
					"  <Grid.RowDefinitions>" +
					"      <RowDefinition Height='25'/>" +
					"      <RowDefinition Height='Auto'/>" +
					"      <RowDefinition Height='Auto'/>" +
					"      <RowDefinition Height='*'/>" +
					"      <RowDefinition Height='25'/>" +
					"  </Grid.RowDefinitions>" +
					"  <Grid.ColumnDefinitions>" +
					"      <ColumnDefinition Width='25'/>" +
					"      <ColumnDefinition Width='Auto'/>" +
					"      <ColumnDefinition Width='Auto'/>" +
					"      <ColumnDefinition Width='*'/>" +
					"      <ColumnDefinition Width='25'/>" +
					"  </Grid.ColumnDefinitions>" +
					"  <Rectangle Grid.Row='1' Grid.Column='1' Width='50' Height='50' Fill='Green'/>" +
					"  <Rectangle Grid.Row='2' Grid.Column='2' Width='75' Height='75' Fill='Blue'/>" +
					"</Grid>");

				TestServices.WindowHelper.WindowContent = grid;
			});

			await TestServices.WindowHelper.WaitForIdle();

			await TestServices.RunOnUIThread(() =>
			{
				// Verify rows.
				TestServices.VERIFY_ARE_VERY_CLOSE(grid.RowDefinitions[0].ActualHeight, 25.0);
				TestServices.VERIFY_ARE_VERY_CLOSE(grid.RowDefinitions[1].ActualHeight, 50.0);
				TestServices.VERIFY_ARE_VERY_CLOSE(grid.RowDefinitions[2].ActualHeight, 75.0);
				TestServices.VERIFY_ARE_VERY_CLOSE(grid.RowDefinitions[3].ActualHeight, 75.0);
				TestServices.VERIFY_ARE_VERY_CLOSE(grid.RowDefinitions[4].ActualHeight, 25.0);

				// Verify columns.
				TestServices.VERIFY_ARE_VERY_CLOSE(grid.ColumnDefinitions[0].ActualWidth, 25.0);
				TestServices.VERIFY_ARE_VERY_CLOSE(grid.ColumnDefinitions[1].ActualWidth, 50.0);
				TestServices.VERIFY_ARE_VERY_CLOSE(grid.ColumnDefinitions[2].ActualWidth, 75.0);
				TestServices.VERIFY_ARE_VERY_CLOSE(grid.ColumnDefinitions[3].ActualWidth, 75.0);
				TestServices.VERIFY_ARE_VERY_CLOSE(grid.ColumnDefinitions[4].ActualWidth, 25.0);
			});
		}

		[TestMethod]
		public async Task VerifyPixelTakesPriorityOverStar()
		{
			TestCleanupWrapper cleanup;
			Rectangle child = null;

			await TestServices.RunOnUIThread(() =>
			{
				var grid = (Grid)(XamlReader.Load(
					"<Grid x:Name='root' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' >" +
					"  <Grid.ColumnDefinitions>" +
					"      <ColumnDefinition Width='*'/>" +
					"      <ColumnDefinition Width='20'/>" +
					"  </Grid.ColumnDefinitions>" +
					"  <Grid.RowDefinitions>" +
					"      <RowDefinition Height='*'/>" +
					"      <RowDefinition Height='20'/>" +
					"  </Grid.RowDefinitions>" +
					"</Grid>"));

				grid.Height = s_rectSize;
				grid.Width = s_rectSize;
				child = new Rectangle();

				grid.Children.Add(child);
				TestServices.WindowHelper.WindowContent = grid;
			});

			await TestServices.WindowHelper.WaitForIdle();

			await TestServices.RunOnUIThread(() =>
			{
				TestServices.VERIFY_ARE_VERY_CLOSE(child.ActualHeight, s_rectSize - 20);
				TestServices.VERIFY_ARE_VERY_CLOSE(child.ActualWidth, s_rectSize - 20);
			});
		}

		[TestMethod]
		public async Task VerifyAutoTakesPriorityOverStar()
		{
			TestCleanupWrapper cleanup;
			Rectangle child1 = null;

			await TestServices.RunOnUIThread(() =>
			{
				var grid = (Grid)(XamlReader.Load(
					"<Grid x:Name='root' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>" +
					"  <Grid.ColumnDefinitions>" +
					"      <ColumnDefinition Width='*'/>" +
					"      <ColumnDefinition Width='Auto'/>" +
					"  </Grid.ColumnDefinitions>" +
					"  <Grid.RowDefinitions>" +
					"      <RowDefinition Height='*'/>" +
					"      <RowDefinition Height='Auto'/>" +
					"  </Grid.RowDefinitions>" +
					"  <Rectangle Grid.Row='1' Grid.Column='1' Fill='Yellow' Height='30' Width='30' />" +
					"</Grid>"));

				grid.Width = s_rectSize * 1.5;
				grid.Height = s_rectSize * 1.5;
				child1 = new Rectangle();

				grid.Children.Add(child1);
				TestServices.WindowHelper.WindowContent = grid;
			});

			await TestServices.WindowHelper.WaitForIdle();

			await TestServices.RunOnUIThread(() =>
			{
				TestServices.VERIFY_IS_LESS_THAN(s_rectSize / 2 - child1.ActualHeight, s_errorMargin);
				TestServices.VERIFY_IS_LESS_THAN(s_rectSize / 2 - child1.ActualWidth, s_errorMargin);
			});
		}

		[TestMethod]
		public async Task ThrowsInvalidArgumentForNegativePixelSize()
		{
			DisableErrorReportingScopeGuard disableErrors;

			await TestServices.RunOnUIThread(() =>
			{
				TestServices.VERIFY_THROWS_WINRT(() => new GridLength(-1), typeof(ArgumentException));
				TestServices.VERIFY_THROWS_WINRT(() => new GridLength(-1), typeof(ArgumentException));
			});
		}

		[TestMethod]
		public async Task CanSetCellDimensionsToZero()
		{
			TestCleanupWrapper cleanup;
			Rectangle child = null;

			await TestServices.RunOnUIThread(() =>
			{
				var grid = (Grid)(XamlReader.Load(
					"<Grid x:Name='root' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' >" +

					"  <Grid.ColumnDefinitions>" +

					"      <ColumnDefinition Width='0'/>" +

					"  </Grid.ColumnDefinitions>" +

					"  <Grid.RowDefinitions>" +

					"      <RowDefinition Height='0'/>" +

					"  </Grid.RowDefinitions>" +

					"</Grid>"));

				child = new Rectangle();

				grid.Children.Add(child);
				Grid.SetRow(child, 1);
				Grid.SetColumn(child, 1);
				TestServices.WindowHelper.WindowContent = grid;
			});

			await TestServices.WindowHelper.WaitForIdle();

			await TestServices.RunOnUIThread(() =>
			{
				TestServices.VERIFY_ARE_EQUAL(child.ActualHeight, 0);
				TestServices.VERIFY_ARE_EQUAL(child.ActualWidth, 0);
			});
		}

		[TestMethod]
		public async Task CanSetCellPixelDimensions()
		{
			TestCleanupWrapper cleanup;
			Rectangle child = null;

			await TestServices.RunOnUIThread(() =>

			{
				var grid = new Grid();
				var row = new RowDefinition();
				var column = new ColumnDefinition();
				row.Height = new GridLength(s_rectSize);
				column.Width = new GridLength(s_rectSize);
				grid.RowDefinitions.Add(row);
				grid.ColumnDefinitions.Add(column);
				child = new Rectangle();

				grid.Children.Add(child);
				TestServices.WindowHelper.WindowContent = grid;
			});

			await TestServices.WindowHelper.WaitForIdle();

			await TestServices.RunOnUIThread(() =>
			{
				TestServices.VERIFY_IS_LESS_THAN((s_rectSize - child.ActualHeight), s_errorMargin);
				TestServices.VERIFY_IS_LESS_THAN((s_rectSize - child.ActualWidth), s_errorMargin);
			});
		}

		[TestMethod]
		[RequiresFullWindow] // the test fails if the available size for window content isn't wide enough
		public async Task ValidateLayoutRoundingForPixelDimensions()
		{
			TestCleanupWrapper cleanup;
			Border child = null;

			await TestServices.RunOnUIThread(() =>
			{
				var grid = new Grid();
				grid.Width = 400;
				var column1 = new ColumnDefinition();
				var column2 = new ColumnDefinition();
				column1.Width = new GridLength(100.25, GridUnitType.Pixel);
				column2.Width = new GridLength(1, GridUnitType.Star);
				grid.ColumnDefinitions.Add(column1);
				grid.ColumnDefinitions.Add(column2);
				child = new Border();

				grid.Children.Add(child);
				Grid.SetColumnSpan(child, 2);
				TestServices.WindowHelper.WindowContent = grid;
			});

			await TestServices.WindowHelper.WaitForIdle();

			await TestServices.RunOnUIThread(() =>
			{
				var availableSize = LayoutInformation.GetAvailableSize(child);
				TestServices.VERIFY_ARE_EQUAL(availableSize.Width, 400.0f);
			});
		}

		[TestMethod]
		public async Task CanStarSizedCellsEquallyDivideAllocatedSpace()
		{
			TestCleanupWrapper cleanup;
			TestServices.WindowHelper.SetWindowSizeOverride(new Size(400, 400));

			Rectangle child1 = null;
			Rectangle child2 = null;

			await TestServices.RunOnUIThread(() =>
			{
				var grid = (Grid)(XamlReader.Load(
					"<Grid x:Name='root' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' >" +

					"  <Grid.ColumnDefinitions>" +

					"      <ColumnDefinition Width='*'/>" +

					"      <ColumnDefinition Width='*'/>" +

					"  </Grid.ColumnDefinitions>" +

					"  <Grid.RowDefinitions>" +

					"      <RowDefinition Height='*'/>" +

					"      <RowDefinition Height='*'/>" +

					"  </Grid.RowDefinitions>" +

					"</Grid>"));
				child1 = new Rectangle();
				child2 = new Rectangle();

				grid.Children.Add(child1);
				grid.Children.Add(child2);
				Grid.SetRow(child2, 1);
				Grid.SetColumn(child2, 1);
				TestServices.WindowHelper.WindowContent = grid;
			});

			await TestServices.WindowHelper.WaitForIdle();

			await TestServices.RunOnUIThread(() =>
			{
				TestServices.VERIFY_ARE_EQUAL(child1.ActualHeight, child2.ActualHeight);
				TestServices.VERIFY_ARE_EQUAL(child1.ActualWidth, child2.ActualWidth);
			});
		}

		[TestMethod]
		public async Task CanNegativelyWeightedCellsThrowInvalidArgument()
		{
			TestCleanupWrapper cleanup;
			DisableErrorReportingScopeGuard disableErrors;

			await TestServices.RunOnUIThread(() =>
			{
				TestServices.VERIFY_THROWS_WINRT(() => new GridLength(-1, GridUnitType.Star), typeof(ArgumentException));
				TestServices.VERIFY_THROWS_WINRT(() => new GridLength(-1, GridUnitType.Star), typeof(ArgumentException));
			});
		}

#if !__ANDROID__ && !__IOS__
		[TestMethod]
#endif
		public async Task CanZeroWeightedCellsShrinkToZeroSize()
		{
			TestCleanupWrapper cleanup;
			Rectangle child = null;

			await TestServices.RunOnUIThread(() =>
			{
				var grid = (Grid)(XamlReader.Load(
					"<Grid x:Name='root' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' >" +

					"  <Grid.ColumnDefinitions>" +

					"      <ColumnDefinition Width='0*'/>" +

					"  </Grid.ColumnDefinitions>" +

					"  <Grid.RowDefinitions>" +

					"      <RowDefinition Height='0*'/>" +

					"  </Grid.RowDefinitions>" +

					"</Grid>"));
				child = new Rectangle();

				grid.Children.Add(child);
				TestServices.WindowHelper.WindowContent = grid;
			});

			await TestServices.WindowHelper.WaitForIdle();

			await TestServices.RunOnUIThread(() =>
			{
				TestServices.VERIFY_ARE_EQUAL(child.ActualHeight, 0);
				TestServices.VERIFY_ARE_EQUAL(child.ActualWidth, 0);
			});
		}

		[TestMethod]
		[Ignore]
		public async Task CanDivideAllocatedSpacingCorrectlyAccordingToStarWeightings()
		{
			TestCleanupWrapper cleanup;

			Rectangle child1 = null;
			Rectangle child2 = null;
			Rectangle child3 = null;

			const double weighting1 = 0.5;
			const double weighting2 = 1;
			const double weighting3 = 2;

			await TestServices.RunOnUIThread(() =>

			{
				var grid = (Grid)(XamlReader.Load(
				"<Grid x:Name='root' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' >" +
					"  <Grid.ColumnDefinitions>" +
					"      <ColumnDefinition Width='0.5*'/>" +
					"      <ColumnDefinition Width='1*'/>" +
					"      <ColumnDefinition Width='2*'/>" +
					"  </Grid.ColumnDefinitions>" +
					"  <Grid.RowDefinitions>" +
					"      <RowDefinition Height='0.5*'/>" +
					"      <RowDefinition Height='1*'/>" +
					"      <RowDefinition Height='2*'/>" +
					"  </Grid.RowDefinitions>" +
					"</Grid>"));

				grid.Height = s_gridSize;
				grid.Width = s_gridSize;
				child1 = new Rectangle();
				child2 = new Rectangle();
				child3 = new Rectangle();

				grid.Children.Add(child1);

				grid.Children.Add(child2);
				Grid.SetRow(child2, 1);
				Grid.SetColumn(child2, 1);

				grid.Children.Add(child3);
				Grid.SetRow(child3, 2);
				Grid.SetColumn(child3, 2);
				TestServices.WindowHelper.WindowContent = grid;
			});

			await TestServices.WindowHelper.WaitForIdle();

			await TestServices.RunOnUIThread(() =>
			{
				double totalWeighting = weighting1 + weighting2 + weighting3;

				TestServices.VERIFY_IS_LESS_THAN((weighting1 * s_gridSize / totalWeighting) - child1.ActualHeight, s_errorMargin);
				TestServices.VERIFY_IS_LESS_THAN((weighting1 * s_gridSize / totalWeighting) - child1.ActualWidth, s_errorMargin);

				TestServices.VERIFY_IS_LESS_THAN((weighting2 * s_gridSize / totalWeighting) - child2.ActualHeight, s_errorMargin);
				TestServices.VERIFY_IS_LESS_THAN((weighting2 * s_gridSize / totalWeighting) - child2.ActualWidth, s_errorMargin);

				TestServices.VERIFY_IS_LESS_THAN((weighting3 * s_gridSize / totalWeighting) - child3.ActualHeight, s_errorMargin);
				TestServices.VERIFY_IS_LESS_THAN((weighting3 * s_gridSize / totalWeighting) - child3.ActualWidth, s_errorMargin);
			});
		}

		[TestMethod]
		public async Task CanAutoSizeCellDefaultToZero()
		{
			TestCleanupWrapper cleanup;
			Border child = null;

			await TestServices.RunOnUIThread(() =>
			{
				var grid = (Grid)(XamlReader.Load(
					"<Grid x:Name='root' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' >" +

					"  <Grid.ColumnDefinitions>" +

					"      <ColumnDefinition Width='auto'/>" +

					"  </Grid.ColumnDefinitions>" +

					"  <Grid.RowDefinitions>" +

					"      <RowDefinition Height='auto'/>" +

					"  </Grid.RowDefinitions>" +

					"</Grid>"));
				child = new Border();

				grid.Children.Add(child);
				TestServices.WindowHelper.WindowContent = grid;
			});

			await TestServices.WindowHelper.WaitForIdle();

			await TestServices.RunOnUIThread(() =>
			{
				TestServices.VERIFY_ARE_EQUAL(child.ActualHeight, 0);
				TestServices.VERIFY_ARE_EQUAL(child.ActualWidth, 0);
			});
		}

		[TestMethod]
#if __WASM__
		[Ignore] // ViewportHeight is not implemented in Wasm
#endif
		public async Task CanLayoutWithColumnSpan()
		{
			TestCleanupWrapper cleanup;
			Rectangle child = null;
			ScrollViewer scrollViewer = null;

			await TestServices.RunOnUIThread(() =>
			{
				var grid = (Grid)(XamlReader.Load(
					"<Grid x:Name='root' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' >" +

					"  <Grid.ColumnDefinitions>" +

					"      <ColumnDefinition Width='Auto'/>" +

					"      <ColumnDefinition Width='*'/>" +

					"  </Grid.ColumnDefinitions>" +

					"  <Grid.RowDefinitions>" +

					"      <RowDefinition Height='Auto'/>" +

					"      <RowDefinition Height='*'/>" +

					"  </Grid.RowDefinitions>" +

					"</Grid>"));

				child = new Rectangle();
				child.Width = 100;
				child.Height = 100;
				grid.Children.Add(child);
				Grid.SetColumn(child, 1);
				Grid.SetColumnSpan(child, 2);

				scrollViewer = new ScrollViewer();

				var rect = new Rectangle();
				var redBrush = new SolidColorBrush(Colors.Red);

				rect.Fill = redBrush;
				rect.Width = 100;
				rect.Height = 8000;
				scrollViewer.Content = rect;
				grid.Children.Add(scrollViewer);
				Grid.SetRow(scrollViewer, 1);

				TestServices.WindowHelper.WindowContent = grid;
			});

			await TestServices.WindowHelper.WaitForIdle();

			await TestServices.RunOnUIThread(() =>
			{
				TestServices.VERIFY_ARE_EQUAL(scrollViewer.ActualHeight, scrollViewer.ViewportHeight);
			});
		}

		[TestMethod]
		public async Task ThrowsInvalidArgumentForNegativeRowAndColumnSpan()
		{
			TestCleanupWrapper cleanup;
			DisableErrorReportingScopeGuard disableErrors;

			await TestServices.RunOnUIThread(() =>
			{
				var rect = new Rectangle();
				var grid = new Grid();
				TestServices.VERIFY_THROWS_WINRT(() => Grid.SetColumnSpan(rect, -1), typeof(ArgumentException));
				TestServices.VERIFY_THROWS_WINRT(() => Grid.SetRowSpan(rect, -1), typeof(ArgumentException));
			});
		}

		[TestMethod]
		public async Task ThrowsInvalidArgumentForZeroRowAndColumnSpan()
		{
			TestCleanupWrapper cleanup;
			DisableErrorReportingScopeGuard disableErrors;

			await TestServices.RunOnUIThread(() =>
			{
				var rect = new Rectangle();
				var grid = new Grid();
				TestServices.VERIFY_THROWS_WINRT(() => Grid.SetColumnSpan(rect, 0), typeof(ArgumentException));
				TestServices.VERIFY_THROWS_WINRT(() => Grid.SetRowSpan(rect, 0), typeof(ArgumentException));
			});
		}

		[TestMethod]
		public async Task CanSpanRowsAndColumns()
		{
			TestCleanupWrapper cleanup;
			Rectangle child = null;

			await TestServices.RunOnUIThread(() =>
			{
				var grid = (Grid)(XamlReader.Load(
					"<Grid x:Name='root' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' >" +
					"  <Grid.ColumnDefinitions>" +
					"      <ColumnDefinition Width='*'/>" +
					"      <ColumnDefinition Width='*'/>" +
					"      <ColumnDefinition Width='*'/>" +
					"  </Grid.ColumnDefinitions>" +
					"  <Grid.RowDefinitions>" +
					"      <RowDefinition Height='*'/>" +
					"      <RowDefinition Height='*'/>" +
					"      <RowDefinition Height='*'/>" +
					"  </Grid.RowDefinitions>" +
					"</Grid>"));

				grid.Height = s_rectSize;
				grid.Width = s_rectSize;
				child = new Rectangle();

				grid.Children.Add(child);
				Grid.SetColumnSpan(child, 2);
				Grid.SetRowSpan(child, 2);
				TestServices.WindowHelper.WindowContent = grid;
			});

			await TestServices.WindowHelper.WaitForIdle();

			await TestServices.RunOnUIThread(() =>
			{
				TestServices.VERIFY_ARE_EQUAL(child.ActualHeight, 2 * s_rectSize / 3);
				TestServices.VERIFY_ARE_EQUAL(child.ActualWidth, 2 * s_rectSize / 3);
			});
		}

		[TestMethod]
		public async Task CanSpanAllRowsAndColumns()
		{
			TestCleanupWrapper cleanup;
			Rectangle child = null;

			await TestServices.RunOnUIThread(() =>
			{
				var grid = (Grid)(XamlReader.Load(
					"<Grid x:Name='root' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' >" +

					"  <Grid.ColumnDefinitions>" +

					"      <ColumnDefinition Width='*'/>" +

					"      <ColumnDefinition Width='*'/>" +

					"      <ColumnDefinition Width='*'/>" +

					"  </Grid.ColumnDefinitions>" +

					"  <Grid.RowDefinitions>" +

					"      <RowDefinition Height='*'/>" +

					"      <RowDefinition Height='*'/>" +

					"      <RowDefinition Height='*'/>" +

					"  </Grid.RowDefinitions>" +

					"</Grid>"));
				grid.Height = s_rectSize;
				grid.Width = s_rectSize;
				child = new Rectangle();

				grid.Children.Add(child);
				Grid.SetColumnSpan(child, 3);
				Grid.SetRowSpan(child, 3);
				TestServices.WindowHelper.WindowContent = grid;
			});

			await TestServices.WindowHelper.WaitForIdle();

			await TestServices.RunOnUIThread(() =>
			{
				TestServices.VERIFY_IS_LESS_THAN(s_rectSize - child.ActualHeight, s_errorMargin);
				TestServices.VERIFY_IS_LESS_THAN(s_rectSize - child.ActualWidth, s_errorMargin);
			});
		}

		[TestMethod]
		public async Task CanSetSpanToMoreThanTotalRowsAndColumns()
		{
			TestCleanupWrapper cleanup;
			Rectangle child = null;

			await TestServices.RunOnUIThread(() =>
			{
				var grid = (Grid)(XamlReader.Load(
					"<Grid x:Name='root' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' >" +
					"  <Grid.ColumnDefinitions>" +
					"      <ColumnDefinition Width='*'/>" +
					"      <ColumnDefinition Width='*'/>" +
					"      <ColumnDefinition Width='*'/>" +
					"  </Grid.ColumnDefinitions>" +
					"  <Grid.RowDefinitions>" +
					"      <RowDefinition Height='*'/>" +
					"      <RowDefinition Height='*'/>" +
					"      <RowDefinition Height='*'/>" +
					"  </Grid.RowDefinitions>" +
					"</Grid>"));

				grid.Height = s_rectSize;
				grid.Width = s_rectSize;
				child = new Rectangle();

				grid.Children.Add(child);
				Grid.SetColumnSpan(child, 4);
				Grid.SetRowSpan(child, 4);
				TestServices.WindowHelper.WindowContent = grid;
			});

			await TestServices.WindowHelper.WaitForIdle();

			await TestServices.RunOnUIThread(() =>
			{
				TestServices.VERIFY_IS_LESS_THAN(s_rectSize - child.ActualHeight, s_errorMargin);
				TestServices.VERIFY_IS_LESS_THAN(s_rectSize - child.ActualWidth, s_errorMargin);
			});
		}

		[TestMethod]
		public async Task ValidateEnsureMinSizeInDefinitionRange()
		{
			TestCleanupWrapper cleanup;
			TestServices.WindowHelper.SetWindowSizeOverride(new Size(400, 400));
			// WUCRenderingScopeGuard guard(DCompRendering.WUCCompleteSynchronousCompTree, false /*resizeWindow*/);

			Grid grid = null;

			await TestServices.RunOnUIThread(() =>
			{
				grid = (Grid)XamlReader.Load(
					"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'" +
					"      xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'" +
					"      Background='Yellow' VerticalAlignment='Center'>" +
					"    <Grid.RowDefinitions>" +
					"        <RowDefinition/>" +
					"        <RowDefinition/>" +
					"    </Grid.RowDefinitions>" +
					"    <Grid.ColumnDefinitions>" +
					"        <ColumnDefinition Width='50'/>" +
					"        <ColumnDefinition Width='50'/>" +
					"    </Grid.ColumnDefinitions>" +
					"    <Border x:Name='b0' Grid.Column='0' Grid.Row='0' Grid.ColumnSpan='2' Background='Red' Opacity='0.5' >" +
					"        <TextBlock TextWrapping='Wrap'>" +
					"          hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world" +
					"          hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world" +
					"        </TextBlock>" +
					"    </Border>" +
					"    <Border x:Name='b1' Grid.Column='0' Grid.Row='1' Background='Blue' Opacity='0.5' >" +
					"        <TextBlock TextWrapping='Wrap'>" +
					"          hello world hello world hello world hello world" +
					"        </TextBlock>" +
					"    </Border>" +
					"    <Border x:Name='b2' Grid.Column='1' Grid.Row='1' Background='Green' Opacity='0.5' >" +
					"        <TextBlock TextWrapping='Wrap'>" +
					"          hello world hello world hello world hello world" +
					"        </TextBlock>" +
					"    </Border>" +
					"</Grid>");

				var root = new Grid();
				root.Background = new SolidColorBrush(Windows.UI.Colors.Purple);
				root.Children.Add(grid);

				TestServices.WindowHelper.WindowContent = root;
			});
			await TestServices.WindowHelper.WaitForIdle();
			TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison, "1");

			await TestServices.RunOnUIThread(() =>
			{
				grid.RowSpacing = 10;
				grid.ColumnSpacing = 10;
			});
			await TestServices.WindowHelper.WaitForIdle();
			TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison, "2");

			await TestServices.RunOnUIThread(() =>
			{
				grid.RowSpacing = 0;
				grid.ColumnSpacing = 0;
				grid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
				grid.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
			});
			await TestServices.WindowHelper.WaitForIdle();
			TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison, "3");

			await TestServices.RunOnUIThread(() =>
			{
				grid.RowSpacing = 10;
				grid.ColumnSpacing = 10;
			});
			await TestServices.WindowHelper.WaitForIdle();
			TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison, "4");

			await TestServices.RunOnUIThread(() =>
			{
				grid.RowSpacing = 0;
				grid.ColumnSpacing = 0;
				grid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Auto);
				grid.ColumnDefinitions[1].Width = new GridLength(50, GridUnitType.Pixel);
			});
			await TestServices.WindowHelper.WaitForIdle();
			TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison, "5");

			await TestServices.RunOnUIThread(() =>
			{
				grid.RowSpacing = 10;
				grid.ColumnSpacing = 10;
			});
			await TestServices.WindowHelper.WaitForIdle();
			TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison, "6");

			await TestServices.RunOnUIThread(() =>
			{
				grid.RowSpacing = 0;
				grid.ColumnSpacing = 0;
				Border b = (Border)grid.Children[0];
				TextBlock tb = (TextBlock)b.Child;
				tb.Text = "hello world hello world hello world hello world hello world";
				grid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Auto);
				grid.ColumnDefinitions[0].MaxWidth = 50;
				grid.ColumnDefinitions[1].Width = new GridLength(50, GridUnitType.Pixel);
				grid.ColumnDefinitions[1].MaxWidth = 50;
			});
			await TestServices.WindowHelper.WaitForIdle();
			TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison, "7");

			await TestServices.RunOnUIThread(() =>
			{
				grid.RowSpacing = 10;
				grid.ColumnSpacing = 10;
			});
			await TestServices.WindowHelper.WaitForIdle();
			TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison, "8");
		}

		[TestMethod]
		[Ignore] // Invalid border calculation
		public async Task BorderChromeForSimpleGrid()
		{
			TestCleanupWrapper cleanup;
			TestServices.WindowHelper.SetWindowSizeOverride(new Size(600, 600));
			// WUCRenderingScopeGuard guard(DCompRendering.WUCCompleteSynchronousCompTree, false /*resizeWindow*/);

			Grid grid = null;

			// Verify that basic layout is performed correctly.
			await TestServices.RunOnUIThread(() =>
			{
				grid = (Grid)XamlReader.Load(
					"<Grid" +
					"  xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'" +
					"  xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'" +
					"  Background='Purple' BorderThickness='20' BorderBrush='Red' CornerRadius='5' Padding='10'>" +
					"  <TextBlock Text='Hello World.' FontSize='20'/>" +
					"</Grid>");

				TestServices.WindowHelper.WindowContent = grid;
			});

			await TestServices.WindowHelper.WaitForIdle();

			TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison, "1");

			// Verify that changing Grid.BorderThickness and Grid.Padding triggers a layout
			// pass and that this is performed correctly.
			Console.WriteLine("Changing border properties: Grid.BorderThickness and Grid.Padding.");
			await TestServices.RunOnUIThread(() =>
			{
				grid.BorderThickness = new Thickness(20, 50, 0, 0);
				grid.Padding = new Thickness(10, 20, 30, 40);
			});
			await TestServices.WindowHelper.WaitForIdle();

			TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison, "2");

			// Verify that changing Grid.BorderBrush will render the new color.
			Console.WriteLine("Changing border properties: Grid.BorderBrush.");
			await TestServices.RunOnUIThread(() =>
			{
				var redBrush = new SolidColorBrush(Colors.Green);
				grid.BorderBrush = redBrush;
			});
			await TestServices.WindowHelper.WaitForIdle();

			TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison, "3");

			// Verify that the desired size of the panel is correct based on the
			// size of its rows, columns and content.
			await TestServices.RunOnUIThread(() =>
			{
#if __WASM__
				// HTML Text rounding causes the length to be off by half a pixel.
				TestServices.VERIFY_ARE_EQUAL(grid.DesiredSize.Width, 169.5f);
#else
				TestServices.VERIFY_ARE_EQUAL(grid.DesiredSize.Width, 170.0f);
#endif

				TestServices.VERIFY_ARE_EQUAL(grid.DesiredSize.Height, 137.0f);
			});
		}

		[TestMethod]
		public async Task BorderChromeForComplexGrid()
		{
			TestCleanupWrapper cleanup;
			TestServices.WindowHelper.SetWindowSizeOverride(new Size(600, 600));
			//WUCRenderingScopeGuard guard(DCompRendering.WUCCompleteSynchronousCompTree, false /*resizeWindow*/);

			Grid grid = null;

			// Verify that basic layout is performed correctly.
			await TestServices.RunOnUIThread(() =>
			{
				grid = (Grid)XamlReader.Load(
					"<Grid BorderThickness = '20' BorderBrush='Pink' Background='Purple' Padding='10' CornerRadius='10' x:Name='root' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' >" +
					"  <Grid.ColumnDefinitions>" +
					"      <ColumnDefinition Width='100'/>" +
					"      <ColumnDefinition Width='*'/>" +
					"  </Grid.ColumnDefinitions>" +
					"  <Grid.RowDefinitions>" +
					"      <RowDefinition Height='100'/>" +
					"      <RowDefinition Height='*'/>" +
					"  </Grid.RowDefinitions>" +
					"  <Rectangle Width='20' Height='20' Fill='Green'/>" +
					"  <Rectangle Width='20' Height='20' Fill='Red' Grid.Row='2'/>" +
					"  <Rectangle Width='20' Height='20' Fill='Blue' Grid.Column='2'/>" +
					"  <Rectangle Width='20' Height='20' Fill='Yellow' Grid.Row='2' Grid.Column='2'/>" +
					"</Grid>");

				TestServices.WindowHelper.WindowContent = grid;
			});

			await TestServices.WindowHelper.WaitForIdle();

			TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison);

			// Verify that the desired size of the panel is correct based on the
			// size of its rows, columns and content.
			await TestServices.RunOnUIThread(() =>
			{
				TestServices.VERIFY_ARE_EQUAL(grid.DesiredSize.Width, 180.0f);
				TestServices.VERIFY_ARE_EQUAL(grid.DesiredSize.Height, 180.0f);
			});
		}

		[TestMethod]
		[Ignore] // Not yet implemented
		public async Task ValidateSpacing()
		{
			TestCleanupWrapper cleanup;
			TestServices.WindowHelper.SetWindowSizeOverride(new Size(400, 400));
			// WUCRenderingScopeGuard guard(DCompRendering.WUCCompleteSynchronousCompTree, false /*resizeWindow*/);

			Grid root = null;
			Grid grid = null;

			await TestServices.RunOnUIThread(() =>
			{
				grid = (Grid)XamlReader.Load(
					"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'" +
					"      xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'" +
					"      Background='Yellow'" +
					"      RowSpacing='10'" +
					"      HorizontalAlignment='Center'" +
					"      VerticalAlignment='Center'>" +
					"  <Grid.RowDefinitions>" +
					"    <RowDefinition Height='50'/>" +
					"    <RowDefinition Height='Auto'/>" +
					"    <RowDefinition Height='*'/>" +
					"    <RowDefinition Height='50'/>" +
					"    <RowDefinition Height='Auto'/>" +
					"    <RowDefinition Height='*'/>" +
					"  </Grid.RowDefinitions>" +
					"  <Border x:Name='b0' Grid.Row='0' Opacity='0.5'             Width='100' Background='Red' />" +
					"  <Border x:Name='b1' Grid.Row='1' Opacity='0.5' Height='50' Width='100' Background='Blue'/>" +
					"  <Border x:Name='b2' Grid.Row='2' Opacity='0.5' Height='50' Width='100' Background='Green'/>" +
					"  <Border x:Name='b3' Grid.Row='3' Opacity='0.5'             Width='100' Background='Red' />" +
					"  <Border x:Name='b4' Grid.Row='4' Opacity='0.5' Height='50' Width='100' Background='Blue'/>" +
					"  <Border x:Name='b5' Grid.Row='5' Opacity='0.5' Height='50' Width='100' Background='Green'/>" +
					"</Grid>");

				root = new Grid();
				root.Background = new SolidColorBrush(Windows.UI.Colors.Purple);
				root.Children.Add(grid);

				TestServices.WindowHelper.WindowContent = root;
			});

			await TestServices.WindowHelper.WaitForIdle();
			TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison, "1");

			await TestServices.RunOnUIThread(() =>
			{
				grid.RowSpacing = -10;
			});
			await TestServices.WindowHelper.WaitForIdle();
			TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison, "2");
		}

		[TestMethod]
		[Ignore] // Not yet implemented
		public async Task ValidateSpacingWithSpans()
		{
			TestCleanupWrapper cleanup;
			TestServices.WindowHelper.SetWindowSizeOverride(new Size(400, 400));
			// WUCRenderingScopeGuard guard(DCompRendering.WUCCompleteSynchronousCompTree, false /*resizeWindow*/);

			Grid root = null;
			Grid grid = null;

			await TestServices.RunOnUIThread(() =>
			{
				grid = (Grid)XamlReader.Load(
					"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'" +
					"      xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'" +
					"      Background='Yellow'" +
					"      RowSpacing='10'" +
					"      ColumnSpacing='10'" +
					"      VerticalAlignment='Center'>" +
					"  <Grid.RowDefinitions>" +
					"    <RowDefinition/>" +
					"    <RowDefinition/>" +
					"    <RowDefinition/>" +
					"  </Grid.RowDefinitions>" +
					"  <Grid.ColumnDefinitions>" +
					"    <ColumnDefinition Width='*'/>" +
					"    <ColumnDefinition Width='*'/>" +
					"    <ColumnDefinition Width='*'/>" +
					"    <ColumnDefinition Width='*'/>" +
					"  </Grid.ColumnDefinitions>" +
					"  <Border x:Name='b0' Grid.Row='0' Grid.Column='0' Grid.ColumnSpan='2' Opacity='0.5' Background='Red'>" +
					"    <TextBlock TextWrapping='Wrap'>" +
					"      hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world" +
					"    </TextBlock>" +
					"  </Border>" +
					"  <Border x:Name='b1' Grid.Row='0' Grid.Column='2' Grid.ColumnSpan='1' Opacity='0.5' Background='Blue'>" +
					"    <TextBlock TextWrapping='Wrap'>" +
					"      hello world hello world hello world hello world hello world" +
					"    </TextBlock>" +
					"  </Border>" +
					"  <Border x:Name='b2' Grid.Row='0' Grid.Column='3' Grid.ColumnSpan='1' Opacity='0.5' Background='Green'>" +
					"    <TextBlock TextWrapping='Wrap'>" +
					"      hello world hello world hello world hello world hello world" +
					"    </TextBlock>" +
					"  </Border>" +
					"  <Border x:Name='b3' Grid.Row='1' Grid.Column='0' Grid.ColumnSpan='1' Opacity='0.5' Background='Red'>" +
					"    <TextBlock TextWrapping='Wrap'>" +
					"      hello world hello world hello world hello world hello world" +
					"    </TextBlock>" +
					"  </Border>" +
					"  <Border x:Name='b4' Grid.Row='1' Grid.Column='1' Grid.ColumnSpan='2' Opacity='0.5' Background='Blue'>" +
					"    <TextBlock TextWrapping='Wrap'>" +
					"      hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world" +
					"    </TextBlock>" +
					"  </Border>" +
					"  <Border x:Name='b5' Grid.Row='1' Grid.Column='3' Grid.ColumnSpan='1' Opacity='0.5' Background='Green'>" +
					"    <TextBlock TextWrapping='Wrap'>" +
					"      hello world hello world hello world hello world hello world" +
					"    </TextBlock>" +
					"  </Border>" +
					"  <Border x:Name='b6' Grid.Row='2' Grid.Column='0' Grid.ColumnSpan='1' Opacity='0.5' Background='Red'>" +
					"    <TextBlock TextWrapping='Wrap'>" +
					"      hello world hello world hello world hello world hello world" +
					"    </TextBlock>" +
					"  </Border>" +
					"  <Border x:Name='b7' Grid.Row='2' Grid.Column='1' Grid.ColumnSpan='1' Opacity='0.5' Background='Blue'>" +
					"    <TextBlock TextWrapping='Wrap'>" +
					"      hello world hello world hello world hello world hello world" +
					"    </TextBlock>" +
					"  </Border>" +
					"  <Border x:Name='b8' Grid.Row='2' Grid.Column='2' Grid.ColumnSpan='2' Opacity='0.5' Background='Green'>" +
					"    <TextBlock TextWrapping='Wrap'>" +
					"      hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world" +
					"    </TextBlock>" +
					"  </Border>" +
					"</Grid>");

				root = new Grid();
				root.Background = new SolidColorBrush(Windows.UI.Colors.Purple);
				root.Children.Add(grid);

				TestServices.WindowHelper.WindowContent = root;
			});

			await TestServices.WindowHelper.WaitForIdle();
			TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison, "1");

			await TestServices.RunOnUIThread(() =>
			{
				grid.RowSpacing = -10;
				grid.ColumnSpacing = -10;
			});

			await TestServices.WindowHelper.WaitForIdle();
			TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison, "2");
		}

		//[TestMethod]  // Not yet implemented
		//[Ignore]
		//public async Task ArrangeOverrideBeforeMeasure()
		//{
		//	TestCleanupWrapper cleanup;
		//	Grid grid = null;

		//	// Verify that basic layout is performed correctly.
		//	await TestServices.RunOnUIThread(() =>
		//	{
		//		grid = (Grid)XamlReader.Load(
		//			"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'" +

		//			"        xmlns:local='using:Private.Tests.Controls.GridIntegrationTests'>" +

		//			"<local:CustomGrid x:Name='customGrid'>" +

		//			"  <Rectangle Width='20' Height='20' Fill='Yellow' />" +

		//			"  <Rectangle Width='20' Height='20' Fill='Yellow' />" +

		//			"</local:CustomGrid" +

		//			"</Grid>");

		//		var size = new Size(100, 100);
		//		Console.WriteLine("FindName: customGrid");
		//		var customGrid = (CustomGrid)(grid.FindName("customGrid"));
		//		customGrid.ArrangeOverride(size);
		//	});
		//}

	}// Windows.UI.Xaml.Tests.Controls.Grid
}
