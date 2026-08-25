#nullable enable

using System;
using System.Diagnostics;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace UITests.Windows_UI_Xaml.Performance.RenderStress
{
	/// <summary>
	/// Shared harness for the representative real-UI render benchmarks (Jankbench-style: a realistic screen driven
	/// continuously at scale). A subclass builds the stage (<see cref="BuildStage"/>) and advances motion each frame
	/// (<see cref="Tick"/> — scroll for lists, data churn for the dashboard); the base owns the fps / avg+max frame
	/// time overlay, the ± count controls, and the per-frame measurement. <c>UNO_PERF_COUNT</c> seeds the count;
	/// <c>UNO_LOG_FPS=1</c> prints to the console. Compare the same sample across the Skia and WebGPU/ProGPU builds.
	/// </summary>
	public abstract class PerfBenchBase : Page
	{
		private readonly TextBlock _overlay = new();
		private EventHandler<object>? _renderHandler;
		private int _framesThisWindow;
		private long _windowStartTs;
		private long _lastFrameTs;
		private long _frame;
		private double _maxFrameMs;

		private protected int Count;

		protected virtual int DefaultCount => 200;
		protected abstract string ScenarioName { get; }

		/// <summary>Builds the stage visual for <paramref name="count"/> items and returns it (store any refs the subclass needs in Tick).</summary>
		protected abstract UIElement BuildStage(int count);

		/// <summary>Called once per frame to advance motion (scroll offset, data churn, …). <paramref name="frame"/> is the frame index.</summary>
		protected abstract void Tick(long frame);

		private readonly Grid _root = new();
		private UIElement? _stage;

		protected PerfBenchBase()
		{
			Count = int.TryParse(Environment.GetEnvironmentVariable("UNO_PERF_COUNT"), out var n) && n > 0 ? n : DefaultCount;

			var overlayPanel = new StackPanel
			{
				HorizontalAlignment = HorizontalAlignment.Right,
				VerticalAlignment = VerticalAlignment.Top,
				Margin = new Thickness(8),
				Background = new SolidColorBrush(Color.FromArgb(0xC0, 0, 0, 0)),
				Padding = new Thickness(8),
				Spacing = 6,
			};
			_overlay.Foreground = new SolidColorBrush(Windows.UI.Colors.LightGreen);
			_overlay.FontSize = 16;
			_overlay.FontFamily = new FontFamily("Consolas");
			var controls = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
			foreach (var (label, op) in new[] { ("-", 0), ("+", 1), ("×2", 2), ("÷2", 3) })
			{
				var b = new Button { Content = label, MinWidth = 40 };
				var o = op;
				b.Click += (_, _) =>
				{
					var step = Math.Max(10, Count / 4);
					Count = o switch { 0 => Math.Max(10, Count - step), 1 => Count + step, 2 => Count * 2, _ => Math.Max(10, Count / 2) };
					Rebuild();
				};
				controls.Children.Add(b);
			}
			overlayPanel.Children.Add(_overlay);
			overlayPanel.Children.Add(controls);

			_root.Children.Add(overlayPanel);   // stage inserted at index 0 in Rebuild (below the overlay)
			Content = _root;

			Loaded += OnLoaded;
			Unloaded += OnUnloaded;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			Rebuild();
			_windowStartTs = Stopwatch.GetTimestamp();
			_lastFrameTs = _windowStartTs;
			_frame = 0;
			_renderHandler = OnRendering;
			CompositionTarget.Rendering += _renderHandler;
			Console.WriteLine($"PERF-RENDER: scenario={ScenarioName} renderer={RendererName} count={Count}");
		}

		private void OnUnloaded(object sender, RoutedEventArgs e)
		{
			if (_renderHandler is not null)
			{
				CompositionTarget.Rendering -= _renderHandler;
				_renderHandler = null;
			}
		}

		private void Rebuild()
		{
			if (_stage is not null)
			{
				_root.Children.Remove(_stage);
			}
			_stage = BuildStage(Count);
			_root.Children.Insert(0, _stage);
		}

		private void OnRendering(object? sender, object e)
		{
			Tick(_frame++);

			var now = Stopwatch.GetTimestamp();
			var frameMs = (now - _lastFrameTs) * 1000.0 / Stopwatch.Frequency;
			_lastFrameTs = now;
			if (frameMs > _maxFrameMs)
			{
				_maxFrameMs = frameMs;
			}

			_framesThisWindow++;
			var elapsed = (now - _windowStartTs) / (double)Stopwatch.Frequency;
			if (elapsed >= 0.5)
			{
				var fps = _framesThisWindow / elapsed;
				var avgMs = elapsed * 1000.0 / _framesThisWindow;
				_overlay.Text = $"{ScenarioName}  {RendererName}\n{fps,6:F1} fps  avg {avgMs,6:F2} ms  max {_maxFrameMs,6:F2} ms  n={Count}";
				if (Environment.GetEnvironmentVariable("UNO_LOG_FPS") is "1" or "true")
				{
					Console.WriteLine($"FPS: {fps:F1}  avg={avgMs:F2}ms  max={_maxFrameMs:F2}ms  n={Count}  scenario={ScenarioName}");
				}
				_framesThisWindow = 0;
				_windowStartTs = now;
				_maxFrameMs = 0;
			}
		}

		private protected static string RendererName
			=> Environment.GetEnvironmentVariable("UNO_PROGPU") is "1" or "true" or "webgpu" ? "ProGPU"
				: Environment.GetEnvironmentVariable("UNO_WEBGPU") is "1" or "true" or "neutral" or "swapchain" ? "WebGPU"
				: "Skia";

		/// <summary>Deterministic PRNG so every renderer draws the identical scene.</summary>
		private protected sealed class Rng
		{
			private uint _s;
			public Rng(uint seed) => _s = seed == 0 ? 0x9E3779B9 : seed;
			public double Next() { _s ^= _s << 13; _s ^= _s >> 17; _s ^= _s << 5; return (_s & 0xFFFFFF) / (double)0x1000000; }
			public double Range(double lo, double hi) => lo + Next() * (hi - lo);
			public int Int(int loInc, int hiEx) => loInc + (int)(Next() * (hiEx - loInc));
			public byte Byte() => (byte)(Next() * 256);
			public Color Color(byte a = 0xFF) => Windows.UI.Color.FromArgb(a, Byte(), Byte(), Byte());
		}

		// Ping-pong auto-scroll helper for the scrolling subclasses.
		private protected static void AdvanceScroll(ScrollViewer sv, ref double offset, ref int dir, double step)
		{
			var scrollable = sv.ScrollableHeight;
			if (scrollable <= 1)
			{
				return;
			}
			offset += dir * step;
			if (offset >= scrollable) { offset = scrollable; dir = -1; }
			else if (offset <= 0) { offset = 0; dir = 1; }
			sv.ChangeView(null, offset, null, disableAnimation: true);
		}
	}
}
