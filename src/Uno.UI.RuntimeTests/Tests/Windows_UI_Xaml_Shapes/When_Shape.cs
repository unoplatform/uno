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


#if __SKIA__ // This needs the netstd layouter + non-legacy shapes layout
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
