#nullable enable

#if __SKIA__

using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI;
using Windows.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests;
using Uno.UI.RuntimeTests.Helpers;
using CommunityToolkit.WinUI.Lottie;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

// Renderer-agnostic: passes on Skottie (default) and on the SkiaSharp-free managed engine (UNO_MANAGED_LOTTIE=1).
// LightBulb.json is a shape-only Lottie (groups, bezier paths, fills, strokes, animated transforms) — the managed
// engine's v1 scope — so a pass under UNO_MANAGED_LOTTIE=1 proves the managed engine renders and animates end-to-end.
[TestClass]
[RunsOnUIThread]
public class Given_ManagedLottie
{
	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
	public async Task When_Lottie_Renders_And_Animates()
	{
		var source = new LottieVisualSource();
		var player = new AnimatedVisualPlayer
		{
			Width = 100,
			Height = 100,
			AutoPlay = false,
			Source = source,
		};
		var host = new Border
		{
			Width = 120,
			Height = 120,
			Background = new SolidColorBrush(Colors.White),
			Child = player,
		};

		try
		{
			await UITestHelper.Load(host);
			await source.SetSourceAsync(new Uri("ms-appx:///Lottie/LightBulb.json"));
			await TestServices.WindowHelper.WaitFor(() => player.IsAnimatedVisualLoaded, timeoutMS: 5000, "LightBulb.json should load.");
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsTrue(player.Duration > TimeSpan.Zero, "The loaded animation should report a duration.");

			player.SetProgress(0.15);
			await TestServices.WindowHelper.WaitForIdle();
			var frameA = await UITestHelper.ScreenShot(host);
			await frameA.Populate();

			player.SetProgress(0.65);
			await TestServices.WindowHelper.WaitForIdle();
			var frameB = await UITestHelper.ScreenShot(host);
			await frameB.Populate();

			var w = (int)host.ActualWidth;
			var h = (int)host.ActualHeight;

			// Drew something: at least one frame differs from the plain white host background.
			Assert.IsTrue(NonBackgroundPixels(frameA, w, h, Colors.White) > 50, "The animation should render visible content, not a blank frame.");
			// Animated: the two progress points are not identical.
			Assert.IsTrue(DifferentPixels(frameA, frameB, w, h) > 50, "Different progress values should render different frames.");
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
	public async Task When_Trim_Animation_Renders_And_Animates()
	{
		// 4930-checkbox-animation.json exercises trim paths (tm) + ellipses + fills/strokes — the managed engine's
		// trim support. Renderer-agnostic: passes on Skottie and on the managed engine (UNO_MANAGED_LOTTIE=1).
		var source = new LottieVisualSource();
		var player = new AnimatedVisualPlayer { Width = 100, Height = 100, AutoPlay = false, Source = source };
		var host = new Border { Width = 120, Height = 120, Background = new SolidColorBrush(Colors.White), Child = player };

		try
		{
			await UITestHelper.Load(host);
			await source.SetSourceAsync(new Uri("ms-appx:///Lottie/4930-checkbox-animation.json"));
			await TestServices.WindowHelper.WaitFor(() => player.IsAnimatedVisualLoaded, timeoutMS: 5000, "checkbox animation should load.");
			await TestServices.WindowHelper.WaitForIdle();

			player.SetProgress(0.3);
			await TestServices.WindowHelper.WaitForIdle();
			var frameA = await UITestHelper.ScreenShot(host);
			await frameA.Populate();

			player.SetProgress(0.9);
			await TestServices.WindowHelper.WaitForIdle();
			var frameB = await UITestHelper.ScreenShot(host);
			await frameB.Populate();

			var w = (int)host.ActualWidth;
			var h = (int)host.ActualHeight;
			Assert.IsTrue(NonBackgroundPixels(frameA, w, h, Colors.White) > 20, "The trim-path animation should render visible content.");
			Assert.IsTrue(DifferentPixels(frameA, frameB, w, h) > 20, "The trim-path animation should animate across progress (trim start/end changing).");
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	// A 40x40 red square on a 20-frame timeline, its fill opacity keyframed in the pre-5.5 form: each keyframe carries
	// its own end ("e") and the track closes on a bare { t } keyframe. The value must hold past it, up to the very end.
	private const string LegacyKeyframesJson = """
		{"v":"5.4.4","fr":10,"ip":0,"op":20,"w":40,"h":40,"layers":[{"ty":4,"ind":1,"ip":0,"op":20,"st":0,
		"ks":{"o":{"a":0,"k":100},"p":{"a":0,"k":[0,0,0]},"a":{"a":0,"k":[0,0,0]},"s":{"a":0,"k":[100,100,100]},"r":{"a":0,"k":0}},
		"shapes":[{"ty":"gr","it":[
			{"ty":"sh","ks":{"a":1,"k":[
				{"t":0,"s":[{"c":true,"v":[[0,0],[40,0],[40,40],[0,40]],"i":[[0,0],[0,0],[0,0],[0,0]],"o":[[0,0],[0,0],[0,0],[0,0]]}],
				       "e":[{"c":true,"v":[[0,0],[40,0],[40,40],[0,40]],"i":[[0,0],[0,0],[0,0],[0,0]],"o":[[0,0],[0,0],[0,0],[0,0]]}]},
				{"t":10}]}},
			{"ty":"fl","c":{"a":0,"k":[1,0,0,1]},"o":{"a":1,"k":[{"t":0,"s":[0],"e":[100]},{"t":10}]}},
			{"ty":"tr","p":{"a":0,"k":[0,0]},"a":{"a":0,"k":[0,0]},"s":{"a":0,"k":[100,100]},"r":{"a":0,"k":0},"o":{"a":0,"k":100}}]}]}]}
		""";

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
	[DataRow(0.75f, DisplayName = "after the last keyframe")]
	[DataRow(1.0f, DisplayName = "at the end of the timeline")]
	public async Task When_Legacy_Keyframes_End_Then_Value_Holds(float progress)
	{
		var pixels = await RenderManagedAsync(LegacyKeyframesJson, progress);

		// The square at full opacity, not the white background.
		Assert.IsTrue(IsRed(pixels, 20, 20), $"Expected the red square at progress {progress}: {Describe(pixels, 20, 20)}.");
	}

	// A red horizontal line across a 40x40 comp, its group followed (not contained) by a trim keeping the first half.
	private const string TrimAfterGroupJson = """
		{"v":"5.4.4","fr":10,"ip":0,"op":20,"w":40,"h":40,"layers":[{"ty":4,"ind":1,"ip":0,"op":20,"st":0,
		"ks":{"o":{"a":0,"k":100},"p":{"a":0,"k":[0,0,0]},"a":{"a":0,"k":[0,0,0]},"s":{"a":0,"k":[100,100,100]},"r":{"a":0,"k":0}},
		"shapes":[
			{"ty":"gr","it":[
				{"ty":"sh","ks":{"a":0,"k":{"c":false,"v":[[0,20],[40,20]],"i":[[0,0],[0,0]],"o":[[0,0],[0,0]]}}},
				{"ty":"st","c":{"a":0,"k":[1,0,0,1]},"o":{"a":0,"k":100},"w":{"a":0,"k":8},"lc":1,"lj":1},
				{"ty":"tr","p":{"a":0,"k":[0,0]},"a":{"a":0,"k":[0,0]},"s":{"a":0,"k":[100,100]},"r":{"a":0,"k":0},"o":{"a":0,"k":100}}]},
			{"ty":"tm","s":{"a":0,"k":0},"e":{"a":0,"k":50},"o":{"a":0,"k":0},"m":1}]}]}
		""";

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
	public async Task When_Trim_Follows_Group_Then_Group_Paths_Are_Trimmed()
	{
		var pixels = await RenderManagedAsync(TrimAfterGroupJson, 0.5f);

		Assert.IsTrue(IsRed(pixels, 10, 20), $"The kept half should be drawn: {Describe(pixels, 10, 20)}.");
		Assert.IsTrue(IsWhite(pixels, 30, 20), $"The trimmed-off half should not be drawn: {Describe(pixels, 30, 20)}.");
	}

	// JSON fragments are written with single quotes (they are mostly braces and quotes) and converted by Json().
	private static string Json(string singleQuoted) => singleQuoted.Replace('\'', '"');

	private const string Transform100 = "'ks':{'o':{'a':0,'k':100},'p':{'a':0,'k':[0,0,0]},'a':{'a':0,'k':[0,0,0]},'s':{'a':0,'k':[100,100,100]},'r':{'a':0,'k':0}}";

	// A filled square group.
	private static string Square(int x, int y, int size, string rgba)
		=> "{'ty':'gr','it':[{'ty':'rc','p':{'a':0,'k':[" + (x + size / 2) + "," + (y + size / 2) + "]},'s':{'a':0,'k':[" + size + "," + size + "]},'r':{'a':0,'k':0}},"
			+ "{'ty':'fl','c':{'a':0,'k':[" + rgba + "]},'o':{'a':0,'k':100}},"
			+ "{'ty':'tr','p':{'a':0,'k':[0,0]},'a':{'a':0,'k':[0,0]},'s':{'a':0,'k':[100,100]},'r':{'a':0,'k':0},'o':{'a':0,'k':100}}]}";

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
	public async Task When_Precomp_With_Split_Position_Then_Asset_Renders_At_It()
	{
		// A 10x10 red square at the origin of an asset, shown by a precomp layer placed at (25, 15) by a split position.
		var json = Json("{'v':'5.4.4','fr':10,'ip':0,'op':20,'w':40,'h':40,"
			+ "'assets':[{'id':'comp_0','layers':[{'ty':4,'ind':1,'ip':0,'op':20,'st':0," + Transform100 + ",'shapes':[" + Square(0, 0, 10, "1,0,0,1") + "]}]}],"
			+ "'layers':[{'ty':0,'ind':1,'refId':'comp_0','ip':0,'op':20,'st':0,'w':40,'h':40,"
			+ "'ks':{'o':{'a':0,'k':100},'a':{'a':0,'k':[0,0,0]},'s':{'a':0,'k':[100,100,100]},'r':{'a':0,'k':0},"
			+ "'p':{'s':true,'x':{'a':0,'k':25},'y':{'a':0,'k':15}}}}]}");

		var pixels = await RenderManagedAsync(json, 0.5f);

		Assert.IsTrue(IsRed(pixels, 30, 20), $"The asset should render at the precomp's position: {Describe(pixels, 30, 20)}.");
		Assert.IsTrue(IsWhite(pixels, 5, 5), $"Nothing should render at the asset's own origin: {Describe(pixels, 5, 5)}.");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
	public async Task When_Groups_Overlap_Then_First_Is_On_Top()
	{
		// Same square twice: blue first in the list, red second. Lottie stacks shapes like layers, first on top.
		var json = Json("{'v':'5.4.4','fr':10,'ip':0,'op':20,'w':40,'h':40,'layers':[{'ty':4,'ind':1,'ip':0,'op':20,'st':0," + Transform100
			+ ",'shapes':[" + Square(10, 10, 20, "0,0,1,1") + "," + Square(10, 10, 20, "1,0,0,1") + "]}]}");

		var pixels = await RenderManagedAsync(json, 0.5f);

		var p = (20 * 40 + 20) * 4;
		Assert.IsTrue(pixels[p] > 200 && pixels[p + 2] < 60, $"The first group should be on top (blue): {Describe(pixels, 20, 20)}.");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
	public async Task When_Layer_Is_Translucent_Then_It_Fades_As_A_Whole()
	{
		// Two identical red squares in a 50% layer: one 50% red over white, not two 50% squares stacking to 75%.
		var json = Json("{'v':'5.4.4','fr':10,'ip':0,'op':20,'w':40,'h':40,'layers':[{'ty':4,'ind':1,'ip':0,'op':20,'st':0,"
			+ "'ks':{'o':{'a':0,'k':50},'p':{'a':0,'k':[0,0,0]},'a':{'a':0,'k':[0,0,0]},'s':{'a':0,'k':[100,100,100]},'r':{'a':0,'k':0}},"
			+ "'shapes':[" + Square(10, 10, 20, "1,0,0,1") + "," + Square(10, 10, 20, "1,0,0,1") + "]}]}");

		var pixels = await RenderManagedAsync(json, 0.5f);

		// 50% red over white is (255, 128, 128); stacking would give (255, 64, 64).
		var p = (20 * 40 + 20) * 4;
		Assert.IsTrue(pixels[p + 1] is > 110 and < 145, $"Expected one 50% red square: {Describe(pixels, 20, 20)}.");
	}

	private static async Task<byte[]> RenderManagedAsync(string json, float progress)
	{
		using var animation = new global::Uno.UI.Composition.Drawing.ManagedLottieRenderer()
			.Load(json, global::Uno.UI.Composition.Drawing.GeometryFactory.Current);
		Assert.IsNotNull(animation, "The animation should load.");

		var factory = global::Uno.UI.Composition.Drawing.DrawingFactory.Current;
		using var texture = factory.RenderOffscreen(40, 40, session =>
		{
			session.Clear(Colors.White);
			animation.Render(session, progress, new global::Windows.Foundation.Rect(0, 0, 40, 40));
		});
		var image = await factory.SnapshotAsync(texture);
		var pixels = new byte[40 * 40 * 4];
		image.CopyPixels(pixels);
		return pixels;
	}

	// BGRA pixels of a 40-wide render.
	private static bool IsRed(byte[] bgra, int x, int y)
	{
		var p = (y * 40 + x) * 4;
		return bgra[p + 2] > 200 && bgra[p + 1] < 60 && bgra[p] < 60;
	}

	private static bool IsWhite(byte[] bgra, int x, int y)
	{
		var p = (y * 40 + x) * 4;
		return bgra[p] > 230 && bgra[p + 1] > 230 && bgra[p + 2] > 230;
	}

	private static string Describe(byte[] bgra, int x, int y)
	{
		var p = (y * 40 + x) * 4;
		return $"({x},{y}) B={bgra[p]} G={bgra[p + 1]} R={bgra[p + 2]}";
	}

	private static int NonBackgroundPixels(RawBitmap bmp, int w, int h, Color background)
	{
		var count = 0;
		for (var y = 0; y < h; y++)
		{
			for (var x = 0; x < w; x++)
			{
				var p = bmp.GetPixel(x, y);
				if (Math.Abs(p.R - background.R) + Math.Abs(p.G - background.G) + Math.Abs(p.B - background.B) > 24)
				{
					count++;
				}
			}
		}
		return count;
	}

	private static int DifferentPixels(RawBitmap a, RawBitmap b, int w, int h)
	{
		var count = 0;
		for (var y = 0; y < h; y++)
		{
			for (var x = 0; x < w; x++)
			{
				var pa = a.GetPixel(x, y);
				var pb = b.GetPixel(x, y);
				if (Math.Abs(pa.R - pb.R) + Math.Abs(pa.G - pb.G) + Math.Abs(pa.B - pb.B) > 24)
				{
					count++;
				}
			}
		}
		return count;
	}
}

#endif
