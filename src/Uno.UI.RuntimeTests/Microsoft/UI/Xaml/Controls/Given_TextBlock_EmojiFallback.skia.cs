#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Composition.Drawing;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_TextBlock_EmojiFallback
{
	[TestMethod]
	public void When_Emoji_Fallback_Is_Triggered_By_Bases_Not_Sequence_Controls()
	{
		Assert.IsTrue(NotoFontFallbackService.IsEmojiCodepoint(0x1F469)); // woman
		Assert.IsTrue(NotoFontFallbackService.IsEmojiCodepoint(0x1F3FD)); // medium skin tone
		Assert.IsTrue(NotoFontFallbackService.IsEmojiCodepoint(0x20E3)); // keycap
		Assert.IsFalse(NotoFontFallbackService.IsEmojiCodepoint('1'));
		Assert.IsFalse(NotoFontFallbackService.IsEmojiCodepoint(0x200D)); // ZWJ
		Assert.IsFalse(NotoFontFallbackService.IsEmojiCodepoint(0xFE0F)); // emoji variation selector
	}

	[TestMethod]
	public void When_Symbol_Defaults_To_Text_Presentation_It_Is_Not_Color_Emoji_By_Default()
	{
		// Emoji_Presentation=No: monochrome unless U+FE0F asks for the emoji form, as with DirectWrite.
		Assert.IsTrue(NotoFontFallbackService.IsEmojiCodepoint(0x26A0)); // warning sign
		Assert.IsFalse(NotoFontFallbackService.IsEmojiPresentationCodepoint(0x26A0));
		Assert.IsFalse(NotoFontFallbackService.IsEmojiPresentationCodepoint(0x25B6)); // play button

		Assert.IsTrue(NotoFontFallbackService.IsEmojiPresentationCodepoint(0x2705)); // check mark button
		Assert.IsTrue(NotoFontFallbackService.IsEmojiPresentationCodepoint(0x1F600)); // grinning face
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Emoji_Fallback_Loads_Shapes_And_Renders_Zwj_Cluster()
	{
		const string emoji = "\U0001F469\U0001F3FD\u200D\U0001F4BB"; // woman technologist, medium skin tone
		var textBlock = new TextBlock { FontSize = 48, Text = emoji };
		var host = new Border { Width = 180, Height = 90, Background = new SolidColorBrush(Microsoft.UI.Colors.White), Child = textBlock };
		try
		{
			await UITestHelper.Load(host);

			var details = await Microsoft.UI.Xaml.Documents.TextFormatting.FontDetailsCache.GetFontForCodepoint(
				0x1F469,
				(float)textBlock.FontSize,
				textBlock.FontWeight,
				textBlock.FontStretch,
				textBlock.FontStyle);
			Assert.IsNotNull(details, "The WASM fallback service should load Noto COLRv1 on first emoji use.");
			Assert.AreEqual("Noto Color Emoji", details.FontHandle.FamilyName);

			var glyphRun = details.FontHandle.Shape(emoji, TextDirection.LeftToRight);
			Assert.AreEqual(1, glyphRun.Count, "The skin-tone ZWJ sequence should shape as one emoji glyph.");

			// The text was laid out before the font resolved; completion must re-lay it out with the color font.
			await WindowHelper.WaitForIdle();
			var bitmap = await UITestHelper.ScreenShot(host);
			var chromaticPixels = 0;
			for (var y = 0; y < bitmap.Height; y++)
			{
				for (var x = 0; x < bitmap.Width; x++)
				{
					var pixel = bitmap.GetPixel(x, y);
					var maximum = Math.Max(pixel.R, Math.Max(pixel.G, pixel.B));
					var minimum = Math.Min(pixel.R, Math.Min(pixel.G, pixel.B));
					if (pixel.A > 200 && maximum - minimum > 24)
					{
						chromaticPixels++;
					}
				}
			}

			Assert.IsGreaterThan(20, chromaticPixels, "COLRv1 rendering should produce colored pixels rather than a monochrome tofu box.");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}
}
