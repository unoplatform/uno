using System.Threading.Tasks;
using Windows.Foundation;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Shapes
{
	// Fill-rule (winding) parity: a path with two same-direction nested rectangles fills the inner region only under
	// NonZero; EvenOdd leaves it a hole. This pins the renderer's winding to WinUI's — the WebGPU backend honours the
	// geometry FillRule (nonzero stencil Inc/DecWrap) rather than hardcoding even-odd, so this must agree with WinUI.
	[TestClass]
	[RunsOnUIThread]
	public class Given_Path_FillRule
	{
		// Two nested clockwise rectangles built programmatically (bypassing path-markup) so FillRule is set explicitly.
		// Center (50,50) is inside both contours; corner (10,10) is inside only the outer.
		private static Grid Build(FillRule rule)
		{
			static PathFigure Rect(double x0, double y0, double x1, double y1)
			{
				var fig = new PathFigure { StartPoint = new Point(x0, y0), IsClosed = true, IsFilled = true };
				var seg = new PolyLineSegment();
				seg.Points.Add(new Point(x1, y0));
				seg.Points.Add(new Point(x1, y1));
				seg.Points.Add(new Point(x0, y1));
				fig.Segments.Add(seg);
				return fig;
			}

			var geo = new PathGeometry { FillRule = rule };
			geo.Figures.Add(Rect(0, 0, 100, 100));
			geo.Figures.Add(Rect(25, 25, 75, 75));

			var container = new Grid { Width = 100, Height = 100, Background = new SolidColorBrush(Colors.White) };
			container.Children.Add(new Path { Fill = new SolidColorBrush(Colors.Red), Data = geo });
			return container;
		}

		[TestMethod]
#if !__SKIA__ && !WINAPPSDK
		[Ignore("Screenshot pixel comparison is validated on Skia and native WinUI (parity target).")]
#endif
		public async Task When_NonZero_Fills_Nested_Region()
		{
			var container = Build(FillRule.Nonzero);
			await UITestHelper.Load(container);
			var screenshot = await UITestHelper.ScreenShot(container);

			// NonZero: both contours wind the same way, so the inner region is filled.
			ImageAssert.HasColorAt(screenshot, 50, 50, Colors.Red, tolerance: 30);
			ImageAssert.HasColorAt(screenshot, 10, 10, Colors.Red, tolerance: 30);
		}

		[TestMethod]
#if !__SKIA__ && !WINAPPSDK
		[Ignore("Screenshot pixel comparison is validated on Skia and native WinUI (parity target).")]
#endif
		public async Task When_EvenOdd_Holes_Nested_Region()
		{
			var container = Build(FillRule.EvenOdd);
			await UITestHelper.Load(container);
			var screenshot = await UITestHelper.ScreenShot(container);

			// EvenOdd: the inner contour cancels the outer, so the centre is a hole (background white)
			// while the ring between the rectangles stays filled.
			ImageAssert.HasColorAt(screenshot, 50, 50, Colors.White, tolerance: 30);
			ImageAssert.HasColorAt(screenshot, 10, 10, Colors.Red, tolerance: 30);
		}
	}
}
