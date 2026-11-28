using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Input.Preview.Injection;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.DevTools.Input;
using static Private.Infrastructure.TestServices;

#nullable enable

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	// Every test here is bounded: injected input can go missing on a CI emulator, and an unbounded test
	// body then hangs the whole job (a healthy Android group runs ~11 min; a hung one burns the 45-minute
	// cap without naming a test).
	// Exercises the Stage 9b pointer/keyboard input pipeline end-to-end via injected input:
	// mouse press-drag-release selection through TextSelectionManager, double-click word
	// selection, shift+click extension, tap-to-clear, cross-paragraph drag, Ctrl+A/Ctrl+C,
	// and hyperlink Click. SkiaWasm is excluded (mouse/keyboard injection + clipboard).
	// Injected input can go missing on a CI emulator, and an unbounded test body then hangs the whole
	// job: a healthy Android group finishes in ~11 min, a hung one burns the 45-minute cap. Bound them
	// so a hang fails with a name instead.
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
		[Timeout(120_000)]
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
				if (start is null || end is null)
				{
					Assert.Fail($"A press-drag-release should produce non-null selection endpoints (start is null: {start is null}, end is null: {end is null})");
					return;
				}

				Assert.IsTrue(end.Offset > start.Offset, $"SelectionEnd ({end.Offset}) should be past SelectionStart ({start.Offset})");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[Timeout(120_000)]
		public async Task When_PointerDrag_In_Overflow_Selects_Range()
		{
			// CRichTextBlockOverflow::OnPointerPressed/Moved/Released feed the master's TextSelectionManager, so a
			// selection can be made inside any column of the chain.
			var master = CreateSingleLine("First line fits here", width: 400);
			master.Blocks.Add(new Paragraph { Inlines = { new Run { Text = "Overflowing words to select" } } });
			master.MaxLines = 1;

			var overflow = new RichTextBlockOverflow { Width = 400 };
			master.OverflowContentTarget = overflow;

			var panel = new StackPanel();
			panel.Children.Add(master);
			panel.Children.Add(overflow);

			try
			{
				await UITestHelper.Load(panel);
				Assert.IsTrue(master.HasOverflowContent, "Precondition: the second paragraph should overflow");

				var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
				using var mouse = injector.GetMouse();

				var bounds = overflow.GetAbsoluteBounds();
				var midY = bounds.Y + bounds.Height / 2;

				mouse.MoveTo(new Point(bounds.X + 2, midY));
				await WindowHelper.WaitForIdle();
				mouse.Press();
				await WindowHelper.WaitForIdle();
				mouse.MoveTo(new Point(bounds.Right - 2, midY));
				await WindowHelper.WaitForIdle();
				mouse.Release();
				await WindowHelper.WaitForIdle();

				Assert.IsTrue(master.SelectedText.Contains("words"), $"Dragging across the overflow should select its text (was '{master.SelectedText}')");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[Timeout(120_000)]
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
		[Timeout(120_000)]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/81")]
		[DataRow(200d)]
		[DataRow(500d)]
		public async Task When_ShiftClick_Extends_Selection(double viewportWidth)
		{
			var SUT = CreateSingleLine("The quick brown fox jumps over the lazy dog");
			SUT.HorizontalAlignment = HorizontalAlignment.Left;
			SUT.VerticalAlignment = VerticalAlignment.Top;
			var viewport = new Border
			{
				Width = viewportWidth,
				Height = 80,
				HorizontalAlignment = HorizontalAlignment.Left,
				Clip = new RectangleGeometry { Rect = new Rect(0, 0, viewportWidth, 80) },
				Child = SUT,
			};

			try
			{
				await UITestHelper.Load(viewport);
				Assert.IsTrue(SUT.Focus(FocusState.Programmatic), "The shift-click target must accept focus");
				await WindowHelper.WaitForIdle();

				var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
				using var mouse = injector.GetMouse();

				var run = (Run)((Paragraph)SUT.Blocks[0]).Inlines[0];
				var textStart = run.ContentStart;
				Assert.IsNotNull(textStart);
				var target = textStart.GetPositionAtOffset(4, LogicalDirection.Forward);
				Assert.IsNotNull(target);

				Point GetClickPoint(TextPointer position)
				{
					var rect = position.GetCharacterRect(LogicalDirection.Forward);
					Assert.IsGreaterThan(0, rect.Height, "The click must target laid-out text");
					return SUT.TransformToVisual(null).TransformPoint(
						new Point(rect.Left + 1, rect.Top + rect.Height / 2));
				}

				// A fixed-width control can extend beyond the runner's visible test area.
				var anchorPoint = GetClickPoint(textStart);
				var targetPoint = GetClickPoint(target);
				Assert.IsTrue(viewport.GetAbsoluteBounds().Contains(anchorPoint), "The anchor must be visible");
				Assert.IsTrue(viewport.GetAbsoluteBounds().Contains(targetPoint), "The shift-click target must be visible");

				mouse.MoveTo(anchorPoint);
				await WindowHelper.WaitForIdle();
				mouse.Press();
				mouse.Release();
				await WindowHelper.WaitForIdle();

				var anchor = SUT.SelectionStart;
				if (anchor is null)
				{
					Assert.Fail("The first click must establish the selection anchor");
					return;
				}

				Assert.AreEqual(string.Empty, SUT.SelectedText, "The first click should place a collapsed caret");

				// Gesture recognition uses injected timestamps; a wall-clock delay does not advance them.
				mouse.MoveTo(targetPoint, steps: 1, stepOffsetInMilliseconds: 600);
				await WindowHelper.WaitForIdle();
				mouse.Press(VirtualKeyModifiers.Shift);
				await WindowHelper.WaitForIdle();
				mouse.Release(VirtualKeyModifiers.Shift);
				await WindowHelper.WaitForIdle();

				Assert.AreEqual("The ", SUT.SelectedText, "Shift+click should select exactly to the targeted text position");
				var start = SUT.SelectionStart;
				var end = SUT.SelectionEnd;
				if (start is null || end is null)
				{
					Assert.Fail($"Shift+click should produce non-null selection endpoints (start is null: {start is null}, end is null: {end is null})");
					return;
				}

				Assert.AreEqual(anchor.Offset, start.Offset, "Shift+click must retain the original anchor");
				Assert.IsTrue(end.Offset > start.Offset, $"Shift+click should extend past the anchor (start {start.Offset}, end {end.Offset})");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[Timeout(120_000)]
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
		[Timeout(120_000)]
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

		// The command modifier is Cmd (not Ctrl) on macOS, and KeyboardHelper only injects Ctrl.
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaMacOS)]
		[TestMethod]
		[Timeout(120_000)]
		public async Task When_CtrlA_Selects_All()
		{
			var SUT = CreateSingleLine("Keyboard select all content");

			try
			{
				await UITestHelper.Load(SUT);

				await KeyboardHelper.PressKeySequence("$d$_ctrl#$d$_a#$u$_a#$u$_ctrl", SUT);
				await WindowHelper.WaitForIdle();

				Assert.AreEqual("Keyboard select all content", SUT.SelectedText, "Ctrl+A should select all content");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		// The command modifier is Cmd (not Ctrl) on macOS, and KeyboardHelper only injects Ctrl.
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaMacOS)]
		[TestMethod]
		[Timeout(120_000)]
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
		[Timeout(120_000)]
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
		[Timeout(120_000)]
		public async Task When_Selectable_Without_Hyperlink_Cursor_Is_IBeam()
		{
			// IsTextSelectionEnabled defaults to true, so nothing ever raises its changed callback
			// and a RichTextBlock with no hyperlink has no other trigger to seed the cursor.
			var SUT = CreateSingleLine("Just some selectable text");

			try
			{
				await UITestHelper.Load(SUT);

				Assert.AreEqual(InputSystemCursorShape.IBeam, SUT.CalculatedFinalCursor,
					"Selectable text should show the I-beam cursor without needing a property change");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[Timeout(120_000)]
		public async Task When_Pointer_Over_Hyperlink_Cursor_Is_Hand()
		{
			var (SUT, _) = CreateHyperlinkSut();

			try
			{
				await UITestHelper.Load(SUT);

				var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
				using var mouse = injector.GetMouse();

				var bounds = SUT.GetAbsoluteBounds();
				var centerX = bounds.X + bounds.Width / 2;

				// First line (top quarter) holds the hyperlink, second line holds plain text.
				mouse.MoveTo(new Point(centerX, bounds.Y + bounds.Height * 0.25));
				await WindowHelper.WaitForIdle();

				Assert.AreEqual(InputSystemCursorShape.Hand, SUT.CalculatedFinalCursor,
					"Hovering a hyperlink should show the hand cursor");

				mouse.MoveTo(new Point(centerX, bounds.Y + bounds.Height * 0.75));
				await WindowHelper.WaitForIdle();

				Assert.AreEqual(InputSystemCursorShape.IBeam, SUT.CalculatedFinalCursor,
					"Hovering selectable text outside a hyperlink should fall back to the I-beam");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[Timeout(120_000)]
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
		[Timeout(120_000)]
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

		[TestMethod]
		[Timeout(120_000)]
		public async Task When_Hyperlink_In_Overflow_Is_Clicked()
		{
			// An overflow column hosts the master's content, so it has to answer for the links inside its
			// own slice. Nothing handled input there, leaving every column after the first inert.
			// The link gets its own paragraph, so MaxLines=1 puts it at the overflow's top-left.
			var master = new RichTextBlock { Width = 200, MaxLines = 1, FontSize = 20 };
			var first = new Paragraph();
			first.Inlines.Add(new Run { Text = "First" });
			master.Blocks.Add(first);

			var second = new Paragraph();
			var hyperlink = new Hyperlink();
			hyperlink.Inlines.Add(new Run { Text = "CLICKMECLICKME" });
			second.Inlines.Add(hyperlink);
			master.Blocks.Add(second);

			var overflow = new RichTextBlockOverflow { Width = 200, Height = 120 };
			master.OverflowContentTarget = overflow;

			var clicks = 0;
			hyperlink.Click += (_, _) => clicks++;

			var root = new StackPanel();
			root.Children.Add(master);
			root.Children.Add(overflow);

			try
			{
				await UITestHelper.Load(root);
				Assert.IsTrue(master.HasOverflowContent, "The link must land in the overflow, not the master");

				var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
				using var mouse = injector.GetMouse();

				var bounds = overflow.GetAbsoluteBounds();
				var target = new Point(bounds.X + 12, bounds.Y + 10);

				mouse.MoveTo(target);
				await WindowHelper.WaitForIdle();
				mouse.Press();
				await WindowHelper.WaitForIdle();
				mouse.Release();
				await WindowHelper.WaitForIdle();

				Assert.AreEqual(1, clicks, "Clicking the link inside the overflow should activate it");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[Timeout(120_000)]
		public async Task When_Hyperlink_In_Overflow_Is_Released_CaptureLost_Is_Not_Raised()
		{
			// WinUI raises no PointerCaptureLost for a link click; the overflow must match the master.
			var master = new RichTextBlock { Width = 200, MaxLines = 1, FontSize = 20 };
			var first = new Paragraph();
			first.Inlines.Add(new Run { Text = "First" });
			master.Blocks.Add(first);

			var second = new Paragraph();
			var hyperlink = new Hyperlink();
			hyperlink.Inlines.Add(new Run { Text = "CLICKMECLICKME" });
			second.Inlines.Add(hyperlink);
			master.Blocks.Add(second);

			var overflow = new RichTextBlockOverflow { Width = 200, Height = 120 };
			master.OverflowContentTarget = overflow;

			var clicks = 0;
			hyperlink.Click += (_, _) => clicks++;
			var captureLost = 0;
			overflow.PointerCaptureLost += (_, _) => captureLost++;

			var root = new StackPanel();
			root.Children.Add(master);
			root.Children.Add(overflow);

			try
			{
				await UITestHelper.Load(root);
				Assert.IsTrue(master.HasOverflowContent, "The link must land in the overflow, not the master");

				var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
				using var mouse = injector.GetMouse();

				var bounds = overflow.GetAbsoluteBounds();
				var onLink = new Point(bounds.X + 12, bounds.Y + 10);
				var offLink = new Point(bounds.X + 12, bounds.Y + 100);

				mouse.MoveTo(onLink);
				await WindowHelper.WaitForIdle();
				mouse.Press();
				await WindowHelper.WaitForIdle();
				mouse.Release();
				await WindowHelper.WaitForIdle();

				Assert.AreEqual(1, clicks);
				Assert.AreEqual(0, captureLost, "A link click should not raise PointerCaptureLost");

				mouse.Press();
				await WindowHelper.WaitForIdle();
				mouse.MoveTo(offLink);
				await WindowHelper.WaitForIdle();
				mouse.Release();
				await WindowHelper.WaitForIdle();

				Assert.AreEqual(1, clicks, "Releasing away from the link should not activate it");
				Assert.AreEqual(0, captureLost, "Releasing away from the link should not raise PointerCaptureLost");

				mouse.MoveTo(onLink);
				await WindowHelper.WaitForIdle();
				mouse.Press();
				await WindowHelper.WaitForIdle();
				mouse.Release();
				await WindowHelper.WaitForIdle();

				Assert.AreEqual(2, clicks, "The aborted press should not leave the link stuck");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[Timeout(120_000)]
		public async Task When_Selection_Drag_In_Overflow_Is_Released_CaptureLost_Is_Raised()
		{
			// Only the capture taken for a link press is released silently. The selection manager releases its drag
			// capture itself, which raises PointerCaptureLost (PointerInputProcessor::ReleasePointerCapture).
			var master = new RichTextBlock { Width = 200, MaxLines = 1, FontSize = 20 };
			var first = new Paragraph();
			first.Inlines.Add(new Run { Text = "First" });
			master.Blocks.Add(first);

			var second = new Paragraph();
			second.Inlines.Add(new Run { Text = "Selectable overflow text" });
			master.Blocks.Add(second);

			var overflow = new RichTextBlockOverflow { Width = 200, Height = 120 };
			master.OverflowContentTarget = overflow;

			var captureLost = 0;
			overflow.PointerCaptureLost += (_, _) => captureLost++;

			var root = new StackPanel();
			root.Children.Add(master);
			root.Children.Add(overflow);

			try
			{
				await UITestHelper.Load(root);
				Assert.IsTrue(master.HasOverflowContent, "The second paragraph must land in the overflow");

				var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
				using var mouse = injector.GetMouse();

				var bounds = overflow.GetAbsoluteBounds();
				mouse.MoveTo(new Point(bounds.X + 4, bounds.Y + 10));
				await WindowHelper.WaitForIdle();
				mouse.Press();
				await WindowHelper.WaitForIdle();
				mouse.MoveTo(new Point(bounds.X + 120, bounds.Y + 10));
				await WindowHelper.WaitForIdle();
				mouse.Release();
				await WindowHelper.WaitForIdle();

				Assert.IsFalse(string.IsNullOrEmpty(master.SelectedText), "The drag should select text in the overflow");
				Assert.AreEqual(1, captureLost, "Releasing a selection drag should raise PointerCaptureLost once");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[Timeout(120_000)]
		public async Task When_Touch_Tap_Selects_Word_Flyout_Opens_Above_Selection()
		{
			var flyout = new Flyout { Content = new Border { Width = 60, Height = 20 } };
			var sut = new RichTextBlock
			{
				IsTextSelectionEnabled = true,
				// Room above for the flyout, whatever the window size.
				Margin = new Thickness(0, 120, 0, 0),
				Padding = new Thickness(0, 10, 0, 0),
				SelectionFlyout = flyout,
				Blocks = { new Paragraph { Inlines = { new Run { Text = "hello uno world" } } } },
			};

			var bounds = await UITestHelper.Load(sut);
			try
			{
				var lineTop = bounds.Y + sut.Padding.Top;
				var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
				using (var finger = injector.GetFinger())
				{
					finger.Press(new Point(bounds.X + 10, bounds.Bottom - 3));
					finger.Release();
				}

				await WindowHelper.WaitFor(() => flyout.IsOpen, timeoutMS: 3000, message: "a touch tap on a word selects it and shows the flyout");
				await WindowHelper.WaitForIdle();

				var presenter = VisualTreeHelper.GetOpenPopupsForXamlRoot(sut.XamlRoot!).Select(p => p.Child).OfType<FlyoutPresenter>().Single();
				var presenterBounds = presenter.TransformToVisual(null).TransformBounds(new Rect(0, 0, presenter.ActualWidth, presenter.ActualHeight));

				// As in WinUI, the flyout is shown at the top of the selection, not at the tap point, so it doesn't cover the
				// line that was tapped.
				Assert.IsTrue(presenterBounds.Bottom <= lineTop + 1, $"the flyout ({presenterBounds}) should end above the selected line (top {lineTop})");
			}
			finally
			{
				flyout.Hide();
			}
		}

		private static (RichTextBlock Master, RichTextBlockOverflow Overflow, StackPanel Panel) CreateWithOverflow(Flyout? selectionFlyout = null)
		{
			var master = new RichTextBlock
			{
				IsTextSelectionEnabled = true,
				Width = 400,
				MaxLines = 1,
				// Room above each line for the flyout, whatever the window size.
				Margin = new Thickness(0, 120, 0, 0),
				SelectionFlyout = selectionFlyout,
				Blocks =
				{
					new Paragraph { Inlines = { new Run { Text = "hello uno world" } } },
					new Paragraph { Inlines = { new Run { Text = "second paragraph words" } } },
				},
			};
			var overflow = new RichTextBlockOverflow { Width = 400, Margin = new Thickness(0, 120, 0, 0) };
			master.OverflowContentTarget = overflow;

			var panel = new StackPanel();
			panel.Children.Add(master);
			panel.Children.Add(overflow);

			return (master, overflow, panel);
		}

		[TestMethod]
		[Timeout(120_000)]
		public async Task When_Touch_Tap_With_Overflow_Flyout_Opens_Above_Selection()
		{
			var flyout = new Flyout { Content = new Border { Width = 60, Height = 20 } };
			var (master, _, panel) = CreateWithOverflow(flyout);

			await UITestHelper.Load(panel);
			try
			{
				Assert.IsTrue(master.HasOverflowContent, "Precondition: the second paragraph should overflow");

				var bounds = master.GetAbsoluteBounds();
				var tap = new Point(bounds.X + 10, bounds.Y + bounds.Height / 2);
				var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
				using (var finger = injector.GetFinger())
				{
					finger.Press(tap);
					finger.Release();
				}

				await WindowHelper.WaitFor(() => flyout.IsOpen, timeoutMS: 3000, message: $"a touch tap on a word shows the selection flyout (selection '{master.SelectedText}')");
				await WindowHelper.WaitForIdle();

				var presenter = VisualTreeHelper.GetOpenPopupsForXamlRoot(master.XamlRoot!).Select(p => p.Child).OfType<FlyoutPresenter>().Single();
				var presenterBounds = presenter.TransformToVisual(null).TransformBounds(new Rect(0, 0, presenter.ActualWidth, presenter.ActualHeight));

				Assert.IsTrue(presenterBounds.Bottom <= bounds.Y + 1, $"the flyout ({presenterBounds}) should end above the selected line (top {bounds.Y})");
				Assert.IsTrue(presenterBounds.Left <= tap.X && tap.X <= presenterBounds.Right, $"the flyout ({presenterBounds}) should be over the tap ({tap})");
			}
			finally
			{
				flyout.Hide();
			}
		}

		[TestMethod]
		[Timeout(120_000)]
		[DataRow(true)]
		[DataRow(false)]
		public async Task When_Selection_In_Overflow_Bounding_Rect_Starts_At_Its_First_Line(bool startsInMaster)
		{
			var (master, overflow, panel) = CreateWithOverflow();

			await UITestHelper.Load(panel);
			Assert.IsTrue(master.HasOverflowContent, "Precondition: the second paragraph should overflow");

			var second = (Run)((Paragraph)master.Blocks[1]).Inlines[0];
			if (startsInMaster)
			{
				master.SelectAll();
			}
			else
			{
				master.Select(second.ContentStart!, second.ContentEnd!);
			}

			await WindowHelper.WaitForIdle();

			var manager = master.GetSelectionManager() ?? throw new InvalidOperationException("The RichTextBlock should have a selection manager");
			var bounds = manager.GetSelectionBoundingRect();

			// In the master's coordinates, as WinUI maps each overflow's rects into the owner's.
			var overflowTop = overflow.TransformToVisual(master).TransformPoint(default).Y;
			var lineHeight = master.ActualHeight;

			Assert.IsFalse(bounds.IsEmpty, "a selection should have bounds");
			Assert.AreEqual(startsInMaster ? 0 : overflowTop, bounds.Top, 1, "the bounds start at the selection's first line");
			Assert.IsTrue(bounds.Bottom >= overflowTop + lineHeight / 2, $"the bounds ({bounds}) should take in the overflow's line (top {overflowTop})");
		}

		[TestMethod]
		[Timeout(120_000)]
		public async Task When_Negative_Rect_Sizes_Disallowed_Touch_Tap_Shows_Flyout()
		{
			var flyout = new Flyout { Content = new Border { Width = 60, Height = 20 } };
			var sut = new RichTextBlock
			{
				IsTextSelectionEnabled = true,
				Margin = new Thickness(0, 120, 0, 0),
				SelectionFlyout = flyout,
				Blocks = { new Paragraph { Inlines = { new Run { Text = "hello uno world" } } } },
			};

			var bounds = await UITestHelper.Load(sut);
			var allowNegative = global::Uno.FoundationFeatureConfiguration.Rect.AllowNegativeWidthHeight;
			global::Uno.FoundationFeatureConfiguration.Rect.AllowNegativeWidthHeight = false;
			try
			{
				var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
				using (var finger = injector.GetFinger())
				{
					finger.Press(new Point(bounds.X + 10, bounds.Y + bounds.Height / 2));
					finger.Release();
				}

				await WindowHelper.WaitFor(() => flyout.IsOpen, timeoutMS: 3000, message: "a touch tap on a word shows the flyout when the app disallows negative rect sizes");
			}
			finally
			{
				global::Uno.FoundationFeatureConfiguration.Rect.AllowNegativeWidthHeight = allowNegative;
				flyout.Hide();
			}
		}

		[TestMethod]
		[Timeout(120_000)]
		public async Task When_Selection_On_Several_Lines_Bounding_Rect_Is_Their_Union()
		{
			// The second line is shorter and starts further left than the selected part of the first.
			var sut = new RichTextBlock
			{
				IsTextSelectionEnabled = true,
				Width = 150,
				TextWrapping = TextWrapping.Wrap,
				Blocks = { new Paragraph { Inlines = { new Run { Text = "alpha beta gamma delta epsilon" } } } },
			};

			await UITestHelper.Load(sut);

			var run = (Run)((Paragraph)sut.Blocks[0]).Inlines[0];
			sut.Select(run.ContentStart!.GetPositionAtOffset(8, LogicalDirection.Forward)!, run.ContentStart!.GetPositionAtOffset(24, LogicalDirection.Forward)!);
			await WindowHelper.WaitForIdle();

			var manager = sut.GetSelectionManager() ?? throw new InvalidOperationException("The RichTextBlock should have a selection manager");
			manager.GetSelectionHighlightRects(sut.GetSingleElementTextView()!, out var lines);
			Assert.IsGreaterThan(1, lines.Length, "Precondition: the selection should span several lines");
			Assert.IsTrue(lines.Select(r => r.Left).Distinct().Count() > 1, "Precondition: the lines should start at different x");

			var bounds = manager.GetSelectionBoundingRect();

			Assert.AreEqual(lines.Min(r => r.Left), bounds.Left, 0.01, "left");
			Assert.AreEqual(lines.Min(r => r.Top), bounds.Top, 0.01, "top");
			Assert.AreEqual(lines.Max(r => r.Right), bounds.Right, 0.01, "right");
			Assert.AreEqual(lines.Max(r => r.Bottom), bounds.Bottom, 0.01, "bottom");
		}

		[TestMethod]
		[Timeout(120_000)]
		public async Task When_Touch_Tap_On_Selection_Toggles_Flyout()
		{
			var flyout = new Flyout { Content = new Border { Width = 60, Height = 20 } };
			var sut = new RichTextBlock
			{
				IsTextSelectionEnabled = true,
				// Room above for the flyout, which opens above the selection, whatever the window size.
				Margin = new Thickness(0, 120, 0, 0),
				SelectionFlyout = flyout,
				Blocks = { new Paragraph { Inlines = { new Run { Text = "hello uno world" } } } },
			};

			var bounds = await UITestHelper.Load(sut);
			try
			{
				var point = new Point(bounds.X + 10, bounds.Y + bounds.Height / 2);

				// Each tap with its own injector, so taps a moment apart don't count as a double-tap.
				void Tap()
				{
					var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
					using var finger = injector.GetFinger();
					finger.Press(point);
					finger.Release();
				}

				Tap();
				await WindowHelper.WaitFor(() => flyout.IsOpen, timeoutMS: 3000, message: "a touch tap on a word selects it and shows the flyout");
				var selected = sut.SelectedText;
				Assert.AreEqual("hello", selected.TrimEnd());

				// The tap that closes the transient flyout reaches the RichTextBlock: it must not reopen it.
				await Task.Delay(600);
				Tap();
				await WindowHelper.WaitForIdle();
				await Task.Delay(300);
				await WindowHelper.WaitForIdle();
				Assert.IsFalse(flyout.IsOpen, "a tap on the selection while the flyout shows should hide it");
				Assert.AreEqual(selected, sut.SelectedText, "the tap must keep the selection");

				await Task.Delay(600);
				Tap();
				await WindowHelper.WaitFor(() => flyout.IsOpen, timeoutMS: 3000, message: "tapping the selection again shows the flyout");
			}
			finally
			{
				flyout.Hide();
			}
		}
	}
}
