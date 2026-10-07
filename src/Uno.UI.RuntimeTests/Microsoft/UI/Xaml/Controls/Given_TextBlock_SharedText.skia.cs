#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_TextBlock_SharedText
{
	[TestMethod]
	public async Task When_SelectionHighlightColor_Null_And_Selection_Rendered()
	{
		var textBlock = new TextBlock
		{
			Text = "Hello world",
			FontSize = 24,
			Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red),
			IsTextSelectionEnabled = true,
			SelectionHighlightColor = null!,
		};

		try
		{
			var host = new Border { Width = 200, Height = 50, Background = new SolidColorBrush(Microsoft.UI.Colors.White), Child = textBlock };
			await UITestHelper.Load(host);
			Assert.IsTrue(textBlock.Focus(FocusState.Programmatic));
			textBlock.Selection = new TextBlock.Range(0, 1);
			await WindowHelper.WaitForIdle();

			// A throwing Draw aborts the render pass, leaving nothing on screen.
			var screenshot = await UITestHelper.ScreenShot(host);
			Assert.AreEqual(200, screenshot.Width, "The render pass must complete.");
			ImageAssert.HasColorInRectangle(
				screenshot,
				new System.Drawing.Rectangle(0, 0, 200, 50),
				Microsoft.UI.Colors.Red,
				tolerance: 40);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_CharacterSpacing_Combining_Mark_Gets_Spacing_Once()
	{
		// U+0301 on 'x' has no precomposed form, so the cluster keeps two glyphs.
		var withMark = await GetSpacingDelta("x\u0301y");
		var withoutMark = await GetSpacingDelta("xy");

		Assert.AreEqual(withoutMark, withMark, 0.5, "A combining mark must not add its own character spacing.");
	}

	private static async Task<double> GetSpacingDelta(string text)
	{
		var spaced = new TextBlock { Text = text, FontSize = 20, CharacterSpacing = 1000 };
		var plain = new TextBlock { Text = text, FontSize = 20 };
		try
		{
			await UITestHelper.Load(new StackPanel { HorizontalAlignment = HorizontalAlignment.Left, Children = { spaced, plain } });
			return spaced.DesiredSize.Width - plain.DesiredSize.Width;
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}
}
