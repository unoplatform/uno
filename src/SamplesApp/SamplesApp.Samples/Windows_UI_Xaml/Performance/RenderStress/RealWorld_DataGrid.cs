#nullable enable

using System.Collections.Generic;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml.Performance.RenderStress
{
	/// <summary>
	/// Table archetype (js-framework-benchmark style): thousands of text cells in a scrolling grid, with a slice of
	/// values churning every frame — stresses text render + per-frame invalidation at scale.
	/// </summary>
	[Sample("Performance", Name = "RealWorld_DataGrid", Description = "Real-UI perf: a large scrolling data table with per-frame value churn. Stresses text at scale. Compare fps/frame-time across Skia and the WebGPU/ProGPU builds.")]
	public sealed class RealWorld_DataGrid : PerfBenchBase
	{
		private static readonly string[] _cols = { "ID", "Name", "Status", "Value", "Δ", "Updated" };
		private static readonly string[] _names = { "Alpha", "Bravo", "Charlie", "Delta", "Echo", "Foxtrot", "Golf", "Hotel" };
		private static readonly string[] _status = { "OK", "WARN", "BUSY", "IDLE" };

		private ScrollViewer _sv = null!;
		private readonly List<TextBlock> _valueCells = new();
		private readonly List<int> _values = new();
		private double _offset;
		private int _dir = 1;

		protected override string ScenarioName => "DataGrid";
		protected override int DefaultCount => 600;

		protected override UIElement BuildStage(int count)
		{
			var rng = new Rng(33);
			_valueCells.Clear();
			_values.Clear();

			var grid = new Grid { Padding = new Thickness(8) };
			for (var c = 0; c < _cols.Length; c++)
			{
				grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(c == 1 ? 2 : 1, GridUnitType.Star) });
			}
			grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });   // header
			for (var r = 0; r < count; r++)
			{
				grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(26) });
			}

			for (var c = 0; c < _cols.Length; c++)
			{
				var h = new TextBlock { Text = _cols[c], FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = new SolidColorBrush(Windows.UI.Colors.White), Margin = new Thickness(6, 4, 6, 4) };
				Grid.SetColumn(h, c);
				Grid.SetRow(h, 0);
				grid.Children.Add(h);
			}

			for (var r = 0; r < count; r++)
			{
				var v = rng.Int(0, 10000);
				_values.Add(v);
				// zebra row background
				if ((r & 1) == 0)
				{
					var bg = new Border { Background = new SolidColorBrush(Color.FromArgb(0x18, 0xFF, 0xFF, 0xFF)) };
					Grid.SetRow(bg, r + 1);
					Grid.SetColumnSpan(bg, _cols.Length);
					grid.Children.Add(bg);
				}
				AddCell(grid, r + 1, 0, r.ToString(), Color.FromArgb(0xFF, 0xA0, 0xA8, 0xC0));
				AddCell(grid, r + 1, 1, _names[r % _names.Length] + "-" + r, Windows.UI.Colors.White);
				AddCell(grid, r + 1, 2, _status[r % _status.Length], Color.FromArgb(0xFF, 0x60, 0xD0, 0x80));
				var valueCell = AddCell(grid, r + 1, 3, v.ToString(), Windows.UI.Colors.White);
				_valueCells.Add(valueCell);
				AddCell(grid, r + 1, 4, "+0", Color.FromArgb(0xFF, 0x80, 0xC0, 0xF0));
				AddCell(grid, r + 1, 5, "just now", Color.FromArgb(0xFF, 0x90, 0x98, 0xB0));
			}

			_sv = new ScrollViewer { Content = grid, VerticalScrollBarVisibility = ScrollBarVisibility.Hidden, HorizontalScrollMode = ScrollMode.Disabled };
			_offset = 0;
			_dir = 1;
			return _sv;
		}

		private static TextBlock AddCell(Grid grid, int row, int col, string text, Color color)
		{
			var tb = new TextBlock { Text = text, Foreground = new SolidColorBrush(color), FontSize = 12, Margin = new Thickness(6, 3, 6, 3) };
			Grid.SetColumn(tb, col);
			Grid.SetRow(tb, row);
			grid.Children.Add(tb);
			return tb;
		}

		protected override void Tick(long frame)
		{
			AdvanceScroll(_sv, ref _offset, ref _dir, 6.0);
			// Churn ~every 8th row's value each frame (js-framework-benchmark's "update every Nth row").
			var start = (int)(frame % 8);
			for (var i = start; i < _valueCells.Count; i += 8)
			{
				_values[i] = (_values[i] + 7) % 10000;
				_valueCells[i].Text = _values[i].ToString();
			}
		}
	}
}
