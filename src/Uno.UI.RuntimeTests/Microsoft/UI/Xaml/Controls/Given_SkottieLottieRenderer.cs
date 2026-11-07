#nullable enable

#if __SKIA__

using System;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Composition.Drawing;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

// Skottie's SkSL effects hand the animation's "sh" string to Skia's SkSL compiler, which is exploitable with crafted
// input (CVE-2026-119261), so the Skottie renderer must never let them through.
[TestClass]
[RunsOnUIThread]
public class Given_SkottieLottieRenderer
{
	private const string BlueShader = "half4 main(float2 p) { return half4(0, 0, 1, 1); }";

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
	[DataRow("SkSL Shader", BlueShader)]
	[DataRow("\\u0053kSL Shader", BlueShader)]
	[DataRow("SkSL Shader\\u0000ignored", BlueShader)]
	public async Task When_SkSL_Effect_Then_It_Is_Not_Applied(string matchName, string sksl)
	{
		var pixels = await RenderAsync(RedSquareWithEffect(matchName, sksl));

		Assert.IsTrue(IsRed(pixels, 20, 20), $"The SkSL effect should not run: {Describe(pixels, 20, 20)}.");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
	public async Task When_SkSL_Effect_Is_Nested_In_Precomp_Then_It_Is_Not_Applied()
	{
		var json = Json("{'v':'5.4.4','fr':10,'ip':0,'op':20,'w':40,'h':40,"
			+ "'assets':[{'id':'comp_0','layers':[" + RedSquareLayer(Effect("SkSL Shader", BlueShader)) + "]}],"
			+ "'layers':[{'ty':0,'ind':1,'refId':'comp_0','ip':0,'op':20,'st':0,'w':40,'h':40," + Transform100 + "}]}");

		var pixels = await RenderAsync(json);

		Assert.IsTrue(IsRed(pixels, 20, 20), $"The SkSL effect should not run: {Describe(pixels, 20, 20)}.");
	}

	[TestMethod]
	public void When_No_SkSL_Effect_Then_Json_Is_Passed_Through()
	{
		var json = Json("{'v':'5.4.4','fr':10,'ip':0,'op':20,'w':40,'h':40,'layers':[" + RedSquareLayer(null) + "]}");

		Assert.AreSame(json, SkottieSkslEffectFilter.Filter(json));
	}

	// Skottie compiles both kinds as shaders, so a color filter never renders differently, but it still reaches the compiler.
	[TestMethod]
	[DataRow("SkSL Shader")]
	[DataRow("SkSL Color Filter")]
	public void When_SkSL_Effect_Then_Other_Effects_Are_Kept(string matchName)
	{
		var tint = "{'ty':20,'mn':'ADBE Tint','ef':[]}";
		var json = Json("{'v':'5.4.4','fr':10,'ip':0,'op':20,'w':40,'h':40,'layers':[" + RedSquareLayer(tint + "," + Effect(matchName, BlueShader)) + "]}");

		var filtered = SkottieSkslEffectFilter.Filter(json);

		Assert.IsNotNull(filtered);
		StringAssert.Contains(filtered, "ADBE Tint");
		Assert.IsFalse(filtered.Contains("SkSL", StringComparison.Ordinal), filtered);
	}

	[TestMethod]
	public void When_Json_Is_Malformed_And_May_Hold_SkSL_Then_Load_Fails()
	{
		var json = Json("{'v':'5.4.4','fr':10,'ip':0,'op':20,'w':40,'h':40,'layers':[" + RedSquareLayer(Effect("SkSL Shader", BlueShader)) + "]");

		Assert.IsNull(new SkottieLottieRenderer().Load(json, GeometryFactory.Current));
	}

	private static string Json(string singleQuoted) => singleQuoted.Replace('\'', '"');

	private const string Transform100 = "'ks':{'o':{'a':0,'k':100},'p':{'a':0,'k':[0,0,0]},'a':{'a':0,'k':[0,0,0]},'s':{'a':0,'k':[100,100,100]},'r':{'a':0,'k':0}}";

	private static string Effect(string matchName, string sksl) => "{'ty':5,'mn':'" + matchName + "','ef':[{'sh':'" + sksl + "'}]}";

	private static string RedSquareWithEffect(string matchName, string sksl)
		=> Json("{'v':'5.4.4','fr':10,'ip':0,'op':20,'w':40,'h':40,'layers':[" + RedSquareLayer(Effect(matchName, sksl)) + "]}");

	private static string RedSquareLayer(string? effects)
		=> "{'ty':4,'ind':1,'ip':0,'op':20,'st':0," + Transform100 + (effects is null ? "" : ",'ef':[" + effects + "]") + ",'shapes':[{'ty':'gr','it':["
			+ "{'ty':'rc','p':{'a':0,'k':[20,20]},'s':{'a':0,'k':[20,20]},'r':{'a':0,'k':0}},"
			+ "{'ty':'fl','c':{'a':0,'k':[1,0,0,1]},'o':{'a':0,'k':100}},"
			+ "{'ty':'tr','p':{'a':0,'k':[0,0]},'a':{'a':0,'k':[0,0]},'s':{'a':0,'k':[100,100]},'r':{'a':0,'k':0},'o':{'a':0,'k':100}}]}]}";

	private static async Task<byte[]> RenderAsync(string json)
	{
		using var animation = new SkottieLottieRenderer().Load(json, GeometryFactory.Current);
		Assert.IsNotNull(animation, "The animation should load.");

		var factory = Private.Infrastructure.TestServices.WindowHelper.XamlRoot?.VisualTree.RootElement?.Visual.CompositionTarget?.Renderer
			?? throw new InvalidOperationException("The test window has no negotiated renderer.");
		using var texture = factory.RenderOffscreen(40, 40, session =>
		{
			session.Clear(Colors.White);
			animation.Render(session, 0.5f, new global::Windows.Foundation.Rect(0, 0, 40, 40));
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

	private static string Describe(byte[] bgra, int x, int y)
	{
		var p = (y * 40 + x) * 4;
		return $"({x},{y}) B={bgra[p]} G={bgra[p + 1]} R={bgra[p + 2]}";
	}
}
#endif
