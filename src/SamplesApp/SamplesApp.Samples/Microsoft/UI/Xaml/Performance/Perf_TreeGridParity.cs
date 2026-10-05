#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Samples.Controls;
using Windows.Foundation;

namespace UITests.Windows_UI_Xaml.Performance
{
	/// <summary>
	/// Replicates the TreeDataGrid parity workload (TreeDataGrid repository, docs/uno-performance-findings.md): a
	/// virtualizing, recycling grid of 10,000 rows x 64 text columns (128 px, 32 px rows, 800x480 viewport, no
	/// headers or scroll bars) whose cells mirror the TreeDataGridTextCell template. Each operation runs 5 warm-up and
	/// 25 measured iterations; the measured interval is the operation plus UpdateLayout on the UI thread, as in the
	/// report. UNO_PERF_PARITY_AUTORUN=1 runs everything on load and prints PARITY lines to the console;
	/// PARITY_OPERATIONS, PARITY_ITERATIONS, PARITY_COLUMNS, PARITY_COLLAPSE_ON_SORT and PARITY_HIDDEN_TEXT override
	/// the defaults.
	/// </summary>
	[Sample("Performance", Name = "Perf_TreeGridParity", IsManualTest = true, IgnoreInSnapshotTests = true,
		Description = "TreeDataGrid parity workload: a recycling 10k x 64 text grid timed over row replacement, column resize, sort and three scroll patterns. Run an operation and compare the median UI-thread time between builds.")]
	public sealed class Perf_TreeGridParity : Page
	{
		private const int RowCount = 10_000;
		private const int ViewportWidth = 800;
		private const int ViewportHeight = 480;
		private const int RowHeight = 32;
		private const int ColumnWidth = 128;
		private const int Warmup = 5;

		private static readonly string[] _operations = ["replace-visible-row", "resize-visible-column", "sort", "scroll-y", "scroll-x", "distant-diagonal-scroll", "visual-tree-walk"];

		private readonly GridModel _model;
		private readonly Border _host;
		private readonly RowsPresenter _rows;
		private readonly TextBlock _logText = new() { FontFamily = new FontFamily("Consolas"), FontSize = 12, TextWrapping = TextWrapping.Wrap };
		private readonly CheckBox _collapseOnSort = new() { Content = "Collapse rows on sort (finding 8)" };
		private readonly CheckBox _hiddenText = new() { Content = "Keep a hidden text copy per cell (finding 4)" };
		private readonly List<Button> _buttons = new();
		private bool _running;

		public Perf_TreeGridParity()
		{
			Background = (Brush)Application.Current.Resources["ApplicationPageBackgroundThemeBrush"];

			_collapseOnSort.IsChecked = IsSet("PARITY_COLLAPSE_ON_SORT");
			_hiddenText.IsChecked = IsSet("PARITY_HIDDEN_TEXT");

			var scroll = new ScrollViewer
			{
				HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
				VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
				HorizontalScrollMode = ScrollMode.Enabled,
				VerticalScrollMode = ScrollMode.Enabled,
			};
			_model = new GridModel(ReadCount("PARITY_COLUMNS", 64, 8, 1000), scroll);
			_rows = new RowsPresenter(_model);
			scroll.Content = _rows;
			scroll.ViewChanged += (_, _) => _rows.InvalidateRealization();

			_host = new Border
			{
				Width = ViewportWidth,
				Height = ViewportHeight,
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				Child = scroll,
			};

			var panel = new StackPanel { Spacing = 6, Margin = new Thickness(12, 0, 0, 0), MinWidth = 320 };
			panel.Children.Add(_collapseOnSort);
			panel.Children.Add(_hiddenText);
			panel.Children.Add(CreateButton("Run all", () => RunAsync(_operations)));
			foreach (var operation in _operations)
			{
				panel.Children.Add(CreateButton(operation, () => RunAsync([operation])));
			}
			panel.Children.Add(new ScrollViewer { Content = _logText, MaxHeight = 420 });

			var root = new Grid { Padding = new Thickness(12) };
			root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
			root.Children.Add(_host);
			Grid.SetColumn(panel, 1);
			root.Children.Add(panel);
			Content = root;

			Loaded += OnLoaded;
		}

