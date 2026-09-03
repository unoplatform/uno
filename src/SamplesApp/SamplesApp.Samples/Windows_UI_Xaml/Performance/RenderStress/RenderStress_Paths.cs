#nullable enable

using System;
using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml.Performance.RenderStress
{
	/// <summary>Filled cubic-Bézier paths — stresses geometry tessellation + solid fill.</summary>
	[Sample("Performance", Name = "RenderStress_Paths", Description = "Renderer stress: N filled Bézier paths redrawn every frame. Compare fps/frame-time across Skia and the WebGPU/ProGPU builds.")]
	public sealed class RenderStress_Paths : RenderStressBase
	{
		protected override string ScenarioName => "Paths (filled Bézier)";
		protected override int DefaultCount => 500;

		protected override void Populate(Canvas host, int count, double width, double height)
		{
			var rng = new Rng(1);
			for (var i = 0; i < count; i++)
			{
				var x = rng.Range(0, width);
				var y = rng.Range(0, height);
				var fig = new PathFigure { StartPoint = new Point(x, y), IsFilled = true, IsClosed = true };
				fig.Segments.Add(new BezierSegment
				{
					Point1 = new Point(x + rng.Range(-60, 60), y + rng.Range(-60, 60)),
					Point2 = new Point(x + rng.Range(-60, 60), y + rng.Range(-60, 60)),
					Point3 = new Point(x + rng.Range(-40, 40), y + rng.Range(-40, 40)),
				});
				fig.Segments.Add(new BezierSegment
				{
					Point1 = new Point(x + rng.Range(-60, 60), y + rng.Range(-60, 60)),
					Point2 = new Point(x + rng.Range(-60, 60), y + rng.Range(-60, 60)),
					Point3 = new Point(x, y),
				});
				var geo = new PathGeometry();
				geo.Figures.Add(fig);
				host.Children.Add(new Path { Data = geo, Fill = new SolidColorBrush(rng.Color(0xC0)) });
			}
		}
	}
}
