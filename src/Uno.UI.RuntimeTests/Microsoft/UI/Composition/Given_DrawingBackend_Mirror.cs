#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using Windows.UI;
using Colors = Microsoft.UI.Colors;
#if HAS_UNO
using Microsoft.UI.Composition;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

/// <summary>
/// Content under a mirror, the transform every right-to-left subtree is drawn under: each scene is compared with the
/// same content drawn without the mirror and flipped in the test, and on WebGPU it must still be served by the
/// coverage atlas.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class Given_DrawingBackend_Mirror
{
	private const int W = 240, H = 100;

	// Room for the big star at 1.25.
	private const int BigW = 720, BigH = 300;

	// Asymmetric glyphs, so text drawn the wrong way round cannot match.
	private const string Text = "Fly 7RJ";

	// WebGPU bakes a mirrored mask from the same outline at the same phase as the unmirrored one and samples it
	// mirrored, texel on pixel, so the two renders agree pixel for pixel: glyphs and small fills exactly. A big fill
	// keys its exact phase, which the mirror reaches through a negated translation in float, so its coverage can be
	// a level off (measured: 10 of 24437 pixels, by 1). A pixel shift reaches 255.
	private const int MirroredTolerance = 1;

	// A counter-mirrored glyph's outline is mirrored in its recording, so its mask snaps its subpixel phase (to a
	// quarter pixel) at the glyph's other edge: each edge can land up to an eighth of a pixel off the plain one,
	// about 32 levels, twice where two edges share a pixel (measured: 60). A pixel shift reaches 255.
	private const int CounterMirroredTolerance = 64;

	[TestMethod]
	[RequiresScaling(1f)]
	[DataRow(1f, 0f, 0f, true, false)]
	[DataRow(1.25f, 0f, 0f, true, false)]
	[DataRow(1.5f, 0f, 0f, true, false)]
	[DataRow(1.75f, 0f, 0f, true, false)]
	[DataRow(1f, 0.3f, 0.6f, true, false)]
	[DataRow(1f, 0f, 0f, false, true)]
	[DataRow(1f, 0f, 0f, true, true)]
	[DataRow(1.25f, 0.3f, 0.6f, true, true)]
	public async Task When_Text_Mirrored_Then_Matches_Flipped_Text(float scale, float tx, float ty, bool mirrorX, bool mirrorY)
	{
		var label = Label(centered: true);
		var actual = await Shoot(Frame(Placed(label, scale, tx, ty, mirrorX, mirrorY)));
		AssertServedByAtlas(label, glyphs: true);

		var expected = await Shoot(Frame(Placed(Label(centered: true), scale, tx, ty, false, false)));

		AssertSameInk(expected, actual, mirrorX, mirrorY);
	}

	[TestMethod]
	[RequiresScaling(1f)]
	[DataRow(false, 1f, 0f, 0f, true, false)]
	[DataRow(false, 1.25f, 0f, 0f, true, false)]
	[DataRow(false, 1.5f, 0f, 0f, true, false)]
	[DataRow(false, 1.75f, 0f, 0f, true, false)]
	[DataRow(false, 1f, 0.3f, 0.6f, true, false)]
	[DataRow(false, 1f, 0f, 0f, false, true)]
	[DataRow(false, 1f, 0f, 0f, true, true)]
	[DataRow(false, 1.25f, 0.3f, 0.6f, true, true)]
	[DataRow(true, 1f, 0f, 0f, true, false)]
	[DataRow(true, 1f, 0.3f, 0.6f, true, false)]
	[DataRow(true, 1.25f, 0.3f, 0.6f, true, true)]
	public async Task When_Shape_Mirrored_Then_Matches_Flipped_Shape(bool big, float scale, float tx, float ty, bool mirrorX, bool mirrorY)
	{
		var star = Star(big);
		var actual = await Shoot(Frame(Placed(star, scale, tx, ty, mirrorX, mirrorY), big));
		AssertServedByAtlas(star, glyphs: false, bigFill: big);

		var expected = await Shoot(Frame(Placed(Star(big), scale, tx, ty, false, false), big));

		AssertSameInk(expected, actual, mirrorX, mirrorY);
	}

	// A cached recording drawn under one sign, then under the other with the same translation, so the subpixel phase
	// is unchanged and only the sign tells the masks placed for the first sign from what the second needs.
	[TestMethod]
	[RequiresScaling(1f)]
	[DataRow("text", true)]
	[DataRow("text", false)]
	[DataRow("big", true)]
	[DataRow("big", false)]
	public async Task When_Mirror_Toggled_Then_Matches_Fresh_Content(string kind, bool startMirrored)
	{
		var big = kind == "big";
		FrameworkElement Content() => big ? Star(big: true) : Label(centered: true);
		var transform = new CompositeTransform { ScaleX = startMirrored ? -1 : 1, TranslateX = 0.3, TranslateY = 0.6 };
		var frame = Frame(new Grid { RenderTransformOrigin = new Point(0.5, 0.5), RenderTransform = transform, Children = { Content() } }, big);
		await Shoot(frame);

		transform.ScaleX = -transform.ScaleX;
		var actual = await Shoot(frame);

		// The unmirrored placement that the end state reflects about the centre line.
		var expected = await Shoot(Frame(Placed(Content(), 1f, startMirrored ? 0.3f : -0.3f, 0.6f, false, false), big));

		AssertSameInk(expected, actual, !startMirrored, false);
	}

#if HAS_UNO && __SKIA__
	// A right-to-left text control mirrors its element and draws its text under a counter-mirror, so the glyphs come
	// out the right way round: the replay transform is a flip, the recording's own transform cancels it.
	[TestMethod]
	[RequiresScaling(1f)]
	public async Task When_Text_CounterMirrored_Under_Flip_Then_Matches_Plain_Text()
	{
		var (mirrored, visual) = CounterMirrored(mirrored: true);
		var actual = await Shoot(Frame(mirrored));
		AssertServedByAtlas(visual, glyphs: true);

		var expected = await Shoot(Frame(CounterMirrored(mirrored: false).Element));

		AssertSameInk(expected, actual, false, false, CounterMirroredTolerance);
	}

	private static (FrameworkElement Element, Visual Visual) CounterMirrored(bool mirrored)
	{
		// The source of the glyphs only: it is laid out but not drawn, the visual draws it.
		var label = Label(centered: false);
		label.Opacity = 0;
		var surface = new Grid { Width = W, Height = H, Children = { label } };
		var visual = new TextSurfaceVisual(surface.Visual.Compositor, label, mirrored) { Size = new Vector2(W, H) };
		ElementCompositionPreview.SetElementChildVisual(surface, visual);
		return (mirrored ? Placed(surface, 1f, 0f, 0f, true, false) : surface, visual);
	}

	private sealed class TextSurfaceVisual : ContainerVisual
	{
		private readonly TextBlock _text;
		private readonly bool _counterMirror;

		public TextSurfaceVisual(Compositor compositor, TextBlock text, bool counterMirror) : base(compositor)
		{
			_text = text;
			_counterMirror = counterMirror;
		}

		internal override void Paint(in PaintingSession session)
		{
			session.Session.Save();
			if (_counterMirror)
			{
				session.Session.Translate(Size.X, 0);
				session.Session.Scale(-1, 1);
			}
			_text.Draw(in session);
			session.Session.Restore();
		}

		internal override bool CanPaint() => true;

		internal override bool PaintsWithinOwnSize => true;
	}
#endif

	private static TextBlock Label(bool centered)
		=> new()
		{
			Text = Text,
			FontSize = 32,
			Foreground = new SolidColorBrush(Colors.Blue),
			HorizontalAlignment = centered ? HorizontalAlignment.Center : HorizontalAlignment.Left,
			VerticalAlignment = centered ? VerticalAlignment.Center : VerticalAlignment.Top,
			Padding = new Thickness(6, 4, 0, 0),
		};

	// Self-overlapping and lopsided: its fill has no tiling triangles, so it draws through a coverage mask. A big
	// one is past the atlas's shared pages, so it draws through a mask of its own (the big-fill route).
	private static Path Star(bool big = false)
	{
		var k = big ? 4.5 : 1;
		return new()
		{
			Fill = new SolidColorBrush(Colors.Blue),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Data = new PathGeometry
			{
				Figures =
				{
					new PathFigure
					{
						StartPoint = new Point(12 * k, 52 * k),
						IsClosed = true,
						IsFilled = true,
						Segments =
						{
							new LineSegment { Point = new Point(70 * k, 6 * k) },
							new LineSegment { Point = new Point(96 * k, 50 * k) },
							new LineSegment { Point = new Point(8 * k, 20 * k) },
							new LineSegment { Point = new Point(120 * k, 28 * k) },
						},
					},
				},
			},
		};
	}

	// Scaled about the frame's centre and moved; a mirrored axis negates both, so the result is the unmirrored
	// placement reflected about the centre line, which is what the flipped reference shows.
	private static CompositeTransform Transform(float scale, float tx, float ty, bool mirrorX, bool mirrorY)
		=> new()
		{
			ScaleX = mirrorX ? -scale : scale,
			ScaleY = mirrorY ? -scale : scale,
			TranslateX = mirrorX ? -tx : tx,
			TranslateY = mirrorY ? -ty : ty,
		};

	private static FrameworkElement Placed(FrameworkElement content, float scale, float tx, float ty, bool mirrorX, bool mirrorY)
		=> new Grid
		{
			RenderTransformOrigin = new Point(0.5, 0.5),
			RenderTransform = Transform(scale, tx, ty, mirrorX, mirrorY),
			Children = { content },
		};

	private static Border Frame(UIElement content, bool big = false)
		=> new() { Width = big ? BigW : W, Height = big ? BigH : H, Background = new SolidColorBrush(Colors.White), Child = content };

	// The frame sits under a transparent host, so the window never draws it: only the screenshot builds its
	// recordings' cache entries, always at the screenshot's scale and placement, and a second shot of the same frame
	// replays what the first built.
	private static async Task<RawBitmap> Shoot(FrameworkElement frame)
	{
		if (frame.Parent is null)
		{
			await UITestHelper.Load(new Grid { Opacity = 0, Children = { frame } });
		}
		var bitmap = await UITestHelper.ScreenShot(frame, scaling: UITestHelper.ScreenShotScalingMode.UsePhysicalPixels);
		await bitmap.Populate();
		return bitmap;
	}

	private static void AssertSameInk(RawBitmap expected, RawBitmap actual, bool flipX, bool flipY, int tolerance = MirroredTolerance)
	{
		Assert.AreEqual(expected.Width, actual.Width);
		Assert.AreEqual(expected.Height, actual.Height);

		var (ink, mismatches, maxDiff) = CompareInk(expected, actual, flipX, flipY, exact: IsWebGpu, tolerance);
		Assert.IsTrue(ink > 200, $"Too little was drawn to compare ({ink} pixels of ink).");
		if (IsWebGpu)
		{
			Assert.AreEqual(0, mismatches, $"{mismatches} of {ink} ink pixels differ from the reference by more than {tolerance} (largest difference {maxDiff}).");
		}
		else
		{
			// Skia rasterizes glyphs for the device transform, with its own hinting and subpixel positioning, so a
			// mirrored glyph is not the mirror image of the unmirrored one: edges differ by a pixel and by many levels.
			Assert.IsTrue(mismatches <= ink * 6 / 100, $"{mismatches} of {ink} ink pixels have no match within a pixel.");
		}

		// The same comparison against the unmirrored reference must fail by far, so a scene drawn the wrong way
		// round can't pass.
		if (flipX || flipY)
		{
			var (wrongInk, wrongMismatches, _) = CompareInk(expected, actual, false, false, exact: false, tolerance);
			Assert.IsTrue(wrongMismatches > wrongInk * 25 / 100, $"The scene is too symmetric to tell a mirror apart ({wrongMismatches} of {wrongInk}).");
		}
	}

	// Exact: each pixel against the reference pixel it maps to, within `tolerance`. Otherwise each pixel needs a pixel of the other
	// image within one pixel of it and close in colour.
	private static (int Ink, int Mismatches, int MaxDiff) CompareInk(RawBitmap expected, RawBitmap actual, bool flipX, bool flipY, bool exact, int tolerance)
	{
		int w = expected.Width, h = expected.Height;
		Color Expected(int x, int y) => expected.GetPixel(flipX ? w - 1 - x : x, flipY ? h - 1 - y : y);

		int ink = 0, mismatches = 0, maxDiff = 0;
		for (var y = 0; y < h; y++)
		{
			for (var x = 0; x < w; x++)
			{
				var e = Expected(x, y);
				var a = actual.GetPixel(x, y);
				if (!IsInk(e) && !IsInk(a))
				{
					continue;
				}
				ink++;
				if (exact)
				{
					var d = Diff(e, a);
					maxDiff = Math.Max(maxDiff, d);
					if (d > tolerance)
					{
						mismatches++;
					}
				}
				else if (!HasNear(actual.GetPixel, w, h, x, y, e) || !HasNear(Expected, w, h, x, y, a))
				{
					mismatches++;
				}
			}
		}
		return (ink, mismatches, maxDiff);
	}

	private static bool IsInk(Color c) => c.R < 200;

	private static int Diff(Color a, Color b) => Math.Max(Math.Abs(a.R - b.R), Math.Max(Math.Abs(a.G - b.G), Math.Abs(a.B - b.B)));

	private static bool HasNear(Func<int, int, Color> image, int w, int h, int x, int y, Color c)
	{
		const int Tolerance = 64;
		for (var dy = -1; dy <= 1; dy++)
		{
			for (var dx = -1; dx <= 1; dx++)
			{
				int nx = x + dx, ny = y + dy;
				if (nx >= 0 && ny >= 0 && nx < w && ny < h && Diff(image(nx, ny), c) <= Tolerance)
				{
					return true;
				}
			}
		}
		return false;
	}

	// ------------------------------------------------------------------------------------------------ WebGPU atlas

#if HAS_UNO && __SKIA__
	private static bool IsWebGpu => RuntimeTestsBackendHelper.CurrentBackend == RuntimeTestBackends.WebGpu;
#else
	private static bool IsWebGpu => false;
#endif

	// Reads the geometry cache entries the visual's recordings compiled to, whoever built them and whenever: none may
	// have been refused the atlas, and the content must have been served by it (glyphs and small fills as atlas
	// quads, a big fill as a mask of its own).
	private static void AssertServedByAtlas(object target, bool glyphs, bool bigFill = false)
	{
#if HAS_UNO && __SKIA__
		if (!IsWebGpu)
		{
			return;
		}

		var entries = new List<object>();
		Collect(target is UIElement element ? element.Visual : (Visual)target, entries);
		Assert.IsTrue(entries.Count > 0, "No recording was compiled.");
		bool Flag(object e, string name) => (bool)e.GetType().GetField(name)!.GetValue(e)!;
		foreach (var e in entries)
		{
			Assert.IsFalse(Flag(e, "AtlasBlockedByScale"), "The replay transform was refused the coverage atlas.");
		}
		Assert.IsTrue(entries.Exists(e => bigFill ? Flag(e, "HasClipMask") : Flag(e, "HasAtlas")),
			glyphs ? "No glyph was drawn from the coverage atlas." : bigFill ? "The big fill was not drawn through a coverage mask." : "No fill was drawn from the coverage atlas.");

		static void Collect(Visual v, List<object> entries)
		{
			if (v.RecordedContentForTesting is { } record
				&& record.GetType().GetField("Compiled", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(record) is { } entry)
			{
				entries.Add(entry);
			}
			if (v is ContainerVisual container)
			{
				foreach (var child in container.Children)
				{
					Collect(child, entries);
				}
			}
		}
#endif
	}
}
