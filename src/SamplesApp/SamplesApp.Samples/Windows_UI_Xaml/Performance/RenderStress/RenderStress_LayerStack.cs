#nullable enable

using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml.Performance.RenderStress
{
	/// <summary>
	/// N sibling groups, each with its own opacity, so each one forces an ISOLATION LAYER: the group renders to
	/// an offscreen surface and is then composited back.
	/// <para>
	/// This is the axis no other RenderStress scene touches. The others are bound by fill rate or by per-shape op
	/// cost; here the cost is per LAYER — allocating a surface, rendering into it, and compositing it — and a
	/// backend that sizes those surfaces to the window rather than to the group's content pays for the whole
	/// viewport N times over regardless of how small the content is. Each group here deliberately covers a small
	/// area, so any cost that scales with the window instead of the content is immediately visible.
	/// </para>
	/// Group opacity is the everyday source of these layers: hover and disabled states, fade transitions, and any
	/// panel given an Opacity in markup.
	/// </summary>
	[Sample("Performance", Name = "RenderStress_LayerStack", Description = "Renderer stress: N small groups each with its own opacity, forcing one isolation layer apiece — measures per-layer cost rather than fill rate. Compare fps/frame-time across Skia and the WebGPU/ProGPU builds.")]
	public sealed class RenderStress_LayerStack : RenderStressBase
	{
		protected override string ScenarioName => "Layer stack (per-layer cost)";

		/// <summary>
		/// Number of opacity groups, i.e. isolation layers per frame. At 96 both backends sit at the vsync cap on
		/// real hardware and measure nothing; this is the count where they separate.
		/// </summary>
		protected override int DefaultCount => 384;

		protected override void Populate(Canvas host, int count, double width, double height)
		{
			var rng = new Rng(419);
			// Jittered grid: independent random placement from this generator clusters, which would leave most
			// groups off-screen and quietly stop measuring anything.
			var cols = (int)System.Math.Ceiling(System.Math.Sqrt(count * width / System.Math.Max(height, 1)));
			cols = System.Math.Max(1, cols);
			var rows = (int)System.Math.Ceiling(count / (double)cols);
			var cellW = width / cols;
			var cellH = height / System.Math.Max(rows, 1);

			for (var i = 0; i < count; i++)
			{
				var w = rng.Range(cellW * 0.7, cellW * 1.3);
				var h = rng.Range(cellH * 0.7, cellH * 1.3);

				// A small group: whatever a layer costs here is NOT justified by its coverage.
				var group = new Canvas
				{
					Width = w,
					Height = h,
					// Fractional opacity is what forces the isolation layer — the group must be composited as a
					// unit, so it cannot be flattened into its parent.
					Opacity = rng.Range(0.35, 0.85),
				};

				group.Children.Add(new Rectangle
				{
					Width = w,
					Height = h,
					RadiusX = 10,
					RadiusY = 10,
					Fill = new SolidColorBrush(rng.Color(0xC0)),
				});

				var inner = rng.Range(w * 0.3, w * 0.6);
				var dot = new Ellipse { Width = inner, Height = inner, Fill = new SolidColorBrush(rng.Color(0xE0)) };
				Canvas.SetLeft(dot, rng.Range(0, w - inner));
				Canvas.SetTop(dot, rng.Range(0, h - inner));
				group.Children.Add(dot);

				Canvas.SetLeft(group, (i % cols) * cellW + rng.Range(-cellW * 0.2, cellW * 0.2));
				Canvas.SetTop(group, (i / cols) * cellH + rng.Range(-cellH * 0.2, cellH * 0.2));
				host.Children.Add(group);
			}
		}
	}
}