		private async void OnLoaded(object sender, RoutedEventArgs e)
		{
			if (IsSet("UNO_PERF_PARITY_AUTORUN"))
			{
				// Startup, first templates and JIT are excluded, as in the report.
				await Task.Delay(500);
				var selected = Environment.GetEnvironmentVariable("PARITY_OPERATIONS")?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
				await RunAsync(selected is { Length: > 0 } ? _operations.Where(selected.Contains).ToArray() : _operations);
				Console.WriteLine("PARITY-DONE");
			}
		}

		private Button CreateButton(string label, Func<Task> action)
		{
			var button = new Button { Content = label, HorizontalAlignment = HorizontalAlignment.Stretch };
			button.Click += async (_, _) => await action();
			_buttons.Add(button);
			return button;
		}

		private async Task RunAsync(IReadOnlyList<string> operations)
		{
			if (_running)
			{
				return;
			}

			_running = true;
			foreach (var b in _buttons)
			{
				b.IsEnabled = false;
			}

			try
			{
				var iterations = ReadCount("PARITY_ITERATIONS", 25, 5, 500);
				Log($"columns={_model.ColumnCount} iterations={iterations} collapseOnSort={_collapseOnSort.IsChecked == true} hiddenText={_hiddenText.IsChecked == true}");
				foreach (var operation in operations)
				{
					await RunOperationAsync(operation, iterations);
				}
			}
			finally
			{
				_running = false;
				foreach (var b in _buttons)
				{
					b.IsEnabled = true;
				}
			}
		}

		private async Task RunOperationAsync(string operation, int iterations)
		{
			var hiddenText = _hiddenText.IsChecked == true;
			if (hiddenText != _model.HiddenText)
			{
				_rows.Clear(); // cells are built with or without the hidden copy
			}

			_model.Reset(hiddenText);
			_rows.ResetRealization(collapse: false);
			Scroll(0, 0);
			_host.UpdateLayout();
			await Task.Delay(200);

			var times = new List<double>(iterations);
			var measures = new List<int>(iterations);
			var arranges = new List<int>(iterations);
			long allocated = 0;
			var cold = 0.0;
			var errors = 0;
			string? firstError = null;

			for (var iteration = -Warmup; iteration < iterations; ++iteration)
			{
				// Lets frames render between operations; outside the measured interval.
				await Task.Delay(20);

				var index = iteration + Warmup;
				var x = 0d;
				var y = 0d;
				var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
				var measuresBefore = UIElement.LayoutMeasureCoreCount;
				var arrangesBefore = UIElement.LayoutArrangeCoreCount;
				var started = Stopwatch.GetTimestamp();

				switch (operation)
				{
					case "replace-visible-row":
						_model.Labels[_model.Order[7]] = $"R00007/{index:D4}";
						_rows.RebindRow(7);
						break;
					case "resize-visible-column":
						_model.SetColumnWidth(3, ColumnWidth + (index % 2) * 16);
						_rows.InvalidateRealization();
						break;
					case "sort":
						_model.Sort(descending: index % 2 == 0);
						_rows.ResetRealization(collapse: _collapseOnSort.IsChecked == true);
						break;
					case "scroll-y":
						y = RowHeight * (index + 1) * 3;
						Scroll(x, y);
						break;
					case "scroll-x":
						x = ColumnWidth * ((index + 1) % (_model.ColumnCount - 7));
						Scroll(x, y);
						break;
					case "distant-diagonal-scroll":
						x = ColumnWidth * (index % (_model.ColumnCount - 7));
						y = RowHeight * ((index * 997 + 521) % 9000);
						Scroll(x, y);
						break;
					case "visual-tree-walk":
						CountTextBlocks(_host);
						break;
				}

				_host.UpdateLayout();
				var elapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
				var allocatedDelta = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
				var measured = UIElement.LayoutMeasureCoreCount - measuresBefore;
				var arranged = UIElement.LayoutArrangeCoreCount - arrangesBefore;

				if (Verify(x, y) is { } error)
				{
					errors++;
					firstError ??= error;
				}

				if (iteration == -Warmup)
				{
					cold = elapsed;
				}
				else if (iteration >= 0)
				{
					times.Add(elapsed);
					measures.Add(measured);
					arranges.Add(arranged);
					allocated += allocatedDelta;
				}
			}

			times.Sort();
			var median = times[times.Count / 2];
			var p90 = times[(int)(times.Count * 0.9)];
			Log($"PARITY op={operation} median={median:F3}ms p90={p90:F3}ms min={times[0]:F3}ms cold={cold:F3}ms alloc={allocated / 1024.0 / times.Count:F1}KB/op measures={Median(measures)} arranges={Median(arranges)} rows={_rows.RealizedRowCount} cells={_rows.RealizedCellCount}"
				+ (errors > 0 ? $" ERRORS={errors} first={firstError}" : ""));
		}

