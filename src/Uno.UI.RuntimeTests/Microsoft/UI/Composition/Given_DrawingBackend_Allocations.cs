#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;
#if HAS_UNO && __SKIA__
using System;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Composition.Drawing;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using Colors = Microsoft.UI.Colors;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

/// <summary>
/// The managed memory a WebGPU repaint costs. Content is re-recorded whenever it changes (a selection moves, a caret
/// blinks), and the backend then builds the recording's geometry, so what each step allocates has to follow the
/// draw calls made, not the glyphs or fills they carry.
/// </summary>
[TestClass]
[RunsOnUIThread]
[BackendCondition(ConditionMode.Include, RuntimeTestBackends.WebGpu)]
public class Given_DrawingBackend_Allocations
{
#if HAS_UNO && __SKIA__
	private const int Width = 800, Height = 600;

	// Every bound below is on GROWTH: the bytes a large run adds over a small run of the same content, divided by
	// the items it adds. Fixed costs (the frame, the surface, the run's own objects) cancel, so the bounds hold
	// wherever those differ: a 64-bit runtime, WebAssembly's 32-bit references, and the Mono interpreter and AOT of
	// the mobile targets, none of which allocate per item where the desktop JIT does not.

	// Per glyph, a run keeps one copy of its instance (a geometry reference and a Vector2: 16 bytes on a 64-bit
	// runtime, 12 on WebAssembly); everything else is per run, or per batch of up to 256 glyphs (an op, a vertex
	// range and its site data: about 10 bytes a glyph). Measured: 26 to 28 on desktop, 17 on WebAssembly.
	private const double MaxBytesPerGlyph = 36;

	// Per character, on top of the glyph run above: the text engine's layout walk (about 50 bytes, which the Skia
	// backend pays as well) and the shared glyph-run builder's element (32 bytes on 64-bit). The test's text is
	// Latin in one font, so a character is a glyph on every target. Measured: 104 on desktop, 81 on WebAssembly.
	private const double MaxTextBytesPerCharacter = 200;

	// The recording keeps a command per DrawRect (the fill's corners, colour and clip: about 140 bytes), so only
	// building it is held to nothing per fill. Measured: 0.
	private const double MaxBuildBytesPerFill = 8;

	// A replay of unchanged content builds nothing, so it costs what a frame with nothing in it costs, plus its
	// reference to the recording.
	private const long MaxReplayOverEmptyFrame = 1024;

	// Between replaying 2,000 and 20,000 unchanged glyphs: the replay reads the built geometry and adds nothing per
	// glyph, so only noise of a few objects may differ.
	private const long MaxReplayGrowth = 1024;

	// Fill content unique to this process and to each recording, retries included: the arena shares the geometry of
	// identical fill-only recordings, and content an earlier test recorded would leave nothing to build.
	private static readonly Windows.UI.Color _fillColor = Windows.UI.Color.FromArgb(255, (byte)Random.Shared.Next(256), (byte)Random.Shared.Next(256), (byte)Random.Shared.Next(256));
	private static int _fillPhase;

	[TestMethod]
	public async Task When_Glyph_Run_ReRecorded_Then_Allocation_Does_Not_Grow_Per_Glyph()
	{
		var factory = await LoadFactory();
		var shapes = GlyphShapes();
		var small = Placed(shapes, 2_000);
		var large = Placed(shapes, 20_000);

		var smallBytes = await MinBytes(() => RecordAndRender(factory, small));
		var largeBytes = await MinBytes(() => RecordAndRender(factory, large));

		var perGlyph = (largeBytes - smallBytes) / (double)(large.Length - small.Length);
		Assert.IsTrue(perGlyph <= MaxBytesPerGlyph, $"Re-recording and building a glyph run allocates {perGlyph:0.0} bytes per glyph ({smallBytes} B for {small.Length}, {largeBytes} B for {large.Length}); at most {MaxBytesPerGlyph} expected.");
	}

