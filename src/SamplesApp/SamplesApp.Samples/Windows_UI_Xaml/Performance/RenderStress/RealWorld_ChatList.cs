#nullable enable

using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml.Performance.RenderStress
{
	/// <summary>Messaging archetype: a scrolling chat with avatars + rounded bubbles + wrapped text + opacity — text and layer stress.</summary>
	[Sample("Performance", Name = "RealWorld_ChatList", Description = "Real-UI perf: a scrolling chat (avatars, rounded bubbles, wrapped text, opacity). Stresses text + layers. Compare fps/frame-time across Skia and the WebGPU/ProGPU builds.")]
	public sealed class RealWorld_ChatList : PerfBenchBase
	{
		private static readonly string[] _msgs =
		{
			"Hey! Did you see the new renderer benchmark numbers?",
			"Yeah — ProGPU on the M3 was almost 2x Skia.",
			"On software it's the other way around though, careful with lavapipe.",
			"Right, GPU-resident retained scene needs a real GPU to pay off.",
			"Let's get the WASM lane wired next.",
			"👍 sounds good, I'll draft the browser provider.",
		};

		private ScrollViewer _sv = null!;
		private double _offset;
		private int _dir = 1;

		protected override string ScenarioName => "ChatList";
		protected override int DefaultCount => 250;

		protected override UIElement BuildStage(int count)
		{
			var rng = new Rng(22);
			var list = new StackPanel { Spacing = 10, Padding = new Thickness(12) };
			for (var i = 0; i < count; i++)
			{
				var mine = (i % 3) == 0;
				var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = mine ? HorizontalAlignment.Right : HorizontalAlignment.Left };

				var avatar = new Ellipse { Width = 32, Height = 32, Fill = new SolidColorBrush(rng.Color()) };
				var bubble = new Border
				{
					CornerRadius = new CornerRadius(14),
					Background = new SolidColorBrush(mine ? Color.FromArgb(0xFF, 0x2A, 0x6F, 0xF0) : Color.FromArgb(0xFF, 0x2A, 0x2A, 0x32)),
					Padding = new Thickness(12, 8, 12, 8),
					MaxWidth = 320,
					Opacity = mine ? 1.0 : 0.92,
				};
				var stack = new StackPanel { Spacing = 2 };
				stack.Children.Add(new TextBlock
				{
					Text = _msgs[i % _msgs.Length],
					Foreground = new SolidColorBrush(Windows.UI.Colors.White),
					TextWrapping = TextWrapping.Wrap,
					FontSize = 14,
				});
				stack.Children.Add(new TextBlock
				{
					Text = $"12:{i % 60:D2}",
					Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0xC0, 0xC8, 0xE0)),
					FontSize = 10,
					HorizontalAlignment = HorizontalAlignment.Right,
				});
				bubble.Child = stack;

				if (mine)
				{
					row.Children.Add(bubble);
					row.Children.Add(avatar);
				}
				else
				{
					row.Children.Add(avatar);
					row.Children.Add(bubble);
				}
				list.Children.Add(row);
			}

			_sv = new ScrollViewer { Content = list, VerticalScrollBarVisibility = ScrollBarVisibility.Hidden, HorizontalScrollMode = ScrollMode.Disabled };
			_offset = 0;
			_dir = 1;
			return _sv;
		}

		protected override void Tick(long frame) => AdvanceScroll(_sv, ref _offset, ref _dir, 7.0);
	}
}
