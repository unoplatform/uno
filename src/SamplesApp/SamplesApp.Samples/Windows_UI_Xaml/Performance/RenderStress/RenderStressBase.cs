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
	/// Base page for drawing-backend stress samples used to compare renderers (e.g. Skia vs the WebGPU/ProGPU
	/// backends). Each subclass fills a host <see cref="Canvas"/> with <c>Count</c> instances of one primitive kind
	/// (paths, strokes, gradients, shadows, text, transparency layers). A per-frame rotation on the host forces the
	/// WHOLE set to redraw every frame — damage-region culling would otherwise leave a static scene unmeasured — so
	/// the on-screen FPS / average+max frame time reflects the backend's real per-frame draw cost.
	///
	/// FPS caps at the display refresh while the scene is cheap, so crank <c>Count</c> until it drops below the cap:
	/// once frame time exceeds the vsync interval the numbers become renderer-bound and comparable. <c>UNO_LOG_FPS=1</c>
	/// also prints <c>FPS:</c> / frame-time lines to the console for scripted capture.
	/// </summary>
	public abstract class RenderStressBase : Page
	{
		private readonly Canvas _host = new();
		private readonly RotateTransform _spin = new();
		private readonly TextBlock _overlay = new();
		private readonly TextBlock _title = new();

		private EventHandler<object>? _renderHandler;
		private int _framesThisWindow;
		private long _windowStartTs;
		private long _lastFrameTs;
		private double _maxFrameMs;
		private double _angle;
		private int _count;

		/// <summary>Default primitive count; overridden per scenario by weight (a shadow costs more than a rect).</summary>
		protected virtual int DefaultCount => 400;

		/// <summary>Short scenario label shown in the overlay.</summary>
		protected abstract string ScenarioName { get; }

		/// <summary>Fills <paramref name="host"/> with <paramref name="count"/> primitives sized to <paramref name="width"/>×<paramref name="height"/>.</summary>
		protected abstract void Populate(Canvas host, int count, double width, double height);

		protected RenderStressBase()
		{
			_count = ResolveInitialCount();

			var root = new Grid();

			// Content host: a large canvas spun a little each frame so every child re-rasterizes.
			_host.Width = 1600;
			_host.Height = 1200;
			_host.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5);
			_host.RenderTransform = _spin;
			var hostContainer = new Border
			{
				Child = _host,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
			};
			root.Children.Add(hostContainer);

			// Overlay (top-left): renderer + count + live FPS / frame time.
			var overlayPanel = new StackPanel
			{
				Orientation = Orientation.Vertical,
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				Margin = new Thickness(8),
				Background = new SolidColorBrush(Color.FromArgb(0xB0, 0, 0, 0)),
				Padding = new Thickness(8),
			};
			_title.Foreground = new SolidColorBrush(Windows.UI.Colors.White);
			_title.FontSize = 14;
			_overlay.Foreground = new SolidColorBrush(Colors.LightGreen);
			_overlay.FontSize = 20;
			_overlay.FontFamily = new FontFamily("Consolas");

			var controls = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 6, 0, 0) };
			foreach (var (label, delta) in new[] { ("-100", -100), ("+100", 100), ("×2", 0), ("÷2", -1) })
			{
				var b = new Button { Content = label };
				var d = delta;
				b.Click += (_, _) =>
				{
					_count = d switch { 0 => _count * 2, -1 => Math.Max(1, _count / 2), _ => Math.Max(1, _count + d) };
					Rebuild();
				};
				controls.Children.Add(b);
			}

			overlayPanel.Children.Add(_title);
			overlayPanel.Children.Add(_overlay);
			overlayPanel.Children.Add(controls);
			root.Children.Add(overlayPanel);

			Content = root;

			Loaded += OnLoaded;
			Unloaded += OnUnloaded;
		}

		private int ResolveInitialCount()
			=> int.TryParse(Environment.GetEnvironmentVariable("UNO_PERF_COUNT"), out var n) && n > 0 ? n : DefaultCount;

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			Rebuild();
			_windowStartTs = Stopwatch.GetTimestamp();
			_lastFrameTs = _windowStartTs;
			_framesThisWindow = 0;
			_maxFrameMs = 0;

			_renderHandler = OnRendering;
			CompositionTarget.Rendering += _renderHandler;

			Console.WriteLine($"PERF-RENDER: scenario={ScenarioName} renderer={RendererName()} count={_count}");
		}

		private void OnUnloaded(object sender, RoutedEventArgs e)
		{
			if (_renderHandler is not null)
			{
				CompositionTarget.Rendering -= _renderHandler;
				_renderHandler = null;
			}
			_host.Children.Clear();
		}

		private void Rebuild()
		{
			_host.Children.Clear();
			Populate(_host, _count, _host.Width, _host.Height);
			_title.Text = $"{ScenarioName}  •  renderer: {RendererName()}";
		}

		private void OnRendering(object? sender, object e)
		{
			// Spin the host a hair each frame → all children damage → full redraw (renderer-bound, not cull-bound).
			_angle = (_angle + 0.35) % 360;
			_spin.Angle = _angle;

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
				_overlay.Text = $"{fps,6:F1} fps   avg {avgMs,6:F2} ms   max {_maxFrameMs,6:F2} ms   n={_count}";

				if (Environment.GetEnvironmentVariable("UNO_LOG_FPS") is "1" or "true")
				{
					Console.WriteLine($"FPS: {fps:F1}  avg={avgMs:F2}ms  max={_maxFrameMs:F2}ms  n={_count}  scenario={ScenarioName}");
				}

				_framesThisWindow = 0;
				_windowStartTs = now;
				_maxFrameMs = 0;
			}
		}

		private static string RendererName()
			=> Environment.GetEnvironmentVariable("UNO_PROGPU") is "1" or "true" or "webgpu" ? "ProGPU"
				: Environment.GetEnvironmentVariable("UNO_WEBGPU") is "1" or "true" or "neutral" or "swapchain" ? "WebGPU"
				: "Skia";

		/// <summary>Deterministic pseudo-random so each run/renderer draws the identical scene (fair comparison).</summary>
		private protected sealed class Rng
		{
			private uint _s;
			public Rng(uint seed) => _s = seed == 0 ? 0x9E3779B9 : seed;
			public double Next() { _s ^= _s << 13; _s ^= _s >> 17; _s ^= _s << 5; return (_s & 0xFFFFFF) / (double)0x1000000; }
			public double Range(double lo, double hi) => lo + Next() * (hi - lo);
			public byte Byte() => (byte)(Next() * 256);
			public Color Color(byte a = 0xFF) => Windows.UI.Color.FromArgb(a, Byte(), Byte(), Byte());
		}
	}
}
