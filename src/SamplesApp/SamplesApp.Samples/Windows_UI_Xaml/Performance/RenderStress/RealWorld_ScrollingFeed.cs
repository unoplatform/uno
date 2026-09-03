#nullable enable

using System;
using System.Diagnostics;
using System.Numerics;
using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml.Performance.RenderStress
{
	/// <summary>
	/// A realistic app screen — a continuously auto-scrolling card feed (thumbnail image + gradient accent bar +
	/// rounded border + shadow + title/body text + a category chip) — rather than isolated primitive stress. It
	/// exercises the mix a real feed hits every frame (image, gradient, clip/rounded-rect, text, shadow) while the
	/// viewport repaints during scroll, and reports fps / avg+max frame time. Compare across the Skia and WebGPU/ProGPU
	/// builds; <c>UNO_PERF_COUNT</c> sets the card count, <c>UNO_LOG_FPS=1</c> prints to the console.
	/// </summary>
	[Sample("Performance", Name = "RealWorld_ScrollingFeed", Description = "Real-UI perf: an auto-scrolling card feed (image+gradient+text+shadow), the mix a real app repaints while scrolling. Compare fps/frame-time across Skia and the WebGPU/ProGPU builds.")]
	public sealed class RealWorld_ScrollingFeed : Page
	{
		private static readonly string[] _thumbs =
		{
			"ms-appx:///Assets/LargeWisteria.jpg",
			"ms-appx:///Assets/ingredient1.png",
			"ms-appx:///Assets/ingredient2.png",
			"ms-appx:///Assets/ingredient3.png",
			"ms-appx:///Assets/ingredient4.png",
			"ms-appx:///Assets/ingredient5.png",
			"ms-appx:///Assets/ingredient6.png",
		};

		private static readonly (string cat, Color a, Color b)[] _themes =
		{
			("NEWS", Color.FromArgb(0xFF, 0x2A, 0x6F, 0xF0), Color.FromArgb(0xFF, 0x6A, 0x36, 0xF0)),
			("FOOD", Color.FromArgb(0xFF, 0xF0, 0x6A, 0x2A), Color.FromArgb(0xFF, 0xF0, 0xC0, 0x36)),
			("TECH", Color.FromArgb(0xFF, 0x12, 0xA5, 0x94), Color.FromArgb(0xFF, 0x22, 0xC0, 0x55)),
			("PLAY", Color.FromArgb(0xFF, 0xE0, 0x2A, 0x6F), Color.FromArgb(0xFF, 0xF0, 0x6A, 0xB0)),
		};

		private static readonly string[] _titles =
		{
			"Uno Platform ships single-project templates",
			"How the drawing backend went renderer-agnostic",
			"Benchmarking WebGPU against Skia on desktop",
			"Five ways to speed up your XAML startup",
			"A field guide to composition brushes",
			"Shadows, blur, and the cost of a SaveLayer",
		};

		private const string Body =
			"A quick summary line that wraps across a couple of rows, the way a real feed card shows a preview of the article body before you tap through to read more.";

		private readonly ScrollViewer _sv = new();
		private readonly StackPanel _feed = new() { Spacing = 12, Padding = new Thickness(16) };
		private readonly TextBlock _overlay = new();
		private readonly ThemeShadow _shadow = new();
		private readonly Border _shadowReceiver = new() { Background = new SolidColorBrush(Windows.UI.Colors.Transparent) };

		private EventHandler<object>? _renderHandler;
		private int _framesThisWindow;
		private long _windowStartTs;
		private long _lastFrameTs;
		private double _maxFrameMs;
		private double _offset;
		private int _dir = 1;
		private int _count;

		public RealWorld_ScrollingFeed()
		{
			_count = int.TryParse(Environment.GetEnvironmentVariable("UNO_PERF_COUNT"), out var n) && n > 0 ? n : 120;

			var root = new Grid();

			_shadowReceiver.HorizontalAlignment = HorizontalAlignment.Stretch;
			_shadowReceiver.VerticalAlignment = VerticalAlignment.Stretch;
			root.Children.Add(_shadowReceiver);
			_shadow.Receivers.Add(_shadowReceiver);

			_sv.Content = _feed;
			_sv.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
			_sv.HorizontalScrollMode = ScrollMode.Disabled;
			root.Children.Add(_sv);

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
			_overlay.FontSize = 18;
			_overlay.FontFamily = new FontFamily("Consolas");
			var controls = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
			foreach (var (label, mul) in new[] { ("-50", 0), ("+50", 1), ("×2", 2), ("÷2", 3) })
			{
				var b = new Button { Content = label };
				var m = mul;
				b.Click += (_, _) => { _count = m switch { 0 => Math.Max(10, _count - 50), 1 => _count + 50, 2 => _count * 2, _ => Math.Max(10, _count / 2) }; Rebuild(); };
				controls.Children.Add(b);
			}
			overlayPanel.Children.Add(_overlay);
			overlayPanel.Children.Add(controls);
			root.Children.Add(overlayPanel);

			Content = root;
			Loaded += OnLoaded;
			Unloaded += OnUnloaded;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			Rebuild();
			_windowStartTs = Stopwatch.GetTimestamp();
			_lastFrameTs = _windowStartTs;
			_renderHandler = OnRendering;
			CompositionTarget.Rendering += _renderHandler;
			Console.WriteLine($"PERF-RENDER: scenario=ScrollingFeed renderer={RendererName()} count={_count}");
		}

		private void OnUnloaded(object sender, RoutedEventArgs e)
		{
			if (_renderHandler is not null)
			{
				CompositionTarget.Rendering -= _renderHandler;
				_renderHandler = null;
			}
			_feed.Children.Clear();
		}

		private void Rebuild()
		{
			_feed.Children.Clear();
			for (var i = 0; i < _count; i++)
			{
				_feed.Children.Add(BuildCard(i));
			}
		}

		private Border BuildCard(int i)
		{
			var (cat, a, b) = _themes[i % _themes.Length];

			var card = new Border
			{
				CornerRadius = new CornerRadius(12),
				Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x1E, 0x1E, 0x24)),
				Padding = new Thickness(0),
				Shadow = _shadow,
				Translation = new Vector3(0, 0, 16),
			};

			var grid = new Grid();
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });        // accent bar
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });       // thumbnail
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // text

			// Gradient accent bar.
			var accent = new LinearGradientBrush { StartPoint = new(0, 0), EndPoint = new(0, 1) };
			accent.GradientStops.Add(new GradientStop { Offset = 0, Color = a });
			accent.GradientStops.Add(new GradientStop { Offset = 1, Color = b });
			var accentRect = new Rectangle { Fill = accent };
			Grid.SetColumn(accentRect, 0);
			grid.Children.Add(accentRect);

			// Thumbnail image (clipped to rounded corner on the leading edge).
			var img = new Image
			{
				Source = new BitmapImage(new Uri(_thumbs[i % _thumbs.Length])),
				Stretch = Stretch.UniformToFill,
				Width = 120,
				Height = 96,
			};
			var thumbHost = new Border { Child = img, CornerRadius = new CornerRadius(0), Height = 96, Margin = new Thickness(0) };
			Grid.SetColumn(thumbHost, 1);
			grid.Children.Add(thumbHost);

			// Text column: category chip + title + body.
			var text = new StackPanel { Margin = new Thickness(12, 8, 12, 8), Spacing = 4 };

			var chip = new Border
			{
				Background = new SolidColorBrush(a),
				CornerRadius = new CornerRadius(4),
				Padding = new Thickness(6, 1, 6, 1),
				HorizontalAlignment = HorizontalAlignment.Left,
				Child = new TextBlock { Text = cat, FontSize = 10, Foreground = new SolidColorBrush(Windows.UI.Colors.White) },
			};
			text.Children.Add(chip);
			text.Children.Add(new TextBlock
			{
				Text = _titles[i % _titles.Length],
				FontSize = 16,
				FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
				Foreground = new SolidColorBrush(Windows.UI.Colors.White),
				TextWrapping = TextWrapping.Wrap,
			});
			text.Children.Add(new TextBlock
			{
				Text = Body,
				FontSize = 12,
				Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0xA8, 0xA8, 0xB0)),
				TextWrapping = TextWrapping.Wrap,
				MaxLines = 2,
			});
			Grid.SetColumn(text, 2);
			grid.Children.Add(text);

			// Clip the child grid to the card's rounded rect so the image/accent honor the corners.
			card.Child = grid;
			return card;
		}

		private void OnRendering(object? sender, object e)
		{
			// Auto-scroll: ping-pong the viewport so the feed repaints continuously (the real per-frame cost of scroll).
			var scrollable = _sv.ScrollableHeight;
			if (scrollable > 1)
			{
				_offset += _dir * 6.0;
				if (_offset >= scrollable) { _offset = scrollable; _dir = -1; }
				else if (_offset <= 0) { _offset = 0; _dir = 1; }
				_sv.ChangeView(null, _offset, null, disableAnimation: true);
			}

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
				_overlay.Text = $"ScrollingFeed  {RendererName()}\n{fps,6:F1} fps  avg {avgMs,6:F2} ms  max {_maxFrameMs,6:F2} ms  n={_count}";
				if (Environment.GetEnvironmentVariable("UNO_LOG_FPS") is "1" or "true")
				{
					Console.WriteLine($"FPS: {fps:F1}  avg={avgMs:F2}ms  max={_maxFrameMs:F2}ms  n={_count}  scenario=ScrollingFeed");
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
	}
}
