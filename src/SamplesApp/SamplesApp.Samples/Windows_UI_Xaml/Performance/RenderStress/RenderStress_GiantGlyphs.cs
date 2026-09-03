#nullable enable

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml.Performance.RenderStress
{
	/// <summary>
	/// A handful of enormous overlapping glyphs. Each is one path fill, so record stays tiny, but the coverage
	/// area per glyph is a large fraction of the viewport — this isolates anti-aliased PATH COVERAGE cost from
	/// the per-glyph overhead that a normal text run measures.
	/// </summary>
	[Sample("Performance", Name = "RenderStress_GiantGlyphs", Description = "Renderer stress: N enormous overlapping glyphs — anti-aliased path coverage over huge areas with almost no geometry. Compare fps/frame-time across Skia and the WebGPU/ProGPU builds.")]
	public sealed class RenderStress_GiantGlyphs : RenderStressBase
	{
		private const string Glyphs = "@#&%WM8Q0BR";

		protected override string ScenarioName => "Giant glyphs (path coverage)";
		protected override int DefaultCount => 40;   // 16 sat right on the vsync cap on Skia (17.1ms, draw 77%)

		protected override void Populate(Canvas host, int count, double width, double height)
		{
			var rng = new Rng(37);
			for (var i = 0; i < count; i++)
			{
				var t = new TextBlock
				{
					Text = Glyphs[i % Glyphs.Length].ToString(),
					FontSize = rng.Range(height * 0.55, height * 0.95),
					FontWeight = Microsoft.UI.Text.FontWeights.Bold,
					Foreground = new SolidColorBrush(rng.Color(0x40)),
				};
				Canvas.SetLeft(t, rng.Range(-width * 0.1, width * 0.75));
				Canvas.SetTop(t, rng.Range(-height * 0.3, height * 0.2));
				host.Children.Add(t);
			}
		}
	}
}
