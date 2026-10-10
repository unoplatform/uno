#nullable enable

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#if __SKIA__
using Microsoft.UI.Text;
using Uno.UI.Composition.Drawing;
using Windows.UI.Text;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Documents;

[TestClass]
[RunsOnUIThread]
public class Given_VariableFont
{
#if __SKIA__
	private const string Text = "Hello World";
	private const float FontSize = 48f;

	// The shaped advances must come from the same weight instance as the drawn outlines, otherwise
	// a bold variable font is drawn with its regular instance's spacing.
	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	[DataRow((ushort)300)]
	[DataRow((ushort)400)]
	[DataRow((ushort)600)]
	[DataRow((ushort)800)]
	public async Task When_Weight_Set_Then_Shaped_Advances_Match_Instance(ushort weight)
	{
		var font = CreateSkiaFont(await LoadVariableFontData(), new FontWeight(weight));

		var run = font.Shape(Text, TextDirection.LeftToRight);

		var shapedWidth = 0f;
		var instanceWidth = 0f;
		for (var i = 0; i < run.Count; i++)
		{
			shapedWidth += run.Advances[i];
			instanceWidth += font.GetGlyphAdvance(run.Glyphs[i]);
		}

		Assert.AreEqual(instanceWidth, shapedWidth, 1f, $"Shaped width {shapedWidth} doesn't match the wght={weight} instance width {instanceWidth}.");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_Bold_Then_Shaped_Wider_Than_Normal()
	{
		var data = await LoadVariableFontData();

		var normal = ShapedWidth(CreateSkiaFont(data, FontWeights.Normal));
		var bold = ShapedWidth(CreateSkiaFont(data, FontWeights.Bold));

		Assert.IsTrue(bold > normal + 1f, $"Bold ({bold}) should shape wider than Normal ({normal}).");
	}

	private static float ShapedWidth(IFont font)
	{
		var run = font.Shape(Text, TextDirection.LeftToRight);
		var width = 0f;
		for (var i = 0; i < run.Count; i++)
		{
			width += run.Advances[i];
		}

		return width;
	}

	// OpenSans.ttf is the variable (wght/wdth) build; the provider is called directly so its .manifest is not consulted.
	private static async Task<byte[]> LoadVariableFontData()
	{
		var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/Fonts/OpenSans/OpenSans.ttf"));
		using var stream = await file.OpenStreamForReadAsync();
		using MemoryStream buffer = new();
		await stream.CopyToAsync(buffer);
		return buffer.ToArray();
	}

	private static IFont CreateSkiaFont(byte[] data, FontWeight weight)
	{
		var font = new SkiaFontProvider().CreateFont(data, null, weight, FontStretch.Normal, FontStyle.Normal, FontSize);
		Assert.IsNotNull(font);
		return font;
	}
#endif
}