	[TestMethod]
	public async Task When_Unchanged_Run_Replayed_Then_Nothing_Is_Allocated_Per_Glyph()
	{
		var factory = await LoadFactory();
		var shapes = GlyphShapes();
		using var small = Record(factory, Placed(shapes, 2_000));
		using var large = Record(factory, Placed(shapes, 20_000));

		using var empty = factory.CreateRecording().Finish();

		var emptyBytes = await MinBytes(() => Render(factory, empty));
		var smallBytes = await MinBytes(() => Render(factory, small));
		var largeBytes = await MinBytes(() => Render(factory, large));

		Assert.IsTrue(largeBytes - smallBytes <= MaxReplayGrowth, $"Replaying an unchanged recording allocates {smallBytes} B for 2,000 glyphs and {largeBytes} B for 20,000; at most {MaxReplayGrowth} B more expected.");
		Assert.IsTrue(largeBytes - emptyBytes <= MaxReplayOverEmptyFrame, $"Replaying an unchanged recording allocates {largeBytes} B, against {emptyBytes} B for a frame with nothing in it; at most {MaxReplayOverEmptyFrame} B more expected.");
	}

	[TestMethod]
	public async Task When_Fills_ReRecorded_Then_Building_Them_Allocates_Nothing_Per_Fill()
	{
		var factory = await LoadFactory();
		// Recorded outside the measure, and moved a little each time, retries included: the arena shares the geometry
		// of identical fill-only recordings, which would leave nothing to build.
		Task<long> BuildBytes(int count) => MinBytes(() => RecordFills(factory, count, ++_fillPhase, _fillColor), record => Render(factory, record));

		var small = await BuildBytes(1_000);
		var large = await BuildBytes(10_000);

		var perFill = (large - small) / 9_000.0;
		Assert.IsTrue(perFill <= MaxBuildBytesPerFill, $"Building recorded fills allocates {perFill:0.0} bytes per fill ({small} B for 1,000, {large} B for 10,000); at most {MaxBuildBytesPerFill} expected.");
	}

	[TestMethod]
	public async Task When_Long_Text_Selection_Changes_Then_Repaint_Allocation_Is_Bounded_Per_Character()
	{
		async Task<long> RepaintBytes(int length)
		{
			var text = new TextBlock { IsTextSelectionEnabled = true, TextWrapping = TextWrapping.Wrap, Text = Words(length) };
			var root = new ScrollViewer { Width = 480, Height = 320, Content = text };
			await UITestHelper.Load(root);
			var factory = root.Visual.CompositionTarget!.Renderer!;
			var k = 0;
			return await MinBytes(() =>
			{
				k++;
				text.Selection = new TextBlock.Range(k % 50, k % 50 + 3);
				factory.RenderOffscreen(480, 320, session => root.Visual.RenderRootVisual(session, offsetOverride: Vector2.Zero)).Dispose();
			});
		}

		var small = await RepaintBytes(2_000);
		var large = await RepaintBytes(20_000);

		var perCharacter = (large - small) / 18_000.0;
		Assert.IsTrue(perCharacter <= MaxTextBytesPerCharacter, $"A repaint after a selection change allocates {perCharacter:0.0} bytes per character ({small} B at 2,000 characters, {large} B at 20,000); at most {MaxTextBytesPerCharacter} expected.");
	}

