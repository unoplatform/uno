using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.Xaml.Core.Scaling;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	public partial class Given_TextBox
	{
		[TestMethod]
		[DataRow(false)]
		[DataRow(true)]
		public async Task When_Wrapped_Then_Arranged_At_Measured_Width(bool withHangingSpaces)
		{
			var text = withHangingSpaces ? HangingSpacesText + "\r" + WrappingParagraphs(8) : WrappingParagraphs(8);
			var SUT = new TextBox
			{
				Width = 300,
				Height = 200,
				TextWrapping = TextWrapping.Wrap,
				AcceptsReturn = true,
				Text = text,
			};

			await UITestHelper.Load(SUT);
			var block = SUT.TextBoxView.DisplayBlock;
			var scrollViewer = (ScrollViewer)SUT.ContentElement;

			AssertArrangedAtLayoutWidth(block, scrollViewer, "");
			AssertNoInkBeyond(block, SUT.Text, scrollViewer.ViewportWidth);
		}

		[TestMethod]
		[DataRow(false)]
		[DataRow(true)]
		public async Task When_Wrapped_Typing_Then_Text_Laid_Out_At_One_Width(bool growHangingSpaces)
		{
			var SUT = new TextBox
			{
				Width = 300,
				Height = 200,
				TextWrapping = TextWrapping.Wrap,
				AcceptsReturn = true,
				Text = growHangingSpaces ? HangingSpacesText + "\r" + WrappingParagraphs(8) : WrappingParagraphs(8),
			};

			await UITestHelper.Load(SUT);
			var block = SUT.TextBoxView.DisplayBlock;

			var scrollViewer = (ScrollViewer)SUT.ContentElement;

			for (var keystroke = 0; keystroke < 4; keystroke++)
			{
				var widths = new List<double>();
				using (TextBlock.ObserveTextLayoutForTesting((laidOut, layoutWidth) =>
				{
					if (laidOut == block)
					{
						widths.Add(layoutWidth);
					}
				}))
				{
					// Either a space in the widest line's hanging spaces, which moves its trailing extent, or a letter
					// in the middle of text whose every line wraps.
					SUT.Select(growHangingSpaces ? HangingSpacesEnd - 1 : SUT.Text.Length / 2, 0);
					SUT.SelectedText = growHangingSpaces ? " " : "a";
					await WindowHelper.WaitForIdle();
				}

				Assert.IsTrue(widths.Count > 0, "The keystroke should have laid the text out.");
				Assert.AreEqual(1, widths.Distinct().Count(), $"Keystroke {keystroke}: the text was laid out at {string.Join(", ", widths.Distinct())}.");
				AssertArrangedAtLayoutWidth(block, scrollViewer, $"Keystroke {keystroke}: ");
			}
		}

		[TestMethod]
		[RequiresScaling(1.75f)]
		public async Task When_Wrapped_At_Fractional_Scale_Then_Arranged_At_Layout_Width()
		{
			// At 175%, measure's rounding up comes out a device pixel above some widths that are already on the pixel
			// grid (floating-point error, one width in seven): a box at every device pixel of 14 meets them.
			for (var pixel = 0; pixel < 14; pixel++)
			{
				var SUT = new TextBox
				{
					Width = 300 + pixel / 1.75,
					TextWrapping = TextWrapping.Wrap,
					AcceptsReturn = true,
					Text = HangingSpacesText,
				};

				await UITestHelper.Load(SUT);
				Assert.AreEqual(1.75, RootScale.GetRasterizationScaleForElement(SUT), 0.001);

				AssertArrangedAtLayoutWidth(SUT.TextBoxView.DisplayBlock, (ScrollViewer)SUT.ContentElement, $"A {SUT.Width} px box: ");
			}
		}

		private static string WrappingParagraphs(int count)
		{
			var words = "lorem ipsum dolor sit amet consectetur adipiscing elit sed do eiusmod tempor incididunt ut labore et dolore magna aliqua".Split(' ');
			var builder = new StringBuilder();
			for (var paragraph = 0; paragraph < count; paragraph++)
			{
				if (paragraph > 0)
				{
					builder.Append('\r');
				}

				for (var word = 0; word < 60; word++)
				{
					if (word > 0)
					{
						builder.Append(' ');
					}

					builder.Append(words[(paragraph * 7 + word * 5) % words.Length]);
				}
			}

			return builder.ToString();
		}

		private static void AssertArrangedAtLayoutWidth(TextBlock block, ScrollViewer scrollViewer, string context)
		{
			Assert.AreEqual(block.TextLayoutWidth, block.RenderSize.Width, $"{context}The display block should be arranged at the width its text was laid out at.");
			Assert.IsTrue(block.RenderSize.Width <= scrollViewer.ViewportWidth, $"{context}The display block's {block.RenderSize.Width} px should fit the {scrollViewer.ViewportWidth} px viewport.");
		}

		private static void AssertNoInkBeyond(TextBlock block, string text, double width)
		{
			for (var index = 0; index < text.Length; index++)
			{
				if (!char.IsWhiteSpace(text[index]))
				{
					var rect = block.ParsedText.GetRectForIndex(index);
					Assert.IsTrue(rect.Right <= width + 0.01, $"'{text[index]}' at {index} ends at {rect.Right}, past the viewport's {width}.");
				}
			}
		}
	}
}
