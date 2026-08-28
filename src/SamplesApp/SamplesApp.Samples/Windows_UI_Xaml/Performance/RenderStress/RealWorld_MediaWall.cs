#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml.Performance.RenderStress
{
	/// <summary>
	/// Media-wall archetype: an un-virtualized wall of elevated photo cards, the shape a photo gallery or a
	/// drawing-log thumbnail grid actually has. Deliberately FILL-RATE bound rather than layout bound — every
	/// card stacks the four constructs that cost draw time and nothing else:
	/// <list type="bullet">
	/// <item>a <see cref="ThemeShadow"/> elevation → an offscreen + blur pass per card</item>
	/// <item>a sub-1 <c>Opacity</c> veil over content → an isolation layer (SaveLayer) per card</item>
	/// <item>a scaled <see cref="Image"/> (UniformToFill) → resampling over the card area</item>
	/// <item>a gradient caption scrim + rounded-corner clip → per-pixel shader work and a non-trivial clip</item>
	/// </list>
	/// The per-frame tick only animates veil opacity and elevation, so measure/arrange stay at zero and the
	/// entire frame cost lands in draw.
	/// </summary>
	[Sample("Performance", Name = "RealWorld_MediaWall", Description = "Real-UI perf: an un-virtualized wall of elevated photo cards (shadow blur + opacity layer + scaled image + gradient scrim each). Deliberately fill-rate bound — the draw-phase worst case. Compare fps/frame-time across Skia and the WebGPU/ProGPU builds.")]
	public sealed class RealWorld_MediaWall : PerfBenchBase
	{
		private const int Columns = 6;
		private const double CardWidth = 240;
		private const double CardHeight = 168;

		private static readonly string[] _imgs =
		{
			"ms-appx:///Assets/LargeWisteria.jpg",
			"ms-appx:///Assets/ingredient1.png", "ms-appx:///Assets/ingredient2.png", "ms-appx:///Assets/ingredient3.png",
			"ms-appx:///Assets/ingredient4.png", "ms-appx:///Assets/ingredient5.png", "ms-appx:///Assets/ingredient6.png",
		};

		private readonly List<Border> _veils = new();
		private readonly List<Border> _cards = new();
		private ScrollViewer _sv = null!;
		private double _offset;
		private int _dir = 1;

		protected override string ScenarioName => "MediaWall";

		protected override int DefaultCount => 120;

		protected override UIElement BuildStage(int count)
		{
			var rng = new Rng(29);
			_veils.Clear();
			_cards.Clear();

			// One shared ThemeShadow over a full-size receiver: every card casts onto it, so the shadow blur is
			// paid per caster the way an elevated card list pays it.
			var shadow = new ThemeShadow();
			var root = new Grid();
			var receiver = new Border { Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x12, 0x14, 0x1C)) };
			root.Children.Add(receiver);
			shadow.Receivers.Add(receiver);

			var wall = new Grid { Padding = new Thickness(16), RowSpacing = 18, ColumnSpacing = 18 };
			for (var c = 0; c < Columns; c++)
			{
				wall.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(CardWidth) });
			}

			var rows = (count + Columns - 1) / Columns;
			for (var r = 0; r < rows; r++)
			{
				wall.RowDefinitions.Add(new RowDefinition { Height = new GridLength(CardHeight) });
			}

			for (var i = 0; i < count; i++)
			{
				// The card: rounded + elevated. CornerRadius over overflowing content forces a real clip, and
				// the Translation Z is what makes the shadow non-trivial.
				var card = new Border
				{
					CornerRadius = new CornerRadius(12),
					Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x1C, 0x20, 0x2C)),
					Shadow = shadow,
					Translation = new Vector3(0, 0, (float)rng.Range(20, 44)),
				};
				_cards.Add(card);

				var content = new Grid();

				content.Children.Add(new Image
				{
					Source = new BitmapImage(new Uri(_imgs[i % _imgs.Length])),
					Stretch = Stretch.UniformToFill,
				});

				// Caption scrim: a vertical gradient over the lower third, evaluated per pixel every frame.
				var scrimBrush = new LinearGradientBrush { StartPoint = new(0, 0), EndPoint = new(0, 1) };
				scrimBrush.GradientStops.Add(new GradientStop { Offset = 0, Color = Color.FromArgb(0x00, 0, 0, 0) });
				scrimBrush.GradientStops.Add(new GradientStop { Offset = 0.55, Color = Color.FromArgb(0x80, 0, 0, 0) });
				scrimBrush.GradientStops.Add(new GradientStop { Offset = 1, Color = Color.FromArgb(0xE0, 0, 0, 0) });
				content.Children.Add(new Border
				{
					VerticalAlignment = VerticalAlignment.Bottom,
					Height = 62,
					Background = scrimBrush,
					Padding = new Thickness(10, 0, 10, 8),
					Child = new StackPanel
					{
						VerticalAlignment = VerticalAlignment.Bottom,
						Children =
						{
							new TextBlock
							{
								Text = $"DWG-{2400 + i} Rev {(char)('A' + (i % 4))}",
								FontSize = 12,
								Foreground = new SolidColorBrush(Windows.UI.Colors.White),
							},
							new TextBlock
							{
								Text = "Issued for construction · 8/27/2026",
								FontSize = 10,
								Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0xC0, 0xC8, 0xE0)),
							},
						},
					},
				});

				// The selection/hover veil: a group with content and Opacity < 1, which must be isolated into its
				// own offscreen layer before compositing. This is the per-card SaveLayer.
				var veil = new Border
				{
					Opacity = 0.35,
					Background = new SolidColorBrush(Color.FromArgb(0x60, 0x2A, 0x6F, 0xF0)),
					BorderThickness = new Thickness(3),
					BorderBrush = new SolidColorBrush(Color.FromArgb(0xC0, 0x8A, 0xC0, 0xFF)),
					CornerRadius = new CornerRadius(12),
					Child = new Border
					{
						Width = 34,
						Height = 34,
						CornerRadius = new CornerRadius(17),
						Background = new SolidColorBrush(Color.FromArgb(0xB0, 0xFF, 0xFF, 0xFF)),
						HorizontalAlignment = HorizontalAlignment.Right,
						VerticalAlignment = VerticalAlignment.Top,
						Margin = new Thickness(8),
					},
				};
				_veils.Add(veil);
				content.Children.Add(veil);

				card.Child = content;
				Grid.SetColumn(card, i % Columns);
				Grid.SetRow(card, i / Columns);
				wall.Children.Add(card);
			}

			_sv = new ScrollViewer
			{
				Content = wall,
				VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
				HorizontalScrollMode = ScrollMode.Disabled,
			};
			root.Children.Add(_sv);

			_offset = 0;
			_dir = 1;
			return root;
		}

		protected override void Tick(long frame)
		{
			AdvanceScroll(_sv, ref _offset, ref _dir, 5.0);

			// Breathe every card's veil opacity and elevation: no measure/arrange, but every card's isolation
			// layer and shadow must be re-rendered, so the whole wall is redrawn every frame.
			var t = frame * 0.06;
			for (var i = 0; i < _veils.Count; i++)
			{
				_veils[i].Opacity = 0.18 + 0.30 * (0.5 + 0.5 * Math.Sin(t + i * 0.17));
				_cards[i].Translation = new Vector3(0, 0, (float)(24 + 18 * (0.5 + 0.5 * Math.Sin(t * 0.7 + i * 0.11))));
			}
		}
	}
}
