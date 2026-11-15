#if HAS_UNO
#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

/// <summary>
/// Synchronous layout micro-benchmarks (not assertions): they replicate the RealWorld_DataGrid /
/// RealWorld_ScrollingFeed per-frame layout work in a tight UpdateLayout loop, so a CPU profiler attached to
/// the process attributes exactly where layout time goes, without any render/present in the way. Each section
/// prints iterations and ms/iteration to the console ([layout-perf] lines).
/// </summary>
[TestClass]
public class Given_LayoutPerfHarness
{
	private static readonly string[] _status = { "OK", "WARN", "BUSY", "IDLE" };

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_LayoutPerf_DataGrid_Churn()
	{
		const int rows = 600;
		var valueCells = new List<TextBlock>(rows);
		var values = new List<int>(rows);

		var grid = new Grid();
		for (var c = 0; c < 6; c++)
		{
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(c == 1 ? 2 : 1, GridUnitType.Star) });
		}
		for (var r = 0; r < rows; r++)
		{
			grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(26) });
		}
		for (var r = 0; r < rows; r++)
		{
			values.Add((r * 37) % 10000);
			for (var c = 0; c < 6; c++)
			{
				var tb = new TextBlock { Text = c == 3 ? values[r].ToString() : $"cell-{r}-{c}", FontSize = 12 };
				Grid.SetRow(tb, r);
				Grid.SetColumn(tb, c);
				grid.Children.Add(tb);
				if (c == 3)
				{
					valueCells.Add(tb);
				}
			}
		}

		var sv = new ScrollViewer { Content = grid, Height = 800, Width = 1200 };
		await UITestHelper.Load(sv);

		try
		{
			RunSection("datagrid-churn", () =>
			{
				for (var i = 0; i < valueCells.Count; i += 8)
				{
					values[i] = (values[i] + 7) % 10000;
					valueCells[i].Text = values[i].ToString();
				}
				sv.UpdateLayout();
			});

			var offset = 0d;
			RunSection("datagrid-scroll", () =>
			{
				offset = (offset + 6) % Math.Max(1, sv.ScrollableHeight);
				sv.ChangeView(null, offset, null, disableAnimation: true);
				sv.UpdateLayout();
			});

			RunSection("datagrid-churn+scroll", () =>
			{
				for (var i = 0; i < valueCells.Count; i += 8)
				{
					values[i] = (values[i] + 7) % 10000;
					valueCells[i].Text = values[i].ToString();
				}
				offset = (offset + 6) % Math.Max(1, sv.ScrollableHeight);
				sv.ChangeView(null, offset, null, disableAnimation: true);
				sv.UpdateLayout();
			});
		}
		finally
		{
			Private.Infrastructure.TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_LayoutPerf_Feed_Scroll()
	{
		var feed = new StackPanel { Spacing = 12 };
		for (var i = 0; i < 120; i++)
		{
			var card = new Border
			{
				CornerRadius = new CornerRadius(12),
				Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x1E, 0x1E, 0x24)),
				Height = 96,
			};
			var inner = new StackPanel { Orientation = Orientation.Horizontal };
			inner.Children.Add(new TextBlock { Text = _status[i % 4], FontSize = 10 });
			inner.Children.Add(new TextBlock { Text = $"Feed card title {i}", FontSize = 16, TextWrapping = TextWrapping.Wrap });
			inner.Children.Add(new TextBlock { Text = "A quick summary line that wraps across a couple of rows.", FontSize = 12, TextWrapping = TextWrapping.Wrap });
			card.Child = inner;
			feed.Children.Add(card);
		}

		var sv = new ScrollViewer { Content = feed, Height = 800, Width = 1200 };
		await UITestHelper.Load(sv);

		try
		{
			var offset = 0d;
			RunSection("feed-scroll", () =>
			{
				offset = (offset + 6) % Math.Max(1, sv.ScrollableHeight);
				sv.ChangeView(null, offset, null, disableAnimation: true);
				sv.UpdateLayout();
			});
		}
		finally
		{
			Private.Infrastructure.TestServices.WindowHelper.WindowContent = null;
		}
	}

	private static void RunSection(string name, Action iteration)
	{
		// Warmup, then run time-boxed so an attached sampling profiler sees a saturated window.
		for (var i = 0; i < 5; i++)
		{
			iteration();
		}

		var sw = Stopwatch.StartNew();
		var iterations = 0;
		while (sw.ElapsedMilliseconds < 5000)
		{
			iteration();
			iterations++;
		}
		sw.Stop();
		Console.WriteLine($"[layout-perf] {name}: {iterations} iterations, {sw.Elapsed.TotalMilliseconds / Math.Max(1, iterations):F2} ms/iter");
	}
}
#endif
