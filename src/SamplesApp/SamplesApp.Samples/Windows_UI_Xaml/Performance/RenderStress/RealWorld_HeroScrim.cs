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
	/// <c>count</c> is the number of poster cards; the hero and its scrims are fixed, so raising it adds
	/// large-image draws over an already-expensive background.
	/// </summary>
	[Sample("Performance", Name = "RealWorld_HeroScrim", Description = "Real-UI perf: a full-bleed hero image with stacked gradient scrims plus N large poster cards, each scrimmed and rounded-clipped. Scales image sampling and blending by AREA, so it is draw-bound. Compare fps/frame-time across Skia and the WebGPU/ProGPU builds.")]
	public sealed class RealWorld_HeroScrim : PerfBenchBase
	{
		private const string Hero = "ms-appx:///Assets/LargeWisteria.jpg";

		private ScrollViewer _sv = null!;
		private double _offset;
		private int _dir = 1;

		protected override string ScenarioName => "HeroScrim";

		/// <summary>Number of large poster cards drawn over the scrimmed hero.</summary>
		protected override int DefaultCount => 24;

		protected override UIElement BuildStage(int count)
		{
			var rng = new Rng(47);
			var root = new Grid();

			// --- Hero: one very large image stretched over the whole viewport. UniformToFill means the decoded
			// bitmap is sampled at a scale factor, which is per-pixel work over the full surface. ---
			root.Children.Add(new Image
			{
				Source = new BitmapImage(new System.Uri(Hero)),
				Stretch = Stretch.UniformToFill,
			});

			// --- Two full-viewport scrims over it. Each is a separate full-surface blend pass. ---
			root.Children.Add(NewScrim(0x00, 0xB0, new Point(0.5, 0), new Point(0.5, 1)));
			root.Children.Add(NewScrim(0x90, 0x00, new Point(0, 0.5), new Point(1, 0.5)));

			// --- Poster rail scrolling over the scrimmed hero. ---
			var rows = new StackPanel { Padding = new Thickness(24, 120, 24, 24), Spacing = 18 };
			for (var i = 0; i < count; i += 4)
			{
				var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 18 };
				for (var c = 0; c < 4 && i + c < count; c++)
				{
					row.Children.Add(BuildPoster(i + c, rng));
				}

				rows.Children.Add(row);
			}

			_sv = new ScrollViewer
			{
				Content = rows,
				VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
				HorizontalScrollMode = ScrollMode.Disabled,
			};
			root.Children.Add(_sv);

			_offset = 0;
			_dir = 1;
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
			// Scroll only: the posters are re-RASTERIZED (large image samples + scrim blends) without being
			// re-RECORDED, keeping the measurement in the draw phase.
			AdvanceScroll(_sv, ref _offset, ref _dir, 6.0);
		}
	}
}
