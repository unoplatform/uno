#nullable enable

using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml.Performance.RenderStress
{
	/// <summary>
	/// App-shell archetype: busy content scrolling behind a set of ACRYLIC panels — a navigation pane, a command
	/// bar, a properties pane and a modal, the shape a real WinUI app takes.
	/// <para>
	/// This is a GPU/fill-rate benchmark, not a layout one. Each acrylic panel is a backdrop blur: it samples the
	/// framebuffer composited so far and blurs it, which is the most expensive per-pixel operation in ordinary UI
	/// and the only one here that scales with panel AREA rather than element count. The content behind scrolls, so
	/// the backdrop changes every frame and the blur can never be cached — the realistic case, and the one that
	/// keeps this in the draw phase instead of record.
	/// </para>
	/// <c>count</c> is the number of acrylic panels. They deliberately overlap, so each one blurs a region that
	/// already contains earlier panels' output and the cost compounds the way a real flyout-over-pane does.
	/// </summary>
	[Sample("Performance", Name = "RealWorld_AcrylicShell", Description = "Real-UI perf: content scrolling behind N overlapping ACRYLIC panels (nav pane, command bar, properties pane, modal). Backdrop blur is the most expensive per-pixel op in real UI and scales with area, so this is draw-bound. Compare fps/frame-time across Skia and the WebGPU/ProGPU builds.")]
	public sealed class RealWorld_AcrylicShell : PerfBenchBase
	{
		private ScrollViewer _sv = null!;
		private double _offset;
		private int _dir = 1;

		protected override string ScenarioName => "AcrylicShell";

		/// <summary>Number of overlapping acrylic panels. Each is a backdrop blur over its own area.</summary>
		protected override int DefaultCount => 6;

		protected override UIElement BuildStage(int count)
		{
			var root = new Grid();

			// --- Content behind: kept cheap on purpose so the cost measured is the blur, not the list. It must
			// still SCROLL, otherwise the backdrop is static and the blur result could be reused. ---
			var list = new StackPanel { Padding = new Thickness(12), Spacing = 5 };
			for (var i = 0; i < 140; i++)
			{
				list.Children.Add(new Border
				{
					Height = 30,
					CornerRadius = new CornerRadius(5),
					Background = new SolidColorBrush(Color.FromArgb(0xFF, (byte)(0x1A + (i % 5) * 9), 0x22, 0x3A)),
					Padding = new Thickness(10, 5, 10, 5),
					Child = new TextBlock
					{
						Text = $"Transmittal {4200 + i} · Issued for construction · Rev {(char)('A' + (i % 6))}",
						FontSize = 12,
						Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0xCE, 0xD8, 0xF0)),
					},
				});
			}

			_sv = new ScrollViewer
			{
				Content = list,
				VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
				HorizontalScrollMode = ScrollMode.Disabled,
			};
			root.Children.Add(_sv);

			// --- The acrylic panels, cascaded so they overlap each other as well as the content. ---
			for (var i = 0; i < count; i++)
			{
				root.Children.Add(BuildPanel(i));
			}

			_offset = 0;
			_dir = 1;
			return root;
		}

		private static UIElement BuildPanel(int index)
		{
			var acrylic = new AcrylicBrush
			{
				TintColor = Color.FromArgb(0xFF, (byte)(0x10 + (index % 4) * 12), 0x18, 0x2E),
				TintOpacity = 0.55,
				FallbackColor = Color.FromArgb(0xD0, 0x14, 0x1C, 0x30),
			};

			// Large, overlapping regions: the blur cost is proportional to area, so these are sized in
			// viewport-fractions rather than fixed pixels.
			var (w, h, ha, va, margin) = (index % 4) switch
			{
				0 => (320d, double.NaN, HorizontalAlignment.Left, VerticalAlignment.Stretch, new Thickness(0)),
				1 => (double.NaN, 96d, HorizontalAlignment.Stretch, VerticalAlignment.Top, new Thickness(0)),
				2 => (380d, double.NaN, HorizontalAlignment.Right, VerticalAlignment.Stretch, new Thickness(0)),
				_ => (760d, 480d, HorizontalAlignment.Center, VerticalAlignment.Center, new Thickness(0)),
			};

			return new Border
			{
				Width = w,
				Height = h,
				HorizontalAlignment = ha,
				VerticalAlignment = va,
				Margin = new Thickness(margin.Left + index * 18, margin.Top + index * 14, margin.Right, margin.Bottom),
				CornerRadius = new CornerRadius(index % 4 == 3 ? 14 : 0),
				Background = acrylic,
				BorderThickness = new Thickness(1),
				BorderBrush = new SolidColorBrush(Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF)),
				Padding = new Thickness(16),
				Child = new StackPanel
				{
					Spacing = 6,
					Children =
					{
						new TextBlock
						{
							Text = $"Panel {index + 1}",
							FontSize = 15,
							Foreground = new SolidColorBrush(Windows.UI.Colors.White),
						},
						new TextBlock
						{
							Text = "Acrylic backdrop — blurs everything composited beneath it.",
							FontSize = 12,
							TextWrapping = TextWrapping.Wrap,
							Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0xC4, 0xCE, 0xE6)),
						},
					},
				},
			};
		}

		protected override void Tick(long frame)
		{
			// Scroll only. The panels themselves never change, so nothing is re-recorded — but the content behind
			// them moves, so every backdrop blur must be recomputed. That keeps the cost in draw.
			AdvanceScroll(_sv, ref _offset, ref _dir, 7.0);
		}
	}
}
