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
	public async Task When_Rtl_TextBlock_Latin_Keeps_Period_At_End()
	{
		var sut = CreateTextBlock(FlowDirection.RightToLeft, R("Hello world", Colors.Blue), R(".", Colors.Red));
		sut.Width = 300;

		try
		{
			var (_, bitmap) = await Show(sut);
			var text = InkX(bitmap, Colors.Blue);
			var period = InkX(bitmap, Colors.Red);

			// "Hello world." is left-to-right text, so the period stays at its end, as on WinUI.
			Assert.IsTrue(period.Min > text.Max, $"The period ({period}) should be drawn right of \"Hello world\" ({text}).");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[DataRow(FlowDirection.RightToLeft)]
	[DataRow(FlowDirection.LeftToRight)]
	public async Task When_Run_FlowDirection_Differs_From_Control(FlowDirection controlDirection)
	{
		var runDirection = controlDirection is FlowDirection.RightToLeft ? FlowDirection.LeftToRight : FlowDirection.RightToLeft;
		var sut = controlDirection is FlowDirection.RightToLeft
			? CreateTextBlock(controlDirection, R("abc ", Colors.Red), R("def", Colors.Blue, runDirection), R(" jkl", Colors.Green), R(".", Colors.Magenta))
			: CreateTextBlock(controlDirection, R("abc ", Colors.Red), R("def", Colors.Blue, runDirection), R("!!", Colors.Magenta, runDirection), R(" jkl", Colors.Green));

		try
		{
			var (_, bitmap) = await Show(sut);
			var red = InkX(bitmap, Colors.Red);
			var blue = InkX(bitmap, Colors.Blue);
			var green = InkX(bitmap, Colors.Green);
			var magenta = InkX(bitmap, Colors.Magenta);
			var description = $"red {red}, blue {blue}, green {green}, magenta {magenta}";

			if (controlDirection is FlowDirection.RightToLeft)
			{
				// Left-to-right text with an explicitly left-to-right run reads naturally, period last.
				Assert.IsTrue(red.Max < blue.Min && blue.Max < green.Min && green.Max < magenta.Min, $"Expected \"abc def jkl.\": {description}.");
			}
			else
			{
				// The right-to-left run is embedded: its trailing "!!" moves to its left.
				Assert.IsTrue(red.Max < magenta.Min && magenta.Max < blue.Min && blue.Max < green.Min, $"Expected \"abc !!def jkl\": {description}.");
			}
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
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
