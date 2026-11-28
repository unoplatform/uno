using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	public partial class Given_RichEditBox
	{
		[TestMethod]
		[DataRow(false)]
		[DataRow(true)]
		public async Task When_Wrapped_Then_Arranged_At_Measured_Width(bool withHangingSpaces)
		{
			var text = withHangingSpaces ? HangingSpacesText + "\r" + WrappingParagraphs(8) : WrappingParagraphs(8);
			var SUT = new RichEditBox
			{
				Width = 300,
				Height = 200,
				TextWrapping = TextWrapping.Wrap,
			};
			SUT.Document.SetText(TextSetOptions.None, text);

			await UITestHelper.Load(SUT);
			var block = GetDisplayBlock(SUT);
			var scrollViewer = SUT.FindFirstChild<ScrollViewer>(viewer => viewer.Name == "ContentElement");
			Assert.IsNotNull(scrollViewer);

			AssertArrangedAtLayoutWidth(block, scrollViewer, "");
			for (var index = 0; index < text.Length; index++)
			{
				if (!char.IsWhiteSpace(text[index]))
				{
					var rect = block.ParsedText.GetRectForIndex(index);
					Assert.IsTrue(rect.Right <= scrollViewer.ViewportWidth + 0.01, $"'{text[index]}' at {index} ends at {rect.Right}, past the viewport's {scrollViewer.ViewportWidth}.");
				}
			}
		}

		[TestMethod]
		[DataRow(false)]
		[DataRow(true)]
		public async Task When_Wrapped_Typing_Then_Text_Laid_Out_At_One_Width(bool growHangingSpaces)
		{
			var SUT = new RichEditBox
			{
				Width = 300,
				Height = 200,
				TextWrapping = TextWrapping.Wrap,
			};
			var text = growHangingSpaces ? HangingSpacesText + "\r" + WrappingParagraphs(8) : WrappingParagraphs(8);
			SUT.Document.SetText(TextSetOptions.None, text);

			await UITestHelper.Load(SUT);
			var block = GetDisplayBlock(SUT);

			var scrollViewer = SUT.FindFirstChild<ScrollViewer>(viewer => viewer.Name == "ContentElement");
			Assert.IsNotNull(scrollViewer);

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
					var position = growHangingSpaces ? HangingSpacesEnd - 1 : text.Length / 2;
					SUT.Document.Selection.SetRange(position, position);
					SUT.Document.Selection.TypeText(growHangingSpaces ? " " : "a");
					await WindowHelper.WaitForIdle();
				}

				Assert.IsTrue(widths.Count > 0, "The keystroke should have laid the text out.");
				Assert.AreEqual(1, widths.Distinct().Count(), $"Keystroke {keystroke}: the text was laid out at {string.Join(", ", widths.Distinct())}.");
				AssertArrangedAtLayoutWidth(block, scrollViewer, $"Keystroke {keystroke}: ");
			}
		}

		private static void AssertArrangedAtLayoutWidth(TextBlock block, ScrollViewer scrollViewer, string context)
		{
			Assert.AreEqual(block.TextLayoutWidth, block.RenderSize.Width, $"{context}The display block should be arranged at the width its text was laid out at.");
			Assert.IsTrue(block.RenderSize.Width <= scrollViewer.ViewportWidth, $"{context}The display block's {block.RenderSize.Width} px should fit the {scrollViewer.ViewportWidth} px viewport.");
		}
	}
}
