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
/// Arrange also keeps the measure layout while the arrange size can't change how the lines break.
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
	public async Task When_A_Custom_Layout_Is_Set_Shaping_Is_Dropped()
	{
		var SUT = new TextBlock { Text = Sentence };
		try
		{
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();
			Assert.IsNotNull(SUT.CachedShapedText);

			SUT.CustomTextLayout = new EmptyTextLayout();

			Assert.IsNull(SUT.CachedShapedText);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Text_Is_Long_No_Shaping_Is_Kept()
	{
		// Long text is mostly edited, which misses the cache anyway, so its shaping isn't kept; a new width still lays it out
		// like a fresh block.
		var text = string.Concat(Enumerable.Repeat(Sentence + " ", 16));
		TextBlock Create(double width) => new() { Text = text, TextWrapping = TextWrapping.Wrap, Width = width, HorizontalAlignment = HorizontalAlignment.Left };
		var SUT = Create(400);
		var shortText = Create(400);
		shortText.Text = Sentence;
		var panel = new StackPanel { Children = { SUT, shortText } };
		try
		{
			WindowHelper.WindowContent = panel;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();
			Assert.IsNull(SUT.CachedShapedText, "Long text should not keep its shaping.");
			Assert.IsNotNull(shortText.CachedShapedText, "Short text should keep its shaping.");

			SUT.Width = 250;
			var fresh = Create(250);
			panel.Children.Add(fresh);
			await WindowHelper.WaitForLoaded(fresh);
			await WindowHelper.WaitForIdle();

			Assert.IsNull(SUT.CachedShapedText);
			AssertSameCharacterRects(fresh, SUT, "after the width change");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
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
	public async Task When_IsSpellCheckEnabled_Changes_The_Corrections_Follow()
	{
		// The spell-check corrections are computed with the layout. Turning spell check on or off must lay out again rather
		// than keep a layout built with the other setting.
		const string text = "One typo here";
		var typoIndex = text.IndexOf("typo", StringComparison.Ordinal);
		UnicodeText.SpellCheckingServiceOverrideForTesting = new TypoSpellCheckingService();
		try
		{
			var SUT = new TextBlock { Text = text };
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();
			Assert.IsNull(((UnicodeText)SUT.ParsedText).GetCorrectionAtIndex(typoIndex), "Spell check is off.");

			SUT.IsSpellCheckEnabled = true;
			SUT.UpdateLayout();
			Assert.IsNotNull(((UnicodeText)SUT.ParsedText).GetCorrectionAtIndex(typoIndex), "Turning spell check on should find the typo.");

			var layoutVersion = SUT.TextLayoutVersion;
			SUT.IsSpellCheckEnabled = true;
			SUT.UpdateLayout();
			Assert.AreEqual(layoutVersion, SUT.TextLayoutVersion, "Setting the same value should not lay out again.");

			SUT.IsSpellCheckEnabled = false;
			SUT.UpdateLayout();
			Assert.IsNull(((UnicodeText)SUT.ParsedText).GetCorrectionAtIndex(typoIndex), "Turning spell check off should drop the correction.");
		}
		finally
		{
			UnicodeText.SpellCheckingServiceOverrideForTesting = null;
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_TextBox_IsSpellCheckEnabled_Changes_The_Corrections_Follow()
	{
		// The same through the public TextBox property, which hands the setting to its display block.
		const string text = "One typo here";
		var typoIndex = text.IndexOf("typo", StringComparison.Ordinal);
		UnicodeText.SpellCheckingServiceOverrideForTesting = new TypoSpellCheckingService();
		try
		{
			var SUT = new TextBox { Text = text, Width = 300, IsSpellCheckEnabled = true };
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();
			var displayBlock = SUT.TextBoxView!.DisplayBlock;
			Assert.IsNotNull(((UnicodeText)displayBlock.ParsedText).GetCorrectionAtIndex(typoIndex), "Spell check is on.");

			SUT.IsSpellCheckEnabled = false;
			await WindowHelper.WaitForIdle();
			Assert.IsNull(((UnicodeText)displayBlock.ParsedText).GetCorrectionAtIndex(typoIndex), "Turning spell check off should drop the correction.");

			SUT.IsSpellCheckEnabled = true;
			await WindowHelper.WaitForIdle();
			Assert.IsNotNull(((UnicodeText)displayBlock.ParsedText).GetCorrectionAtIndex(typoIndex), "Turning spell check on again should find the typo.");
		}
		finally
		{
			UnicodeText.SpellCheckingServiceOverrideForTesting = null;
			WindowHelper.WindowContent = null;
		}
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

	// Arrange reuses the layout built by measure when laying out again at the arrange size would break, trim and truncate the
	// lines identically, and builds a new one otherwise. Either way the result must equal a layout made at the arrange size.
	// NaN arrange width/height = the TextBlock's desired size. (An element is never arranged below its desired size, it is
	// clipped, so a narrower or shorter arrange can only reach its desired size.)
	[TestMethod]
	[DataRow(Sentence, TextWrapping.Wrap, TextTrimming.None, FlowDirection.LeftToRight, 300, double.PositiveInfinity, double.NaN, double.NaN, true, DisplayName = "Centred wrapping label arranged at its desired width: reused")]
	[DataRow(Sentence, TextWrapping.Wrap, TextTrimming.CharacterEllipsis, FlowDirection.LeftToRight, 200, 40, double.NaN, double.NaN, true, DisplayName = "Wrapping label cut by height, arranged at its desired size: reused")]
	[DataRow("Hello שלום עולם world مرحبا بالعالم end", TextWrapping.Wrap, TextTrimming.None, FlowDirection.RightToLeft, 160, double.PositiveInfinity, double.NaN, double.NaN, true, DisplayName = "RTL wrapping label arranged at its desired width: reused")]
	[DataRow(Sentence, TextWrapping.Wrap, TextTrimming.None, FlowDirection.LeftToRight, 150, double.PositiveInfinity, 400, 1000, false, DisplayName = "Arranged wider than measured, lines could join: rebuilt")]
	[DataRow(Sentence, TextWrapping.NoWrap, TextTrimming.CharacterEllipsis, FlowDirection.LeftToRight, 120, double.PositiveInfinity, 300, double.NaN, false, DisplayName = "Trimmed, arranged wide enough to show more: rebuilt")]
	[DataRow(Sentence, TextWrapping.Wrap, TextTrimming.CharacterEllipsis, FlowDirection.LeftToRight, 200, 40, double.NaN, 200, false, DisplayName = "Cut by height, arranged tall enough for more lines: rebuilt")]
	public async Task When_Arranged_At_Another_Size_Than_Measured(string text, TextWrapping wrapping, TextTrimming trimming, FlowDirection flowDirection, double measureWidth, double measureHeight, double arrangeWidth, double arrangeHeight, bool expectReuse)
	{
		await ArrangeAndCompareWithFresh(
			() => new TextBlock
			{
				Text = text,
				TextWrapping = wrapping,
				TextTrimming = trimming,
				FlowDirection = flowDirection,
				TextAlignment = TextAlignment.Center,
			},
			new Size(measureWidth, measureHeight), arrangeWidth, arrangeHeight, expectReuse);
	}

	[TestMethod]
	[DataRow(double.NaN, double.NaN, true, DisplayName = "Cut by height with BlockLineHeight, arranged at its desired size: reused")]
	[DataRow(double.NaN, 200, false, DisplayName = "Cut by height with BlockLineHeight, arranged taller: rebuilt")]
	public async Task When_Arranged_With_BlockLineHeight(double arrangeWidth, double arrangeHeight, bool expectReuse)
	{
		// The height comparisons use LineHeight, not the font's line height, for each next line.
		await ArrangeAndCompareWithFresh(
			() => new TextBlock
			{
				Text = Sentence,
				TextWrapping = TextWrapping.Wrap,
				TextTrimming = TextTrimming.CharacterEllipsis,
				LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
				LineHeight = 30,
				TextAlignment = TextAlignment.Center,
			},
			new Size(200, 70), arrangeWidth, arrangeHeight, expectReuse);
	}

	[TestMethod]
	[DataRow(300, double.NaN, double.NaN, true, DisplayName = "WrapWholeWords arranged at its desired width: reused")]
	[DataRow(150, 400, 1000, false, DisplayName = "WrapWholeWords arranged wider, lines could join: rebuilt")]
	public async Task When_Arranged_With_WrapWholeWords(double measureWidth, double arrangeWidth, double arrangeHeight, bool expectReuse)
	{
		// WrapWholeWords only breaks between chunks (no mid-chunk break), including after a word longer than the line.
		await ArrangeAndCompareWithFresh(
			() => new TextBlock
			{
				Text = Sentence + " Supercalifragilisticexpialidociously",
				TextWrapping = TextWrapping.WrapWholeWords,
				TextAlignment = TextAlignment.Right,
			},
			new Size(measureWidth, double.PositiveInfinity), arrangeWidth, arrangeHeight, expectReuse);
	}

	[TestMethod]
	public async Task When_Justified_Arranged_Wider_Than_Measured()
	{
		// Justification spreads each line over the width itself, so a justified layout is only reused at that exact width,
		// even where the lines would break the same.
		await ArrangeAndCompareWithFresh(
			() => new TextBlock
			{
				Text = Sentence,
				TextWrapping = TextWrapping.Wrap,
				TextAlignment = TextAlignment.Justify,
			},
			new Size(300, double.PositiveInfinity), 302, double.NaN, expectReuse: false);
	}

	[TestMethod]
	[DataRow(true, DisplayName = "Justified paragraph")]
	[DataRow(false, DisplayName = "Indented paragraph")]
	public async Task When_Paragraph_Format_Reads_The_Width_Layout_Is_Only_Reused_At_That_Width(bool justified)
	{
		// RichEditBox paragraph formats: per-paragraph justification and indents read the width itself.
		var (SUT, _) = await ArrangeAndCompareWithFresh(
			() =>
			{
				var run = new Run { Text = Sentence };
				if (justified)
				{
					run.ParagraphAlignment = TextAlignment.Justify;
				}
				else
				{
					run.ParagraphLayout = new ParagraphLayoutInfo { LeftIndent = 20, RightIndent = 30, FirstLineIndent = 10 };
				}

				var textBlock = new TextBlock { TextWrapping = TextWrapping.Wrap };
				textBlock.Inlines.Add(run);
				return textBlock;
			},
			new Size(300, double.PositiveInfinity), 302, double.NaN, expectReuse: false);

		Assert.AreEqual((302f, MathF.BitIncrement(302f)), ((UnicodeText)SUT.ParsedText).ReusableWidths);
	}

	[TestMethod]
	public async Task When_RichEditBox_Width_Changes_Layout_Matches_A_Fresh_Editor()
	{
		// A small document goes through the display block's own TextBlock path, so its paragraph formats (justification,
		// indents, list markers) go through the shaping cache and the arrange reuse.
		RichEditBox Create(double width) => new() { Width = width };
		void Populate(RichEditBox editor)
		{
			editor.Document.SetText(Microsoft.UI.Text.TextSetOptions.None, Sentence + "\r" + Sentence);
			var first = editor.Document.GetRange(0, Sentence.Length).ParagraphFormat;
			first.Alignment = Microsoft.UI.Text.ParagraphAlignment.Justify;
			first.SetIndents(10, 20, 30);
			editor.Document.GetRange(Sentence.Length + 1, Sentence.Length + 2).ParagraphFormat.ListType = Microsoft.UI.Text.MarkerType.Bullet;
		}

		var SUT = Create(320);
		var panel = new StackPanel { Children = { SUT } };
		try
		{
			WindowHelper.WindowContent = panel;
			await WindowHelper.WaitForLoaded(SUT);
			Populate(SUT);
			await WindowHelper.WaitForIdle();
			var displayBlock = GetDisplayBlock(SUT);
			Assert.IsNull(displayBlock.CustomTextLayout, "The document should be laid out by the TextBlock itself.");

			foreach (var width in new double[] { 280, 360, 300 })
			{
				SUT.Width = width;
				var fresh = Create(width);
				panel.Children.Add(fresh);
				await WindowHelper.WaitForLoaded(fresh);
				Populate(fresh);
				await WindowHelper.WaitForIdle();

				AssertSameCharacterRects(GetDisplayBlock(fresh), displayBlock, $"at width {width}");
				panel.Children.Remove(fresh);
			}
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Arrange_Layout_Reuse_Is_Disabled_Arrange_Lays_Out_Again()
	{
		try
		{
			FeatureConfiguration.TextBlock.EnableArrangeLayoutReuse = false;
			await ArrangeAndCompareWithFresh(
				() => new TextBlock { Text = Sentence, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center },
				new Size(300, double.PositiveInfinity), double.NaN, double.NaN, expectReuse: false);
		}
		finally
		{
			FeatureConfiguration.TextBlock.EnableArrangeLayoutReuse = true;
		}
	}

	[TestMethod]
	[DataRow(TextAlignment.Center, DisplayName = "Center")]
	[DataRow(TextAlignment.Right, DisplayName = "Right")]
	public async Task When_Arrange_Reuses_Layout_Hit_Testing_Matches_A_Fresh_Layout(TextAlignment alignment)
	{
		// Hit-testing reads the available size (for alignment) at call time, so it must follow the arrange size.
		TextBlock Create()
		{
			var textBlock = new TextBlock { TextWrapping = TextWrapping.Wrap, TextAlignment = alignment };
			textBlock.Inlines.Add(new Run { Text = "Read the " });
			var hyperlink = new Hyperlink();
			hyperlink.Inlines.Add(new Run { Text = "documentation" });
			textBlock.Inlines.Add(hyperlink);
			textBlock.Inlines.Add(new Run { Text = " for more about wrapping, trimming and alignment of text." });
			return textBlock;
		}

		var (SUT, fresh) = await ArrangeAndCompareWithFresh(Create, new Size(400, double.PositiveInfinity), double.NaN, double.NaN, expectReuse: true);

		var hyperlinkHits = 0;
		for (var y = 1.0; y < SUT.DesiredSize.Height; y += 4)
		{
			for (var x = -5.0; x < SUT.DesiredSize.Width + 5; x += 3)
			{
				var point = new Point(x, y);
				Assert.AreEqual(fresh.ParsedText.GetIndexAt(point, false, false), SUT.ParsedText.GetIndexAt(point, false, false), $"GetIndexAt {point}");
				Assert.AreEqual(fresh.ParsedText.GetIndexAt(point, true, true), SUT.ParsedText.GetIndexAt(point, true, true), $"GetIndexAt (extended) {point}");
				var hit = SUT.ParsedText.GetHyperlinkAt(point) is not null;
				Assert.AreEqual(fresh.ParsedText.GetHyperlinkAt(point) is not null, hit, $"GetHyperlinkAt {point}");
				hyperlinkHits += hit ? 1 : 0;
			}
		}

		Assert.IsTrue(hyperlinkHits > 0, "No point hit the hyperlink.");
	}

	[TestMethod]
	[DataRow(TextAlignment.Center, FlowDirection.LeftToRight, DisplayName = "Center")]
	[DataRow(TextAlignment.Right, FlowDirection.LeftToRight, DisplayName = "Right")]
	[DataRow(TextAlignment.Right, FlowDirection.RightToLeft, DisplayName = "RTL")]
	public async Task When_Padding_Is_Larger_Than_The_Arrange_Size(TextAlignment alignment, FlowDirection flowDirection)
	{
		// Measure clamps the size without padding to zero; arrange doesn't. With Padding 10, an ArrangeOverride at 5x5 lays out
		// at -15x-15, and that layout must not be reused from measure (at 0): it must be the one a fresh TextBlock builds there.
		// Layout never arranges an element below its desired size, which includes the padding, so this calls ArrangeOverride
		// directly, as a custom panel could.
		const string text = "Hello world, again";
		ArrangeOverrideTextBlock Create() => new()
		{
			Text = text,
			Padding = new Thickness(10),
			TextAlignment = alignment,
			FlowDirection = flowDirection,
			TextWrapping = TextWrapping.Wrap,
		};

		var SUT = Create();
		var fresh = Create();
		WindowHelper.WindowContent = new StackPanel { Children = { SUT, fresh } };
		await WindowHelper.WaitForLoaded(SUT);
		await WindowHelper.WaitForLoaded(fresh);
		await WaitForFontsLoaded(SUT);

		var measured = (UnicodeText)SUT.ParsedText;
		SUT.CallArrangeOverride(new Size(5, 5));
		Assert.IsFalse(measured.SharesLinesWith((UnicodeText)SUT.ParsedText), "Arrange should lay out again at the negative size.");
		Assert.AreEqual(new Size(-15, -15), ((UnicodeText)SUT.ParsedText).AvailableSize);

		fresh.CallArrangeOverride(new Size(5, 5));
		Assert.AreEqual(new Size(-15, -15), ((UnicodeText)fresh.ParsedText).AvailableSize);

		AssertSameCharacterRects(fresh, SUT, "at the negative size");
	}

	[TestMethod]
	public async Task When_Arranged_At_The_Edges_Of_The_Reuse_Range()
	{
		// The layout is reusable for a width in [min, maxExclusive): min is the widest content a comparison kept on a line,
		// maxExclusive the narrowest overflow that broke one. Check both edges exactly, and that a reused layout at each
		// in-range edge equals a fresh one.
		var SUT = new TextBlock { Text = Sentence, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center };
		var panel = new SizingPanel { MeasureSize = new Size(150, double.PositiveInfinity), Children = { SUT } };
		WindowHelper.WindowContent = panel;
		await WindowHelper.WaitForLoaded(SUT);
		await WaitForFontsLoaded(SUT);

		var measured = (UnicodeText)SUT.ParsedText;
		var (min, maxExclusive) = measured.ReusableWidths;
		Assert.IsTrue(min > 0 && min <= 150 && maxExclusive > 150 && !float.IsInfinity(maxExclusive), $"Unexpected range [{min}, {maxExclusive})");

		const double height = 1000;
		Assert.IsFalse(measured.TryReuseForSize(new Size(MathF.BitDecrement(min), height), out _), "Just below min");
		Assert.IsFalse(measured.TryReuseForSize(new Size(maxExclusive, height), out _), "At maxExclusive");

		WindowHelper.WindowContent = null;
		foreach (var width in new double[] { min, MathF.BitDecrement(maxExclusive) })
		{
			Assert.IsTrue(measured.TryReuseForSize(new Size(width, height), out var reused), $"At {width}");

			// No layout rounding, which would round the desired width up and arrange (and center) the reference wider.
			var fresh = new TextBlock { Text = Sentence, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center, UseLayoutRounding = false };
			var freshPanel = new SizingPanel { MeasureSize = new Size(width, height), ArrangeSize = new Size(width, height), Children = { fresh }, UseLayoutRounding = false };
			WindowHelper.WindowContent = freshPanel;
			await WindowHelper.WaitForLoaded(fresh);
			freshPanel.UpdateLayout();

			for (var index = 0; index <= Sentence.Length; index++)
			{
				Assert.AreEqual(fresh.ParsedText.GetRectForIndex(index), reused.GetRectForIndex(index), $"Rect of index {index} at width {width}");
			}
		}
	}

	/// <summary>
	/// Measures a TextBlock at <paramref name="measureSize"/> and arranges it at the given size (NaN = its desired size),
	/// checks whether arrange reused the measure layout, and compares it with a TextBlock measured and arranged at that size.
	/// </summary>
	private static async Task<(TextBlock SUT, TextBlock fresh)> ArrangeAndCompareWithFresh(Func<TextBlock> create, Size measureSize, double arrangeWidth, double arrangeHeight, bool expectReuse)
	{
		TextBlock Create()
		{
			var textBlock = create();
			textBlock.HorizontalAlignment = HorizontalAlignment.Stretch;
			textBlock.VerticalAlignment = VerticalAlignment.Stretch;
			return textBlock;
		}

		var SUT = Create();
		var panel = new SizingPanel { MeasureSize = measureSize, Children = { SUT } };
		var fresh = Create();
		var freshPanel = new SizingPanel { Children = { fresh } };
		WindowHelper.WindowContent = new StackPanel { Children = { panel, freshPanel } };
		await WindowHelper.WaitForLoaded(SUT);
		await WaitForFontsLoaded(SUT);

		var arrangeSize = new Size(
			double.IsNaN(arrangeWidth) ? SUT.DesiredSize.Width : arrangeWidth,
			double.IsNaN(arrangeHeight) ? SUT.DesiredSize.Height : arrangeHeight);
		panel.ArrangeSize = arrangeSize;
		// Measure and arrange as the panel does, and check whether arrange kept the measure layout: a reused layout shares its
		// lines with the measure layout, a new one has its own.
		SUT.InvalidateMeasure();
		SUT.Measure(panel.MeasureSize);
		var measured = (UnicodeText)SUT.ParsedText;
		SUT.InvalidateArrange();
		SUT.Arrange(new Rect(new Point(0, 0), arrangeSize));
		var reused = measured.SharesLinesWith((UnicodeText)SUT.ParsedText);

		Assert.AreEqual(expectReuse, reused, expectReuse ? "Arrange should reuse the measure layout." : "Arrange should build a new layout.");

		// The reference: a TextBlock measured and arranged at SUT's arrange size, so built once at that size.
		freshPanel.MeasureSize = arrangeSize;
		freshPanel.ArrangeSize = arrangeSize;
		freshPanel.InvalidateMeasure();
		freshPanel.InvalidateArrange();
		freshPanel.UpdateLayout();
		if (expectReuse)
		{
			// Not compared after a rebuild: IsTextTrimmed is computed in ArrangeOverride against the ActualWidth and
			// ActualHeight of the previous arrange (they are updated after ArrangeOverride), so it lags a size change. That is
			// unrelated to the layout reuse, which only takes the path where the layout, and so its size, is unchanged.
			Assert.AreEqual(fresh.IsTextTrimmed, SUT.IsTextTrimmed, "IsTextTrimmed");
		}

		AssertSameCharacterRects(fresh, SUT, "at the arrange size");
		return (SUT, fresh);
	}

	/// <summary>Measures its children at <see cref="MeasureSize"/> and arranges them at <see cref="ArrangeSize"/>.</summary>
	private sealed class SizingPanel : Panel
	{
		public Size MeasureSize { get; set; } = new(double.PositiveInfinity, double.PositiveInfinity);

		public Size? ArrangeSize { get; set; }

		protected override Size MeasureOverride(Size availableSize)
		{
			foreach (var child in Children)
			{
				child.Measure(MeasureSize);
			}

			return new Size(1000, 200);
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			foreach (var child in Children)
			{
				child.Arrange(new Rect(new Point(0, 0), ArrangeSize ?? child.DesiredSize));
			}

			return finalSize;
		}
	}

	private sealed class ArrangeOverrideTextBlock : TextBlock
	{
		public Size CallArrangeOverride(Size finalSize) => ArrangeOverride(finalSize);
	}

	private static void AssertSameCharacterRects(TextBlock expected, TextBlock actual, string context)
	{
		var length = actual.Inlines.TraversedTree.leafTree.Sum(inline => inline.GetText()?.Length ?? 0);
		for (var index = 0; index <= length; index++)
		{
			Assert.AreEqual(expected.ParsedText.GetRectForIndex(index), actual.ParsedText.GetRectForIndex(index), $"Rect of index {index} {context}");
		}
	}

	private sealed class EmptyTextLayout : ICustomTextLayout
	{
		public IParsedText Create(Size availableSize, Inline[] inlines, FontDetails defaultFontDetails, UnicodeText.IFontCacheUpdateListener fontListener, Microsoft.UI.Xaml.Media.Brush defaultForeground, TextAlignment? textAlignment, out Size size)
		{
			size = default;
			return ParsedText.Empty;
		}
	}

	private static TextBlock GetDisplayBlock(RichEditBox editor)
	{
		var block = editor.FindFirstChild<ScrollViewer>(viewer => viewer.Name == "ContentElement")?.Content as TextBlock;
		Assert.IsNotNull(block);
		return block;
	}

	/// <summary>Flags every word "typo".</summary>
	private sealed class TypoSpellCheckingService : ISpellCheckingService
	{
		public System.Collections.Generic.List<(int correctionStart, int correctionEnd)?> SpellCheck(System.Collections.Generic.List<int> wordBoundaries, string text)
		{
			var corrections = new System.Collections.Generic.List<(int correctionStart, int correctionEnd)?>(wordBoundaries.Count);
			var wordStart = 0;
			foreach (var wordEnd in wordBoundaries)
			{
				var word = text.Substring(wordStart, wordEnd - wordStart);
				var trimmed = word.Trim();
				if (trimmed == "typo")
				{
					var offset = word.IndexOf(trimmed, StringComparison.Ordinal);
					corrections.Add((offset, offset + trimmed.Length));
				}
				else
				{
					corrections.Add(null);
				}

				wordStart = wordEnd;
			}

			return corrections;
		}

		public (int replaceIndexStart, int replaceIndexEnd, System.Collections.Generic.List<string> suggestions)? GetSpellCheckSuggestions(string text, System.Collections.Generic.List<int> wordBoundaries, int correctionStart, int correctionEnd)
			=> null;
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
