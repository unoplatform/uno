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
	public class When_Shape
	{
		[TestMethod]
		[RunsOnUIThread]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282! epic")]
#endif
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
#endif
	}
}
#endif
