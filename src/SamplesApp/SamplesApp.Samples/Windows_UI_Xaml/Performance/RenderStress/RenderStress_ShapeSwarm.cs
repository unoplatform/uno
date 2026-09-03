#nullable enable

using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml.Performance.RenderStress
{
	/// <summary>
	/// Thousands of SMALL, individually distinct polygons. Every other RenderStress scene draws a handful of
	/// enormous shapes and is bound by fill rate; this one covers almost no area and is bound by the per-shape
	/// pipeline instead — flattening, op building, buffer packing and draw batching.
	/// <para>
	/// Distinctness is the point: each polygon has its own vertex count and outline, so nothing can be shared or
	/// instanced, and a renderer's per-op overhead shows up multiplied by N. That is the shape of charts, maps,
	/// diagram canvases and icon-dense dashboards.
	/// </para>
	/// </summary>
	[Sample("Performance", Name = "RenderStress_ShapeSwarm", Description = "Renderer stress: N small distinct polygons — bound by per-shape op building and batching rather than fill rate. Compare fps/frame-time across Skia and the WebGPU/ProGPU builds.")]
	public sealed class RenderStress_ShapeSwarm : RenderStressBase
	{
		protected override string ScenarioName => "Shape swarm (per-op cost)";

		/// <summary>Number of distinct polygons. Enough that per-op cost dominates the frame, not coverage.</summary>
		protected override int DefaultCount => 5000;

		protected override void Populate(Canvas host, int count, double width, double height)
		{
			var rng = new Rng(877);
			// Jittered grid rather than independent random x/y: successive draws from this generator correlate
			// enough to leave diagonal streaks and bare regions, which would make coverage depend on the count.
			var cols = (int)System.Math.Ceiling(System.Math.Sqrt(count * width / System.Math.Max(height, 1)));
			cols = System.Math.Max(1, cols);
			var rows = (int)System.Math.Ceiling(count / (double)cols);
			var cellW = width / cols;
			var cellH = height / System.Math.Max(rows, 1);

			for (var i = 0; i < count; i++)
			{
				// 5-9 sides with per-vertex jitter: every outline differs, so no cache keyed on shape can hit.
				var sides = (int)rng.Range(5, 10);
				var r = rng.Range(6, 16);
				var cx = (i % cols + 0.5) * cellW + rng.Range(-cellW * 0.35, cellW * 0.35);
				var cy = (i / cols + 0.5) * cellH + rng.Range(-cellH * 0.35, cellH * 0.35);

				var pts = new PointCollection();
				for (var s = 0; s < sides; s++)
				{
					var a = s / (double)sides * System.Math.PI * 2 + rng.Range(0, 0.4);
					var rr = r * rng.Range(0.6, 1.0);
					pts.Add(new Point(cx + System.Math.Cos(a) * rr, cy + System.Math.Sin(a) * rr));
				}

				host.Children.Add(new Polygon
				{
					Points = pts,
					Fill = new SolidColorBrush(rng.Color(0xB0)),
				});
			}
		}
	}
}
