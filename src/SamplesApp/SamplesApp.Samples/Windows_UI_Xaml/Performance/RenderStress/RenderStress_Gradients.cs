#nullable enable

using Windows.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml.Performance.RenderStress
{
	/// <summary>Linear-gradient-filled rectangles — stresses gradient shader setup + fill.</summary>
	[Sample("Performance", Name = "RenderStress_Gradients", Description = "Renderer stress: N linear-gradient rectangles redrawn every frame. Compare fps/frame-time across Skia and the WebGPU/ProGPU builds.")]
	public sealed class RenderStress_Gradients : RenderStressBase
	{
		protected override string ScenarioName => "Gradients (linear fill)";
		protected override int DefaultCount => 500;

		protected override void Populate(Canvas host, int count, double width, double height)
		{
			var rng = new Rng(3);
			for (var i = 0; i < count; i++)
			{
				var brush = new LinearGradientBrush { StartPoint = new(0, 0), EndPoint = new(1, 1) };
				brush.GradientStops.Add(new GradientStop { Offset = 0, Color = rng.Color() });
				brush.GradientStops.Add(new GradientStop { Offset = 0.5, Color = rng.Color() });
				brush.GradientStops.Add(new GradientStop { Offset = 1, Color = rng.Color() });

				var r = new Rectangle
				{
					Width = rng.Range(30, 90),
					Height = rng.Range(30, 90),
					RadiusX = 6,
					RadiusY = 6,
					Fill = brush,
				};
				Canvas.SetLeft(r, rng.Range(0, width - 90));
				Canvas.SetTop(r, rng.Range(0, height - 90));
				host.Children.Add(r);
			}
		}
	}
}
