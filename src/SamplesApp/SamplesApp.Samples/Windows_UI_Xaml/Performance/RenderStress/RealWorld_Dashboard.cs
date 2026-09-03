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
	/// Analytics-dashboard archetype: KPI cards (gradient + shadow), an animated bar chart (scale-transformed bars)
	/// and a live line chart (per-frame polyline) — stresses gradients, shadows, transforms and stroked-geometry
	/// churn together, no scroll. <c>count</c> scales the number of bars / line points.
	/// </summary>
	[Sample("Performance", Name = "RealWorld_Dashboard", Description = "Real-UI perf: an analytics dashboard with animated bar + line charts, gradient/shadow KPI cards. Compare fps/frame-time across Skia and the WebGPU/ProGPU builds.")]
	public sealed class RealWorld_Dashboard : PerfBenchBase
	{
		private readonly List<ScaleTransform> _bars = new();
		private Polyline _line = null!;
		private double _lineW, _lineH;
		private readonly List<TextBlock> _kpi = new();
		private readonly ThemeShadow _shadow = new();

		protected override string ScenarioName => "Dashboard";
		protected override int DefaultCount => 120;

		protected override UIElement BuildStage(int count)
		{
			_bars.Clear();
			_kpi.Clear();
			var rng = new Rng(44);

			var receiver = new Border { Background = new SolidColorBrush(Windows.UI.Colors.Transparent) };
			var root = new Grid { Padding = new Thickness(16), RowSpacing = 16 };
			root.Children.Add(receiver);
			_shadow.Receivers.Add(receiver);
			root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });               // KPI row
			root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // bar chart
			root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // line chart

			// KPI cards.
			var kpiRow = new Grid { ColumnSpacing = 12, Height = 90 };
			var themes = new (string, Color, Color)[]
			{
				("REVENUE", Color.FromArgb(0xFF, 0x2A, 0x6F, 0xF0), Color.FromArgb(0xFF, 0x6A, 0x36, 0xF0)),
				("USERS", Color.FromArgb(0xFF, 0x12, 0xA5, 0x94), Color.FromArgb(0xFF, 0x22, 0xC0, 0x55)),
				("ORDERS", Color.FromArgb(0xFF, 0xF0, 0x6A, 0x2A), Color.FromArgb(0xFF, 0xF0, 0xC0, 0x36)),
				("ERRORS", Color.FromArgb(0xFF, 0xE0, 0x2A, 0x6F), Color.FromArgb(0xFF, 0xF0, 0x6A, 0xB0)),
			};
			for (var i = 0; i < themes.Length; i++)
			{
				kpiRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
				var (label, a, b) = themes[i];
				var bg = new LinearGradientBrush { StartPoint = new(0, 0), EndPoint = new(1, 1) };
				bg.GradientStops.Add(new GradientStop { Offset = 0, Color = a });
				bg.GradientStops.Add(new GradientStop { Offset = 1, Color = b });
				var card = new Border { CornerRadius = new CornerRadius(12), Background = bg, Padding = new Thickness(14), Shadow = _shadow, Translation = new System.Numerics.Vector3(0, 0, 20) };
				var st = new StackPanel { Spacing = 4 };
				st.Children.Add(new TextBlock { Text = label, Foreground = new SolidColorBrush(Color.FromArgb(0xD0, 0xFF, 0xFF, 0xFF)), FontSize = 12 });
				var num = new TextBlock { Text = "0", Foreground = new SolidColorBrush(Windows.UI.Colors.White), FontSize = 26, FontWeight = Microsoft.UI.Text.FontWeights.Bold };
				_kpi.Add(num);
				st.Children.Add(num);
				card.Child = st;
				Grid.SetColumn(card, i);
				kpiRow.Children.Add(card);
			}
			Grid.SetRow(kpiRow, 0);
			root.Children.Add(kpiRow);

			// Bar chart card.
			var barCount = Math.Max(4, count);
			// Stretch (not Bottom): a Bottom-aligned Grid sizes to content, and the Stretch bars have no
			// intrinsic height — they'd measure 0px tall and the chart would render empty on every backend.
			var barHost = new Grid { ColumnSpacing = 3 };
			for (var i = 0; i < barCount; i++)
			{
				barHost.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
				var scale = new ScaleTransform { ScaleY = 0.5, CenterY = 1 };
				var barBrush = new LinearGradientBrush { StartPoint = new(0, 1), EndPoint = new(0, 0) };
				barBrush.GradientStops.Add(new GradientStop { Offset = 0, Color = Color.FromArgb(0xFF, 0x2A, 0x6F, 0xF0) });
				barBrush.GradientStops.Add(new GradientStop { Offset = 1, Color = Color.FromArgb(0xFF, 0x6A, 0xC0, 0xF0) });
				var bar = new Rectangle { Fill = barBrush, VerticalAlignment = VerticalAlignment.Stretch, RenderTransformOrigin = new Point(0.5, 1), RenderTransform = scale };
				_bars.Add(scale);
				Grid.SetColumn(bar, i);
				barHost.Children.Add(bar);
			}
			var barCard = new Border { CornerRadius = new CornerRadius(12), Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x1A, 0x1A, 0x22)), Padding = new Thickness(12), Child = barHost };
			Grid.SetRow(barCard, 1);
			root.Children.Add(barCard);

			// Line chart card.
			_line = new Polyline { Stroke = new SolidColorBrush(Color.FromArgb(0xFF, 0x30, 0xE0, 0xA0)), StrokeThickness = 2, StrokeLineJoin = PenLineJoin.Round };
			_lineW = 900;
			_lineH = 220;
			var lineHost = new Grid();
			lineHost.Children.Add(_line);
			lineHost.SizeChanged += (_, e) => { _lineW = e.NewSize.Width; _lineH = e.NewSize.Height; };
			var lineCard = new Border { CornerRadius = new CornerRadius(12), Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x1A, 0x1A, 0x22)), Padding = new Thickness(12), Child = lineHost };
			Grid.SetRow(lineCard, 2);
			root.Children.Add(lineCard);

			return root;
		}

		protected override void Tick(long frame)
		{
			var t = frame * 0.08;

			for (var i = 0; i < _bars.Count; i++)
			{
				_bars[i].ScaleY = 0.15 + 0.85 * (0.5 + 0.5 * Math.Sin(t + i * 0.35));
			}

			for (var i = 0; i < _kpi.Count; i++)
			{
				_kpi[i].Text = ((long)(1000 + 500 * Math.Sin(t + i)) + frame).ToString("N0");
			}

			// Rebuild the line's points each frame (stroked-geometry churn).
			var n = Math.Max(8, _bars.Count);
			var pts = new PointCollection();
			for (var i = 0; i < n; i++)
			{
				var x = _lineW * i / (n - 1);
				var y = _lineH * (0.5 - 0.42 * Math.Sin(t * 1.3 + i * 0.4));
				pts.Add(new Point(x, y));
			}
			_line.Points = pts;
		}
	}
}
