using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI;
using Windows.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public class Given_TextBlock_TextDecorations
	{
		// Regression guard: the Skia text renderer (UnicodeText) used to ignore TextDecorations entirely,
		// so Underline/Strikethrough were never painted for a TextBlock. These render an underlined/struck
		// TextBlock next to an identical plain one and assert the decoration actually changed the pixels.

		private static Border MakeContainer(FrameworkElement child) => new Border
		{
			Width = 260,
			Height = 90,
			Background = new SolidColorBrush(Colors.White),
			Child = child,
		};

		private static TextBlock MakeTextBlock(TextDecorations decorations, bool onRun)
		{
			var textBlock = new TextBlock { FontSize = 40, Foreground = new SolidColorBrush(Colors.Red) };
			if (onRun)
			{
				textBlock.Inlines.Add(new Run { Text = "Deco", TextDecorations = decorations });
			}
			else
			{
				textBlock.Text = "Deco";
				textBlock.TextDecorations = decorations;
			}

			return textBlock;
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23687")]
		public async Task When_Run_Underline_And_Strikethrough_Then_Rendered()
		{
			var underlined = MakeContainer(MakeTextBlock(TextDecorations.Underline, onRun: true));
			var plain = MakeContainer(MakeTextBlock(TextDecorations.None, onRun: true));
			var struck = MakeContainer(MakeTextBlock(TextDecorations.Strikethrough, onRun: true));
			var stack = new StackPanel { Children = { underlined, plain, struck } };

			await UITestHelper.Load(stack);

			var underlinedShot = await UITestHelper.ScreenShot(underlined);
			var plainShot = await UITestHelper.ScreenShot(plain);
			var struckShot = await UITestHelper.ScreenShot(struck);

			// The only difference between each decorated variant and the plain one is the decoration line,
			// so a working renderer must produce different pixels; a regression makes them identical.
			await ImageAssert.AreNotEqualAsync(underlinedShot, plainShot);
			await ImageAssert.AreNotEqualAsync(struckShot, plainShot);

			// Underline (below the baseline) and strikethrough (through the text) must land at different places.
			await ImageAssert.AreNotEqualAsync(underlinedShot, struckShot);
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23687")]
		public async Task When_Block_Level_Underline_Then_Rendered()
		{
			// TextDecorations is an inherited dependency property, so setting it on the TextBlock must
			// reach the inline text and paint an underline.
			var blockUnderlined = MakeContainer(MakeTextBlock(TextDecorations.Underline, onRun: false));
			var plain = MakeContainer(MakeTextBlock(TextDecorations.None, onRun: false));
			var stack = new StackPanel { Children = { blockUnderlined, plain } };

			await UITestHelper.Load(stack);

			var blockShot = await UITestHelper.ScreenShot(blockUnderlined);
			var plainShot = await UITestHelper.ScreenShot(plain);

			await ImageAssert.AreNotEqualAsync(blockShot, plainShot);
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23687")]
		public async Task When_Underline_And_Strikethrough_Combined_Then_Both_Rendered()
		{
			// Underline | Strikethrough draws both lines, so the combined variant must differ from each
			// single-decoration variant (a regression that drops one would match the other).
			var both = MakeContainer(MakeTextBlock(TextDecorations.Underline | TextDecorations.Strikethrough, onRun: true));
			var underlined = MakeContainer(MakeTextBlock(TextDecorations.Underline, onRun: true));
			var struck = MakeContainer(MakeTextBlock(TextDecorations.Strikethrough, onRun: true));
			var stack = new StackPanel { Children = { both, underlined, struck } };

			await UITestHelper.Load(stack);

			var bothShot = await UITestHelper.ScreenShot(both);
			var underlinedShot = await UITestHelper.ScreenShot(underlined);
			var struckShot = await UITestHelper.ScreenShot(struck);

			await ImageAssert.AreNotEqualAsync(bothShot, underlinedShot);
			await ImageAssert.AreNotEqualAsync(bothShot, struckShot);
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23687")]
		public async Task When_Underline_With_Trailing_Whitespace_Then_Not_Extended()
		{
			// WinUI/DWrite do not decorate collapsed line-trailing whitespace. A left-aligned underlined
			// "X" followed by trailing spaces must underline only the "X", rendering identically to a
			// left-aligned underlined "X" with no trailing spaces. Before the fix the underline extended
			// under the trailing spaces, so the two differed.
			static Border MakeTrailing(string text) => MakeContainer(new TextBlock
			{
				FontSize = 40,
				Foreground = new SolidColorBrush(Colors.Red),
				HorizontalAlignment = HorizontalAlignment.Left,
				TextDecorations = TextDecorations.Underline,
				Text = text,
			});

			var withTrailing = MakeTrailing("X          ");
			var withoutTrailing = MakeTrailing("X");
			var stack = new StackPanel { Children = { withTrailing, withoutTrailing } };

			await UITestHelper.Load(stack);

			var withTrailingShot = await UITestHelper.ScreenShot(withTrailing);
			var withoutTrailingShot = await UITestHelper.ScreenShot(withoutTrailing);

			await ImageAssert.AreEqualAsync(withTrailingShot, withoutTrailingShot);
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23687")]
		public async Task When_TextDecorations_Changed_Then_Rerendered()
		{
			// Ported from WinUI's TextBlockTests::ChangeTextDecorations: toggling TextDecorations after the
			// text is laid out must invalidate and repaint, both from the TextBlock and from an inline Run.
			var run = new Run { Text = "Deco" };
			var textBlock = new TextBlock { FontSize = 40, Foreground = new SolidColorBrush(Colors.Red) };
			textBlock.Inlines.Add(run);
			var container = MakeContainer(textBlock);

			await UITestHelper.Load(container);
			var plain = await UITestHelper.ScreenShot(container);

			textBlock.TextDecorations = TextDecorations.Underline;
			await UITestHelper.WaitForIdle();
			var underlined = await UITestHelper.ScreenShot(container);
			await ImageAssert.AreNotEqualAsync(underlined, plain);

			textBlock.TextDecorations = TextDecorations.Strikethrough | TextDecorations.Underline;
			await UITestHelper.WaitForIdle();
			var both = await UITestHelper.ScreenShot(container);
			await ImageAssert.AreNotEqualAsync(both, underlined);

			textBlock.TextDecorations = TextDecorations.None;
			await UITestHelper.WaitForIdle();
			await ImageAssert.AreEqualAsync(await UITestHelper.ScreenShot(container), plain);

			run.TextDecorations = TextDecorations.Underline;
			await UITestHelper.WaitForIdle();
			await ImageAssert.AreEqualAsync(await UITestHelper.ScreenShot(container), underlined);

			run.TextDecorations = TextDecorations.None;
			await UITestHelper.WaitForIdle();
			await ImageAssert.AreEqualAsync(await UITestHelper.ScreenShot(container), plain);
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23687")]
		public async Task When_Underline_Then_Drawn_At_Font_Metrics_Offset()
		{
#if __SKIA__
			// WinUI draws the underline as a rect whose top edge is at baseline + the font's underline
			// position and whose height is the font's underline thickness (DWriteTextRenderer::DrawUnderline
			// offsets the baseline by DWRITE_UNDERLINE.offset, then D2DTextDrawingContext fills a
			// { 0, 0, width, thickness } rect). SKFontMetrics reports the same top-edge offset, so the
			// painted band must start at the metric offset instead of being centred on it.
			const int fontSize = 200;

			var run = new Run { Text = "X", TextDecorations = TextDecorations.Underline };
			var textBlock = new TextBlock
			{
				FontSize = fontSize,
				// Pin a bundled font so the expected geometry comes from known metrics rather than from
				// whatever the host happens to resolve. Roboto-Regular publishes post.underlinePosition
				// (-150/2048 em) and underlineThickness (100/2048 em).
				FontFamily = new FontFamily(RobotoAsset),
				// MaxHeight line stacking keeps the baseline at -Ascent and puts the extra room below the
				// text, so the underline can never be clipped by the line box.
				LineHeight = fontSize * 2,
				Foreground = new SolidColorBrush(Colors.Red),
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
			};
			textBlock.Inlines.Add(run);

			var container = new Border
			{
				Background = new SolidColorBrush(Colors.White),
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				Child = textBlock,
			};

			await UITestHelper.Load(container);

			// Font assets resolve asynchronously, so wait until the run actually paints with Roboto.
			await UITestHelper.WaitFor(
				() => run.FontInfo.SKFont.Typeface?.FamilyName == RobotoFamily,
				5000,
				$"Timed out waiting for {RobotoAsset} to be resolved for the run.");
			await UITestHelper.WaitForIdle();

			var metrics = run.FontInfo.SKFontMetrics;
			if (metrics.UnderlinePosition is not { } underlinePosition || metrics.UnderlineThickness is not { } underlineThickness)
			{
				Assert.Fail($"{RobotoFamily} publishes underline metrics, so their absence means the run did not resolve to the bundled asset.");
				return;
			}

			var screenshot = await UITestHelper.ScreenShot(container);
			var inkRows = GetInkRows(screenshot);
			Assert.IsTrue(inkRows.Count > 0, "Nothing was painted.");

			// "X" sits entirely above the baseline, so the last contiguous band of ink is the underline.
			var bandEnd = inkRows[^1];
			var bandStart = bandEnd;
			for (var i = inkRows.Count - 2; i >= 0 && inkRows[i] == bandStart - 1; i--)
			{
				bandStart = inkRows[i];
			}

			Assert.AreNotEqual(inkRows[0], bandStart, "The underline was not painted below the glyph.");

			var expectedTop = -metrics.Ascent + underlinePosition;
			Assert.AreEqual(expectedTop, bandStart, 2d, $"Underline top row is {bandStart}, expected {expectedTop}.");
			Assert.AreEqual(underlineThickness, bandEnd - bandStart + 1, 2d, $"Underline is {bandEnd - bandStart + 1} rows, expected {underlineThickness}.");
#else
			await Task.CompletedTask;
#endif
		}

#if __SKIA__
		private const string RobotoAsset = "ms-appx:///Uno.UI.RuntimeTests/Assets/Fonts/Roboto-Regular.ttf";
		private const string RobotoFamily = "Roboto";

		private static List<int> GetInkRows(RawBitmap bitmap)
		{
			var rows = new List<int>();
			for (var y = 0; y < bitmap.Height; y++)
			{
				var inkCount = 0;
				for (var x = 0; x < bitmap.Width; x++)
				{
					var pixel = bitmap.GetPixel(x, y);
					if (pixel.R > 128 && pixel.G < 128 && pixel.B < 128)
					{
						inkCount++;
					}
				}

				if (inkCount >= 3)
				{
					rows.Add(y);
				}
			}

			return rows;
		}
#endif
	}
}
