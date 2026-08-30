#nullable enable

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml.Performance.RenderStress
{
	/// <summary>
	/// Many small gradient-filled ellipses. A shape filled with a non-solid brush is drawn as a CLIP plus a paint,
	/// so this is dominated by per-clip cost rather than by fill: each shape covers few pixels, but every one needs
	/// its own clip established and torn down. That is the avatar/chip/badge archetype — a list full of small
	/// rounded, clipped thumbnails.
	/// </summary>
	[Sample("Performance", Name = "RenderStress_ClipGrid", Description = "Renderer stress: N small gradient-filled ellipses — dominated by per-clip setup rather than fill rate. Compare fps/frame-time across Skia and the WebGPU/ProGPU builds.")]
	public sealed class RenderStress_ClipGrid : RenderStressBase
	{
		protected override string ScenarioName => "Clip grid (per-clip cost)";
		protected override int DefaultCount => 900;

		protected override void Populate(Canvas host, int count, double width, double height)
		{
			var rng = new Rng(53);
			for (var i = 0; i < count; i++)
			{
				var d = rng.Range(18, 34);

				var brush = new LinearGradientBrush { StartPoint = new(0, 0), EndPoint = new(1, 1) };
				brush.GradientStops.Add(new GradientStop { Offset = 0, Color = rng.Color(0xC0) });
				brush.GradientStops.Add(new GradientStop { Offset = 1, Color = rng.Color(0xC0) });

				var e = new Ellipse { Width = d, Height = d, Fill = brush };
				Canvas.SetLeft(e, rng.Range(0, width - d));
				Canvas.SetTop(e, rng.Range(0, height - d));
				host.Children.Add(e);
			}
		}
	}
}
