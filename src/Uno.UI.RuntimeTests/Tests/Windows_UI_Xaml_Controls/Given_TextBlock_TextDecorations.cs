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
	public class Given_TextBlock_TextDecorations
	{
#if __SKIA__
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
#endif
	}
}
