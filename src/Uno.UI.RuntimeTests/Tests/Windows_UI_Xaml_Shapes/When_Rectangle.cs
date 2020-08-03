#if __IOS__ || __MACOS__ || __SKIA__
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.Extensions;
using Windows.Devices.Perception;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Shapes
{
	[TestClass]
	class When_Rectangle
	{
		[TestMethod]
		[RunsOnUIThread]
		[DataRow(Stretch.Fill, double.NaN, double.NaN, 0d, 0d, 50d, 50d, 0d, 0d)]
		[DataRow(Stretch.Fill, double.NaN, double.NaN, 10d, 20d, 50d, 50d, 10d, 20d)]
		[DataRow(Stretch.Fill, 30d, double.NaN, 10d, 20d, 50d, 50d, 30d, 20d)]
		[DataRow(Stretch.Fill, double.NaN, 30d, 10d, 20d, 50d, 50d, 10d, 30d)]
		[DataRow(Stretch.UniformToFill, double.NaN, double.NaN, 0d, 0d, double.PositiveInfinity, double.PositiveInfinity, 0d, 0d)]
		[DataRow(Stretch.UniformToFill, double.NaN, double.NaN, 10d, 20d, double.PositiveInfinity, double.PositiveInfinity, 10d, 20d)]
		public async Task When_RectangleMeasure(
			Stretch stretch,
			double width,
			double height,
			double minWidth,
			double minHeight,
			double availableWidth,
			double availableHeight,
			double expectedWidth,
			double expectedHeight)
		{
			try
			{
				var SUT = new Rectangle();

				TestServices.WindowHelper.WindowContent = SUT;
				await TestServices.WindowHelper.WaitForIdle();

				SUT.Width = width;
				SUT.Height = height;
				SUT.MinWidth = minWidth;
				SUT.MinHeight = minHeight;
				SUT.Stretch = stretch;
				SUT.Measure(new Windows.Foundation.Size(availableWidth, availableHeight));

				Assert.AreEqual(new Size(expectedWidth, expectedHeight), SUT.DesiredSize);
			}
			finally
			{
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Shape_Stretch_None()
		{
			var topLevelGrid = new Grid();

			var grid = new Grid() { Width = 200, Height = 200 };
			topLevelGrid.Children.Add(grid);

			var SUT = new Path();
			grid.Children.Add(SUT);

			var g = new PathGeometry();
			var fig = new PathFigure() { StartPoint = new Point(50, 50) };
			var arc = new ArcSegment() {
				Size = new Size(50, 50),
				RotationAngle = 45,
				IsLargeArc = false,
				SweepDirection = SweepDirection.Clockwise,
				Point = new Point(70, 70)
			};

			fig.Segments.Add(arc);
			g.Figures.Add(fig);
			SUT.Data = g;

			TestServices.WindowHelper.WindowContent = topLevelGrid;
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(new Rect(0, 0, 200, 200), SUT.LayoutSlotWithMarginsAndAlignments);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Shape_Stretch_UniformToFill()
		{
			var topLevelGrid = new Grid();

			var grid = new Grid() { Width = 200, Height = 100 };
			topLevelGrid.Children.Add(grid);

			var SUT = new Path() { Stretch = Stretch.Uniform };
			grid.Children.Add(SUT);

			var g = new PathGeometry();
			var fig = new PathFigure() { StartPoint = new Point(50, 50) };
			var arc = new ArcSegment()
			{
				Size = new Size(50, 50),
				RotationAngle = 45,
				IsLargeArc = false,
				SweepDirection = SweepDirection.Clockwise,
				Point = new Point(70, 70)
			};

			fig.Segments.Add(arc);
			g.Figures.Add(fig);
			SUT.Data = g;

			TestServices.WindowHelper.WindowContent = topLevelGrid;
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsTrue(MathEx.ApproxEqual(50, SUT.LayoutSlotWithMarginsAndAlignments.X, 1E-3));
			Assert.IsTrue(MathEx.ApproxEqual(0, SUT.LayoutSlotWithMarginsAndAlignments.Y, 1E-3));
			Assert.IsTrue(MathEx.ApproxEqual(100, SUT.LayoutSlotWithMarginsAndAlignments.Width, 1E-3));
			Assert.IsTrue(MathEx.ApproxEqual(100, SUT.LayoutSlotWithMarginsAndAlignments.Height, 1E-3));
		}
	}
}
#endif
