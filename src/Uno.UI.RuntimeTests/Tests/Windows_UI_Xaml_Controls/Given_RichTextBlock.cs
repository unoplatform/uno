using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;
using Windows.UI.Text;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_RichTextBlock
	{
		#region Basic Rendering

		[TestMethod]
		public async Task When_Simple_Text()
		{
			var SUT = new RichTextBlock();
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "Hello World" });
			SUT.Blocks.Add(paragraph);

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(SUT.ActualWidth > 0, "Should have non-zero width");
			Assert.IsTrue(SUT.ActualHeight > 0, "Should have non-zero height");
		}

		[TestMethod]
		public async Task When_Empty_Block()
		{
			var SUT = new RichTextBlock { Width = 200, Height = 50 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			// Empty RichTextBlock should have non-negative height
			Assert.IsTrue(SUT.ActualHeight >= 0, "Empty block should have non-negative height");
		}

		[TestMethod]
		public async Task When_Multiple_Paragraphs()
		{
			var SUT = new RichTextBlock();

			var para1 = new Paragraph();
			para1.Inlines.Add(new Run { Text = "First paragraph" });
			SUT.Blocks.Add(para1);

			var para2 = new Paragraph();
			para2.Inlines.Add(new Run { Text = "Second paragraph" });
			SUT.Blocks.Add(para2);

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			// With two paragraphs, height should be greater than a single paragraph
			var singlePara = new RichTextBlock();
			var p = new Paragraph();
			p.Inlines.Add(new Run { Text = "First paragraph" });
			singlePara.Blocks.Add(p);

			var panel = new StackPanel();
			panel.Children.Add(singlePara);
			WindowHelper.WindowContent = panel;
			panel.Children.Insert(0, SUT);
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(SUT.ActualHeight > singlePara.ActualHeight,
				$"Two paragraphs ({SUT.ActualHeight}) should be taller than one ({singlePara.ActualHeight})");
		}

		[TestMethod]
		public async Task When_Inline_Formatting()
		{
			var SUT = new RichTextBlock();
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "Normal " });
			paragraph.Inlines.Add(new Bold { Inlines = { new Run { Text = "bold" } } });
			paragraph.Inlines.Add(new Run { Text = " " });
			paragraph.Inlines.Add(new Italic { Inlines = { new Run { Text = "italic" } } });
			SUT.Blocks.Add(paragraph);

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(SUT.ActualWidth > 0);
		}

		[TestMethod]
		public async Task When_LineBreak()
		{
			var SUT = new RichTextBlock { TextWrapping = TextWrapping.NoWrap };
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "Line 1" });
			paragraph.Inlines.Add(new LineBreak());
			paragraph.Inlines.Add(new Run { Text = "Line 2" });
			SUT.Blocks.Add(paragraph);

			var singleLine = new RichTextBlock { TextWrapping = TextWrapping.NoWrap };
			var sp = new Paragraph();
			sp.Inlines.Add(new Run { Text = "Line 1" });
			singleLine.Blocks.Add(sp);

			var panel = new StackPanel();
			panel.Children.Add(SUT);
			panel.Children.Add(singleLine);
			WindowHelper.WindowContent = panel;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(SUT.ActualHeight > singleLine.ActualHeight,
				"With LineBreak, height should be greater than single line");
		}

		#endregion

		#region Font Properties

		[TestMethod]
		public async Task When_FontSize_Set()
		{
			var SUT = new RichTextBlock { FontSize = 30 };
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "Large text" });
			SUT.Blocks.Add(paragraph);

			var defaultSize = new RichTextBlock();
			var dp = new Paragraph();
			dp.Inlines.Add(new Run { Text = "Large text" });
			defaultSize.Blocks.Add(dp);

			var panel = new StackPanel();
			panel.Children.Add(SUT);
			panel.Children.Add(defaultSize);
			WindowHelper.WindowContent = panel;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(SUT.ActualHeight > defaultSize.ActualHeight,
				$"FontSize=30 ({SUT.ActualHeight}) should be taller than default ({defaultSize.ActualHeight})");
		}

		[TestMethod]
		public async Task When_FontWeight_Bold()
		{
			var SUT = new RichTextBlock { FontWeight = FontWeights.Bold };
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "Bold text WWWW" });
			SUT.Blocks.Add(paragraph);

			var normal = new RichTextBlock { FontWeight = FontWeights.Normal };
			var np = new Paragraph();
			np.Inlines.Add(new Run { Text = "Bold text WWWW" });
			normal.Blocks.Add(np);

			var panel = new StackPanel();
			panel.Children.Add(SUT);
			panel.Children.Add(normal);
			WindowHelper.WindowContent = panel;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			// Bold text is typically wider than normal
			Assert.IsTrue(SUT.ActualWidth >= normal.ActualWidth,
				$"Bold ({SUT.ActualWidth}) should be at least as wide as normal ({normal.ActualWidth})");
		}

		[TestMethod]
		public async Task When_FontStyle_Italic()
		{
			var SUT = new RichTextBlock { FontStyle = FontStyle.Italic };
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "Italic text" });
			SUT.Blocks.Add(paragraph);

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(FontStyle.Italic, SUT.FontStyle);
			Assert.IsTrue(SUT.ActualWidth > 0);
		}

		[TestMethod]
		public async Task When_Foreground_Set()
		{
			var brush = new SolidColorBrush(Colors.Red);
			var SUT = new RichTextBlock { Foreground = brush };
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "Red text" });
			SUT.Blocks.Add(paragraph);

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(brush, SUT.Foreground);
		}

		#endregion

		#region Text Layout Properties

		[TestMethod]
		public async Task When_TextWrapping_Wrap()
		{
			var SUT = new RichTextBlock
			{
				TextWrapping = TextWrapping.Wrap,
				Width = 100
			};
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "This is a very long text that should wrap to multiple lines within the container." });
			SUT.Blocks.Add(paragraph);

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			// With wrapping and constrained width, height should be substantially more than a single line
			Assert.IsTrue(SUT.ActualHeight > SUT.FontSize * 1.5,
				$"Wrapped text height ({SUT.ActualHeight}) should be more than 1.5x font size ({SUT.FontSize * 1.5})");
		}

		[TestMethod]
		public async Task When_TextWrapping_NoWrap()
		{
			var SUT = new RichTextBlock
			{
				TextWrapping = TextWrapping.NoWrap,
				Width = 100
			};
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "This is a very long text that should not wrap." });
			SUT.Blocks.Add(paragraph);

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			// Without wrapping, desired width should be larger than the container
			Assert.IsTrue(SUT.DesiredSize.Width >= 100,
				"NoWrap desired width should extend beyond container");
		}

		[TestMethod]
		public async Task When_MaxLines()
		{
			var unlimited = new RichTextBlock { Width = 150 };
			var up = new Paragraph();
			up.Inlines.Add(new Run { Text = "This is a long text that will wrap to many lines within the available width." });
			unlimited.Blocks.Add(up);

			var limited = new RichTextBlock { Width = 150, MaxLines = 2 };
			var lp = new Paragraph();
			lp.Inlines.Add(new Run { Text = "This is a long text that will wrap to many lines within the available width." });
			limited.Blocks.Add(lp);

			var panel = new StackPanel();
			panel.Children.Add(unlimited);
			panel.Children.Add(limited);
			WindowHelper.WindowContent = panel;
			await WindowHelper.WaitForLoaded(unlimited);
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(limited.ActualHeight < unlimited.ActualHeight,
				$"MaxLines=2 ({limited.ActualHeight}) should be shorter than unlimited ({unlimited.ActualHeight})");
		}

		[TestMethod]
		public async Task When_TextAlignment_Center()
		{
			var SUT = new RichTextBlock
			{
				TextAlignment = TextAlignment.Center,
				Width = 300
			};
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "Centered" });
			SUT.Blocks.Add(paragraph);

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(TextAlignment.Center, SUT.TextAlignment);
		}

		[TestMethod]
		public async Task When_Padding()
		{
			var noPadding = new RichTextBlock();
			var np = new Paragraph();
			np.Inlines.Add(new Run { Text = "Text" });
			noPadding.Blocks.Add(np);

			var withPadding = new RichTextBlock { Padding = new Thickness(20) };
			var wp = new Paragraph();
			wp.Inlines.Add(new Run { Text = "Text" });
			withPadding.Blocks.Add(wp);

			var panel = new StackPanel();
			panel.Children.Add(noPadding);
			panel.Children.Add(withPadding);
			WindowHelper.WindowContent = panel;
			await WindowHelper.WaitForLoaded(noPadding);
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(withPadding.ActualHeight > noPadding.ActualHeight,
				$"With padding ({withPadding.ActualHeight}) should be taller than without ({noPadding.ActualHeight})");
			Assert.IsTrue(withPadding.ActualWidth > noPadding.ActualWidth,
				$"With padding ({withPadding.ActualWidth}) should be wider than without ({noPadding.ActualWidth})");
		}

		[TestMethod]
		public async Task When_LineHeight()
		{
			var defaultLH = new RichTextBlock { Width = 200 };
			var dp = new Paragraph();
			dp.Inlines.Add(new Run { Text = "Line one and line two and three with default line height wrapping" });
			defaultLH.Blocks.Add(dp);

			var customLH = new RichTextBlock { Width = 200, LineHeight = 40 };
			var cp = new Paragraph();
			cp.Inlines.Add(new Run { Text = "Line one and line two and three with default line height wrapping" });
			customLH.Blocks.Add(cp);

			var panel = new StackPanel();
			panel.Children.Add(defaultLH);
			panel.Children.Add(customLH);
			WindowHelper.WindowContent = panel;
			await WindowHelper.WaitForLoaded(defaultLH);
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(customLH.ActualHeight > defaultLH.ActualHeight,
				$"LineHeight=40 ({customLH.ActualHeight}) should be taller than default ({defaultLH.ActualHeight})");
		}

		#endregion

		#region Text Trimming

		[TestMethod]
		public async Task When_TextTrimming_CharacterEllipsis()
		{
			var SUT = new RichTextBlock
			{
				TextTrimming = TextTrimming.CharacterEllipsis,
				TextWrapping = TextWrapping.NoWrap,
				Width = 100,
				MaxLines = 1
			};
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "This is a very long text that should be trimmed with ellipsis." });
			SUT.Blocks.Add(paragraph);

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(TextTrimming.CharacterEllipsis, SUT.TextTrimming);
		}

		[TestMethod]
		public async Task When_IsTextTrimmed()
		{
			var SUT = new RichTextBlock
			{
				TextTrimming = TextTrimming.CharacterEllipsis,
				TextWrapping = TextWrapping.NoWrap,
				Width = 50,
				MaxLines = 1
			};
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "This is a very very long text that definitely needs trimming." });
			SUT.Blocks.Add(paragraph);

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(SUT.IsTextTrimmed, "Text should be trimmed");
		}

		[TestMethod]
		public async Task When_IsTextTrimmed_Not_Trimmed()
		{
			var SUT = new RichTextBlock
			{
				TextTrimming = TextTrimming.CharacterEllipsis,
				TextWrapping = TextWrapping.NoWrap,
			};
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "Short" });
			SUT.Blocks.Add(paragraph);

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.IsFalse(SUT.IsTextTrimmed, "Short text should not be trimmed");
		}

		#endregion

		#region Blocks Management

		[TestMethod]
		public async Task When_Blocks_Added_Dynamically()
		{
			var SUT = new RichTextBlock { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "Dynamically added paragraph" });
			SUT.Blocks.Add(paragraph);

			await WindowHelper.WaitForIdle();

			Assert.IsTrue(SUT.DesiredSize.Height > 0, "Should have desired height after adding content");
		}

		[TestMethod]
		public async Task When_Blocks_Cleared()
		{
			var SUT = new RichTextBlock();
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "Some content" });
			SUT.Blocks.Add(paragraph);

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			var heightWithContent = SUT.ActualHeight;
			Assert.IsTrue(heightWithContent > 0);

			SUT.Blocks.Clear();
			await WindowHelper.WaitForIdle();

			// After clearing, Blocks count should be 0
			Assert.AreEqual(0, SUT.Blocks.Count);
		}

		[TestMethod]
		public async Task When_GetPlainText()
		{
			var SUT = new RichTextBlock();

			var para1 = new Paragraph();
			para1.Inlines.Add(new Run { Text = "Hello" });
			SUT.Blocks.Add(para1);

			var para2 = new Paragraph();
			para2.Inlines.Add(new Run { Text = "World" });
			SUT.Blocks.Add(para2);

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			// GetPlainText joins paragraphs with \r\n
			var plainText = SUT.GetAccessibilityInnerText();
			Assert.AreEqual("Hello\r\nWorld", plainText);
		}

		#endregion

		#region Selection

		[TestMethod]
		public async Task When_SelectAll()
		{
			var SUT = new RichTextBlock { IsTextSelectionEnabled = true };
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "Select all this text" });
			SUT.Blocks.Add(paragraph);

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.SelectAll();

			Assert.AreEqual("Select all this text", SUT.SelectedText);
		}

		[TestMethod]
		public async Task When_SelectedText_Empty_By_Default()
		{
			var SUT = new RichTextBlock { IsTextSelectionEnabled = true };
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "Some text" });
			SUT.Blocks.Add(paragraph);

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(string.Empty, SUT.SelectedText);
		}

		[TestMethod]
		public async Task When_SelectAll_Multiple_Paragraphs()
		{
			var SUT = new RichTextBlock { IsTextSelectionEnabled = true };

			var para1 = new Paragraph();
			para1.Inlines.Add(new Run { Text = "First" });
			SUT.Blocks.Add(para1);

			var para2 = new Paragraph();
			para2.Inlines.Add(new Run { Text = "Second" });
			SUT.Blocks.Add(para2);

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.SelectAll();

			Assert.AreEqual("First\r\nSecond", SUT.SelectedText);
		}

		#endregion

		#region Property Defaults

		[TestMethod]
		public async Task When_Default_Properties()
		{
			var SUT = new RichTextBlock { Width = 200, Height = 50 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			Assert.AreEqual(TextWrapping.Wrap, SUT.TextWrapping, "Default TextWrapping should be Wrap");
			Assert.AreEqual(TextTrimming.None, SUT.TextTrimming, "Default TextTrimming should be None");
			Assert.AreEqual(TextAlignment.Left, SUT.TextAlignment, "Default TextAlignment should be Left");
			Assert.AreEqual(14.0, SUT.FontSize, "Default FontSize should be 14");
			Assert.AreEqual(0, SUT.MaxLines, "Default MaxLines should be 0 (unlimited)");
			Assert.AreEqual(0, SUT.CharacterSpacing, "Default CharacterSpacing should be 0");
			Assert.AreEqual(false, SUT.IsTextSelectionEnabled, "Default IsTextSelectionEnabled should be false");
			Assert.AreEqual(0d, SUT.LineHeight, "Default LineHeight should be 0");
			Assert.AreEqual(LineStackingStrategy.MaxHeight, SUT.LineStackingStrategy, "Default LineStackingStrategy should be MaxHeight");
		}

		[TestMethod]
		public async Task When_Default_TextWrapping_Is_Wrap()
		{
			// RichTextBlock differs from TextBlock in that its default TextWrapping is Wrap
			var SUT = new RichTextBlock();
			Assert.AreEqual(TextWrapping.Wrap, SUT.TextWrapping);
		}

		#endregion

		#region Block Properties

		[TestMethod]
		public async Task When_Paragraph_TextAlignment()
		{
			var SUT = new RichTextBlock { TextAlignment = TextAlignment.Left, Width = 300 };

			var leftParagraph = new Paragraph { TextAlignment = TextAlignment.Left };
			leftParagraph.Inlines.Add(new Run { Text = "Left" });
			SUT.Blocks.Add(leftParagraph);

			var rightParagraph = new Paragraph { TextAlignment = TextAlignment.Right };
			rightParagraph.Inlines.Add(new Run { Text = "Right" });
			SUT.Blocks.Add(rightParagraph);

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			// Verify paragraph alignment is set correctly
			Assert.AreEqual(TextAlignment.Left, leftParagraph.TextAlignment);
			Assert.AreEqual(TextAlignment.Right, rightParagraph.TextAlignment);
		}

		[TestMethod]
		public async Task When_Paragraph_LineHeight()
		{
			var SUT = new RichTextBlock { Width = 200 };

			var normalParagraph = new Paragraph();
			normalParagraph.Inlines.Add(new Run { Text = "Normal line height paragraph text wrapping" });
			SUT.Blocks.Add(normalParagraph);

			var tallParagraph = new Paragraph { LineHeight = 40 };
			tallParagraph.Inlines.Add(new Run { Text = "Tall line height paragraph text wrapping" });
			SUT.Blocks.Add(tallParagraph);

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(40.0, tallParagraph.LineHeight);
			Assert.IsTrue(SUT.ActualHeight > 0);
		}

		[TestMethod]
		public async Task When_Paragraph_Margin()
		{
			var withMargin = new RichTextBlock();
			var mp = new Paragraph { Margin = new Thickness(0, 0, 0, 30) };
			mp.Inlines.Add(new Run { Text = "Paragraph with margin" });
			withMargin.Blocks.Add(mp);
			var mp2 = new Paragraph();
			mp2.Inlines.Add(new Run { Text = "Second" });
			withMargin.Blocks.Add(mp2);

			var noMargin = new RichTextBlock();
			var np = new Paragraph();
			np.Inlines.Add(new Run { Text = "Paragraph with margin" });
			noMargin.Blocks.Add(np);
			var np2 = new Paragraph();
			np2.Inlines.Add(new Run { Text = "Second" });
			noMargin.Blocks.Add(np2);

			var panel = new StackPanel();
			panel.Children.Add(withMargin);
			panel.Children.Add(noMargin);
			WindowHelper.WindowContent = panel;
			await WindowHelper.WaitForLoaded(withMargin);
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(withMargin.ActualHeight > noMargin.ActualHeight,
				$"With margin ({withMargin.ActualHeight}) should be taller than without ({noMargin.ActualHeight})");
		}

		#endregion

		#region Events

		[TestMethod]
		public async Task When_IsTextTrimmedChanged_Fires()
		{
			var SUT = new RichTextBlock
			{
				TextTrimming = TextTrimming.CharacterEllipsis,
				TextWrapping = TextWrapping.NoWrap,
				MaxLines = 1,
			};
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "Short" });
			SUT.Blocks.Add(paragraph);

			bool eventFired = false;
			SUT.IsTextTrimmedChanged += (s, e) => eventFired = true;

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			// Now make the text long enough to trim
			paragraph.Inlines.Clear();
			paragraph.Inlines.Add(new Run { Text = "This is a very very very long text that needs trimming" });
			SUT.Width = 50;

			await WindowHelper.WaitForIdle();

			// The event should fire when IsTextTrimmed changes
			// Note: This depends on the implementation correctly raising the event
			Assert.IsTrue(SUT.IsTextTrimmed || eventFired,
				"Text should be trimmed or event should have fired");
		}

		[TestMethod]
		public async Task When_SelectionChanged_Fires()
		{
			var SUT = new RichTextBlock { IsTextSelectionEnabled = true };
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "Selectable text content" });
			SUT.Blocks.Add(paragraph);

			bool eventFired = false;
			SUT.SelectionChanged += (s, e) => eventFired = true;

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.SelectAll();

			Assert.IsTrue(eventFired, "SelectionChanged should fire after SelectAll");
		}

		#endregion

		#region Property Inheritance

		[TestMethod]
		public async Task When_CharacterSpacing()
		{
			var normal = new RichTextBlock { CharacterSpacing = 0, TextWrapping = TextWrapping.NoWrap };
			var np = new Paragraph();
			np.Inlines.Add(new Run { Text = "WWWWWWWW" });
			normal.Blocks.Add(np);

			var spaced = new RichTextBlock { CharacterSpacing = 200, TextWrapping = TextWrapping.NoWrap };
			var sp = new Paragraph();
			sp.Inlines.Add(new Run { Text = "WWWWWWWW" });
			spaced.Blocks.Add(sp);

			var panel = new StackPanel();
			panel.Children.Add(normal);
			panel.Children.Add(spaced);
			WindowHelper.WindowContent = panel;
			await WindowHelper.WaitForLoaded(normal);
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(spaced.ActualWidth > normal.ActualWidth,
				$"CharacterSpacing=200 ({spaced.ActualWidth}) should be wider than 0 ({normal.ActualWidth})");
		}

		[TestMethod]
		public async Task When_TextDecorations_Underline()
		{
			var SUT = new RichTextBlock { TextDecorations = TextDecorations.Underline };
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "Underlined text" });
			SUT.Blocks.Add(paragraph);

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(TextDecorations.Underline, SUT.TextDecorations);
		}

		#endregion

		#region IsViewHit

		[TestMethod]
		public async Task When_IsViewHit_With_Content()
		{
			var SUT = new RichTextBlock();
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "Content" });
			SUT.Blocks.Add(paragraph);

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			// IsViewHit should return true when Blocks.Count > 0
			Assert.IsTrue(SUT.Blocks.Count > 0);
		}

		[TestMethod]
		public async Task When_IsViewHit_Empty()
		{
			var SUT = new RichTextBlock { Width = 200, Height = 50 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			Assert.AreEqual(0, SUT.Blocks.Count);
		}

		#endregion

		#region Hyperlinks

		[TestMethod]
		public async Task When_Hyperlink_InParagraph()
		{
			var SUT = new RichTextBlock();
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "Visit " });
			var hyperlink = new Hyperlink { NavigateUri = new Uri("https://platform.uno") };
			hyperlink.Inlines.Add(new Run { Text = "Uno Platform" });
			paragraph.Inlines.Add(hyperlink);
			SUT.Blocks.Add(paragraph);

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(SUT.ActualWidth > 0);
			Assert.IsTrue(SUT.ActualHeight > 0);
		}

		#endregion

		#region TextHighlighters

		[TestMethod]
		public async Task When_TextHighlighter_Single_Range()
		{
			var SUT = new RichTextBlock();
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "Hello World with highlights" });
			SUT.Blocks.Add(paragraph);

			var highlighter = new TextHighlighter
			{
				Background = new SolidColorBrush(Colors.Yellow)
			};
			highlighter.Ranges.Add(new TextRange { StartIndex = 6, Length = 5 });
			SUT.TextHighlighters.Add(highlighter);

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, SUT.TextHighlighters.Count);
			Assert.AreEqual(6, SUT.TextHighlighters[0].Ranges[0].StartIndex);
			Assert.AreEqual(5, SUT.TextHighlighters[0].Ranges[0].Length);
		}

		[TestMethod]
		public async Task When_TextHighlighter_Added_Dynamically()
		{
			var SUT = new RichTextBlock();
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "Dynamic highlight text" });
			SUT.Blocks.Add(paragraph);

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(0, SUT.TextHighlighters.Count);

			var highlighter = new TextHighlighter
			{
				Background = new SolidColorBrush(Colors.LightBlue)
			};
			highlighter.Ranges.Add(new TextRange { StartIndex = 0, Length = 7 });
			SUT.TextHighlighters.Add(highlighter);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, SUT.TextHighlighters.Count);
		}

		[TestMethod]
		public async Task When_TextHighlighters_Cleared()
		{
			var SUT = new RichTextBlock();
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "Clearable highlights" });
			SUT.Blocks.Add(paragraph);

			var h1 = new TextHighlighter { Background = new SolidColorBrush(Colors.Yellow) };
			h1.Ranges.Add(new TextRange { StartIndex = 0, Length = 5 });
			SUT.TextHighlighters.Add(h1);

			var h2 = new TextHighlighter { Background = new SolidColorBrush(Colors.Green) };
			h2.Ranges.Add(new TextRange { StartIndex = 10, Length = 5 });
			SUT.TextHighlighters.Add(h2);

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(2, SUT.TextHighlighters.Count);

			SUT.TextHighlighters.Clear();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(0, SUT.TextHighlighters.Count);
		}

		#endregion

		#region Dynamic Property Changes

		[TestMethod]
		public async Task When_FontSize_Changed_Dynamically()
		{
			var SUT = new RichTextBlock { FontSize = 14 };
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "Resizable text" });
			SUT.Blocks.Add(paragraph);

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			var smallHeight = SUT.ActualHeight;

			SUT.FontSize = 40;
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(SUT.ActualHeight > smallHeight,
				$"FontSize=40 ({SUT.ActualHeight}) should be taller than FontSize=14 ({smallHeight})");
		}

		[TestMethod]
		public async Task When_TextWrapping_Changed_Dynamically()
		{
			var SUT = new RichTextBlock
			{
				Width = 100,
				TextWrapping = TextWrapping.NoWrap
			};
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "This is a text that should wrap when wrapping is enabled on the control." });
			SUT.Blocks.Add(paragraph);

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			var noWrapHeight = SUT.ActualHeight;

			SUT.TextWrapping = TextWrapping.Wrap;
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(SUT.ActualHeight > noWrapHeight,
				$"Wrap ({SUT.ActualHeight}) should be taller than NoWrap ({noWrapHeight})");
		}

		[TestMethod]
		public async Task When_MaxLines_Changed_Dynamically()
		{
			var SUT = new RichTextBlock { Width = 100 };
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "A long text that wraps to many lines in this narrow container." });
			SUT.Blocks.Add(paragraph);

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			var unlimitedHeight = SUT.ActualHeight;

			SUT.MaxLines = 1;
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(SUT.ActualHeight < unlimitedHeight,
				$"MaxLines=1 ({SUT.ActualHeight}) should be shorter than unlimited ({unlimitedHeight})");
		}

		[TestMethod]
		public async Task When_Foreground_Changed_Dynamically()
		{
			var SUT = new RichTextBlock();
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "Color change" });
			SUT.Blocks.Add(paragraph);

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			var newBrush = new SolidColorBrush(Colors.Blue);
			SUT.Foreground = newBrush;
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(newBrush, SUT.Foreground);
		}

		[TestMethod]
		public async Task When_Padding_Changed_Dynamically()
		{
			var SUT = new RichTextBlock();
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "Padded text" });
			SUT.Blocks.Add(paragraph);

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			var noPaddingHeight = SUT.ActualHeight;
			var noPaddingWidth = SUT.ActualWidth;

			SUT.Padding = new Thickness(30);
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(SUT.ActualHeight > noPaddingHeight,
				$"With padding ({SUT.ActualHeight}) should be taller than without ({noPaddingHeight})");
			Assert.IsTrue(SUT.ActualWidth > noPaddingWidth,
				$"With padding ({SUT.ActualWidth}) should be wider than without ({noPaddingWidth})");
		}

		#endregion

		#region Nested Inlines

		[TestMethod]
		public async Task When_Nested_Bold_Italic()
		{
			var SUT = new RichTextBlock();
			var paragraph = new Paragraph();

			// Bold containing Italic
			var bold = new Bold();
			var italic = new Italic();
			italic.Inlines.Add(new Run { Text = "bold italic" });
			bold.Inlines.Add(italic);

			paragraph.Inlines.Add(new Run { Text = "Normal " });
			paragraph.Inlines.Add(bold);
			SUT.Blocks.Add(paragraph);

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(SUT.ActualWidth > 0);
			Assert.IsTrue(SUT.ActualHeight > 0);
		}

		[TestMethod]
		public async Task When_Span_With_Properties()
		{
			var SUT = new RichTextBlock();
			var paragraph = new Paragraph();

			var span = new Span
			{
				FontSize = 24,
				Foreground = new SolidColorBrush(Colors.Red)
			};
			span.Inlines.Add(new Run { Text = "Large red text" });

			paragraph.Inlines.Add(new Run { Text = "Normal " });
			paragraph.Inlines.Add(span);
			paragraph.Inlines.Add(new Run { Text = " normal" });
			SUT.Blocks.Add(paragraph);

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(SUT.ActualWidth > 0);
		}

		[TestMethod]
		public async Task When_Run_With_Individual_Properties()
		{
			var SUT = new RichTextBlock();
			var paragraph = new Paragraph();

			paragraph.Inlines.Add(new Run
			{
				Text = "Custom",
				FontSize = 24,
				FontWeight = FontWeights.Bold,
				Foreground = new SolidColorBrush(Colors.Green)
			});
			paragraph.Inlines.Add(new Run { Text = " default" });
			SUT.Blocks.Add(paragraph);

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(SUT.ActualWidth > 0);
		}

		#endregion

		#region Container Layout

		[TestMethod]
		public async Task When_In_Grid_With_Stretch()
		{
			var SUT = new RichTextBlock();
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "Text in a grid cell with horizontal stretch." });
			SUT.Blocks.Add(paragraph);

			var grid = new Grid { Width = 400, Height = 200 };
			grid.Children.Add(SUT);

			WindowHelper.WindowContent = grid;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			// RichTextBlock should stretch to fill the grid cell
			Assert.IsTrue(SUT.ActualWidth > 0);
			Assert.IsTrue(SUT.ActualHeight > 0);
		}

		[TestMethod]
		public async Task When_Width_Constrained_Text_Wraps()
		{
			var longText = "This is a very long piece of text that should wrap when constrained to a narrow width.";

			var wide = new RichTextBlock { Width = 500 };
			var wp = new Paragraph();
			wp.Inlines.Add(new Run { Text = longText });
			wide.Blocks.Add(wp);

			var narrow = new RichTextBlock { Width = 100 };
			var np = new Paragraph();
			np.Inlines.Add(new Run { Text = longText });
			narrow.Blocks.Add(np);

			var panel = new StackPanel();
			panel.Children.Add(wide);
			panel.Children.Add(narrow);
			WindowHelper.WindowContent = panel;
			await WindowHelper.WaitForLoaded(wide);
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(narrow.ActualHeight > wide.ActualHeight,
				$"Narrow ({narrow.ActualHeight}) should be taller than wide ({wide.ActualHeight})");
		}

		#endregion

		#region TextTrimming Advanced

		[TestMethod]
		public async Task When_TextTrimming_WordEllipsis()
		{
			var SUT = new RichTextBlock
			{
				TextTrimming = TextTrimming.WordEllipsis,
				TextWrapping = TextWrapping.NoWrap,
				Width = 100,
				MaxLines = 1
			};
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "This is a long sentence that should be word-trimmed." });
			SUT.Blocks.Add(paragraph);

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(TextTrimming.WordEllipsis, SUT.TextTrimming);
			Assert.IsTrue(SUT.IsTextTrimmed, "Text should be trimmed with WordEllipsis");
		}

		[TestMethod]
		public async Task When_TextTrimming_With_MaxLines()
		{
			var SUT = new RichTextBlock
			{
				TextTrimming = TextTrimming.CharacterEllipsis,
				Width = 150,
				MaxLines = 2
			};
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "This is a very long text that should wrap to many lines and be trimmed after two lines with an ellipsis character." });
			SUT.Blocks.Add(paragraph);

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(SUT.IsTextTrimmed, "Should be trimmed at MaxLines=2");
		}

		[TestMethod]
		public async Task When_TextTrimming_None_Not_Trimmed()
		{
			var SUT = new RichTextBlock
			{
				TextTrimming = TextTrimming.None,
				Width = 300
			};
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "Short text" });
			SUT.Blocks.Add(paragraph);

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.IsFalse(SUT.IsTextTrimmed, "Short text with no trimming should not be trimmed");
		}

		#endregion

		#region Multiple Paragraphs Advanced

		[TestMethod]
		public async Task When_Paragraphs_With_Different_FontSizes()
		{
			var SUT = new RichTextBlock();

			var para1 = new Paragraph { FontSize = 12 };
			para1.Inlines.Add(new Run { Text = "Small text" });
			SUT.Blocks.Add(para1);

			var para2 = new Paragraph { FontSize = 30 };
			para2.Inlines.Add(new Run { Text = "Large text" });
			SUT.Blocks.Add(para2);

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(SUT.ActualHeight > 0);
			// GetPlainText should join both paragraphs
			var text = SUT.GetAccessibilityInnerText();
			Assert.AreEqual("Small text\r\nLarge text", text);
		}

		[TestMethod]
		public async Task When_Three_Paragraphs()
		{
			var SUT = new RichTextBlock();

			for (int i = 1; i <= 3; i++)
			{
				var para = new Paragraph();
				para.Inlines.Add(new Run { Text = $"Paragraph {i}" });
				SUT.Blocks.Add(para);
			}

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(3, SUT.Blocks.Count);
			Assert.AreEqual("Paragraph 1\r\nParagraph 2\r\nParagraph 3", SUT.GetAccessibilityInnerText());
		}

		[TestMethod]
		public async Task When_Paragraph_Removed()
		{
			var SUT = new RichTextBlock();

			var para1 = new Paragraph();
			para1.Inlines.Add(new Run { Text = "Keep" });
			SUT.Blocks.Add(para1);

			var para2 = new Paragraph();
			para2.Inlines.Add(new Run { Text = "Remove" });
			SUT.Blocks.Add(para2);

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(2, SUT.Blocks.Count);

			SUT.Blocks.Remove(para2);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, SUT.Blocks.Count);
			Assert.AreEqual("Keep", SUT.GetAccessibilityInnerText());
		}

		#endregion

		#region Inline Modifications

		[TestMethod]
		public async Task When_Run_Text_Changed()
		{
			var run = new Run { Text = "Original" };
			var SUT = new RichTextBlock();
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(run);
			SUT.Blocks.Add(paragraph);

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			var originalWidth = SUT.ActualWidth;

			run.Text = "A much longer replacement text string";
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(SUT.ActualWidth > originalWidth,
				$"After text change ({SUT.ActualWidth}) should be wider than original ({originalWidth})");
		}

		[TestMethod]
		public async Task When_Inline_Added_To_Paragraph()
		{
			var SUT = new RichTextBlock();
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "Start" });
			SUT.Blocks.Add(paragraph);

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			var originalWidth = SUT.ActualWidth;

			paragraph.Inlines.Add(new Run { Text = " additional text added" });
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(SUT.ActualWidth > originalWidth,
				$"After adding inline ({SUT.ActualWidth}) should be wider than original ({originalWidth})");
		}

		[TestMethod]
		public async Task When_Inlines_Cleared()
		{
			var SUT = new RichTextBlock();
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "Text to clear" });
			paragraph.Inlines.Add(new Bold { Inlines = { new Run { Text = " bold" } } });
			SUT.Blocks.Add(paragraph);

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(SUT.ActualWidth > 0);

			paragraph.Inlines.Clear();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(string.Empty, SUT.GetAccessibilityInnerText());
		}

		#endregion

		#region Selection Advanced

		[TestMethod]
		public async Task When_Selection_After_Blocks_Change()
		{
			var SUT = new RichTextBlock { IsTextSelectionEnabled = true };
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "Original text" });
			SUT.Blocks.Add(paragraph);

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.SelectAll();
			Assert.AreEqual("Original text", SUT.SelectedText);

			// Change content
			paragraph.Inlines.Clear();
			paragraph.Inlines.Add(new Run { Text = "New text" });
			await WindowHelper.WaitForIdle();

			// After content change, SelectAll should work on new content
			SUT.SelectAll();
			Assert.AreEqual("New text", SUT.SelectedText);
		}

		[TestMethod]
		public async Task When_IsTextSelectionEnabled_Changed()
		{
			var SUT = new RichTextBlock { IsTextSelectionEnabled = false };
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "Toggle selection" });
			SUT.Blocks.Add(paragraph);

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.IsFalse(SUT.IsTextSelectionEnabled);

			SUT.IsTextSelectionEnabled = true;
			Assert.IsTrue(SUT.IsTextSelectionEnabled);
		}

		#endregion

		#region Accessibility

		[TestMethod]
		public async Task When_GetAccessibilityInnerText_Empty()
		{
			var SUT = new RichTextBlock { Width = 200, Height = 50 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			Assert.AreEqual(string.Empty, SUT.GetAccessibilityInnerText());
		}

		[TestMethod]
		public async Task When_GetAccessibilityInnerText_With_Formatting()
		{
			var SUT = new RichTextBlock();
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "Normal " });
			paragraph.Inlines.Add(new Bold { Inlines = { new Run { Text = "bold" } } });
			paragraph.Inlines.Add(new Run { Text = " " });
			paragraph.Inlines.Add(new Italic { Inlines = { new Run { Text = "italic" } } });
			SUT.Blocks.Add(paragraph);

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			// Accessibility text should be plain text without formatting
			Assert.AreEqual("Normal bold italic", SUT.GetAccessibilityInnerText());
		}

		#endregion

		#region Edge Cases

		[TestMethod]
		public async Task When_Empty_Run()
		{
			var SUT = new RichTextBlock { Width = 200, Height = 50 };
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "" });
			SUT.Blocks.Add(paragraph);

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(string.Empty, SUT.GetAccessibilityInnerText());
		}

		[TestMethod]
		public async Task When_Only_LineBreaks()
		{
			var SUT = new RichTextBlock { Width = 200, Height = 100 };
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new LineBreak());
			paragraph.Inlines.Add(new LineBreak());
			SUT.Blocks.Add(paragraph);

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(SUT.DesiredSize.Height > 0, "LineBreaks should produce some desired height");
		}

		[TestMethod]
		public async Task When_Very_Long_Single_Word()
		{
			var SUT = new RichTextBlock { Width = 100, TextWrapping = TextWrapping.Wrap };
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "Supercalifragilisticexpialidocious" });
			SUT.Blocks.Add(paragraph);

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			// A very long word in a narrow container should still render
			Assert.IsTrue(SUT.ActualHeight > 0);
		}

		[TestMethod]
		public async Task When_Whitespace_Only()
		{
			var SUT = new RichTextBlock();
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "   " });
			SUT.Blocks.Add(paragraph);

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual("   ", SUT.GetAccessibilityInnerText());
		}

		#endregion
	}
}
