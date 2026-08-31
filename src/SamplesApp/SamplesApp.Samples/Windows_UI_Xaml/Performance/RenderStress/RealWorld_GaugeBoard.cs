#nullable enable

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
	/// Operations-monitoring archetype: a scrolling wall of circular gauges, the shape of an NOC dashboard or a
	/// service health board.
	/// <para>
	/// Deliberately GPU-bound rather than record-bound. Each gauge is three cheap shapes carrying expensive
	/// per-pixel work: a RADIAL-gradient glow behind it, a radial-gradient face, and a gradient-stroked ring drawn
	/// as a dashed ellipse so the arc has real stroke coverage. Geometry stays trivial — a handful of ellipses per
	/// gauge — while the shaded area is large, which is the combination that puts the frame in `draw` instead of
	/// in layout or op building.
	/// </para>
	/// No other RealWorld scene uses radial gradients, and they are the most reliably GPU-bound brush here: every
	/// covered pixel maps into the unit ellipse and solves along the ray.
	/// </summary>
	[Sample("Performance", Name = "RealWorld_GaugeBoard", Description = "Real-UI perf: a scrolling wall of radial-gradient gauges with gradient-stroked arcs and glows — deliberately GPU-bound (per-pixel shading over trivial geometry). Compare fps/frame-time across Skia and the WebGPU/ProGPU builds.")]
	public sealed class RealWorld_GaugeBoard : PerfBenchBase
	{
		private ScrollViewer _sv = null!;
		private double _offset;
		private int _dir = 1;

		protected override string ScenarioName => "GaugeBoard";

		/// <summary>Number of gauges. Each contributes real shaded area, so cost scales with this.</summary>
		protected override int DefaultCount => 120;

		protected override UIElement BuildStage(int count)
		{
			var rng = new Rng(6421);
			var root = new Grid { Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x0B, 0x0E, 0x14)) };

			// Fixed-width tiles wrapped into rows: a monitoring wall is a grid, and a grid keeps the shaded area
			// per screenful independent of the count.
			const double Tile = 180;
			const double Gauge = 132;
			var perRow = 6;
			var stage = new Canvas { Width = perRow * Tile, Height = (count / perRow + 1) * Tile };

			for (var i = 0; i < count; i++)
			{
				var left = (i % perRow) * Tile;
				var top = (i / perRow) * Tile;
				var hue = rng.Color(0xFF);

				// 1) Glow — a large, soft radial falloff behind the gauge. Cheap geometry, wide shaded area.
				var glow = new RadialGradientBrush
				{
					Center = new Point(0.5, 0.5),
					GradientOrigin = new Point(0.5, 0.5),
					RadiusX = 0.5,
					RadiusY = 0.5,
				};
				glow.GradientStops.Add(new GradientStop { Offset = 0, Color = Color.FromArgb(0x66, hue.R, hue.G, hue.B) });
				glow.GradientStops.Add(new GradientStop { Offset = 0.6, Color = Color.FromArgb(0x22, hue.R, hue.G, hue.B) });
				glow.GradientStops.Add(new GradientStop { Offset = 1, Color = Color.FromArgb(0, hue.R, hue.G, hue.B) });

				// The halo is deliberately much larger than its tile, so neighbouring glows OVERLAP. Shaded area per
				// screenful — not gauge count — is what makes this GPU-bound; the wall scrolls, so raising the
				// count only lengthens the scroll.
				const double Halo = Tile * 2.6;
				var glowEl = new Ellipse { Width = Halo, Height = Halo, Fill = glow };
				Canvas.SetLeft(glowEl, left - (Halo - Tile) / 2);
				Canvas.SetTop(glowEl, top - (Halo - Tile) / 2);
				stage.Children.Add(glowEl);

				// 2) Face — a second radial gradient, so the gauge interior is shaded per pixel too.
				var face = new RadialGradientBrush
				{
					Center = new Point(0.42, 0.38),
					GradientOrigin = new Point(0.42, 0.38),
					RadiusX = 0.62,
					RadiusY = 0.62,
				};
				face.GradientStops.Add(new GradientStop { Offset = 0, Color = Color.FromArgb(0xFF, 0x1E, 0x26, 0x34) });
				face.GradientStops.Add(new GradientStop { Offset = 1, Color = Color.FromArgb(0xFF, 0x0E, 0x13, 0x1B) });

				var faceEl = new Ellipse { Width = Gauge, Height = Gauge, Fill = face };
				Canvas.SetLeft(faceEl, left + (Tile - Gauge) / 2);
				Canvas.SetTop(faceEl, top + (Tile - Gauge) / 2);
				stage.Children.Add(faceEl);

				// 3) Arc — a gradient-stroked ring, dashed so it reads as a partial arc. A thick stroke over a
				// circle is genuine anti-aliased coverage, not a rectangle fill.
				var ring = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 1) };
				ring.GradientStops.Add(new GradientStop { Offset = 0, Color = Color.FromArgb(0xFF, hue.R, hue.G, hue.B) });
				ring.GradientStops.Add(new GradientStop { Offset = 1, Color = Color.FromArgb(0x80, 0xFF, 0xFF, 0xFF) });

				var arc = new Ellipse
				{
					Width = Gauge - 14,
					Height = Gauge - 14,
					Stroke = ring,
					StrokeThickness = 13,
					// Dash the ring so only part of it draws — the usual way a gauge shows a value.
					StrokeDashArray = new DoubleCollection { rng.Range(3, 14), 40 },
					StrokeDashCap = PenLineCap.Round,
				};
				Canvas.SetLeft(arc, left + (Tile - Gauge + 14) / 2);
				Canvas.SetTop(arc, top + (Tile - Gauge + 14) / 2);
				stage.Children.Add(arc);

				var label = new TextBlock
				{
					Text = $"{(int)rng.Range(1, 100)}%",
					FontSize = 20,
					Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0xE8, 0xEE, 0xF6)),
				};
				Canvas.SetLeft(label, left + Tile / 2 - 20);
				Canvas.SetTop(label, top + Tile / 2 - 14);
				stage.Children.Add(label);
			}

			_sv = new ScrollViewer
			{
				Content = stage,
				VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
				HorizontalScrollMode = ScrollMode.Disabled,
			};
			root.Children.Add(_sv);

			_offset = 0;
			_dir = 1;
			return root;
		}

		protected override void Tick(long frame)
		{
			// Scroll only: the gauges re-SHADE every frame without being re-RECORDED, which is what keeps the
			// measurement on the GPU rather than on op building.
			AdvanceScroll(_sv, ref _offset, ref _dir, 5.0);
		}
	}
}
