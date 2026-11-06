#nullable enable

#if __SKIA__
using System;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml.Performance.RenderStress
{
	/// <summary>
	/// Counts frames on a scene that never changes, to tell a demand-driven render loop from a free-running one.
	/// Mirrors Given_AcrylicBrush.When_Idle, which expects under 100 frames in 5 s, but reports the count instead of
	/// asserting so the scene can be bisected: UNO_IDLE_PROBE selects which half is on screen (acrylic, scrollbar,
	/// both, neither). Prints once a second to stdout.
	/// </summary>
	[Sample("Performance", Name = "IdleFrameProbe", Description = "Diagnostic: counts rendered frames on a static scene. UNO_IDLE_PROBE=acrylic|scrollbar|both|none picks the content.")]
	public sealed class IdleFrameProbe : Page
	{
		private int _frames;
		private int _lastReported;
		private long _startTicks;
		private DispatcherTimer? _timer;
		private CompositionTarget? _target;
		private Action? _onFrameRendered;

		public IdleFrameProbe()
		{
			var mode = Environment.GetEnvironmentVariable("UNO_IDLE_PROBE") ?? "both";
			var panel = new StackPanel { Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x20, 0x20, 0x20)) };

			if (mode is "acrylic" or "both")
			{
				// MenuFlyoutPresenter carries the Fluent acrylic background, which is what the test exercises.
				panel.Children.Add(new MenuFlyoutPresenter());
			}

			if (mode is "scrollbar" or "both")
			{
				var scrollBar = new ScrollBar { Orientation = Orientation.Vertical, Width = 30, Height = 200 };
				scrollBar.LayoutUpdated += (_, _) =>
				{
					VisualStateManager.GoToState(scrollBar, "MouseIndicator", true);
					VisualStateManager.GoToState(scrollBar, "Expanded", true);
				};
				panel.Children.Add(scrollBar);
			}

			var status = new TextBlock
			{
				Text = $"idle probe: {mode}",
				FontSize = 20,
				Foreground = new SolidColorBrush(Microsoft.UI.Colors.LightGreen),
			};
			panel.Children.Add(status);
			Content = panel;

			Loaded += (_, _) =>
			{
				Console.WriteLine($"IDLE-PROBE: mode={mode}");
				_startTicks = DateTime.UtcNow.Ticks;
				if (Visual.CompositionTarget is CompositionTarget target)
				{
					_target = target;
					_onFrameRendered = () => _frames++;
					target.FrameRendered += _onFrameRendered;
				}
				else
				{
					Console.WriteLine("IDLE-PROBE: no CompositionTarget");
				}

				_timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
				_timer.Tick += (_, _) =>
				{
					var total = _frames;
					var elapsed = (DateTime.UtcNow.Ticks - _startTicks) / (double)TimeSpan.TicksPerSecond;
					Console.WriteLine($"IDLE-PROBE: t={elapsed:F1}s frames={total} lastSecond={total - _lastReported}");
					status.Text = $"idle probe: {mode}\nframes={total} ({total - _lastReported}/s)";
					_lastReported = total;
				};
				_timer.Start();
			};

			Unloaded += (_, _) =>
			{
				_timer?.Stop();
				if (_target is { } t && _onFrameRendered is { } h)
				{
					t.FrameRendered -= h;
				}
			};
		}
	}
}
#endif
