#nullable enable

using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml.Performance.RenderStress
{
	/// <summary>
	/// Log/console archetype: a dense scrolling list of small monospaced text rows.
	/// <para>
	/// Covers the GPU axis the other samples miss — GLYPH COUNT rather than fill area. Every other RenderStress
	/// scene draws a few enormous shapes; this one draws thousands of tiny ones. That is the shape of real
	/// application text (logs, tables, code views), and it is where a renderer that rasterizes glyphs as filled
	/// paths diverges from one that stamps them from an atlas.
	/// </para>
	/// <c>count</c> is the number of rows; scrolling keeps them re-rasterizing every frame without re-recording.
	/// </summary>
	[Sample("Performance", Name = "RealWorld_LogView", Description = "Real-UI perf: a dense scrolling log of small text rows — thousands of glyphs per frame. Stresses per-glyph rasterization rather than fill area. Compare fps/frame-time across Skia and the WebGPU/ProGPU builds.")]
	public sealed class RealWorld_LogView : PerfBenchBase
	{
		private static readonly string[] Levels = { "TRACE", "DEBUG", "INFO ", "WARN ", "ERROR" };

		private static readonly Color[] LevelColors =
		{
			Color.FromArgb(0xFF, 0x7A, 0x8A, 0x99),
			Color.FromArgb(0xFF, 0x9C, 0xC4, 0xE4),
			Color.FromArgb(0xFF, 0xB6, 0xE3, 0xB6),
			Color.FromArgb(0xFF, 0xE8, 0xD0, 0x8A),
			Color.FromArgb(0xFF, 0xF0, 0x9A, 0x9A),
		};

		private static readonly string[] Subsystems =
		{
			"Composition", "Layout", "Dispatcher", "Storage", "Network", "Renderer", "Input", "Binding",
		};

		private static readonly string[] Messages =
		{
			"frame committed in 12.4ms, 318 ops emitted to the queue",
			"resolved 47 bindings for ItemsRepeater realized range 0..23",
			"texture atlas grew to 2048x2048 after 1,204 glyph insertions",
			"request completed: 200 OK in 84ms (cache miss, 12.1 KB)",
			"measure pass skipped, no dirty children under this subtree",
			"pointer capture released, routing to parent scroll presenter",
			"deserialized 1,982 records, 3 skipped as malformed input",
			"swapchain resized to 1920x945, recreating depth attachment",
		};

		private ScrollViewer _sv = null!;
		private double _offset;
		private int _dir = 1;

		protected override string ScenarioName => "LogView (glyph count)";

		/// <summary>Number of log rows. Only the visible window rasterizes, so this sets the scroll range.</summary>
		protected override int DefaultCount => 600;

		protected override UIElement BuildStage(int count)
		{
			var rng = new Rng(9173);
			var root = new Grid { Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x10, 0x14, 0x1A)) };

			var stage = new StackPanel();
			for (var i = 0; i < count; i++)
			{
				stage.Children.Add(BuildRow(i, rng));
			}

			_sv = new ScrollViewer
			{
				Content = stage,
				VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
				HorizontalScrollMode = ScrollMode.Disabled,
			};
			root.Children.Add(_sv);

			_offset = 0;
			_dir = 1;
			return root;
		}

		private static UIElement BuildRow(int index, Rng rng)
		{
			var level = rng.Int(0, Levels.Length);
			var row = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 8,
				Padding = new Thickness(6, 0, 6, 0),
			};

			// A full log line — timestamp, level, thread, subsystem, message, duration — at a small size, so a
			// screenful is thousands of glyphs. Density is the whole point: it is what makes the scene
			// glyph-bound rather than fill-bound.
			row.Children.Add(Cell($"{index / 3600 % 24:D2}:{index / 60 % 60:D2}:{index % 60:D2}.{index * 7 % 1000:D3}", Color.FromArgb(0xFF, 0x6B, 0x7A, 0x8A)));
			row.Children.Add(Cell(Levels[level], LevelColors[level]));
			row.Children.Add(Cell($"[{rng.Int(1, 64):D2}]", Color.FromArgb(0xFF, 0x84, 0x94, 0xA4)));
			row.Children.Add(Cell(Subsystems[rng.Int(0, Subsystems.Length)], Color.FromArgb(0xFF, 0xC0, 0xA8, 0xE0)));
			row.Children.Add(Cell(Messages[rng.Int(0, Messages.Length)], Color.FromArgb(0xFF, 0xD4, 0xDC, 0xE4)));
			row.Children.Add(Cell($"+{rng.Int(1, 9999):D4}us", Color.FromArgb(0xFF, 0x7A, 0x8A, 0x99)));
			return row;
		}

		private static TextBlock Cell(string text, Color color) => new()
		{
			Text = text,
			FontSize = 11,
			FontFamily = new FontFamily("Consolas"),
			Foreground = new SolidColorBrush(color),
		};

		protected override void Tick(long frame)
		{
			// Scroll only: rows re-RASTERIZE every frame without being re-RECORDED.
			AdvanceScroll(_sv, ref _offset, ref _dir, 6.0);
		}
	}
}
