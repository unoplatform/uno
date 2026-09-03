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
	/// TV / game-launcher archetype: a grid of tiles under a moving spotlight, with a full-viewport vignette over
	/// the top — the shape of a console dashboard or a smart-TV home screen.
	/// <para>
	/// GPU-bound by construction, and unlike the scrolling scenes the expensive part MOVES every frame: the
	/// spotlight is a large radial gradient that travels across the wall, and each tile carries a diagonal
	/// gradient sheen. Nothing re-records — only the spotlight's position animates — so the frame cost is
	/// per-pixel shading, not op building.
	/// </para>
	/// The full-viewport vignette on top means every pixel is shaded at least twice, which is what a real
	/// launcher does and what makes this a fill-rate scene rather than a geometry one.
	/// </summary>
	[Sample("Performance", Name = "RealWorld_SpotlightMenu", Description = "Real-UI perf: a launcher tile wall under a moving radial spotlight and a full-viewport vignette — GPU-bound per-pixel shading with an animated light. Compare fps/frame-time across Skia and the WebGPU/ProGPU builds.")]
	public sealed class RealWorld_SpotlightMenu : PerfBenchBase
	{
		private TranslateTransform _spot = null!;
		private double _phase;
		private double _spanX, _spanY;

		protected override string ScenarioName => "SpotlightMenu";

		/// <summary>Number of tiles on the wall.</summary>
		protected override int DefaultCount => 60;

		protected override UIElement BuildStage(int count)
		{
			var rng = new Rng(3307);
			var root = new Grid { Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x07, 0x09, 0x0F)) };

			const double TileW = 240;
			const double TileH = 150;
			const double Gap = 16;
			var perRow = 5;

			var wall = new Canvas();
			for (var i = 0; i < count; i++)
			{
				var left = (i % perRow) * (TileW + Gap);
				var top = (i / perRow) * (TileH + Gap);
				var hue = rng.Color(0xFF);

				// Tile body: a diagonal gradient, so the whole tile is shaded per pixel rather than flat-filled.
				var body = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 1) };
				body.GradientStops.Add(new GradientStop { Offset = 0, Color = Color.FromArgb(0xFF, hue.R, hue.G, hue.B) });
				body.GradientStops.Add(new GradientStop { Offset = 0.55, Color = Color.FromArgb(0xC0, (byte)(hue.R / 2), (byte)(hue.G / 2), (byte)(hue.B / 2)) });
				body.GradientStops.Add(new GradientStop { Offset = 1, Color = Color.FromArgb(0xFF, 0x12, 0x16, 0x1E) });

				var tile = new Rectangle
				{
					Width = TileW,
					Height = TileH,
					RadiusX = 14,
					RadiusY = 14,
					Fill = body,
				};
				Canvas.SetLeft(tile, left);
				Canvas.SetTop(tile, top);
				wall.Children.Add(tile);

				// Sheen: a second translucent gradient over the same area — the "glass" highlight launchers use.
				var sheen = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0.7, 1) };
				sheen.GradientStops.Add(new GradientStop { Offset = 0, Color = Color.FromArgb(0x50, 0xFF, 0xFF, 0xFF) });
				sheen.GradientStops.Add(new GradientStop { Offset = 0.45, Color = Color.FromArgb(0x10, 0xFF, 0xFF, 0xFF) });
				sheen.GradientStops.Add(new GradientStop { Offset = 1, Color = Color.FromArgb(0, 0xFF, 0xFF, 0xFF) });

				var gloss = new Rectangle { Width = TileW, Height = TileH * 0.7, RadiusX = 14, RadiusY = 14, Fill = sheen };
				Canvas.SetLeft(gloss, left);
				Canvas.SetTop(gloss, top);
				wall.Children.Add(gloss);

				var label = new TextBlock
				{
					Text = "Title " + (i + 1),
					FontSize = 16,
					Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0xF0, 0xF4, 0xFA)),
				};
				Canvas.SetLeft(label, left + 16);
				Canvas.SetTop(label, top + TileH - 30);
				wall.Children.Add(label);
			}
			root.Children.Add(wall);

			// The spotlight: one large radial gradient that MOVES. It re-shades a big area every frame without
			// anything being re-recorded, which is the cleanest way to keep a scene in `draw`.
			_spanX = perRow * (TileW + Gap);
			_spanY = (count / perRow + 1) * (TileH + Gap);

			var light = new RadialGradientBrush
			{
				Center = new Point(0.5, 0.5),
				GradientOrigin = new Point(0.5, 0.5),
				RadiusX = 0.5,
				RadiusY = 0.5,
			};
			light.GradientStops.Add(new GradientStop { Offset = 0, Color = Color.FromArgb(0x8C, 0xFF, 0xF2, 0xC8) });
			light.GradientStops.Add(new GradientStop { Offset = 0.45, Color = Color.FromArgb(0x3A, 0xFF, 0xE0, 0xA0) });
			light.GradientStops.Add(new GradientStop { Offset = 1, Color = Color.FromArgb(0, 0xFF, 0xD0, 0x80) });

			// SEVERAL large lights, not one: a single spotlight shades too little of the viewport to be GPU-bound,
			// and an ambient wash of overlapping lights is what these launchers actually look like.
			_spot = new TranslateTransform();
			for (var l = 0; l < 12; l++)
			{
				var lamp = new Ellipse
				{
					Width = 1500,
					Height = 1500,
					Fill = light,
					IsHitTestVisible = false,
					RenderTransform = _spot,
				};
				Canvas.SetLeft(lamp, (l % 4) * 340 - 260);
				Canvas.SetTop(lamp, (l / 4) * 320 - 200);
				wall.Children.Add(lamp);
			}

			// Vignette over everything: guarantees a second shading pass across the whole viewport.
			var vig = new RadialGradientBrush
			{
				Center = new Point(0.5, 0.5),
				GradientOrigin = new Point(0.5, 0.5),
				RadiusX = 0.75,
				RadiusY = 0.75,
			};
			vig.GradientStops.Add(new GradientStop { Offset = 0, Color = Color.FromArgb(0, 0, 0, 0) });
			vig.GradientStops.Add(new GradientStop { Offset = 0.6, Color = Color.FromArgb(0x40, 0, 0, 0) });
			vig.GradientStops.Add(new GradientStop { Offset = 1, Color = Color.FromArgb(0xC8, 0, 0, 0) });
			root.Children.Add(new Rectangle { Fill = vig, IsHitTestVisible = false });

			_phase = 0;
			return root;
		}

		protected override void Tick(long frame)
		{
			// Move the light on a Lissajous path so it sweeps the whole wall without repeating a straight line.
			_phase += 0.035;
			_spot.X = (System.Math.Sin(_phase) * 0.5 + 0.5) * System.Math.Max(_spanX - 900, 0);
			_spot.Y = (System.Math.Sin(_phase * 0.7) * 0.5 + 0.5) * System.Math.Max(_spanY - 900, 0);
		}
	}
}
