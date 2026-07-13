using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Input.Preview.Injection;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.Toolkit.DevTools.Input;
using static Private.Infrastructure.TestServices;

#nullable enable

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	// Exercises the Stage 9b pointer/keyboard input pipeline end-to-end via injected input:
	// mouse press-drag-release selection through TextSelectionManager, double-click word
	// selection, shift+click extension, tap-to-clear, cross-paragraph drag, Ctrl+A/Ctrl+C,
	// and hyperlink Click. SkiaWasm is excluded (mouse/keyboard injection + clipboard).
	[TestClass]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm)]
	public class Given_RichTextBlock_Input
	{
		private static Point Center(Rect r) => new(r.X + r.Width / 2, r.Y + r.Height / 2);

		private static RichTextBlock CreateSingleLine(string text, double width = 400)
		{
			var rtb = new RichTextBlock { Width = width, TextWrapping = TextWrapping.NoWrap };
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = text });
			rtb.Blocks.Add(paragraph);
			return rtb;
		}

		#region Pointer selection

		[TestMethod]
		public async Task When_PointerDrag_Selects_Range()
		{
			var SUT = CreateSingleLine("The quick brown fox jumps over the lazy dog");

			try
			{
				await UITestHelper.Load(SUT);

				var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
				using var mouse = injector.GetMouse();

				var bounds = SUT.GetAbsoluteBounds();
				var midY = bounds.Y + bounds.Height / 2;

				// Drag from just inside the left edge to just past the right edge of the line.
				mouse.MoveTo(new Point(bounds.X + 2, midY));
				await WindowHelper.WaitForIdle();
				mouse.Press();
				await WindowHelper.WaitForIdle();
				mouse.MoveTo(new Point(bounds.Right - 2, midY));
				await WindowHelper.WaitForIdle();
				mouse.Release();
				await WindowHelper.WaitForIdle();

				Assert.IsFalse(string.IsNullOrEmpty(SUT.SelectedText), "A press-drag-release should produce a non-empty selection");
				Assert.IsTrue(SUT.SelectedText.Contains("brown"), $"The dragged range should cover the middle of the line (was '{SUT.SelectedText}')");

				var start = SUT.SelectionStart;
				var end = SUT.SelectionEnd;
				Assert.IsNotNull(start);
				Assert.IsNotNull(end);
				Assert.IsTrue(end!.Offset > start!.Offset, $"SelectionEnd ({end.Offset}) should be past SelectionStart ({start.Offset})");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_DoubleClick_Selects_Word()
		{
			var SUT = CreateSingleLine("Wonderful sunny afternoon");

			try
			{
				await UITestHelper.Load(SUT);

				var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
				using var mouse = injector.GetMouse();

				var bounds = SUT.GetAbsoluteBounds();
				// Position over the first word "Wonderful".
				mouse.MoveTo(new Point(bounds.X + bounds.Width * 0.1, bounds.Y + bounds.Height / 2));
				await WindowHelper.WaitForIdle();

				mouse.Press();
				mouse.Release();
				mouse.Press();
				mouse.Release();
				await WindowHelper.WaitForIdle();

				// Double-click selects the word under the pointer (word breaker may include a trailing space).
				Assert.AreEqual("Wonderful", SUT.SelectedText.TrimEnd(), $"Double-click should select a single word (was '{SUT.SelectedText}')");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_ShiftClick_Extends_Selection()
		{
			var SUT = CreateSingleLine("The quick brown fox jumps over the lazy dog");

			try
			{
				await UITestHelper.Load(SUT);

				var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
				using var mouse = injector.GetMouse();

				var bounds = SUT.GetAbsoluteBounds();
				var midY = bounds.Y + bounds.Height / 2;

				// Click near the start to place the caret/anchor.
				mouse.MoveTo(new Point(bounds.X + 2, midY));
				await WindowHelper.WaitForIdle();
				mouse.Press();
				mouse.Release();
				await WindowHelper.WaitForIdle();

				// Delay so the shift+click is not coalesced into a double-tap (word selection).
				await Task.Delay(600);

				// Shift+click far to the right extends the selection from the anchor.
				mouse.MoveTo(new Point(bounds.Right - 2, midY));
				await WindowHelper.WaitForIdle();
				mouse.Press(VirtualKeyModifiers.Shift);
				mouse.Release(VirtualKeyModifiers.Shift);
				await WindowHelper.WaitForIdle();

				Assert.IsFalse(string.IsNullOrEmpty(SUT.SelectedText), "Shift+click should extend the selection to a non-empty range");
				var start = SUT.SelectionStart;
				var end = SUT.SelectionEnd;
				Assert.IsNotNull(start);
				Assert.IsNotNull(end);
				Assert.IsTrue(end!.Offset > start!.Offset, $"Shift+click should extend past the anchor (start {start!.Offset}, end {end.Offset})");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Tap_Clears_Selection()
		{
			var SUT = CreateSingleLine("Select then clear this line");

			try
			{
				await UITestHelper.Load(SUT);

				SUT.SelectAll();
				await WindowHelper.WaitForIdle();
				Assert.IsFalse(string.IsNullOrEmpty(SUT.SelectedText), "Precondition: SelectAll should select content");

				var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
				using var mouse = injector.GetMouse();

				// A single mouse click collapses the selection (caret placement).
				mouse.MoveTo(Center(SUT.GetAbsoluteBounds()));
				await WindowHelper.WaitForIdle();
				mouse.Press();
				mouse.Release();
				await WindowHelper.WaitForIdle();

				Assert.AreEqual(string.Empty, SUT.SelectedText, "A single click should clear the selection");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_MultiParagraph_Drag_Crosses_Boundary()
		{
			var SUT = new RichTextBlock { Width = 400, TextWrapping = TextWrapping.NoWrap };
			var para1 = new Paragraph();
			para1.Inlines.Add(new Run { Text = "First paragraph line" });
			SUT.Blocks.Add(para1);
			var para2 = new Paragraph();
			para2.Inlines.Add(new Run { Text = "Second paragraph line" });
			SUT.Blocks.Add(para2);

			try
			{
				await UITestHelper.Load(SUT);

				var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
				using var mouse = injector.GetMouse();

				var bounds = SUT.GetAbsoluteBounds();

				// Drag from within the first paragraph (top quarter) down into the second (bottom quarter).
				mouse.MoveTo(new Point(bounds.X + bounds.Width * 0.25, bounds.Y + bounds.Height * 0.25));
				await WindowHelper.WaitForIdle();
				mouse.Press();
				await WindowHelper.WaitForIdle();
				mouse.MoveTo(new Point(bounds.X + bounds.Width * 0.75, bounds.Y + bounds.Height * 0.75));
				await WindowHelper.WaitForIdle();
				mouse.Release();
				await WindowHelper.WaitForIdle();

				Assert.IsFalse(string.IsNullOrEmpty(SUT.SelectedText), "A cross-paragraph drag should select content");
				Assert.IsTrue(
					SUT.SelectedText.Contains("\r\n"),
					$"A selection spanning the paragraph boundary should include the inter-paragraph separator (was '{SUT.SelectedText}')");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		#endregion

		#region Keyboard

		[TestMethod]
		public async Task When_CtrlA_Selects_All()
		{
			var SUT = CreateSingleLine("Keyboard select all content");

			try
			{
				await UITestHelper.Load(SUT);

				if (OperatingSystem.IsMacOS())
				{
					Assert.Inconclusive("The command modifier is Cmd (not Ctrl) on macOS; KeyboardHelper only injects Ctrl.");
					return;
				}

				await KeyboardHelper.PressKeySequence("$d$_ctrl#$d$_a#$u$_a#$u$_ctrl", SUT);
				await WindowHelper.WaitForIdle();

				Assert.AreEqual("Keyboard select all content", SUT.SelectedText, "Ctrl+A should select all content");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_CtrlC_Copies_Selection()
		{
			if (!Uno.Foundation.Extensibility.ApiExtensibility.IsRegistered<Uno.ApplicationModel.DataTransfer.IClipboardExtension>())
			{
				Assert.Inconclusive("Platform does not support clipboard operations.");
			}

			var SUT = CreateSingleLine("Copy this text with the keyboard");

			try
			{
				await UITestHelper.Load(SUT);

				if (OperatingSystem.IsMacOS())
				{
					Assert.Inconclusive("The command modifier is Cmd (not Ctrl) on macOS; KeyboardHelper only injects Ctrl.");
					return;
				}

				await KeyboardHelper.PressKeySequence("$d$_ctrl#$d$_a#$u$_a#$u$_ctrl", SUT);
				await WindowHelper.WaitForIdle();
				Assert.IsFalse(string.IsNullOrEmpty(SUT.SelectedText), "Precondition: Ctrl+A should select content");

				await KeyboardHelper.PressKeySequence("$d$_ctrl#$d$_c#$u$_c#$u$_ctrl", SUT);
				await WindowHelper.WaitForIdle();

				var clipboard = await Clipboard.GetContent()!.GetTextAsync();
				// Contains (not exact) tolerates a trailing paragraph mark in the serialized selection while
				// still failing if the copy is empty or carries the wrong text.
				Assert.IsTrue(
					clipboard.Contains("Copy this text with the keyboard"),
					$"Ctrl+C should copy the selected text to the clipboard (was '{clipboard}')");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_CopySelectionToClipboard_Api_Copies_Across_Paragraphs()
		{
			if (!Uno.Foundation.Extensibility.ApiExtensibility.IsRegistered<Uno.ApplicationModel.DataTransfer.IClipboardExtension>())
			{
				Assert.Inconclusive("Platform does not support clipboard operations.");
			}

			var SUT = new RichTextBlock { IsTextSelectionEnabled = true };
			var para1 = new Paragraph();
			para1.Inlines.Add(new Run { Text = "First" });
			SUT.Blocks.Add(para1);
			var para2 = new Paragraph();
			para2.Inlines.Add(new Run { Text = "Second" });
			SUT.Blocks.Add(para2);

			try
			{
				await UITestHelper.Load(SUT);

				SUT.SelectAll();
				await WindowHelper.WaitForIdle();
				Assert.AreEqual("First\r\nSecond", SUT.SelectedText, "Precondition: SelectAll should join paragraphs with the separator");

				SUT.CopySelectionToClipboard();
				await WindowHelper.WaitForIdle();

				var clipboard = await Clipboard.GetContent()!.GetTextAsync();
				// The copied text must carry both paragraphs joined by the inter-paragraph separator
				// (Contains tolerates a trailing paragraph mark while still asserting the separator).
				Assert.IsTrue(
					clipboard.Contains("First\r\nSecond"),
					$"CopySelectionToClipboard should copy the selection including the inter-paragraph separator (was '{clipboard}')");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		#endregion

		#region Hyperlink

		private static (RichTextBlock rtb, Hyperlink link) CreateHyperlinkSut()
		{
			var rtb = new RichTextBlock { TextWrapping = TextWrapping.NoWrap };

			var linkPara = new Paragraph();
			var hyperlink = new Hyperlink();
			hyperlink.Inlines.Add(new Run { Text = "Open the link here now" });
			linkPara.Inlines.Add(hyperlink);
			rtb.Blocks.Add(linkPara);

			var plainPara = new Paragraph();
			plainPara.Inlines.Add(new Run { Text = "Just some plain text now" });
			rtb.Blocks.Add(plainPara);

			return (rtb, hyperlink);
		}

		[TestMethod]
		public async Task When_Click_On_Hyperlink_Raises_Click()
		{
			var (SUT, hyperlink) = CreateHyperlinkSut();
			var clicked = false;
			hyperlink.Click += (_, _) => clicked = true;

			try
			{
				await UITestHelper.Load(SUT);

				var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
				using var mouse = injector.GetMouse();

				var bounds = SUT.GetAbsoluteBounds();
				// First line (top quarter) holds the hyperlink.
				mouse.MoveTo(new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height * 0.25));
				await WindowHelper.WaitForIdle();
				mouse.Press();
				mouse.Release();
				await WindowHelper.WaitForIdle();

				Assert.IsTrue(clicked, "Clicking on the hyperlink should raise its Click event");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Click_Off_Hyperlink_Does_Not_Raise_Click()
		{
			var (SUT, hyperlink) = CreateHyperlinkSut();
			var clicked = false;
			hyperlink.Click += (_, _) => clicked = true;

			try
			{
				await UITestHelper.Load(SUT);

				var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
				using var mouse = injector.GetMouse();

				var bounds = SUT.GetAbsoluteBounds();
				// Second line (bottom quarter) is plain text, not the hyperlink.
				mouse.MoveTo(new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height * 0.75));
				await WindowHelper.WaitForIdle();
				mouse.Press();
				mouse.Release();
				await WindowHelper.WaitForIdle();

				Assert.IsFalse(clicked, "Clicking off the hyperlink must not raise its Click event");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		#endregion
	}
}
