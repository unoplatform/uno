#nullable enable

using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml.Performance.RenderStress
{
	/// <summary>
	/// Card-veil archetype: a scrolling grid of translucent cards — the shape a real dashboard, gallery or
	/// record list takes when cards carry a sub-1 Opacity (hover veils, disabled/pending states, fade-ins).
	/// <para>
	/// This is the counterpart to <c>RealWorld_OverlayStack</c>. That one stacks a few FULL-VIEWPORT layers;
	/// this one puts MANY SMALL ones on screen at once. Both are isolation layers, but only this shape shows
	/// whether a backend's per-layer cost scales with the layer's own size or with the whole window — a
	/// distinction the full-screen scrim cannot expose, and the one that decides real-app cost, since a
	/// backend paying window-sized cost for a 300x160 card pays ~40x what the pixels are worth.
	/// </para>
	/// <c>count</c> is the number of cards; off-screen ones are culled, so roughly 30 layers are live at 1080p.
	/// </summary>
	[Sample("Performance", Name = "RealWorld_CardVeil", Description = "Real-UI perf: a scrolling grid of translucent cards, each a SMALL isolation layer. Measures whether per-layer cost scales with layer size or with window size. Compare fps/frame-time across Skia and the WebGPU/ProGPU builds.")]
	public sealed class RealWorld_CardVeil : PerfBenchBase
	{
		private const int Columns = 5;

		private ScrollViewer _sv = null!;
		private double _offset;
		private int _dir = 1;

		protected override string ScenarioName => "CardVeil";

		/// <summary>
		/// Number of translucent cards. Each visible one is its own isolation layer. Kept modest on purpose:
		/// the panel is not virtualized, so a large count turns this into a layout benchmark (at 240 it measured
		/// ~28ms record and ~28ms layout, swamping the per-layer draw cost it exists to isolate).
		/// </summary>
		protected override int DefaultCount => 90;

		protected override UIElement BuildStage(int count)
		{
			var rng = new Rng(29);
			var rows = new StackPanel { Padding = new Thickness(14), Spacing = 12 };

			for (var i = 0; i < count; i += Columns)
			{
				var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
				for (var c = 0; c < Columns && i + c < count; c++)
				{
					row.Children.Add(BuildCard(i + c, rng));
				}

				rows.Children.Add(row);
			}

			_sv = new ScrollViewer
			{
				Content = rows,
				VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
				HorizontalScrollMode = ScrollMode.Disabled,
			};

			_offset = 0;
			_dir = 1;
			return new Grid { Children = { _sv } };
		}

		private static UIElement BuildCard(int index, Rng rng)
		{
			var accent = Color.FromArgb(0xFF, rng.Byte(), (byte)(0x60 + (index % 5) * 20), 0xD0);

			// Opacity < 1 on an element WITH content is what forces the isolation layer. The card is 300x160,
			// so a backend that sizes the layer to its content touches ~48k pixels; one that uses the window
			// touches ~1.8M for the same card.
			return new Border
			{
				Width = 300,
				Height = 160,
				Opacity = 0.88,
				CornerRadius = new CornerRadius(10),
				Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x1B, 0x20, 0x30)),
				BorderThickness = new Thickness(1),
				BorderBrush = new SolidColorBrush(Color.FromArgb(0x50, 0xFF, 0xFF, 0xFF)),
				Padding = new Thickness(14),
				Child = new StackPanel
				{
					Spacing = 6,
					Children =
					{
						new Border
						{
							Width = 54,
							Height = 20,
							CornerRadius = new CornerRadius(10),
							HorizontalAlignment = HorizontalAlignment.Left,
							Background = new SolidColorBrush(accent),
						},
						new TextBlock
						{
							Text = $"Submittal {2400 + index}",
							FontSize = 15,
							Foreground = new SolidColorBrush(Windows.UI.Colors.White),
						},
						new TextBlock
						{
							Text = "Structural steel shop drawings — revision C",
							FontSize = 12,
							TextWrapping = TextWrapping.Wrap,
							Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0xB8, 0xC2, 0xDC)),
						},
						new TextBlock
						{
							Text = $"Due 9/{1 + (index % 28):D2} · Ball in court: Architect",
							FontSize = 11,
							Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0x8A, 0x96, 0xB4)),
						},
					},
				},
			};
		}

		protected override void Tick(long frame)
		{
			// Scroll only, so the cards are re-RASTERIZED without being re-RECORDED — the cost under measurement
			// is per-layer rasterization, not recording churn.
			AdvanceScroll(_sv, ref _offset, ref _dir, 6.0);
		}
	}
}