		private static int Median(List<int> values)
		{
			values.Sort();
			return values[values.Count / 2];
		}

		private void Scroll(double x, double y)
		{
			_model.Scroll.ChangeView(x, y, null, disableAnimation: true);
			// The report's grid reacts to ViewChanged; invalidating here keeps the realization inside the measured interval.
			_rows.InvalidateRealization();
		}

		private string? Verify(double x, double y)
		{
			var scroll = _model.Scroll;
			if (Math.Abs(scroll.HorizontalOffset - x) > 0.5 || Math.Abs(scroll.VerticalOffset - y) > 0.5)
			{
				return $"offset ({scroll.HorizontalOffset},{scroll.VerticalOffset}) expected ({x},{y})";
			}

			var first = (int)(y / RowHeight);
			if (_rows.TryGetRow(first) is not { } row || _rows.TryGetRow(first + 14) is null)
			{
				return "coverage";
			}

			return row.Cells.FirstText() == _model.LabelAt(first) ? null : "stale text";
		}

		private static int CountTextBlocks(DependencyObject root)
		{
			var count = root is TextBlock ? 1 : 0;
			var children = VisualTreeHelper.GetChildrenCount(root);
			for (var i = 0; i < children; i++)
			{
				count += CountTextBlocks(VisualTreeHelper.GetChild(root, i));
			}

			return count;
		}

		private void Log(string line)
		{
			Console.WriteLine(line);
			_logText.Text = line + "\n" + _logText.Text;
		}

		private static bool IsSet(string name) => Environment.GetEnvironmentVariable(name) is "1" or "true";

		private static int ReadCount(string name, int fallback, int minimum, int maximum)
			=> int.TryParse(Environment.GetEnvironmentVariable(name), out var value) && value >= minimum && value <= maximum ? value : fallback;

		private sealed class GridModel
		{
			public GridModel(int columnCount, ScrollViewer scroll)
			{
				ColumnCount = columnCount;
				Scroll = scroll;
				Widths = new double[columnCount];
				Xs = new double[columnCount];
				Reset(hiddenText: false);
			}

			public int ColumnCount { get; }
			public ScrollViewer Scroll { get; }
			public string[] Labels { get; } = new string[RowCount];
			public int[] Order { get; } = new int[RowCount];
			public double[] Widths { get; }
			public double[] Xs { get; }
			public double ExtentWidth { get; private set; }
			public bool HiddenText { get; private set; }

			public string LabelAt(int row) => Labels[Order[row]];

			public void Reset(bool hiddenText)
			{
				HiddenText = hiddenText;
				for (var i = 0; i < RowCount; i++)
				{
					Labels[i] = $"R{i:D5}";
					Order[i] = i;
				}

				Array.Fill(Widths, ColumnWidth);
				UpdatePositions();
			}

			public void SetColumnWidth(int column, double width)
			{
				Widths[column] = width;
				UpdatePositions();
			}

			public void Sort(bool descending)
			{
				for (var i = 0; i < RowCount; i++)
				{
					Order[i] = descending ? RowCount - 1 - i : i;
				}
			}

			private void UpdatePositions()
			{
				var x = 0.0;
				for (var c = 0; c < ColumnCount; c++)
				{
					Xs[c] = x;
					x += Widths[c];
				}

				ExtentWidth = x;
			}
		}

		/// <summary>Realizes the rows in the viewport at absolute positions and recycles the rest, like TreeDataGridRowsPresenter.</summary>
		private sealed class RowsPresenter : Panel
		{
			private readonly GridModel _model;
			private readonly Dictionary<int, GridRow> _realized = new();
			private readonly Stack<GridRow> _pool = new();
			private readonly List<int> _scratch = new();

