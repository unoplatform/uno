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
	/// Overlapping sub-1 opacity groups — each element with content and <c>Opacity &lt; 1</c> forces an offscreen
	/// transparency layer (SaveLayer), which the review flagged as a WebGPU weak spot (full-target offscreen alloc).
	/// </summary>
	[Sample("Performance", Name = "RenderStress_TransparencyLayers", Description = "Renderer stress: N sub-1-opacity groups → offscreen layers, redrawn every frame. Compare fps/frame-time across Skia and the WebGPU/ProGPU builds.")]
	public sealed class RenderStress_TransparencyLayers : RenderStressBase
	{
		protected override string ScenarioName => "Transparency layers (opacity)";
		protected override int DefaultCount => 250;

		protected override void Populate(Canvas host, int count, double width, double height)
		{
			var rng = new Rng(5);
			for (var i = 0; i < count; i++)
			{
				// A group with two overlapping shapes and group Opacity < 1 → the compositor must isolate it in a
				// layer before compositing (otherwise the overlaps would double-blend), exercising SaveLayer.
				var group = new Grid { Opacity = rng.Range(0.25, 0.6), Width = 100, Height = 100 };
				group.Children.Add(new Ellipse { Width = 80, Height = 80, Fill = new SolidColorBrush(rng.Color()), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top });
				group.Children.Add(new Rectangle { Width = 80, Height = 80, Fill = new SolidColorBrush(rng.Color()), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom });
				Canvas.SetLeft(group, rng.Range(0, width - 100));
				Canvas.SetTop(group, rng.Range(0, height - 100));
				host.Children.Add(group);
			}
		}
	}
}