	// Content large enough to cross what a build splits it into: glyphs past several 1 MB vertex chunks and spread
	// over several atlas pages, and rects past one coalesced op and one chunk. Every item is checked against what it
	// is known to draw, so a range bound to the wrong chunk or a batch sampling the wrong page shows up as items
	// drawn wrong or not at all.
	[TestMethod]
	public async Task When_Content_Spans_Chunks_And_Atlas_Pages_Then_Every_Item_Is_Drawn()
	{
		var factory = await LoadFactory();

		// 12,000 glyphs of 12 x 12 pixels, each three by three cells with one cell left empty, at whole pixels. Each
		// differs from the others in its last edge by a small fraction, so each is an atlas entry of its own: about
		// 210 texels, so the glyphs need three pages, and 48 floats a quad, so their vertices need three chunks.
		const int Glyphs = 12_000, Columns = 100, Pitch = 14;
		var glyphs = new PathInstance[Glyphs];
		for (var i = 0; i < Glyphs; i++)
		{
			glyphs[i] = new PathInstance(CellGlyph(hole: i % 9, variant: i % 4000), new Vector2(1 + (i % Columns) * Pitch, 1 + (i / Columns) * Pitch));
		}
		var glyphWidth = Columns * Pitch;
		var glyphHeight = (Glyphs / Columns) * Pitch;
		var recorder = factory.CreateRecording();
		recorder.DrawRect(new Rect(0, 0, glyphWidth, glyphHeight), Colors.White);
		recorder.DrawPaths(glyphs, Colors.Black);
		var wrongGlyphs = await CountWrong(factory, recorder.Finish(), glyphWidth, glyphHeight, Glyphs, (pixels, i) =>
		{
			int x0 = 1 + (i % Columns) * Pitch, y0 = 1 + (i / Columns) * Pitch;
			for (var cell = 0; cell < 9; cell++)
			{
				var expected = cell == i % 9 ? Colors.White : Colors.Black;
				if (!Near(pixels, glyphWidth, x0 + (cell % 3) * 4 + 2, y0 + (cell / 3) * 4 + 2, expected))
				{
					return true;
				}
			}
			return false;
		});

		// 2,000 rects in five colours: past the 1,985 rects one op takes, and past one chunk.
		const int Rects = 2_000, RectColumns = 50, RectPitch = 10;
		var palette = new[] { Colors.Red, Colors.Lime, Colors.Blue, Colors.Magenta, Colors.Cyan };
		var rectWidth = RectColumns * RectPitch;
		var rectHeight = (Rects / RectColumns) * RectPitch;
		recorder = factory.CreateRecording();
		recorder.DrawRect(new Rect(0, 0, rectWidth, rectHeight), Colors.White);
		for (var i = 0; i < Rects; i++)
		{
			recorder.DrawRect(new Rect((i % RectColumns) * RectPitch + 1, (i / RectColumns) * RectPitch + 1, 8, 8), palette[i % palette.Length]);
		}
		var wrongRects = await CountWrong(factory, recorder.Finish(), rectWidth, rectHeight, Rects,
			(pixels, i) => !Near(pixels, rectWidth, (i % RectColumns) * RectPitch + 5, (i / RectColumns) * RectPitch + 5, palette[i % palette.Length]));

		Assert.AreEqual(0, wrongGlyphs, $"{wrongGlyphs} of {Glyphs} glyphs were drawn wrong.");
		Assert.AreEqual(0, wrongRects, $"{wrongRects} of {Rects} rects were drawn wrong.");
	}

	private static async Task<int> CountWrong(IDrawingFactory factory, IRenderRecord record, int width, int height, int count, Func<byte[], int, bool> isWrong)
	{
		using (record)
		{
			using var texture = factory.RenderOffscreen(width, height, session => record.Replay(session));
			using var image = await factory.SnapshotAsync(texture);
			var pixels = new byte[width * height * 4];
			image.CopyPixels(pixels);
			var wrong = 0;
			for (var i = 0; i < count; i++)
			{
				if (isWrong(pixels, i))
				{
					wrong++;
				}
			}
			return wrong;
		}
	}

	// BGRA, as snapshots are.
	private static bool Near(byte[] pixels, int width, int x, int y, Windows.UI.Color expected)
	{
		var at = (y * width + x) * 4;
		return Math.Abs(pixels[at] - expected.B) <= 40 && Math.Abs(pixels[at + 1] - expected.G) <= 40 && Math.Abs(pixels[at + 2] - expected.R) <= 40;
	}

	// Three by three cells of 4 pixels, one left empty; the last cell's far edge moved by a fraction too small to
	// see, so each variant has an outline (and so an atlas entry) of its own.
	private static IGeometry CellGlyph(int hole, int variant)
	{
		var builder = GeometryFactory.Current.CreatePathBuilder();
		var last = hole == 8 ? 7 : 8;
		for (var cell = 0; cell < 9; cell++)
		{
			if (cell == hole)
			{
				continue;
			}
			float x = (cell % 3) * 4, y = (cell / 3) * 4;
			var right = x + 4 + (cell == last ? (variant + 1) * 1e-5f : 0f);
			builder.MoveTo(new Vector2(x, y));
			builder.LineTo(new Vector2(right, y));
			builder.LineTo(new Vector2(right, y + 4));
			builder.LineTo(new Vector2(x, y + 4));
			builder.Close();
		}
		return builder.Build();
	}

