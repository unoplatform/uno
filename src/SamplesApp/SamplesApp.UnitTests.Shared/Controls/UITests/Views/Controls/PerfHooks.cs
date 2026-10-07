using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SampleControl.Presentation;

namespace Uno.UI.Samples.Controls
{
	public sealed partial class SampleChooserControl
	{
		private void InitializePerfHooks()
		{
			// Benchmark hook: UNO_PERF_OPEN_MENU=1 opens the settings (gear) flyout after the scene settles,
			// so the flyout-over-animated-content cost is measurable in scripted runs.
			if (Environment.GetEnvironmentVariable("UNO_PERF_OPEN_MENU") is "1" or "true")
			{
				Loaded += async (_, _) =>
				{
					await Task.Delay(TimeSpan.FromSeconds(30));
					// Narrow windows move the button into the "..." menu; anchor to the command bar then.
					FrameworkElement anchor = OverflowSettingsButton.IsInOverflow ? ShellCommandBar : OverflowSettingsButton;
					OverflowSettingsButton.Flyout?.ShowAt(anchor);
					Console.WriteLine("PERF: gear menu opened");
				};
			}

			// Benchmark hook: UNO_PERF_CYCLE=<seconds> walks every sample, dwelling <seconds> on each, with
			// "PERF-NAV:" markers; pairs with the UNO_LOG_FPS hook below for a per-sample FPS sweep.
			if (int.TryParse(Environment.GetEnvironmentVariable("UNO_PERF_CYCLE"), out var dwellSeconds) && dwellSeconds > 0)
			{
				Loaded += async (_, _) =>
				{
					if (Environment.GetEnvironmentVariable("UNO_PERF_MAXIMIZE") is "1" or "true"
						&& SamplesApp.App.MainWindow?.AppWindow?.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
					{
						presenter.Maximize();
						Console.WriteLine("PERF: window maximized");
					}
					await Task.Delay(TimeSpan.FromSeconds(10));
					await ViewModel.CycleAllSamplesForPerf(dwellSeconds, CancellationToken.None);
				};
			}

			// Benchmark hook: UNO_PERF_SCROLL=1 auto-scrolls the samples list at 60Hz (bounces at the ends),
			// reproducing the realize/derealize churn of manual scrolling without synthesizing input.
			if (Environment.GetEnvironmentVariable("UNO_PERF_SCROLL") is "1" or "true")
			{
				Loaded += async (_, _) =>
				{
					await Task.Delay(TimeSpan.FromSeconds(12));
					var sv = FindBrowserListScrollViewer() ?? FindTallestScrollViewer(this);
					if (sv is null)
					{
						Console.WriteLine("PERF-SCROLL: no scrollable ScrollViewer found");
						return;
					}
					Console.WriteLine($"PERF-SCROLL: start (scrollable={sv.ScrollableHeight:F0})");
					var dir = 1d;
					var scrollTimer = new Microsoft.UI.Xaml.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
					scrollTimer.Tick += (_, _) =>
					{
						var next = sv.VerticalOffset + dir * 25;
						if (next >= sv.ScrollableHeight) { next = sv.ScrollableHeight; dir = -1; }
						else if (next <= 0) { next = 0; dir = 1; }
						sv.ChangeView(null, next, null, disableAnimation: true);
					};
					scrollTimer.Start();
				};
			}


			if (Environment.GetEnvironmentVariable("UNO_LOG_FPS") is "1" or "true")
			{
				var frames = 0;
				var windowStart = DateTime.UtcNow;
				Microsoft.UI.Xaml.Media.CompositionTarget.Rendering += (_, _) =>
				{
					frames++;
					var elapsed = (DateTime.UtcNow - windowStart).TotalSeconds;
					if (elapsed >= 1)
					{
						Console.WriteLine($"FPS: {frames / elapsed:F1}");
						frames = 0;
						windowStart = DateTime.UtcNow;
					}
				};
			}
		}

		// The browser's sample list is the scenario the hook was written for; fall back to any tall list.
		private ScrollViewer FindBrowserListScrollViewer()
		{
			if (ShellSamplesList is { IsLoaded: true, Visibility: Visibility.Visible, ActualHeight: > 0 } list)
			{
				var sv = FindTallestScrollViewer(list);
				return sv is { ScrollableHeight: > 0 } ? sv : null;
			}

			return null;
		}

		private static ScrollViewer FindTallestScrollViewer(DependencyObject root)
		{
			ScrollViewer best = null;
			var queue = new Queue<DependencyObject>();
			queue.Enqueue(root);
			while (queue.Count > 0)
			{
				var current = queue.Dequeue();
				if (current is ScrollViewer sv && sv.ScrollableHeight > 0 && (best is null || sv.ScrollableHeight > best.ScrollableHeight))
				{
					best = sv;
				}
				var count = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(current);
				for (var i = 0; i < count; i++)
				{
					queue.Enqueue(Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(current, i));
				}
			}
			return best;
		}
	}
}
