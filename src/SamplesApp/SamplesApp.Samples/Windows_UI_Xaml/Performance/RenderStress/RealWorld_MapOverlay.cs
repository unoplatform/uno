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
	/// Map/analytics-overlay archetype: large irregular translucent polygons (zones, heat regions, coverage areas)
	/// stacked over a grid, with stroked routes across them.
	/// <para>
	/// Covers a GPU axis none of the other real-world samples do: ANTI-ALIASED PATH FILLS over large areas. A big
	/// many-sided polygon costs coverage computation per fragment, and the shapes deliberately overlap so the
	/// translucent blends compound. Distinct from <c>RealWorld_OverlayStack</c> (isolation layers),
	/// <c>RealWorld_AcrylicShell</c> (backdrop blur) and <c>RealWorld_HeroScrim</c> (image sampling).
	/// </para>
	/// <c>count</c> is the number of overlay polygons; each is sized as a large fraction of the viewport, so cost
	/// scales with AREA and stays in the draw phase.
	/// </summary>
	[Sample("Performance", Name = "RealWorld_MapOverlay", Description = "Real-UI perf: N large irregular translucent polygons (map zones / heat regions) plus stroked routes, overlapping over a grid. Stresses anti-aliased PATH FILL coverage by area, so it is draw-bound. Compare fps/frame-time across Skia and the WebGPU/ProGPU builds.")]
	public sealed class RealWorld_MapOverlay : PerfBenchBase
	{
		private const int Sides = 44;

		private ScrollViewer _sv = null!;
		private double _offset;
		private int _dir = 1;

		protected override string ScenarioName => "MapOverlay";

		/// <summary>Number of large translucent overlay polygons.</summary>
		protected override int DefaultCount => 18;

		protected override UIElement BuildStage(int count)
		{
			var rng = new Rng(151);
			var root = new Grid();

			// --- Base "map": a cheap grid so there is something under the overlays without costing record time. ---
			var grid = new Canvas { Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x0E, 0x14, 0x1E)) };
			for (var i = 0; i < 60; i++)
			{
				grid.Children.Add(new Rectangle
				{
					Width = 2400,
					Height = 1,
					Fill = new SolidColorBrush(Color.FromArgb(0x30, 0x6A, 0x86, 0xB0)),
					RenderTransform = new TranslateTransform { Y = i * 46 },
				});
			}

			var stage = new Grid { Width = 2400, Height = 2800, Children = { grid } };

			// --- Overlay polygons: big, many-sided, translucent, overlapping. ---
			for (var i = 0; i < count; i++)
			{
				stage.Children.Add(BuildZone(i, rng));
			}

			// --- Routes: long anti-aliased strokes across the same area. ---
			for (var i = 0; i < count / 2; i++)
			{
				stage.Children.Add(BuildRoute(i, rng));
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

		private static UIElement BuildZone(int index, Rng rng)
		{
			// An irregular blob: many vertices so the fill is a genuine path rasterization, not a rounded rect.
			var cx = rng.Range(200, 2000);
			var cy = rng.Range(200, 2500);
			var rx = rng.Range(260, 620);
			var ry = rng.Range(220, 560);

			var pts = new PointCollection();
			for (var s = 0; s < Sides; s++)
			{
				var a = s / (double)Sides * System.Math.PI * 2;
				var wobble = 0.72 + rng.Range(0, 0.5);
				pts.Add(new Point(cx + System.Math.Cos(a) * rx * wobble, cy + System.Math.Sin(a) * ry * wobble));
			}

			return new Polygon
			{
				Points = pts,
				Fill = new SolidColorBrush(Color.FromArgb(0x46, (byte)rng.Range(60, 220), (byte)rng.Range(90, 200), 0xE0)),
				Stroke = new SolidColorBrush(Color.FromArgb(0x90, 0xCF, 0xE4, 0xFF)),
				StrokeThickness = 2,
			};
		}

		private static UIElement BuildRoute(int index, Rng rng)
		{
			var pts = new PointCollection();
			var x = rng.Range(0, 400);
			var y = rng.Range(0, 2600);
			for (var s = 0; s < 26; s++)
			{
				x += rng.Range(40, 110);
				y += rng.Range(-90, 90);
				pts.Add(new Point(x, y));
			}

			return new Polyline
			{
				Points = pts,
				Stroke = new SolidColorBrush(Color.FromArgb(0xC0, 0xFF, 0xC4, 0x6A)),
				StrokeThickness = 6,
			};
		}

		protected override void Tick(long frame)
		{
			// Scroll only: the polygons are re-RASTERIZED (AA coverage over large areas) without being re-RECORDED.
			AdvanceScroll(_sv, ref _offset, ref _dir, 8.0);
		}
	}
}
