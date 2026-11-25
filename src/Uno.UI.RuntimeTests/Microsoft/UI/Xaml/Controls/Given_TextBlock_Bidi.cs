using System;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI;
using static Private.Infrastructure.TestServices;
using static Uno.UI.RuntimeTests.Helpers.TextInkHelper;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

// Visual order of bidi text, read from the colour of each run's ink. The expected orders are WinUI's.
[TestClass]
[RunsOnUIThread]
public class Given_TextBlock_Bidi
{
	private static Run R(string text, Color color, FlowDirection? flowDirection = null)
	{
		var run = new Run { Text = text, Foreground = new SolidColorBrush(color) };
		if (flowDirection is { } direction)
		{
			run.FlowDirection = direction;
		}
		return run;
	}

	private static TextBlock CreateTextBlock(FlowDirection flowDirection, params Inline[] inlines)
	{
		var textBlock = new TextBlock { FontSize = 24, FlowDirection = flowDirection };
		foreach (var inline in inlines)
		{
			textBlock.Inlines.Add(inline);
		}
		return textBlock;
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)] // Uno's text engine
	public async Task When_Bidi_Buffers_Outlive_SetPara()
	{
#if HAS_UNO
		// ICU keeps the pointers it gets in ubidi_setPara and reads them again for each line. Between the two, hand
		// out the pooled array the engine just returned (Uno's own pool) and overwrite it, and move what the GC can move.
		UnicodeText.ParagraphBidiResolvedForTests = length =>
		{
			var array = Uno.Buffers.ArrayPool<byte>.Shared.Rent(length);
			Array.Clear(array);
			Uno.Buffers.ArrayPool<byte>.Shared.Return(array);
			GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
		};

		try
		{
			var sut = CreateTextBlock(FlowDirection.LeftToRight, R("ab", Colors.Blue, FlowDirection.RightToLeft), R("!!", Colors.Red, FlowDirection.RightToLeft));
			var (_, bitmap) = await Show(sut);
			var blue = InkX(bitmap, Colors.Blue);
			var red = InkX(bitmap, Colors.Red);

			Assert.IsTrue(red.Max < blue.Min, $"\"!!\" ({red}) should be drawn left of \"ab\" ({blue}), as in the right-to-left embedding.");
		}
		finally
		{
			UnicodeText.ParagraphBidiResolvedForTests = null;
			WindowHelper.WindowContent = null;
		}
#else
		await Task.CompletedTask;
#endif
	}
}
