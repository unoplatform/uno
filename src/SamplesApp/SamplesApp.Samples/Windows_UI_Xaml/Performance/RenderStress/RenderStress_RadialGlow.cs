#nullable enable

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml.Performance.RenderStress
{
	/// <summary>
	/// Large overlapping radial-gradient glows. A radial gradient costs real per-pixel maths (map into the unit
	/// ellipse, then solve along the ray) and each glow covers a large share of the viewport, so the cost is
	/// per-pixel shading rather than geometry — the archetype for spotlights, glows and hero backdrops.
	/// </summary>
	[Sample("Performance", Name = "RenderStress_RadialGlow", Description = "Renderer stress: N large overlapping radial-gradient glows — per-pixel gradient maths over big areas with trivial geometry. Compare fps/frame-time across Skia and the WebGPU/ProGPU builds.")]
	public sealed class RenderStress_RadialGlow : RenderStressBase
	{
		protected override string ScenarioName => "Radial glow (per-pixel gradient)";
		protected override int DefaultCount => 32;

		protected override void Populate(Canvas host, int count, double width, double height)
		{
			var rng = new Rng(23);
			for (var i = 0; i < count; i++)
			{
				var w = rng.Range(width * 0.45, width * 0.8);
				var h = rng.Range(height * 0.45, height * 0.8);

				var brush = new RadialGradientBrush
				{
					Center = new Windows.Foundation.Point(0.5, 0.5),
					GradientOrigin = new Windows.Foundation.Point(rng.Range(0.35, 0.65), rng.Range(0.35, 0.65)),
					RadiusX = 0.5,
					RadiusY = 0.5,
				};
				brush.GradientStops.Add(new GradientStop { Offset = 0, Color = rng.Color(0x5A) });
				brush.GradientStops.Add(new GradientStop { Offset = 0.55, Color = rng.Color(0x30) });
				brush.GradientStops.Add(new GradientStop { Offset = 1, Color = Windows.UI.Color.FromArgb(0, 0, 0, 0) });

				var e = new Ellipse { Width = w, Height = h, Fill = brush };
				Canvas.SetLeft(e, rng.Range(-w * 0.25, width - w * 0.75));
				Canvas.SetTop(e, rng.Range(-h * 0.25, height - h * 0.75));
				host.Children.Add(e);
			}
		}
	}
}