			public RowsPresenter(GridModel model) => _model = model;

			public int RealizedRowCount => _realized.Count;

			public int RealizedCellCount
			{
				get
				{
					var count = 0;
					foreach (var row in _realized.Values)
					{
						count += row.Cells.RealizedCount;
					}

					return count;
				}
			}

			public GridRow? TryGetRow(int index) => _realized.TryGetValue(index, out var row) ? row : null;

			public void InvalidateRealization()
			{
				InvalidateMeasure();
				foreach (var row in _realized.Values)
				{
					row.Cells.InvalidateMeasure();
				}
			}

			public void RebindRow(int index)
			{
				if (_realized.TryGetValue(index, out var row))
				{
					row.Cells.Rebind(index);
				}
			}

			public void Clear()
			{
				Children.Clear();
				_realized.Clear();
				_pool.Clear();
				InvalidateMeasure();
			}

			/// <summary>A source reset: every realized row goes back to the pool and is realized again.</summary>
			public void ResetRealization(bool collapse)
			{
				foreach (var row in _realized.Values)
				{
					if (collapse)
					{
						row.Visibility = Visibility.Collapsed;
					}

					_pool.Push(row);
				}

				_realized.Clear();
				InvalidateMeasure();
			}

			protected override Size MeasureOverride(Size availableSize)
			{
				var scroll = _model.Scroll;
				var viewportHeight = scroll.ViewportHeight > 0 ? scroll.ViewportHeight : ViewportHeight;
				var first = Math.Max(0, (int)(scroll.VerticalOffset / RowHeight));
				var last = Math.Min(RowCount - 1, (int)((scroll.VerticalOffset + viewportHeight) / RowHeight));

				_scratch.Clear();
				foreach (var index in _realized.Keys)
				{
					if (index < first || index > last)
					{
						_scratch.Add(index);
					}
				}

				foreach (var index in _scratch)
				{
					_pool.Push(_realized[index]);
					_realized.Remove(index);
				}

				for (var index = first; index <= last; index++)
				{
					if (!_realized.ContainsKey(index))
					{
						var row = _pool.Count > 0 ? _pool.Pop() : CreateRow();
						row.Visibility = Visibility.Visible;
						row.Cells.Rebind(index);
						_realized[index] = row;
					}
				}

				foreach (var row in _pool)
				{
					row.Visibility = Visibility.Collapsed;
				}

				var rowSize = new Size(_model.ExtentWidth, RowHeight);
				foreach (var row in _realized.Values)
				{
					row.Measure(rowSize);
				}

				return new Size(_model.ExtentWidth, RowCount * (double)RowHeight);
			}

			protected override Size ArrangeOverride(Size finalSize)
			{
				foreach (var (index, row) in _realized)
				{
					row.Arrange(new Rect(0, index * (double)RowHeight, _model.ExtentWidth, RowHeight));
				}

				return finalSize;
			}

			private GridRow CreateRow()
			{
				var row = new GridRow(_model);
				Children.Add(row);
				return row;
			}
		}

		/// <summary>TreeDataGridRow's template: a border over a grid holding the selection background and the cells presenter.</summary>
		private sealed class GridRow : Border
		{
			public GridRow(GridModel model)
			{
				Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
				Cells = new CellsPresenter(model);
				var grid = new Grid();
				grid.Children.Add(new Border { Opacity = 0, Background = new SolidColorBrush(Microsoft.UI.Colors.SteelBlue) });
				grid.Children.Add(Cells);
				Child = grid;
			}

			public CellsPresenter Cells { get; }
		}

		/// <summary>Realizes the columns in the horizontal viewport and recycles the rest, like TreeDataGridCellsPresenter.</summary>
		private sealed class CellsPresenter : Panel
		{
			private readonly GridModel _model;
			private readonly Dictionary<int, GridCell> _realized = new();
			private readonly Stack<GridCell> _pool = new();
			private readonly List<int> _scratch = new();
			private int _rowIndex = -1;

			public CellsPresenter(GridModel model) => _model = model;

