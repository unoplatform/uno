#nullable enable

using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml.Performance.RenderStress
{
	/// <summary>Thick multi-segment stroked polylines — stresses stroke expansion / joins / caps.</summary>
	[Sample("Performance", Name = "RenderStress_Strokes", Description = "Renderer stress: N thick stroked polylines redrawn every frame. Compare fps/frame-time across Skia and the WebGPU/ProGPU builds.")]
	public sealed class RenderStress_Strokes : RenderStressBase
	{
		protected override string ScenarioName => "Strokes (thick polylines)";
		protected override int DefaultCount => 500;

		protected override void Populate(Canvas host, int count, double width, double height)
		{
			var rng = new Rng(2);
			for (var i = 0; i < count; i++)
			{
				var x = rng.Range(0, width);
				var y = rng.Range(0, height);
				var pts = new PointCollection();
				for (var p = 0; p < 6; p++)
				{
					pts.Add(new Point(x + rng.Range(-80, 80), y + rng.Range(-80, 80)));
				}
				host.Children.Add(new Polyline
				{
					Points = pts,
					Stroke = new SolidColorBrush(rng.Color()),
					StrokeThickness = rng.Range(2, 8),
					StrokeLineJoin = PenLineJoin.Round,
					StrokeStartLineCap = PenLineCap.Round,
					StrokeEndLineCap = PenLineCap.Round,
				});
			}
		}
	}
}
