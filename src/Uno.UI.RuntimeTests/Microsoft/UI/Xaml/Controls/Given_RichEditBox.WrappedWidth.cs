using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

#if WINAPPSDK
using Uno.UI.Extensions;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	public partial class Given_RichEditBox
	{
		// A line whose spaces don't fit in the box: they don't wrap, so they hang past its right edge.
		private static readonly string HangingSpacesText = "aaaa" + new string(' ', 80) + "bbbb";
		private const int HangingSpacesEnd = 84;

		[TestMethod]
		public async Task When_Wrapped_Line_Ends_In_Spaces_Then_Other_Lines_Wrap_At_Viewport()
		{
			var text = HangingSpacesText + "\r" + WrappingParagraphs(2);
			var SUT = new RichEditBox
			{
				Width = 300,
				Height = 400,
				TextWrapping = TextWrapping.Wrap,
			};
			SUT.Document.SetText(TextSetOptions.None, text);

			await UITestHelper.Load(SUT);
			var scrollViewer = SUT.FindFirstChild<ScrollViewer>(viewer => viewer.Name == "ContentElement");
			Assert.IsNotNull(scrollViewer);

			// The hanging spaces don't widen the layout of the paragraphs after them.
			SUT.Document.GetRange(0, 1).GetRect(PointOptions.ClientCoordinates, out var first, out _);
			for (var index = HangingSpacesEnd; index < text.Length; index++)
			{
				if (!char.IsWhiteSpace(text[index]))
				{
					SUT.Document.GetRange(index, index + 1).GetRect(PointOptions.ClientCoordinates, out var rect, out _);
					Assert.IsTrue(rect.Right - first.Left <= scrollViewer.ViewportWidth + 1, $"'{text[index]}' at {index} ends {rect.Right - first.Left} px from the left edge, past the {scrollViewer.ViewportWidth} px viewport.");
				}
			}

			Assert.AreEqual(0, scrollViewer.ScrollableWidth, "A wrapped box should not scroll horizontally.");
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
	}
}
