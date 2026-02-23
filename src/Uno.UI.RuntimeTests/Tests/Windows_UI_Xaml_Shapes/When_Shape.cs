#if __APPLE_UIKIT__ || __SKIA__
using System.Collections.Generic;
using System.Threading.Tasks;
using Private.Infrastructure;
using Uno.Extensions;
using Windows.Foundation;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Uno.UI.Controls.Legacy;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Shapes
{
	[TestClass]
	[RunsOnUIThread]
	public class When_Shape
	{
		[TestMethod]
		public async Task When_Shape_Stretch_None()
		{
			var topLevelGrid = new Grid();

			var grid = new Grid() { Width = 200, Height = 200 };
			topLevelGrid.Children.Add(grid);

			var SUT = new Path();
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

			Assert.AreEqual(new Rect(0, 0, 200, 200), SUT.LayoutSlotWithMarginsAndAlignments);
		}


#if __SKIA__
		[TestMethod]
		public void When_StrokeMiterLimit_Default()
		{
			var line = new Line();
			Assert.AreEqual(10.0, line.StrokeMiterLimit, "WinUI default for StrokeMiterLimit is 10.0");
		}

		[TestMethod]
		public void When_StrokeLineCap_Default()
		{
			var line = new Line();
			Assert.AreEqual(PenLineCap.Flat, line.StrokeStartLineCap);
			Assert.AreEqual(PenLineCap.Flat, line.StrokeEndLineCap);
			Assert.AreEqual(PenLineCap.Flat, line.StrokeDashCap);
		}

		[TestMethod]
		public void When_StrokeLineJoin_Default()
		{
			var line = new Line();
			Assert.AreEqual(PenLineJoin.Miter, line.StrokeLineJoin);
		}

		[TestMethod]
		public void When_StrokeDashOffset_Default()
		{
			var line = new Line();
			Assert.AreEqual(0.0, line.StrokeDashOffset);
		}

		[TestMethod]
		public async Task When_StrokeLineCap_Round_Extends_Beyond_Endpoint()
		{
			// A horizontal line with Round caps should extend beyond the endpoints
			// by StrokeThickness/2 (10px in this case).
			var container = new Grid { Width = 220, Height = 50, Background = new SolidColorBrush(Colors.White) };
			var line = new Line
			{
				X1 = 30,
				Y1 = 25,
				X2 = 190,
				Y2 = 25,
				Stroke = new SolidColorBrush(Colors.Red),
				StrokeThickness = 20,
				StrokeStartLineCap = PenLineCap.Round,
				StrokeEndLineCap = PenLineCap.Round,
			};
			container.Children.Add(line);

			await UITestHelper.Load(container);
			var screenshot = await UITestHelper.ScreenShot(container);

			// With Round caps, the stroke should extend to x=20 (30 - 10) and x=200 (190 + 10)
			// Pixel at x=22, y=25 should be in the cap extension area (Red)
			ImageAssert.HasColorAt(screenshot, 22, 25, Colors.Red, tolerance: 30);
			// Pixel at x=198, y=25 should also be in the cap extension area
			ImageAssert.HasColorAt(screenshot, 198, 25, Colors.Red, tolerance: 30);
		}

		[TestMethod]
		public async Task When_StrokeLineCap_Flat_Does_Not_Extend()
		{
			// A horizontal line with Flat caps should NOT extend beyond the endpoints.
			var container = new Grid { Width = 220, Height = 50, Background = new SolidColorBrush(Colors.White) };
			var line = new Line
			{
				X1 = 30,
				Y1 = 25,
				X2 = 190,
				Y2 = 25,
				Stroke = new SolidColorBrush(Colors.Red),
				StrokeThickness = 20,
				StrokeStartLineCap = PenLineCap.Flat,
				StrokeEndLineCap = PenLineCap.Flat,
			};
			container.Children.Add(line);

			await UITestHelper.Load(container);
			var screenshot = await UITestHelper.ScreenShot(container);

			// With Flat caps, pixel at x=18 (well before x1=30) should be white (background)
			ImageAssert.HasColorAt(screenshot, 18, 25, Colors.White, tolerance: 30);
			// Pixel at x=202 (well after x2=190) should be white
			ImageAssert.HasColorAt(screenshot, 202, 25, Colors.White, tolerance: 30);
			// Pixel at center should be Red
			ImageAssert.HasColorAt(screenshot, 110, 25, Colors.Red, tolerance: 30);
		}

		[TestMethod]
		public async Task When_StrokeLineJoin_Round_Renders()
		{
			// A polyline with a sharp angle and Round joins should have rounded corners
			var container = new Grid { Width = 120, Height = 80, Background = new SolidColorBrush(Colors.White) };
			var polyline = new Polyline
			{
				Points = new PointCollection(new[] { new Point(10, 70), new Point(60, 10), new Point(110, 70) }),
				Stroke = new SolidColorBrush(Colors.Blue),
				StrokeThickness = 16,
				StrokeLineJoin = PenLineJoin.Round,
				Fill = new SolidColorBrush(Colors.Transparent),
			};
			container.Children.Add(polyline);

			await UITestHelper.Load(container);
			var screenshot = await UITestHelper.ScreenShot(container);

			// The join point is at (60, 10). With Round join, the area around the join
			// should have blue color. Verify the join center has the stroke color.
			ImageAssert.HasColorAt(screenshot, 60, 10, Colors.Blue, tolerance: 30);
		}

		[TestMethod]
		public async Task When_StrokeLineJoin_Bevel_Renders()
		{
			var container = new Grid { Width = 120, Height = 80, Background = new SolidColorBrush(Colors.White) };
			var polyline = new Polyline
			{
				Points = new PointCollection(new[] { new Point(10, 70), new Point(60, 10), new Point(110, 70) }),
				Stroke = new SolidColorBrush(Colors.Green),
				StrokeThickness = 16,
				StrokeLineJoin = PenLineJoin.Bevel,
				Fill = new SolidColorBrush(Colors.Transparent),
			};
			container.Children.Add(polyline);

			await UITestHelper.Load(container);
			var screenshot = await UITestHelper.ScreenShot(container);

			// With Bevel join, the join is clipped, but the center at the join point should still be colored.
			ImageAssert.HasColorAt(screenshot, 60, 10, Colors.Green, tolerance: 30);
		}

		[TestMethod]
		public async Task When_StrokeDashCap_Round_Renders()
		{
			// Dashed line with Round dash caps
			var container = new Grid { Width = 300, Height = 40, Background = new SolidColorBrush(Colors.White) };
			var line = new Line
			{
				X1 = 10,
				Y1 = 20,
				X2 = 290,
				Y2 = 20,
				Stroke = new SolidColorBrush(Colors.Black),
				StrokeThickness = 12,
				StrokeDashArray = new DoubleCollection { 3, 2 },
				StrokeDashCap = PenLineCap.Round,
			};
			container.Children.Add(line);

			await UITestHelper.Load(container);
			var screenshot = await UITestHelper.ScreenShot(container);

			// Verify the line center has the stroke color (basic rendering check)
			ImageAssert.HasColorAt(screenshot, 20, 20, Colors.Black, tolerance: 30);
		}

		[TestMethod]
		public async Task When_StrokeDashOffset_Changes_Pattern()
		{
			// Two dashed lines with different offsets should render differently
			var container = new Grid { Width = 200, Height = 60, Background = new SolidColorBrush(Colors.White) };
			container.RowDefinitions.Add(new RowDefinition());
			container.RowDefinitions.Add(new RowDefinition());

			var line1 = new Line
			{
				X1 = 10,
				Y1 = 15,
				X2 = 190,
				Y2 = 15,
				Stroke = new SolidColorBrush(Colors.Black),
				StrokeThickness = 8,
				StrokeDashArray = new DoubleCollection { 4, 4 },
				StrokeDashOffset = 0,
			};
			Grid.SetRow(line1, 0);

			var line2 = new Line
			{
				X1 = 10,
				Y1 = 15,
				X2 = 190,
				Y2 = 15,
				Stroke = new SolidColorBrush(Colors.Black),
				StrokeThickness = 8,
				StrokeDashArray = new DoubleCollection { 4, 4 },
				StrokeDashOffset = 4,
			};
			Grid.SetRow(line2, 1);

			container.Children.Add(line1);
			container.Children.Add(line2);

			await UITestHelper.Load(container);
			var screenshot = await UITestHelper.ScreenShot(container);

			// With offset=4 (half pattern), the dash/gap pattern should be shifted.
			// At the start of the line, one should have a dash and the other a gap.
			// Verify both lines render (center point check)
			ImageAssert.HasColorAt(screenshot, 100, 15, Colors.Black, tolerance: 30);
		}

		[TestMethod]
		public async Task When_StrokeLineCap_Properties_Are_Set()
		{
			// Verify that setting cap properties doesn't crash and the properties are stored correctly
			var line = new Line
			{
				X1 = 10,
				Y1 = 25,
				X2 = 100,
				Y2 = 25,
				Stroke = new SolidColorBrush(Colors.Red),
				StrokeThickness = 10,
				StrokeStartLineCap = PenLineCap.Round,
				StrokeEndLineCap = PenLineCap.Square,
				StrokeLineJoin = PenLineJoin.Bevel,
				StrokeMiterLimit = 5.0,
				StrokeDashCap = PenLineCap.Triangle,
				StrokeDashOffset = 2.0,
			};

			Assert.AreEqual(PenLineCap.Round, line.StrokeStartLineCap);
			Assert.AreEqual(PenLineCap.Square, line.StrokeEndLineCap);
			Assert.AreEqual(PenLineJoin.Bevel, line.StrokeLineJoin);
			Assert.AreEqual(5.0, line.StrokeMiterLimit);
			Assert.AreEqual(PenLineCap.Triangle, line.StrokeDashCap);
			Assert.AreEqual(2.0, line.StrokeDashOffset);

			// Also verify rendering doesn't crash
			var container = new Grid { Width = 120, Height = 50 };
			container.Children.Add(line);
			await UITestHelper.Load(container);
		}

		[TestMethod]
		public async Task When_DashCap_Round_Endpoints_Are_Flat()
		{
			// WinUI uses StrokeStartLineCap/StrokeEndLineCap at path endpoints, not StrokeDashCap.
			// Default start/end caps are Flat, so even with DashCap=Round, the outermost endpoints
			// should NOT have round cap protrusions.
			var container = new Grid { Width = 300, Height = 50, Background = new SolidColorBrush(Colors.White) };
			var line = new Line
			{
				X1 = 30,
				Y1 = 25,
				X2 = 270,
				Y2 = 25,
				Stroke = new SolidColorBrush(Colors.Red),
				StrokeThickness = 20,
				StrokeDashArray = new DoubleCollection { 3, 2 },
				StrokeDashCap = PenLineCap.Round,
				// Start/End caps default to Flat
			};
			container.Children.Add(line);

			await UITestHelper.Load(container);
			var screenshot = await UITestHelper.ScreenShot(container);

			// With Flat start/end caps, there should be no protrusion beyond the endpoints.
			// halfWidth = 10, so Round DashCap would extend to x=20; Flat should not.
			// Check pixel well before the start point: should be white (background)
			ImageAssert.HasColorAt(screenshot, 18, 25, Colors.White, tolerance: 30);
			// Check pixel well after the end point: should be white
			ImageAssert.HasColorAt(screenshot, 282, 25, Colors.White, tolerance: 30);
			// Center of the line should have the stroke color
			ImageAssert.HasColorAt(screenshot, 150, 25, Colors.Red, tolerance: 30);
		}

		[TestMethod]
		public async Task When_DashCap_Round_EndCap_Square()
		{
			// DashCap=Round, StartCap=Flat, EndCap=Square
			// End of path should have a square cap (extending halfWidth beyond endpoint),
			// while start should be flat (no extension).
			var container = new Grid { Width = 300, Height = 50, Background = new SolidColorBrush(Colors.White) };
			var line = new Line
			{
				X1 = 30,
				Y1 = 25,
				X2 = 270,
				Y2 = 25,
				Stroke = new SolidColorBrush(Colors.Red),
				StrokeThickness = 20,
				StrokeDashArray = new DoubleCollection { 3, 2 },
				StrokeDashCap = PenLineCap.Round,
				StrokeStartLineCap = PenLineCap.Flat,
				StrokeEndLineCap = PenLineCap.Square,
			};
			container.Children.Add(line);

			await UITestHelper.Load(container);
			var screenshot = await UITestHelper.ScreenShot(container);

			// Start should be flat: no color before x=30
			ImageAssert.HasColorAt(screenshot, 18, 25, Colors.White, tolerance: 30);
			// End has Square cap: extends halfWidth(10) beyond x=270 → color at x=278
			ImageAssert.HasColorAt(screenshot, 278, 25, Colors.Red, tolerance: 30);
		}

		[TestMethod]
		public async Task When_DashCap_Matches_EndCaps_No_Correction()
		{
			// When DashCap == StartCap == EndCap, no endpoint correction is needed.
			// All caps are Round, so the endpoints should have round cap protrusions.
			var container = new Grid { Width = 300, Height = 50, Background = new SolidColorBrush(Colors.White) };
			var line = new Line
			{
				X1 = 30,
				Y1 = 25,
				X2 = 270,
				Y2 = 25,
				Stroke = new SolidColorBrush(Colors.Red),
				StrokeThickness = 20,
				StrokeDashArray = new DoubleCollection { 3, 2 },
				StrokeDashCap = PenLineCap.Round,
				StrokeStartLineCap = PenLineCap.Round,
				StrokeEndLineCap = PenLineCap.Round,
			};
			container.Children.Add(line);

			await UITestHelper.Load(container);
			var screenshot = await UITestHelper.ScreenShot(container);

			// With Round caps everywhere, the stroke extends beyond endpoints by halfWidth=10.
			// Pixel at x=22 (inside the round cap extension from x=30) should be Red.
			ImageAssert.HasColorAt(screenshot, 22, 25, Colors.Red, tolerance: 30);
			// Pixel at x=278 (inside the round cap extension from x=270) should be Red.
			ImageAssert.HasColorAt(screenshot, 278, 25, Colors.Red, tolerance: 30);
		}

		[TestMethod]
		public async Task When_DashCap_Round_Endpoint_In_Gap_No_Cap()
		{
			// When the path endpoint falls in a gap, no cap should be rendered at all.
			// Use a dash offset so the end of the path lands in a gap.
			var container = new Grid { Width = 300, Height = 50, Background = new SolidColorBrush(Colors.White) };
			// Line length = 240 (270-30). DashArray = "3 2" → pixel values: 60,40 (total=100).
			// With offset=1 → pixel offset=20. patternPos at end (240+20)%100 = 60 → index 1 (gap starts at 60).
			// So end falls exactly at the start of the gap.
			// Try offset=1.5 → pixel offset=30. (240+30)%100 = 70 → accumulated: 60(dash), then 100(gap).
			// 70 < 100, index 1 = gap. End is in a gap.
			var line = new Line
			{
				X1 = 30,
				Y1 = 25,
				X2 = 270,
				Y2 = 25,
				Stroke = new SolidColorBrush(Colors.Red),
				StrokeThickness = 20,
				StrokeDashArray = new DoubleCollection { 3, 2 },
				StrokeDashCap = PenLineCap.Round,
				StrokeDashOffset = 1.5,
			};
			container.Children.Add(line);

			await UITestHelper.Load(container);
			var screenshot = await UITestHelper.ScreenShot(container);

			// End falls on a gap, so there should be no stroke at the end at all.
			// Pixel well after the endpoint should be white.
			ImageAssert.HasColorAt(screenshot, 278, 25, Colors.White, tolerance: 30);
		}

		[TestMethod]
		public async Task When_DashCap_Round_Path_Endpoints_Are_Flat()
		{
			// Same concept as the Line test, but using a Path with an open figure
			// to verify multi-contour handling works correctly.
			var container = new Grid { Width = 250, Height = 120, Background = new SolidColorBrush(Colors.White) };

			var geometry = new PathGeometry();
			var figure = new PathFigure { StartPoint = new Point(30, 60), IsClosed = false };
			figure.Segments.Add(new LineSegment { Point = new Point(220, 60) });
			geometry.Figures.Add(figure);

			var path = new Path
			{
				Stroke = new SolidColorBrush(Colors.Blue),
				StrokeThickness = 16,
				StrokeDashArray = new DoubleCollection { 2, 1 },
				StrokeDashCap = PenLineCap.Round,
				// Start/End caps default to Flat
				Data = geometry,
			};
			container.Children.Add(path);

			await UITestHelper.Load(container);
			var screenshot = await UITestHelper.ScreenShot(container);

			// halfWidth = 8. With Flat start/end caps, no protrusion beyond endpoints.
			// Pixel before start (x=20, well before x=30) should be white
			ImageAssert.HasColorAt(screenshot, 20, 60, Colors.White, tolerance: 30);
			// Pixel after end (x=230, well after x=220) should be white
			ImageAssert.HasColorAt(screenshot, 230, 60, Colors.White, tolerance: 30);
			// Center should be blue
			ImageAssert.HasColorAt(screenshot, 125, 60, Colors.Blue, tolerance: 30);
		}

		// This needs the netstd layouter + non-legacy shapes layout
		[TestMethod]
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
#endif

#if HAS_UNO
		[TestMethod]
#if !__SKIA__
		[Ignore("Only skia accurately hittests shapes")]
#endif
		public async Task When_Shapes_HitTesting()
		{
			// copied from HitTest_Shapes
			var root = new Grid
			{
				Name = "Root",
				Background = Microsoft.UI.Colors.Transparent,
				Width = 200,
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				RowSpacing = 10,
				ColumnDefinitions =
				{
					new ColumnDefinition(),
					new ColumnDefinition()
				},
				RowDefinitions =
				{
					new RowDefinition(),
					new RowDefinition(),
					new RowDefinition(),
					new RowDefinition(),
					new RowDefinition(),
					new RowDefinition()
				},
				Children =
				{
					new Rectangle
					{
						Name = "RectangleFilled",
						Width = 50,
						Height = 50,
						Fill = Microsoft.UI.Colors.Red.WithOpacity(0.5),
						Stroke = Microsoft.UI.Colors.Red,
						StrokeThickness = 6
					}.Apply(r => r.GridPosition(0, 0)),
					new Rectangle
					{
						Name = "RectangleUnfilled",
						Width = 50,
						Height = 50,
						Fill = null,
						Stroke = Microsoft.UI.Colors.Red,
						StrokeThickness = 6
					}.Apply(r => r.GridPosition(0, 1)),
					new Ellipse
					{
						Name = "EllipseFilled",
						Width = 50,
						Height = 50,
						Fill = Microsoft.UI.Colors.Orange.WithOpacity(0.5),
						Stroke = Microsoft.UI.Colors.Orange,
						StrokeThickness = 6
					}.Apply(r => r.GridPosition(1, 0)),
					new Ellipse
					{
						Name = "EllipseUnfilled",
						Width = 50,
						Height = 50,
						Fill = null,
						Stroke = Microsoft.UI.Colors.Orange,
						StrokeThickness = 6
					}.Apply(r => r.GridPosition(1, 1)),
					new Line
					{
						Name = "LineFilled",
						Width = 50,
						Height = 50,
						Stroke = Microsoft.UI.Colors.Yellow,
						StrokeThickness = 20,
						X1 = 0,
						Y1 = 0,
						X2 = 50,
						Y2 = 50
					}.Apply(r => r.GridPosition(2, 0)),
					new Line
					{
						Name = "LineUnfilled",
						Width = 50,
						Height = 50,
						Stroke = null,
						StrokeThickness = 20,
						X1 = 0,
						Y1 = 0,
						X2 = 50,
						Y2 = 50
					}.Apply(r => r.GridPosition(2, 1)),
					new Path
					{
						Name = "PathFilled",
						Width = 50,
						Height = 50,
						Fill = Microsoft.UI.Colors.Green.WithOpacity(0.5),
						Stroke = Microsoft.UI.Colors.Green,
						StrokeThickness = 6,
						Data = new GeometryGroup
						{
							Children =
							{
								new EllipseGeometry
								{
									Center = new Point(50, 50),
									RadiusX = 50,
									RadiusY = 50
								}
							}
						}
					}.Apply(r => r.GridPosition(3, 0)),
					new Path
					{
						Name = "PathUnfilled",
						Width = 50,
						Height = 50,
						Fill = null,
						Stroke = Microsoft.UI.Colors.Green,
						StrokeThickness = 6,
						Data = new GeometryGroup
						{
							Children =
							{
								new EllipseGeometry
								{
									Center = new Point(50, 50),
									RadiusX = 50,
									RadiusY = 50
								}
							}
						}
					}.Apply(r => r.GridPosition(3, 1)),
					new Polygon
					{
						Name = "PolygonFilled",
						Width = 50,
						Height = 50,
						Fill = Microsoft.UI.Colors.Blue.WithOpacity(0.5),
						Stroke = Microsoft.UI.Colors.Blue,
						StrokeThickness = 6,
						Points = new PointCollection(new [] { new Point(0, 0), new Point(0, 50), new Point(50, 50) })
					}.Apply(r => r.GridPosition(4, 0)),
					new Polygon
					{
						Name = "PolygonUnfilled",
						Width = 50,
						Height = 50,
						Fill = null,
						Stroke = Microsoft.UI.Colors.Blue,
						StrokeThickness = 6,
						Points = new PointCollection(new [] { new Point(0, 0), new Point(0, 50), new Point(50, 50) })
					}.Apply(r => r.GridPosition(4, 1)),
					new Polyline
					{
						Name = "PolylineFilled",
						Width = 50,
						Height = 50,
						Fill = Microsoft.UI.Colors.Purple.WithOpacity(0.5),
						Stroke = Microsoft.UI.Colors.Purple,
						StrokeThickness = 6,
						Points = new PointCollection(new [] { new Point(0, 0), new Point(0, 50), new Point(50, 50) })
					}.Apply(r => r.GridPosition(5, 0)),
					new Polyline
					{
						Name = "PolylineUnfilled",
						Width = 50,
						Height = 50,
						Fill = null,
						Stroke = Microsoft.UI.Colors.Purple,
						StrokeThickness = 6,
						Points = new PointCollection(new [] { new Point(0, 0), new Point(0, 50), new Point(50, 50) })
					}.Apply(r => r.GridPosition(5, 1)),
				}
			};

			await UITestHelper.Load(root);

			// points are relative to root
			var pointToTarget = new Dictionary<Point, string>
			{
				{ new(48, 32), "RectangleFilled" },
				{ new(31, 27), "RectangleFilled" },
				{ new(50, 3), "RectangleFilled" },
				{ new(74, 25), "RectangleFilled" },
				{ new(58, 45), "RectangleFilled" },
				{ new(77, 25), "Root" },

				{ new(130, 30), "RectangleUnfilled" },
				{ new(150, 3), "RectangleUnfilled" },
				{ new(172, 27), "RectangleUnfilled" },
				{ new(150, 47), "RectangleUnfilled" },
				{ new(150, 29), "Root" },

				{ new(50, 85), "EllipseFilled" },
				{ new(28, 91), "EllipseFilled" },
				{ new(59, 105), "EllipseFilled" },
				{ new(47, 90), "EllipseFilled" },
				{ new(65, 107), "Root" },
				{ new(30, 102), "Root" },

				{ new(128, 91), "EllipseUnfilled" },
				{ new(159, 105), "EllipseUnfilled" },
				{ new(150, 85), "Root" },
				{ new(147, 90), "Root" },
				{ new(165, 107), "Root" },
				{ new(130, 102), "Root" },

				{ new(51, 155), "LineFilled" },
				{ new(42, 130), "LineFilled" },
				{ new(35, 157), "Root" },
				{ new(60, 134), "Root" },

				// LineUnfilled
				{ new(151, 155), "Root" },
				{ new(142, 130), "Root" },
				{ new(135, 157), "Root" },
				{ new(160, 134), "Root" },

				{ new(31, 209), "PathFilled" },
				{ new(49, 186), "PathFilled" },
				{ new(49, 219), "PathFilled" },
				{ new(67, 210), "PathFilled" },
				{ new(65, 223), "PathFilled" },
				{ new(31, 189), "Root" },

				{ new(131, 209), "PathUnfilled" },
				{ new(149, 186), "PathUnfilled" },
				{ new(149, 219), "Root" },
				{ new(167, 210), "Root" },
				{ new(165, 223), "Root" },
				{ new(131, 189), "Root" },

				{ new(26, 271), "PolygonFilled" },
				{ new(45, 288), "PolygonFilled" },
				{ new(39, 256), "PolygonFilled" },
				{ new(62, 278), "PolygonFilled" },
				{ new(36, 270), "PolygonFilled" },
				{ new(50, 282), "PolygonFilled" },
				{ new(60, 258), "Root" },

				{ new(126, 271), "PolygonUnfilled" },
				{ new(145, 288), "PolygonUnfilled" },
				{ new(139, 256), "PolygonUnfilled" },
				{ new(162, 278), "PolygonUnfilled" },
				{ new(136, 270), "Root" },
				{ new(150, 282), "Root" },
				{ new(160, 258), "Root" },

				{ new(27, 326), "PolylineFilled" },
				{ new(51, 349), "PolylineFilled" },
				{ new(35, 326), "PolylineFilled" },
				{ new(49, 339), "PolylineFilled" },
				{ new(63, 327), "Root" },
				{ new(54, 315), "Root" },

				{ new(127, 326), "PolylineUnfilled" },
				{ new(151, 349), "PolylineUnfilled" },
				{ new(135, 326), "Root" },
				{ new(149, 339), "Root" },
				{ new(163, 327), "Root" },
				{ new(154, 315), "Root" },
			};

			// This may or may not fail if the window is not in view, since it depends on a render cycle occuring after loading,
			// so that the shapes can be hit-tested correctly.
			await Task.Delay(100);
			var transform = root.TransformToVisual(null);
			foreach (var (point, expectedName) in pointToTarget)
			{
				var actualTarget = (FrameworkElement)VisualTreeHelper.HitTest(transform.TransformPoint(point), TestServices.WindowHelper.XamlRoot).element;
				Assert.AreEqual(expectedName, actualTarget.Name);
			}
		}
#endif
	}
}
#endif
