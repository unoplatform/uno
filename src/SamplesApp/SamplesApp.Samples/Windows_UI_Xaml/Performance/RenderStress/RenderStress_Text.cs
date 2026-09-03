#nullable enable

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml.Performance.RenderStress
{
	/// <summary>Many short text runs — stresses glyph shaping/rasterization and the glyph draw path.</summary>
	[Sample("Performance", Name = "RenderStress_Text", Description = "Renderer stress: N text runs redrawn every frame. Compare fps/frame-time across Skia and the WebGPU/ProGPU builds.")]
	public sealed class RenderStress_Text : RenderStressBase
	{
		private static readonly string[] _words = { "Uno", "Platform", "WebGPU", "Skia", "ProGPU", "render", "glyph", "frame", "0123", "MMMM" };

		protected override string ScenarioName => "Text (glyph runs)";
		protected override int DefaultCount => 600;

		protected override void Populate(Canvas host, int count, double width, double height)
		{
			var rng = new Rng(4);
			for (var i = 0; i < count; i++)
			{
				var tb = new TextBlock
				{
					Text = _words[i % _words.Length] + (i % 97),
					FontSize = rng.Range(12, 28),
					Foreground = new SolidColorBrush(rng.Color()),
				};
				Canvas.SetLeft(tb, rng.Range(0, width - 120));
				Canvas.SetTop(tb, rng.Range(0, height - 30));
				host.Children.Add(tb);
			}
		}
	}
}
