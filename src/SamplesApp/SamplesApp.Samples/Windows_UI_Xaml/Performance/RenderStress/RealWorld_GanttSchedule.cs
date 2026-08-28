#nullable enable

using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml.Performance.RenderStress
{
	/// <summary>
	/// Schedule archetype: a non-virtualized Gantt — a task row per item (bar + baseline), a dependency arrow
	/// (multi-segment polyline) per link, and TWO date-scale strips whose interval count explodes as the
	/// timescale zooms in. Modelled on the real schedule view, where the cost is thousands of small stroked
	/// geometries plus scale labels rather than text rows.
	/// </summary>
	[Sample("Performance", Name = "RealWorld_GanttSchedule", Description = "Real-UI perf: a non-virtualized Gantt schedule (task bars, dependency arrows, dual date scales) scrolling per frame. Stresses stroked geometry at scale. Compare fps/frame-time across Skia and the WebGPU/ProGPU builds.")]
	public sealed class RealWorld_GanttSchedule : PerfBenchBase
	{
		private const double RowHeight = 26;
		private const double DayWidth = 18;
		private const int Days = 120;

		private readonly List<TranslateTransform> _barShifts = new();
		private readonly List<TextBlock> _dateLabels = new();
		private ScrollViewer _sv = null!;
		private double _offset;
		private int _dir = 1;

		protected override string ScenarioName => "GanttSchedule";

		protected override int DefaultCount => 520;

		protected override UIElement BuildStage(int count)
		{
			var rng = new Rng(97);
			_barShifts.Clear();
			_dateLabels.Clear();

			var root = new Grid();
			root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });   // month scale
			root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });   // day scale
			root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

			// Two independent scale strips: every interval is a bordered cell with its own label, which is what
			// makes zooming to day granularity so expensive on a multi-month schedule.
			var monthScale = BuildScale(Days / 7, DayWidth * 7, "W{0}", Color.FromArgb(0xFF, 0x1E, 0x24, 0x33));
			Grid.SetRow(monthScale, 0);
			root.Children.Add(monthScale);

			var dayScale = BuildScale(Days, DayWidth, "{0}", Color.FromArgb(0xFF, 0x16, 0x1A, 0x26));
			Grid.SetRow(dayScale, 1);
			root.Children.Add(dayScale);

			// Task rows: a StackPanel, so nothing virtualizes — every bar and arrow is live.
			var rows = new StackPanel();
			for (var i = 0; i < count; i++)
			{
				var start = rng.Int(0, Days - 20);
				var length = rng.Int(4, 18);

				var row = new Grid { Height = RowHeight, Width = Days * DayWidth };

				// Baseline bar (ghost) + actual bar: the "show baseline" state doubles bar geometry.
				row.Children.Add(new Rectangle
				{
					Width = length * DayWidth,
					Height = 8,
					Margin = new Thickness(start * DayWidth, 14, 0, 0),
					HorizontalAlignment = HorizontalAlignment.Left,
					VerticalAlignment = VerticalAlignment.Top,
					RadiusX = 3,
					RadiusY = 3,
					Fill = new SolidColorBrush(Color.FromArgb(0x50, 0x88, 0x92, 0xB0)),
				});

				var shift = new TranslateTransform();
				_barShifts.Add(shift);
				row.Children.Add(new Rectangle
				{
					Width = length * DayWidth,
					Height = 12,
					Margin = new Thickness(start * DayWidth, 2, 0, 0),
					HorizontalAlignment = HorizontalAlignment.Left,
					VerticalAlignment = VerticalAlignment.Top,
					RadiusX = 4,
					RadiusY = 4,
					Fill = new SolidColorBrush(i % 9 == 0
						? Color.FromArgb(0xFF, 0xE0, 0x8A, 0x30)
						: Color.FromArgb(0xFF, 0x2A, 0x6F, 0xF0)),
					RenderTransform = shift,
				});

				// Dependency arrow to the previous row: an elbow polyline plus its arrowhead, the geometry that
				// dominates a dense schedule (two stroked paths per link).
				if (i > 0)
				{
					var x = start * DayWidth;
					row.Children.Add(new Polyline
					{
						Stroke = new SolidColorBrush(Color.FromArgb(0xC0, 0x9A, 0xA4, 0xC0)),
						StrokeThickness = 1,
						Points = new PointCollection
						{
							new Point(Math.Max(0, x - 14), -6),
							new Point(Math.Max(0, x - 6), -6),
							new Point(Math.Max(0, x - 6), 8),
							new Point(x, 8),
						},
					});
					row.Children.Add(new Polygon
					{
						Fill = new SolidColorBrush(Color.FromArgb(0xC0, 0x9A, 0xA4, 0xC0)),
						Points = new PointCollection
						{
							new Point(x, 8), new Point(x - 5, 5), new Point(x - 5, 11),
						},
					});
				}

				rows.Children.Add(row);
			}

			var scroller = new ScrollViewer
			{
				Content = rows,
				VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
				HorizontalScrollMode = ScrollMode.Disabled,
			};
			Grid.SetRow(scroller, 2);
			root.Children.Add(scroller);

			_sv = scroller;
			_offset = 0;
			_dir = 1;
			return root;
		}

		private StackPanel BuildScale(int intervals, double width, string format, Color background)
		{
			var scale = new StackPanel { Orientation = Orientation.Horizontal };
			for (var i = 0; i < intervals; i++)
			{
				var label = new TextBlock
				{
					Text = string.Format(format, i + 1),
					FontSize = 10,
					Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0xA8, 0xB0, 0xC8)),
					HorizontalAlignment = HorizontalAlignment.Center,
				};
				_dateLabels.Add(label);

				scale.Children.Add(new Border
				{
					Width = width,
					Height = 20,
					Background = new SolidColorBrush(background),
					BorderThickness = new Thickness(0, 0, 1, 1),
					BorderBrush = new SolidColorBrush(Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF)),
					Child = label,
				});
			}

			return scale;
		}

		protected override void Tick(long frame)
		{
			AdvanceScroll(_sv, ref _offset, ref _dir, 6.0);

			// Progress bars creep along the timeline: a transform change per row, so every bar's damage moves
			// every frame without re-measuring the schedule.
			var phase = frame * 0.35;
			for (var i = 0; i < _barShifts.Count; i++)
			{
				_barShifts[i].X = 6.0 * Math.Sin((phase + i * 0.22) * 0.08);
			}
		}
	}
}
