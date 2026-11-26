#if __SKIA__
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using Uno.UI.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

/// <summary>
/// A TextBlock keeps the part of its layout that doesn't depend on the available size (shaping, bidi, ICU boundaries) and
/// reuses it while its text and formatting are unchanged, so a layout at a new size only breaks the lines again.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class Given_TextBlock_ShapingCache
{
	private const string Sentence = "The quick brown fox jumps over the lazy dog. Pack my box with five dozen liquor jugs.";

	[TestMethod]
	public async Task When_Width_Changes_Text_Is_Not_Shaped_Again()
	{
		var SUT = new TextBlock { Text = Sentence, TextWrapping = TextWrapping.Wrap, Width = 400 };
		WindowHelper.WindowContent = new Border { Child = SUT };
		await WindowHelper.WaitForLoaded(SUT);
		await WindowHelper.WaitForIdle();

		var shaped = SUT.CachedShapedText;
		Assert.IsNotNull(shaped);
		var initialHeight = SUT.ActualHeight;

		foreach (var width in new double[] { 300, 120, 60, 250, 400 })
		{
			SUT.Width = width;
			SUT.UpdateLayout();
			Assert.AreSame(shaped, SUT.CachedShapedText, $"Text was shaped again after changing the width to {width}.");
		}

		SUT.Width = 60;
		SUT.UpdateLayout();
		Assert.IsTrue(SUT.ActualHeight > initialHeight, "The text should wrap onto more lines at a narrower width.");
	}

	[TestMethod]
	public async Task When_Inlines_Change_Then_Old_Shaping_Dropped_Before_Layout()
	{
		var SUT = new TextBlock { Text = Sentence };
		WindowHelper.WindowContent = new Border { Child = SUT };
		await WindowHelper.WaitForLoaded(SUT);
		await WindowHelper.WaitForIdle();
		Assert.IsNotNull(SUT.CachedShapedText);

		SUT.Inlines.Clear();
		SUT.Inlines.Add(new Run { Text = "Other" });

		// A miss anyway: dropped at once rather than holding the old Runs until the next layout.
		Assert.IsNull(SUT.CachedShapedText);
		await WindowHelper.WaitForIdle();
		Assert.IsNotNull(SUT.CachedShapedText);
	}

	[TestMethod]
	public async Task When_Unloaded_Then_Shaping_Dropped()
	{
		var SUT = new TextBlock { Text = Sentence };
		var host = new Border { Child = SUT };
		WindowHelper.WindowContent = host;
		await WindowHelper.WaitForLoaded(SUT);
		await WindowHelper.WaitForIdle();
		Assert.IsNotNull(SUT.CachedShapedText);

		host.Child = null;
		await WindowHelper.WaitFor(() => !SUT.IsLoaded);

		Assert.IsNull(SUT.CachedShapedText);
	}

	[TestMethod]
	public async Task When_Arranged_Narrower_Than_Measured_Text_Is_Not_Shaped_Again()
	{
		// A Left-aligned TextBlock is measured with the parent's width and arranged with its own desired width,
		// so measure and arrange see different sizes.
		var SUT = new TextBlock { Text = "Short", HorizontalAlignment = HorizontalAlignment.Left };
		var host = new Border { Width = 300, Child = SUT };
		WindowHelper.WindowContent = host;
		await WindowHelper.WaitForLoaded(SUT);
		await WindowHelper.WaitForIdle();

		var shaped = SUT.CachedShapedText;
		Assert.IsNotNull(shaped);

		for (var width = 200; width <= 320; width += 20)
		{
			host.Width = width;
			host.UpdateLayout();
			Assert.AreSame(shaped, SUT.CachedShapedText);
		}
	}

	[TestMethod]
	public async Task When_Text_Changes_Text_Is_Shaped_Again()
	{
		var SUT = new TextBlock { Text = "Hello" };
		WindowHelper.WindowContent = SUT;
		await WindowHelper.WaitForLoaded(SUT);
		await WindowHelper.WaitForIdle();

		var shaped = SUT.CachedShapedText;
		var width = SUT.ActualWidth;

		SUT.Text = "Hello, world";
		SUT.UpdateLayout();

		Assert.AreNotSame(shaped, SUT.CachedShapedText);
		Assert.IsTrue(SUT.ActualWidth > width);
	}

	[TestMethod]
	public async Task When_Run_Text_Changes_Text_Is_Shaped_Again()
	{
		var run = new Run { Text = "Hello" };
		var SUT = new TextBlock();
		SUT.Inlines.Add(run);
		WindowHelper.WindowContent = SUT;
		await WindowHelper.WaitForLoaded(SUT);
		await WindowHelper.WaitForIdle();

		var shaped = SUT.CachedShapedText;
		var width = SUT.ActualWidth;

		run.Text = "Hello, world";
		SUT.UpdateLayout();

		Assert.AreNotSame(shaped, SUT.CachedShapedText);
		Assert.IsTrue(SUT.ActualWidth > width);
	}

	[TestMethod]
	public async Task When_FontSize_Changes_Text_Is_Shaped_Again()
	{
		var SUT = new TextBlock { Text = "Hello", FontSize = 14 };
		WindowHelper.WindowContent = SUT;
		await WindowHelper.WaitForLoaded(SUT);
		await WindowHelper.WaitForIdle();

		var shaped = SUT.CachedShapedText;
		var width = SUT.ActualWidth;

		SUT.FontSize = 28;
		SUT.UpdateLayout();

		Assert.AreNotSame(shaped, SUT.CachedShapedText);
		Assert.IsTrue(SUT.ActualWidth > width * 1.5);
	}

	[TestMethod]
	public async Task When_CharacterSpacing_Changes_Text_Is_Shaped_Again()
	{
		// Character spacing widens every cluster but keeps the same font, so it has to be part of the reuse check.
		var SUT = new TextBlock { Text = "Hello" };
		WindowHelper.WindowContent = SUT;
		await WindowHelper.WaitForLoaded(SUT);
		await WindowHelper.WaitForIdle();

		var shaped = SUT.CachedShapedText;
		var width = SUT.ActualWidth;

		SUT.CharacterSpacing = 500;
		SUT.UpdateLayout();

		Assert.AreNotSame(shaped, SUT.CachedShapedText);
		Assert.IsTrue(SUT.ActualWidth > width * 1.5, $"{SUT.ActualWidth} vs {width}");
	}

	[TestMethod]
	public async Task When_Foreground_Changes_The_Next_Layout_Uses_The_New_Brush()
	{
		// A Foreground change only repaints, so the shaping (which holds each run's brush) is still there for the next
		// layout, which must not reuse it.
		var SUT = new TextBlock { Text = Sentence, TextWrapping = TextWrapping.Wrap, Width = 300 };
		WindowHelper.WindowContent = SUT;
		await WindowHelper.WaitForLoaded(SUT);
		await WindowHelper.WaitForIdle();
		var shaped = SUT.CachedShapedText;

		var brush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
		SUT.Foreground = brush;
		SUT.Width = 200;
		SUT.UpdateLayout();

		Assert.AreNotSame(shaped, SUT.CachedShapedText);
		Assert.AreSame(brush, SUT.CachedShapedText!.RunBreaks[0].foreground);
	}

	[TestMethod]
	[DataRow(false, DisplayName = "Set on the TextBlock")]
	[DataRow(true, DisplayName = "Inherited from a parent")]
	public async Task When_Foreground_Changes_To_An_Equal_Brush_The_Old_Brush_Is_Not_Drawn(bool inherited)
	{
		// SolidColorBrush.Equals compares colors, but the text draws with the brush its layout keeps: once the TextBlock uses
		// another brush, a change to the old one must not show.
		var oldBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Black);
		var newBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Black);
		var SUT = new TextBlock { Text = Sentence, TextWrapping = TextWrapping.Wrap, Width = 300, FontSize = 24 };
		var parent = new ContentControl { Content = SUT };
		var host = new Border { Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White), Child = parent };
		void SetForeground(Microsoft.UI.Xaml.Media.Brush brush)
		{
			if (inherited)
			{
				parent.Foreground = brush;
			}
			else
			{
				SUT.Foreground = brush;
			}
		}

		try
		{
			SetForeground(oldBrush);
			WindowHelper.WindowContent = host;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SetForeground(newBrush);
			SUT.Width = 250;
			await WindowHelper.WaitForIdle();
			oldBrush.Color = Microsoft.UI.Colors.Red;
			SUT.Width = 200;
			await WindowHelper.WaitForIdle();

			var screenshot = await UITestHelper.ScreenShot(SUT);
			ImageAssert.DoesNotHaveColorInRectangle(screenshot, new System.Drawing.Rectangle(0, 0, screenshot.Width, screenshot.Height), Microsoft.UI.Colors.Red, tolerance: 60);
			ImageAssert.HasColorInRectangle(screenshot, new System.Drawing.Rectangle(0, 0, screenshot.Width, screenshot.Height), Microsoft.UI.Colors.Black, tolerance: 60);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_RichEditBox_Run_Format_Changes_In_Place_Layout_Matches_A_Fresh_Editor()
	{
		// A small RichEditBox document is laid out by its display block's own TextBlock path, and a format change updates
		// the existing Run in place: the next layout must not reuse the shaping of the old format.
		const string text = "Mixed formatting in a paragraph long enough to wrap onto a few lines.";
		var SUT = new RichEditBox { Width = 300 };
		var fresh = new RichEditBox { Width = 300 };
		void ApplyFormat(RichEditBox editor)
		{
			var format = editor.Document.GetRange(6, 16).CharacterFormat;
			format.SmallCaps = Microsoft.UI.Text.FormatEffect.On;
			format.Position = 3;
		}

		try
		{
			WindowHelper.WindowContent = new StackPanel { Children = { SUT, fresh } };
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForLoaded(fresh);
			SUT.Document.SetText(Microsoft.UI.Text.TextSetOptions.None, text);
			await WindowHelper.WaitForIdle();
			var displayBlock = GetDisplayBlock(SUT);
			Assert.IsNull(displayBlock.CustomTextLayout, "The document should be laid out by the TextBlock itself.");
			Assert.IsNotNull(displayBlock.CachedShapedText);

			ApplyFormat(SUT);
			fresh.Document.SetText(Microsoft.UI.Text.TextSetOptions.None, text);
			ApplyFormat(fresh);
			await WindowHelper.WaitForIdle();

			AssertSameCharacterRects(GetDisplayBlock(fresh), displayBlock, "after the format change");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Shaping_Cache_Is_Disabled_Nothing_Is_Kept()
	{
		var SUT = new TextBlock { Text = Sentence, TextWrapping = TextWrapping.Wrap, Width = 300 };
		try
		{
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();
			Assert.IsNotNull(SUT.CachedShapedText);

			FeatureConfiguration.TextBlock.EnableShapingCache = false;
			SUT.Width = 200;
			SUT.UpdateLayout();

			Assert.IsNull(SUT.CachedShapedText);
		}
		finally
		{
			FeatureConfiguration.TextBlock.EnableShapingCache = true;
			WindowHelper.WindowContent = null;
		}
	}

	// The layout made from a reused shaping result, after a sequence of width changes, must equal a fresh layout at that width.
	[TestMethod]
	[DataRow(Sentence, TextWrapping.Wrap, TextTrimming.None, 0, FlowDirection.LeftToRight, DisplayName = "Wrap")]
	[DataRow(Sentence, TextWrapping.NoWrap, TextTrimming.CharacterEllipsis, 0, FlowDirection.LeftToRight, DisplayName = "CharacterEllipsis")]
	[DataRow(Sentence, TextWrapping.NoWrap, TextTrimming.WordEllipsis, 0, FlowDirection.LeftToRight, DisplayName = "WordEllipsis")]
	[DataRow(Sentence, TextWrapping.Wrap, TextTrimming.WordEllipsis, 2, FlowDirection.LeftToRight, DisplayName = "Wrap + MaxLines + WordEllipsis")]
	[DataRow("a\tbb\tccc\tdddd\teeeee words\tand more words\tend", TextWrapping.Wrap, TextTrimming.None, 0, FlowDirection.LeftToRight, DisplayName = "Tabs")]
	[DataRow("Hello שלום עולם world مرحبا بالعالم end", TextWrapping.Wrap, TextTrimming.None, 0, FlowDirection.LeftToRight, DisplayName = "Mixed RTL")]
	[DataRow("Hello שלום עולם world مرحبا بالعالم end", TextWrapping.NoWrap, TextTrimming.CharacterEllipsis, 0, FlowDirection.RightToLeft, DisplayName = "RTL paragraph + CharacterEllipsis")]
	[DataRow("Hi \U0001F600 there \U0001F389 friends ✓ done", TextWrapping.Wrap, TextTrimming.None, 0, FlowDirection.LeftToRight, DisplayName = "Fallback fonts")]
	public async Task When_Width_Changes_Layout_Matches_A_Fresh_Layout(string text, TextWrapping wrapping, TextTrimming trimming, int maxLines, FlowDirection flowDirection)
	{
		TextBlock Create(double width) => new()
		{
			Text = text,
			TextWrapping = wrapping,
			TextTrimming = trimming,
			MaxLines = maxLines,
			FlowDirection = flowDirection,
			TextAlignment = TextAlignment.Center,
			Width = width,
			HorizontalAlignment = HorizontalAlignment.Left,
		};

		var widths = new double[] { 600, 180, 70, 35, 260, 600 };
		var SUT = Create(widths[0]);
		var panel = new StackPanel { Children = { SUT } };
		WindowHelper.WindowContent = panel;
		await WindowHelper.WaitForLoaded(SUT);
		await WaitForFontsLoaded(SUT);
		var shaped = SUT.CachedShapedText;

		foreach (var width in widths)
		{
			SUT.Width = width;
			var fresh = Create(width);
			panel.Children.Add(fresh);
			panel.UpdateLayout();

			Assert.AreSame(shaped, SUT.CachedShapedText, "The width change should reuse the shaping result.");
			Assert.AreNotSame(SUT.CachedShapedText, fresh.CachedShapedText);
			Assert.AreEqual(fresh.DesiredSize, SUT.DesiredSize, $"DesiredSize at width {width}");
			Assert.AreEqual(fresh.IsTextTrimmed, SUT.IsTextTrimmed, $"IsTextTrimmed at width {width}");
			AssertSameCharacterRects(fresh, SUT, $"at width {width}");

			panel.Children.Remove(fresh);
		}
	}

	[TestMethod]
	public async Task When_FlowDirection_Changes_Text_Is_Shaped_Again()
	{
		// FlowDirection sets the paragraph's bidi level and doesn't go through InvalidateInlines.
		const string text = "Hello שלום world";
		var SUT = new TextBlock { Text = text };
		var reference = new TextBlock { Text = text, FlowDirection = FlowDirection.RightToLeft };
		WindowHelper.WindowContent = new StackPanel { Children = { SUT, reference } };
		await WindowHelper.WaitForLoaded(SUT);
		await WaitForFontsLoaded(SUT);
		var shaped = SUT.CachedShapedText;

		SUT.FlowDirection = FlowDirection.RightToLeft;
		SUT.UpdateLayout();

		Assert.AreNotSame(shaped, SUT.CachedShapedText);
		AssertSameCharacterRects(reference, SUT, "after the flow direction change");
	}

	[TestMethod]
	public async Task When_UICulture_Changes_Text_Is_Shaped_Again()
	{
		// ICU line and word boundaries depend on the UI culture. A culture change doesn't invalidate the TextBlock,
		// so the next layout, for whatever reason, must notice it.
		var original = CultureInfo.CurrentUICulture;
		try
		{
			var SUT = new TextBlock { Text = Sentence };
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			var shaped = SUT.CachedShapedText;

			CultureInfo.CurrentUICulture = new CultureInfo(original.Name == "fr-FR" ? "de-DE" : "fr-FR");
			SUT.InvalidateMeasure();
			SUT.UpdateLayout();

			Assert.AreNotSame(shaped, SUT.CachedShapedText);
		}
		finally
		{
			CultureInfo.CurrentUICulture = original;
		}
	}

	[TestMethod]
	public async Task When_Text_Scale_Changes_Text_Is_Shaped_Again()
	{
		// A text-scale change gives each inline another font size (FontInfo).
		var originalOverride = FeatureConfiguration.Font.TextScaleFactor;
		try
		{
			var SUT = new TextBlock { Text = "Hello", IsTextScaleFactorEnabled = true };
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			var shaped = SUT.CachedShapedText;
			var width = SUT.ActualWidth;

			FeatureConfiguration.Font.TextScaleFactor = 1.5;
			global::Uno.UI.Xaml.Core.CoreServices.Instance.UpdateFontScale(1.5);
			SUT.UpdateLayout();

			Assert.AreNotSame(shaped, SUT.CachedShapedText);
			Assert.IsTrue(SUT.ActualWidth > width * 1.2, $"{SUT.ActualWidth} vs {width}");
		}
		finally
		{
			FeatureConfiguration.Font.TextScaleFactor = originalOverride;
			global::Uno.UI.Xaml.Core.CoreServices.Instance.UpdateFontScale(originalOverride ?? 1.0);
		}
	}

	[TestMethod]
	public async Task When_A_Font_Loads_Text_Is_Shaped_Again()
	{
		// A fallback font that finishes loading after the text was shaped calls the TextBlock's IFontCacheUpdateListener,
		// which must drop the shaping even when measure is already invalid. The fallback service is fixed at startup, so
		// this calls the listener directly, as GetFallbackFont's continuation does.
		var SUT = new TextBlock { Text = Sentence };
		WindowHelper.WindowContent = SUT;
		await WindowHelper.WaitForLoaded(SUT);
		var shaped = SUT.CachedShapedText;

		SUT.InvalidateMeasure(); // already dirty, so the InvalidateMeasure inside Invalidate() returns early
		((UnicodeText.IFontCacheUpdateListener)SUT).Invalidate();
		Assert.IsNull(SUT.CachedShapedText);
		SUT.UpdateLayout();

		Assert.IsNotNull(SUT.CachedShapedText);
		Assert.AreNotSame(shaped, SUT.CachedShapedText);
	}

	[TestMethod]
	public async Task When_TextBox_Narrows_Layout_Matches_A_Fresh_TextBox()
	{
		// A TextBox's display block measures trailing spaces and adds the caret thickness to its size: check every caret
		// position, including after the trailing spaces, once narrowed, against a TextBox created at that width.
		const string text = "Hello world   ";
		TextBox Create(double width) => new() { Text = text, Width = width, TextAlignment = TextAlignment.Right };
		var SUT = Create(300);
		var panel = new StackPanel { Children = { SUT } };
		WindowHelper.WindowContent = panel;
		await WindowHelper.WaitForLoaded(SUT);
		await WaitForFontsLoaded(SUT.TextBoxView!.DisplayBlock);
		var shaped = SUT.TextBoxView.DisplayBlock.CachedShapedText;

		SUT.Width = 200;
		var fresh = Create(200);
		panel.Children.Add(fresh);
		await WindowHelper.WaitForLoaded(fresh);
		await WindowHelper.WaitForIdle();

		Assert.AreSame(shaped, SUT.TextBoxView.DisplayBlock.CachedShapedText, "Narrowing should not shape again.");
		AssertSameCharacterRects(fresh.TextBoxView!.DisplayBlock, SUT.TextBoxView.DisplayBlock, "after narrowing");
	}

	[TestMethod]
	public async Task When_TextBox_Default_Alignment_Comes_From_The_Text()
	{
		// A TextBox with the default TextAlignment passes no alignment to the layout, which then takes it from the first
		// bidi run: Right for text that starts in Hebrew. Check it after narrowing.
		const string text = "שלום world";
		TextBox Create(double width) => new() { Text = text, Width = width };
		var SUT = Create(300);
		var panel = new StackPanel { Children = { SUT } };
		WindowHelper.WindowContent = panel;
		await WindowHelper.WaitForLoaded(SUT);
		await WaitForFontsLoaded(SUT.TextBoxView!.DisplayBlock);

		SUT.Width = 200;
		var fresh = Create(200);
		panel.Children.Add(fresh);
		await WindowHelper.WaitForLoaded(fresh);
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(TextAlignment.Right, ((UnicodeText)SUT.TextBoxView.DisplayBlock.ParsedText).TextAlignment);
		AssertSameCharacterRects(fresh.TextBoxView!.DisplayBlock, SUT.TextBoxView.DisplayBlock, "after narrowing");
	}

	private static void AssertSameCharacterRects(TextBlock expected, TextBlock actual, string context)
	{
		var length = actual.Inlines.TraversedTree.leafTree.Sum(inline => inline.GetText()?.Length ?? 0);
		for (var index = 0; index <= length; index++)
		{
			Assert.AreEqual(expected.ParsedText.GetRectForIndex(index), actual.ParsedText.GetRectForIndex(index), $"Rect of index {index} {context}");
		}
	}

	private static TextBlock GetDisplayBlock(RichEditBox editor)
	{
		var block = editor.FindFirstChild<ScrollViewer>(viewer => viewer.Name == "ContentElement")?.Content as TextBlock;
		Assert.IsNotNull(block);
		return block;
	}

	// A fallback font (emoji, Hebrew, Arabic) may be fetched asynchronously, and its arrival legitimately shapes the text
	// again. Wait for every font the text needs, then for the layouts that follow, so the AreSame checks afterwards hold.
	private static async Task WaitForFontsLoaded(TextBlock textBlock)
	{
		var size = (float)textBlock.FontSize;
		var (weight, stretch, style) = (textBlock.FontWeight, textBlock.FontStretch, textBlock.FontStyle);
		var primary = await FontDetailsCache.GetFont(textBlock.FontFamily?.Source, size, weight, stretch, style).loadedTask;
		await IgnoreFailure(FontDetailsCache.GetFont(FeatureConfiguration.Font.SymbolsFont, size, weight, stretch, style).loadedTask);
		var text = string.Concat(textBlock.Inlines.TraversedTree.leafTree.Select(inline => inline.GetText()));
		foreach (var rune in text.EnumerateRunes())
		{
			if (!primary.FontHandle.ContainsGlyph(rune.Value))
			{
				await IgnoreFailure(FontDetailsCache.GetFontForCodepoint(rune.Value, size, weight, stretch, style));
			}
		}

		// The loads re-invalidate the text through the dispatcher: wait until the shaping holds for a few idle passes.
		var stablePasses = 0;
		for (var i = 0; i < 100 && stablePasses < 3; i++)
		{
			var shaped = textBlock.CachedShapedText;
			await Task.Delay(50);
			await WindowHelper.WaitForIdle();
			stablePasses = shaped is not null && ReferenceEquals(shaped, textBlock.CachedShapedText) ? stablePasses + 1 : 0;
		}

		Assert.AreEqual(3, stablePasses, "The text kept being shaped again after its fonts loaded.");

		static async Task IgnoreFailure(Task task)
		{
			try
			{
				await task;
			}
			catch (Exception)
			{
				// A font that fails to load leaves the text on its fallback, which is what the layout then keeps.
			}
		}
	}
}
#endif
