using System.Threading.Tasks;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Uno.UI.RuntimeTests.Helpers;
using Path = Microsoft.UI.Xaml.Shapes.Path;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Shapes;

[TestClass]
public class Given_Path_UITest
{
	// Matches SamplesApp.UITests Path_Tests.Test_FillRule / PathTestsControl.Path_FillRule sample:
	// a self-intersecting outline with an inner "hole" figure, rendered with EvenOdd vs. Nonzero
	// fill rules, expressed via the three ways Path.Data can be authored.
	public enum DataStyle
	{
		PathDataString,
		PathGeometryFigures,
		GeometryGroup,
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWasm)]
	[DataRow(DataStyle.PathDataString, FillRule.EvenOdd)]
	[DataRow(DataStyle.PathDataString, FillRule.Nonzero)]
	[DataRow(DataStyle.PathGeometryFigures, FillRule.EvenOdd)]
	[DataRow(DataStyle.PathGeometryFigures, FillRule.Nonzero)]
	[DataRow(DataStyle.GeometryGroup, FillRule.EvenOdd)]
	[DataRow(DataStyle.GeometryGroup, FillRule.Nonzero)]
	public async Task When_Path_FillRule(DataStyle style, FillRule fillRule)
	{
		var fillColor = fillRule is FillRule.EvenOdd ? Microsoft.UI.Colors.Green : Microsoft.UI.Colors.Blue;

		var path = new Path
		{
			Data = BuildData(style, fillRule),
			Fill = new SolidColorBrush(fillColor),
			Stroke = new SolidColorBrush(Microsoft.UI.Colors.Black),
			StrokeThickness = 1,
		};

		var container = new Grid
		{
			Width = 91,
			Height = 91,
			Background = new SolidColorBrush(Microsoft.UI.Colors.Beige),
			Children = { path },
		};

		try
		{
			await UITestHelper.Load(container);

			var screenshot = await UITestHelper.ScreenShot(container);

			// Inner "hole" figure always punches through, regardless of the fill rule.
			ImageAssert.HasColorAt(screenshot, new Point(15, 45), Microsoft.UI.Colors.Beige);

			// EvenOdd: overlapping self-intersection cancels out the fill at the center.
			// Nonzero: overlapping windings accumulate, so the center stays filled.
			var expectedCenterColor = fillRule is FillRule.EvenOdd ? Microsoft.UI.Colors.Beige : fillColor;
			ImageAssert.HasColorAt(screenshot, new Point(45, 45), expectedCenterColor);

			ImageAssert.HasColorAt(screenshot, new Point(76, 45), fillColor);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	private static Geometry BuildData(DataStyle style, FillRule fillRule) => style switch
	{
		DataStyle.PathDataString => (Geometry)XamlBindingHelper.ConvertValue(
			typeof(Geometry),
			(fillRule is FillRule.EvenOdd ? "F0" : "F1") + "M0,0L0,60 90,60 90,30 30,30 30,90 60,90 60,0z M10,10 L20,10 20,80 10,80Z"),
		DataStyle.PathGeometryFigures => new PathGeometry
		{
			FillRule = fillRule,
			Figures =
			{
				MakeFigure(new Point(0, 0), new Point(0, 60), new Point(90, 60), new Point(90, 30), new Point(30, 30), new Point(30, 90), new Point(60, 90), new Point(60, 0)),
				MakeFigure(new Point(10, 10), new Point(20, 10), new Point(20, 80), new Point(10, 80)),
			},
		},
		DataStyle.GeometryGroup => new GeometryGroup
		{
			FillRule = fillRule,
			Children =
			{
				new PathGeometry { Figures = { MakeFigure(new Point(0, 0), new Point(0, 60), new Point(90, 60), new Point(90, 30), new Point(30, 30), new Point(30, 90), new Point(60, 90), new Point(60, 0)) } },
				new PathGeometry { Figures = { MakeFigure(new Point(10, 10), new Point(20, 10), new Point(20, 80), new Point(10, 80)) } },
			},
		},
		_ => throw new System.NotSupportedException(style.ToString()),
	};

	private static PathFigure MakeFigure(Point start, params Point[] points) => new()
	{
		StartPoint = start,
		IsClosed = true,
		Segments = { new PolyLineSegment { Points = new PointCollection(points) } },
	};
}
