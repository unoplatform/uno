#nullable enable

using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml.Performance.RenderStress
{
	/// <summary>
	/// Streaming/media home-screen archetype: a full-bleed hero image scaled to the viewport, stacked gradient
	/// scrims over it, and rows of large poster cards that each carry their own scrim and rounded clip.
	/// <para>
	/// GPU/fill-rate bound by construction. Unlike a wall of small thumbnails (<c>RealWorld_ImageGrid</c>) the cost
	/// here is a few VERY large sampled images plus full-width translucent scrims — work that scales with area, not
	/// with element count, so it stays in draw rather than record or layout. Distinct from
	/// <c>RealWorld_OverlayStack</c> too: that scales isolation layers, this scales image sampling and blending.
	/// </para>
	/// <c>count</c> is the number of FULL-VIEWPORT image layers stacked over each other. They are always on
	/// screen, so nothing is culled — an earlier version scaled a scrolling poster rail instead and stayed pinned
	/// at 60fps because the extra posters simply scrolled out of view (record FELL as count rose, which is the
	/// tell). Each layer is a full-surface bitmap sample plus a blend.
	/// </summary>
	[Sample("Performance", Name = "RealWorld_HeroScrim", Description = "Real-UI perf: a full-bleed hero image with stacked gradient scrims plus N large poster cards, each scrimmed and rounded-clipped. Scales image sampling and blending by AREA, so it is draw-bound. Compare fps/frame-time across Skia and the WebGPU/ProGPU builds.")]
	public sealed class RealWorld_HeroScrim : PerfBenchBase
	{
		private const string Hero = "ms-appx:///Assets/LargeWisteria.jpg";

		private Image? _layer;

		protected override string ScenarioName => "HeroScrim";

		/// <summary>Number of stacked full-viewport image layers. Each is a full-surface sample + blend.</summary>
		protected override int DefaultCount => 10;

		protected override UIElement BuildStage(int count)
		{
			var rng = new Rng(47);
			var root = new Grid();

			// --- N full-viewport image layers. UniformToFill means each is sampled at a scale factor over the
			// whole surface, and each carries a sub-1 opacity so it also blends. Always visible => never culled. ---
			for (var i = 0; i < count; i++)
			{
				var layer = new Image
				{
					Source = new BitmapImage(new System.Uri(Hero)),
					Stretch = i % 2 == 0 ? Stretch.UniformToFill : Stretch.Fill,
					Opacity = 0.35 + (i % 3) * 0.12,
				};
				_layer ??= layer;
				root.Children.Add(layer);
			}

			// --- Full-viewport scrims over them: more full-surface blend passes. ---
			root.Children.Add(NewScrim(0x00, 0xB0, new Point(0.5, 0), new Point(0.5, 1)));
			root.Children.Add(NewScrim(0x90, 0x00, new Point(0, 0.5), new Point(1, 0.5)));

			// --- A fixed rail of large posters, sized to stay on screen so it never culls. ---
			var row = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 18,
				VerticalAlignment = VerticalAlignment.Bottom,
				Margin = new Thickness(24),
			};
			for (var c = 0; c < 4; c++)
			{
				row.Children.Add(BuildPoster(c, rng));
			}

			root.Children.Add(row);
			return root;
		}

		private static UIElement NewScrim(byte startAlpha, byte endAlpha, Point start, Point end)
		{
			var brush = new LinearGradientBrush { StartPoint = start, EndPoint = end };
			brush.GradientStops.Add(new GradientStop { Offset = 0, Color = Color.FromArgb(startAlpha, 0x05, 0x07, 0x10) });
			brush.GradientStops.Add(new GradientStop { Offset = 1, Color = Color.FromArgb(endAlpha, 0x05, 0x07, 0x10) });
			return new Border { Background = brush };
		}

		private static UIElement BuildPoster(int index, Rng rng)
		{
			// Each poster is a large sampled image under its own gradient scrim, inside a rounded clip — the
			// rounded corner forces per-fragment clip coverage over the whole card.
			var scrim = new LinearGradientBrush { StartPoint = new Point(0.5, 0.35), EndPoint = new Point(0.5, 1) };
			scrim.GradientStops.Add(new GradientStop { Offset = 0, Color = Color.FromArgb(0x00, 0x00, 0x00, 0x00) });
			scrim.GradientStops.Add(new GradientStop { Offset = 1, Color = Color.FromArgb(0xE0, 0x03, 0x05, 0x0C) });

			return new Border
			{
				Width = 340,
				Height = 470,
				CornerRadius = new CornerRadius(16),
				Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x0C, 0x10, 0x1C)),
				Child = new Grid
				{
					Children =
					{
						new Image
						{
							Source = new BitmapImage(new System.Uri(Hero)),
							Stretch = Stretch.UniformToFill,
						},
						new Border { Background = scrim },
						new StackPanel
						{
							VerticalAlignment = VerticalAlignment.Bottom,
							Padding = new Thickness(16),
							Spacing = 4,
							Children =
							{
								new TextBlock
								{
									Text = $"Feature {index + 1}",
									FontSize = 17,
									Foreground = new SolidColorBrush(Windows.UI.Colors.White),
								},
								new TextBlock
								{
									Text = $"{1 + (int)rng.Range(1, 3)}h {(int)rng.Range(2, 58)}m · 4K HDR",
									FontSize = 12,
									Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0xC0, 0xC8, 0xDC)),
								},
							},
						},
					},
				},
			};
		}

		protected override void Tick(long frame)
		{
			// Nudge one layer's opacity so the frame is not trivially cacheable, without re-recording geometry:
			// the cost under measurement is re-sampling and re-blending N full-viewport images.
			if (_layer is { } l) { l.Opacity = 0.30 + ((frame % 20) / 20.0) * 0.35; }
		}

	}
}