	private static async Task<IDrawingFactory> LoadFactory()
	{
		var host = new Border { Width = 20, Height = 20, Background = new SolidColorBrush(Colors.White) };
		await UITestHelper.Load(host);
		return host.Visual.CompositionTarget!.Renderer!;
	}

	// The smallest of several runs, a frame between each: offscreen renders with no frame in between pile up on the
	// device. The first runs bake the atlas and warm the pools.
	private static Task<long> MinBytes(Action action)
		=> MinBytes<object?>(() => null, _ => action());

	// As above, with what the action works on made outside the measure and disposed after it.
	private static async Task<long> MinBytes<T>(Func<T> setup, Action<T> action)
	{
		var min = long.MaxValue;
		for (var i = 0; i < 6; i++)
		{
			await NextFrame();
			var subject = setup();
			var before = GC.GetAllocatedBytesForCurrentThread();
			action(subject);
			var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
			(subject as IDisposable)?.Dispose();
			if (i >= 2)
			{
				min = Math.Min(min, bytes);
			}
		}
		return min;
	}

	private static async Task NextFrame()
	{
		try
		{
			await UITestHelper.WaitForRender(1, 200);
		}
		catch (Exception)
		{
			// No frame was due; the wait still let the device retire its work.
		}
	}

	// Glyph-sized outlines with curves, so they are drawn from the coverage atlas as text is.
	private static IGeometry[] GlyphShapes()
	{
		var shapes = new IGeometry[26];
		for (var i = 0; i < shapes.Length; i++)
		{
			var w = 4f + i % 5;
			var builder = GeometryFactory.Current.CreatePathBuilder();
			builder.MoveTo(new Vector2(0, 0));
			builder.CubicTo(new Vector2(w, -2), new Vector2(w + 1, 4 + i % 3), new Vector2(w, 9));
			builder.LineTo(new Vector2(1, 9));
			builder.Close();
			shapes[i] = builder.Build();
		}
		return shapes;
	}

	private static PathInstance[] Placed(IGeometry[] shapes, int count)
	{
		var placed = new PathInstance[count];
		for (var i = 0; i < count; i++)
		{
			placed[i] = new PathInstance(shapes[(i * 7) % shapes.Length], new Vector2((i % 80) * 9.37f, (i / 80) * 12 % Height));
		}
		return placed;
	}

	private static IRenderRecord Record(IDrawingFactory factory, PathInstance[] glyphs)
	{
		var recorder = factory.CreateRecording();
		recorder.DrawPaths(glyphs, Colors.Black);
		return recorder.Finish();
	}

	private static void RecordAndRender(IDrawingFactory factory, PathInstance[] glyphs)
	{
		using var record = Record(factory, glyphs);
		Render(factory, record);
	}

	private static IRenderRecord RecordFills(IDrawingFactory factory, int count, int phase, Windows.UI.Color color)
	{
		var recorder = factory.CreateRecording();
		for (var i = 0; i < count; i++)
		{
			recorder.DrawRect(new Rect((i % 100) * 4 + phase / 64.0, (i / 100) * 4 % Height, 3, 3), color);
		}
		return recorder.Finish();
	}

	// The recording nested as a visual's content is, so it is built into geometry it keeps.
	private static void Render(IDrawingFactory factory, IRenderRecord record)
		=> factory.RenderOffscreen(Width, Height, session => record.Replay(session)).Dispose();

	private static string Words(int length)
	{
		var words = "lorem ipsum dolor sit amet consectetur adipiscing elit sed do eiusmod tempor incididunt ut labore".Split(' ');
		var text = new StringBuilder();
		for (var i = 0; text.Length < length; i++)
		{
			text.Append(words[i * 7 % words.Length]).Append(' ');
		}
		text.Length = length;
		return text.ToString();
	}
#endif
}
