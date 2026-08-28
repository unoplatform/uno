#nullable enable

using System;
using System.Collections.Generic;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml.Performance.RenderStress
{
	/// <summary>
	/// Scheduling-calendar archetype: a week grid built as seven independent, NON-VIRTUALIZED day columns of
	/// time slots, with booking pills laid over them. Modelled on the real delivery calendar, where each day is
	/// its own ItemsControl over a vertical StackPanel inside one ScrollViewer, so every slot and every pill in
	/// the week is realized at once regardless of viewport.
	/// </summary>
	[Sample("Performance", Name = "RealWorld_CalendarWeek", Description = "Real-UI perf: a non-virtualized week calendar (7 day columns of time slots + booking pills) with a moving now-line. Stresses slot/pill count at scale. Compare fps/frame-time across Skia and the WebGPU/ProGPU builds.")]
	public sealed class RealWorld_CalendarWeek : PerfBenchBase
	{
		private const int DayColumns = 7;
		private const double SlotHeight = 22;

		private static readonly string[] _days = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
		private static readonly string[] _crews = { "Crew A", "Crew B", "Concrete", "Steel", "Glazing", "MEP", "Inspection" };

		private readonly List<TranslateTransform> _pillShifts = new();
		private readonly List<TextBlock> _pillLabels = new();
		private Border _nowLine = null!;
		private ScrollViewer _sv = null!;
		private double _offset;
		private int _dir = 1;

		protected override string ScenarioName => "CalendarWeek";

		/// <summary>Slots per day column (a full day at 15-minute granularity is ~96).</summary>
		protected override int DefaultCount => 96;

		protected override UIElement BuildStage(int count)
		{
			var rng = new Rng(53);
			_pillShifts.Clear();
			_pillLabels.Clear();

			var root = new Grid();
			root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

			// Day header row.
			var header = new Grid { Height = 30 };
			header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(56) });
			for (var d = 0; d < DayColumns; d++)
			{
				header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
				var head = new Border
				{
					Background = new SolidColorBrush(Color.FromArgb(0x30, 0x2A, 0x6F, 0xF0)),
					BorderThickness = new Thickness(0, 0, 1, 1),
					BorderBrush = new SolidColorBrush(Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF)),
					Child = new TextBlock
					{
						Text = _days[d],
						FontSize = 12,
						FontWeight = Microsoft.UI.Text.FontWeights.Bold,
						Foreground = new SolidColorBrush(Windows.UI.Colors.White),
						HorizontalAlignment = HorizontalAlignment.Center,
						VerticalAlignment = VerticalAlignment.Center,
					},
				};
				Grid.SetColumn(head, d + 1);
				header.Children.Add(head);
			}

			Grid.SetRow(header, 0);
			root.Children.Add(header);

			// The week body: a time gutter plus seven day columns, each its own StackPanel of slots.
			var body = new Grid();
			body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(56) });

			var gutter = new StackPanel();
			for (var s = 0; s < count; s++)
			{
				gutter.Children.Add(new Border
				{
					Height = SlotHeight,
					BorderThickness = new Thickness(0, 0, 1, s % 4 == 0 ? 1 : 0),
					BorderBrush = new SolidColorBrush(Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF)),
					Child = s % 4 == 0
						? new TextBlock
						{
							Text = $"{s / 4:D2}:00",
							FontSize = 10,
							Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0x88, 0x92, 0xB0)),
							HorizontalAlignment = HorizontalAlignment.Right,
							Margin = new Thickness(0, 0, 6, 0),
						}
						: null,
				});
			}

			body.Children.Add(gutter);

			for (var d = 0; d < DayColumns; d++)
			{
				body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

				var dayHost = new Grid();

				var slots = new StackPanel();
				for (var s = 0; s < count; s++)
				{
					slots.Children.Add(new Border
					{
						Height = SlotHeight,
						Background = new SolidColorBrush(s % 8 < 4
							? Color.FromArgb(0x0C, 0xFF, 0xFF, 0xFF)
							: Color.FromArgb(0x00, 0, 0, 0)),
						BorderThickness = new Thickness(0, 0, 1, s % 4 == 3 ? 1 : 0),
						BorderBrush = new SolidColorBrush(Color.FromArgb(0x28, 0xFF, 0xFF, 0xFF)),
					});
				}

				dayHost.Children.Add(slots);

				// Booking pills stacked over the slots — each is a rounded, tinted card with two text lines.
				var pillHost = new StackPanel { Margin = new Thickness(3, 0, 3, 0) };
				var cursor = rng.Int(2, 6);
				while (cursor < count - 6)
				{
					var span = rng.Int(2, 6);
					var shift = new TranslateTransform();
					_pillShifts.Add(shift);

					var label = new TextBlock
					{
						Text = _crews[rng.Int(0, _crews.Length)],
						FontSize = 11,
						Foreground = new SolidColorBrush(Windows.UI.Colors.White),
					};
					_pillLabels.Add(label);

					var content = new StackPanel();
					content.Children.Add(label);
					content.Children.Add(new TextBlock
					{
						Text = $"{cursor / 4:D2}:{(cursor % 4) * 15:D2} · Dock {1 + (cursor % 4)}",
						FontSize = 9,
						Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0xD0, 0xD8, 0xF0)),
					});

					pillHost.Children.Add(new Border
					{
						Height = span * SlotHeight - 4,
						Margin = new Thickness(0, 2, 0, 2),
						CornerRadius = new CornerRadius(5),
						Padding = new Thickness(6, 2, 6, 2),
						Background = new SolidColorBrush(rng.Color(0xC0)),
						RenderTransform = shift,
						Child = content,
					});

					cursor += span + rng.Int(1, 5);
				}

				dayHost.Children.Add(pillHost);
				Grid.SetColumn(dayHost, d + 1);
				body.Children.Add(dayHost);
			}

			// The "now" indicator sweeps the week, so something always moves over the static grid.
			_nowLine = new Border
			{
				Height = 2,
				Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xE0, 0x40, 0x50)),
				VerticalAlignment = VerticalAlignment.Top,
				HorizontalAlignment = HorizontalAlignment.Stretch,
				RenderTransform = new TranslateTransform(),
			};
			Grid.SetColumnSpan(_nowLine, DayColumns + 1);
			body.Children.Add(_nowLine);

			_sv = new ScrollViewer
			{
				Content = body,
				VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
				HorizontalScrollMode = ScrollMode.Disabled,
			};
			Grid.SetRow(_sv, 1);
			root.Children.Add(_sv);

			_offset = 0;
			_dir = 1;
			return root;
		}

		protected override void Tick(long frame)
		{
			AdvanceScroll(_sv, ref _offset, ref _dir, 4.0);

			((TranslateTransform)_nowLine.RenderTransform).Y = (frame * 2.0) % (DefaultCount * SlotHeight);

			// Nudge a slice of the bookings each frame (drag/reflow), so pill damage is spread across the week.
			var start = (int)(frame % 5);
			for (var i = start; i < _pillShifts.Count; i += 5)
			{
				_pillShifts[i].X = 2.0 * Math.Sin((frame + i * 3) * 0.05);
			}
		}
	}
}