			public int RealizedCount => _realized.Count;

			public string? FirstText()
			{
				GridCell? first = null;
				var firstColumn = int.MaxValue;
				foreach (var (column, cell) in _realized)
				{
					if (column < firstColumn)
					{
						firstColumn = column;
						first = cell;
					}
				}

				return first?.Text;
			}

			public void Rebind(int rowIndex)
			{
				_rowIndex = rowIndex;
				var label = _model.LabelAt(rowIndex);
				foreach (var cell in _realized.Values)
				{
					cell.SetText(label);
				}
			}

			protected override Size MeasureOverride(Size availableSize)
			{
				var scroll = _model.Scroll;
				var viewportWidth = scroll.ViewportWidth > 0 ? scroll.ViewportWidth : ViewportWidth;
				var left = scroll.HorizontalOffset;
				var right = left + viewportWidth;

				_scratch.Clear();
				foreach (var column in _realized.Keys)
				{
					if (!IsVisible(column, left, right))
					{
						_scratch.Add(column);
					}
				}

				foreach (var column in _scratch)
				{
					_pool.Push(_realized[column]);
					_realized.Remove(column);
				}

				var label = _rowIndex >= 0 ? _model.LabelAt(_rowIndex) : "";
				for (var column = 0; column < _model.ColumnCount; column++)
				{
					if (IsVisible(column, left, right) && !_realized.ContainsKey(column))
					{
						var cell = _pool.Count > 0 ? _pool.Pop() : CreateCell();
						cell.Visibility = Visibility.Visible;
						cell.SetText(label);
						_realized[column] = cell;
					}
				}

				foreach (var cell in _pool)
				{
					cell.Visibility = Visibility.Collapsed;
				}

				foreach (var (column, cell) in _realized)
				{
					cell.Measure(new Size(_model.Widths[column], RowHeight));
				}

				return new Size(_model.ExtentWidth, RowHeight);
			}

			protected override Size ArrangeOverride(Size finalSize)
			{
				foreach (var (column, cell) in _realized)
				{
					cell.Arrange(new Rect(_model.Xs[column], 0, _model.Widths[column], RowHeight));
				}

				return finalSize;
			}

			private bool IsVisible(int column, double left, double right)
				=> _model.Xs[column] + _model.Widths[column] > left && _model.Xs[column] < right;

			private GridCell CreateCell()
			{
				var cell = new GridCell(_model.HiddenText);
				Children.Add(cell);
				return cell;
			}
		}

		/// <summary>
		/// TreeDataGridTextCell's template: a border over a content panel holding the selection background, the
		/// vertically centred, trimmed text, the collapsed editor hosts and the current/validation borders.
		/// </summary>
		private sealed class GridCell : Border
		{
			private readonly TextBlock _text;
			private readonly TextBlock? _hidden;

			public GridCell(bool hiddenText)
			{
				_text = new TextBlock
				{
					Margin = new Thickness(4, 0, 4, 0),
					VerticalAlignment = VerticalAlignment.Center,
					IsHitTestVisible = false,
					TextTrimming = TextTrimming.CharacterEllipsis,
					FontSize = 14,
				};

				var content = new Grid();
				content.Children.Add(new Border { Opacity = 0, Background = new SolidColorBrush(Microsoft.UI.Colors.SteelBlue) });
				content.Children.Add(_text);
				content.Children.Add(new Grid { Visibility = Visibility.Collapsed });
				content.Children.Add(new ContentPresenter { Visibility = Visibility.Collapsed });
				content.Children.Add(new Border { Opacity = 0, BorderThickness = new Thickness(1), BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.DodgerBlue), IsHitTestVisible = false });
				content.Children.Add(new Border { Opacity = 0, BorderThickness = new Thickness(1), BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Red), IsHitTestVisible = false });
				if (hiddenText)
				{
					_hidden = new TextBlock { Visibility = Visibility.Collapsed, FontSize = 14 };
					content.Children.Add(_hidden);
				}

				Child = content;
			}

			public string Text => _text.Text;

			public void SetText(string text)
			{
				if (_text.Text != text)
				{
					_text.Text = text;
				}

				if (_hidden is not null)
				{
					_hidden.Text = text;
				}
			}
		}
	}
}
