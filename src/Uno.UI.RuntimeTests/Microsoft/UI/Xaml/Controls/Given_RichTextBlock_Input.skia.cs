using System;
using System.Threading.Tasks;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Text;
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

		[TestMethod]
		[Timeout(120_000)]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24748")]
		[DataRow(FlowDirection.LeftToRight, TextAlignment.Left)]
		[DataRow(FlowDirection.LeftToRight, TextAlignment.Right)]
		[DataRow(FlowDirection.LeftToRight, TextAlignment.Center)]
		[DataRow(FlowDirection.RightToLeft, TextAlignment.Left)]
		public async Task When_Click_On_Drawn_Short_Line_Hyperlink_Raises_Click(FlowDirection flowDirection, TextAlignment textAlignment)
			=> await AssertClickOnDrawnHyperlink(flowDirection, textAlignment, "Link");

		[TestMethod]
		[Timeout(120_000)]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24748")]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)] // Needs an Arabic fallback font, which only Windows is known to have here.
		public async Task When_RightToLeft_Click_On_Drawn_Arabic_Hyperlink_Raises_Click()
			=> await AssertClickOnDrawnHyperlink(FlowDirection.RightToLeft, TextAlignment.Left, "رابط");

		// A line shorter than the control sits on one side, on the right for right-to-left. A click on the link where it is
		// drawn raises its Click. So does a click on the empty space beside a line that holds only the link, on either side:
		// as WinUI's HitTestLink, the point maps to the nearest text position, which is in the link (measured on WinUI for
		// LTR Left, Right and Center and for RTL). The cursor is a hand wherever a click would activate the link.
		private static async Task AssertClickOnDrawnHyperlink(FlowDirection flowDirection, TextAlignment textAlignment, string text)
		{
			var hyperlink = new Hyperlink { Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red), UnderlineStyle = UnderlineStyle.None };
			hyperlink.Inlines.Add(new Run { Text = text });
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(hyperlink);
			var SUT = new RichTextBlock { Width = 300, FontSize = 24, FlowDirection = flowDirection, TextAlignment = textAlignment, TextWrapping = TextWrapping.NoWrap };
			SUT.Blocks.Add(paragraph);
			var host = CreateWhiteHost(SUT);

			var clicks = 0;
			hyperlink.Click += (_, _) => clicks++;

			try
			{
				await UITestHelper.Load(host);

				var ink = await GetInkBounds(host);
				var bounds = host.GetAbsoluteBounds();
				var drawnOnRight = (flowDirection == FlowDirection.RightToLeft) != (textAlignment == TextAlignment.Right);
				if (textAlignment != TextAlignment.Center)
				{
					Assert.IsTrue(
						drawnOnRight ? ink.Left > bounds.X + bounds.Width / 2 : ink.Right < bounds.X + bounds.Width / 2,
						$"Precondition: the link should be drawn on the {(drawnOnRight ? "right" : "left")} (ink {ink}, control {bounds})");
				}

				var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
				using var mouse = injector.GetMouse();
				var y = ink.Y + ink.Height / 2;

				async Task Click(Point point, int expected, string where)
				{
					mouse.MoveTo(new Point(point.X, bounds.Bottom + 50));
					await WindowHelper.WaitForIdle();
					mouse.MoveTo(point);
					await WindowHelper.WaitForIdle();
					Assert.AreEqual(InputSystemCursorShape.Hand, SUT.CalculatedFinalCursor, $"The cursor should be a hand {where}");
					mouse.Press();
					mouse.Release();
					await WindowHelper.WaitForIdle();
					Assert.AreEqual(expected, clicks, $"A click {where} should raise the link's Click event");
				}

				await Click(new Point(ink.X + ink.Width / 2, y), 1, "on the drawn link");

				// The empty space beside the line, where there is some.
				var expected = 1;
				if (ink.Left - bounds.Left > 20)
				{
					await Click(new Point((bounds.Left + ink.Left) / 2, y), ++expected, "left of the line");
				}
				if (bounds.Right - ink.Right > 20)
				{
					await Click(new Point((ink.Right + bounds.Right) / 2, y), ++expected, "right of the line");
				}

				mouse.MoveTo(new Point(bounds.X, bounds.Bottom + 50));
				await WindowHelper.WaitForIdle();
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[Timeout(120_000)]
		public async Task When_Click_Right_Of_Line_After_Plain_Text_Does_Not_Raise_Click()
		{
			// Not measured on WinUI, but it follows from the same mapping: right of the line "Link more text", the nearest
			// position is the end of the plain run, which is not in the link.
			var hyperlink = new Hyperlink { Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red), UnderlineStyle = UnderlineStyle.None };
			hyperlink.Inlines.Add(new Run { Text = "Link" });
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(hyperlink);
			paragraph.Inlines.Add(new Run { Text = " more text" });
			var SUT = new RichTextBlock { Width = 300, FontSize = 24, TextWrapping = TextWrapping.NoWrap };
			SUT.Blocks.Add(paragraph);
			var host = CreateWhiteHost(SUT);

			var clicks = 0;
			hyperlink.Click += (_, _) => clicks++;

			try
			{
				await UITestHelper.Load(host);
				var bounds = host.GetAbsoluteBounds();
				var ink = await GetInkBounds(host);

				var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
				using var mouse = injector.GetMouse();
				mouse.MoveTo(new Point(bounds.Right - 10, ink.Y + ink.Height / 2));
				await WindowHelper.WaitForIdle();
				Assert.AreEqual(InputSystemCursorShape.IBeam, SUT.CalculatedFinalCursor, "Right of the plain text, the cursor should not be a hand");
				mouse.Press();
				mouse.Release();
				await WindowHelper.WaitForIdle();
				Assert.AreEqual(0, clicks, "A click right of the plain text should not raise the link's Click event");

				mouse.MoveTo(new Point(ink.X + ink.Width / 2, ink.Y + ink.Height / 2));
				await WindowHelper.WaitForIdle();
				mouse.Press();
				mouse.Release();
				await WindowHelper.WaitForIdle();
				Assert.AreEqual(1, clicks, "A click on the drawn link should raise its Click event");

				mouse.MoveTo(new Point(bounds.X, bounds.Bottom + 50));
				await WindowHelper.WaitForIdle();
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[Timeout(120_000)]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24748")]
		public async Task When_RightToLeft_PointerDrag_Over_Drawn_Short_Line_Selects_It()
		{
			var SUT = new RichTextBlock
			{
				Width = 300,
				FontSize = 24,
				FlowDirection = FlowDirection.RightToLeft,
				TextWrapping = TextWrapping.NoWrap,
				Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red),
			};
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "Hello" });
			SUT.Blocks.Add(paragraph);
			var host = CreateWhiteHost(SUT);

			try
			{
				await UITestHelper.Load(host);

				var ink = await GetInkBounds(host);
				var midY = ink.Y + ink.Height / 2;

				var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
				using var mouse = injector.GetMouse();

				// From just before the drawn word to just past it.
				mouse.MoveTo(new Point(ink.Left - 3, midY));
				await WindowHelper.WaitForIdle();
				mouse.Press();
				await WindowHelper.WaitForIdle();
				mouse.MoveTo(new Point(ink.Right + 3, midY));
				await WindowHelper.WaitForIdle();
				mouse.Release();
				await WindowHelper.WaitForIdle();

				Assert.AreEqual("Hello", SUT.SelectedText, "Dragging across the drawn word should select exactly that word");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[Timeout(120_000)]
		public async Task When_Overflow_Hyperlink_Rtl()
		{
			// The link lands in the overflow of a right-to-left master; a click where it is drawn raises its Click.
			var master = new RichTextBlock { Width = 300, Height = 30, FontSize = 24, FlowDirection = FlowDirection.RightToLeft };
			var first = new Paragraph();
			first.Inlines.Add(new Run { Text = "First" });
			master.Blocks.Add(first);
			var hyperlink = new Hyperlink { Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red), UnderlineStyle = UnderlineStyle.None };
			hyperlink.Inlines.Add(new Run { Text = "Link" });
			var second = new Paragraph();
			second.Inlines.Add(hyperlink);
			master.Blocks.Add(second);
			var overflow = new RichTextBlockOverflow { Width = 300, Height = 60 };
			master.OverflowContentTarget = overflow;
			var panel = new StackPanel();
			panel.Children.Add(master);
			panel.Children.Add(overflow);
			var host = CreateWhiteHost(panel);

			var clicks = 0;
			hyperlink.Click += (_, _) => clicks++;

			try
			{
				await UITestHelper.Load(host);
				Assert.IsTrue(master.HasOverflowContent, "Precondition: the link is in the overflow");

				var ink = await GetInkBounds(host);
				// ActualWidth is the width of the text, as in WinUI.
				var bounds = overflow.TransformToVisual(null).TransformBounds(new Rect(0, 0, overflow.Width, overflow.Height));
				Assert.IsTrue(ink.Top >= bounds.Top && ink.Left > bounds.X + bounds.Width / 2, $"Precondition: the link is drawn on the right of the overflow (ink {ink}, overflow {bounds})");

				var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
				using var mouse = injector.GetMouse();
				mouse.MoveTo(new Point(ink.X + ink.Width / 2, ink.Y + ink.Height / 2));
				await WindowHelper.WaitForIdle();
				mouse.Press();
				mouse.Release();
				await WindowHelper.WaitForIdle();

				Assert.AreEqual(1, clicks, "Clicking the drawn link should raise its Click event");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[Timeout(120_000)]
		public async Task When_Hyperlink_Hover_Changes_Colour()
		{
			// The pointer-over brush replaces the link's foreground, which only repaints: a brush colors what is drawn.
			var hyperlink = new Hyperlink { UnderlineStyle = UnderlineStyle.None };
			hyperlink.Inlines.Add(new Run { Text = "Link text" });
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(hyperlink);
			var SUT = new RichTextBlock { Width = 300, FontSize = 24, FlowDirection = FlowDirection.RightToLeft, TextWrapping = TextWrapping.NoWrap };
			SUT.Blocks.Add(paragraph);
			var host = CreateWhiteHost(SUT);

			try
			{
				await UITestHelper.Load(host);
				var bounds = host.GetAbsoluteBounds();
				var before = await UITestHelper.ScreenShot(host);
				var normal = ((SolidColorBrush)hyperlink.Foreground).Color;
				var linkInk = ImageAssert.GetColorBounds(before, normal, tolerance: 60);
				Assert.IsTrue(linkInk.Width > 0, "Precondition: the link is drawn in its foreground");

				var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
				using var mouse = injector.GetMouse();
				var layoutUpdates = 0;
				void OnLayoutUpdated(object? sender, object e) => layoutUpdates++;
				SUT.LayoutUpdated += OnLayoutUpdated;
				RawBitmap during;
				Windows.UI.Color hover;
				try
				{
					mouse.MoveTo(new Point(bounds.X + linkInk.X + linkInk.Width / 2, bounds.Y + linkInk.Y + linkInk.Height / 2));
					await WindowHelper.WaitForIdle();
					hover = ((SolidColorBrush)hyperlink.Foreground).Color;
					during = await UITestHelper.ScreenShot(host);
				}
				finally
				{
					SUT.LayoutUpdated -= OnLayoutUpdated;
				}

				Assert.AreNotEqual(normal, hover, "Precondition: the pointer-over brush has another colour");
				// Anti-aliased edges of one colour can come close to the other, so compare how much of the ink takes it.
				var hoverBefore = CountPixels(before, hover);
				var hoverDuring = CountPixels(during, hover);
				Assert.IsTrue(hoverDuring > 20 && hoverDuring > 3 * hoverBefore, $"The link should be drawn in its pointer-over colour {hover} ({hoverBefore} pixels before, {hoverDuring} during)");
				Assert.AreEqual(0, layoutUpdates, "The hover should repaint without a layout pass");

				mouse.MoveTo(new Point(bounds.X + 5, bounds.Bottom + 50));
				await WindowHelper.WaitForIdle();
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		private static int CountPixels(RawBitmap bitmap, Windows.UI.Color color)
		{
			var count = 0;
			for (var y = 0; y < bitmap.Height; y++)
			{
				for (var x = 0; x < bitmap.Width; x++)
				{
					var pixel = bitmap.GetPixel(x, y);
					if (Math.Abs(pixel.R - color.R) + Math.Abs(pixel.G - color.G) + Math.Abs(pixel.B - color.B) <= 10)
					{
						count++;
					}
				}
			}

			return count;
		}

		[TestMethod]
		[Timeout(120_000)]
		public async Task When_Rtl_Selection_Flyout_Near_Selection()
		{
			// A touch release over a selection opens the selection flyout at the release point, on the mirrored target.
			var SUT = new RichTextBlock { Width = 300, FontSize = 24, FlowDirection = FlowDirection.RightToLeft, Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red) };
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "Hello world" });
			SUT.Blocks.Add(paragraph);
			var flyout = new Flyout { Content = new Border { Width = 40, Height = 20 } };
			SUT.SelectionFlyout = flyout;
			var host = CreateWhiteHost(SUT);

			try
			{
				await UITestHelper.Load(host);
				var ink = await GetInkBounds(host);
				// Near the start of the drawn text, far from the control's origin (its right edge in right-to-left).
				var touch = new Point(ink.X + 10, ink.Y + ink.Height / 2);

				var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
				using var finger = injector.GetFinger();
				finger.Press(touch);
				finger.Release();
				await WindowHelper.WaitForIdle();

				SUT.SelectAll();
				finger.Press(touch);
				finger.Release();
				await WindowHelper.WaitForIdle();
				await Task.Delay(200);
				await WindowHelper.WaitForIdle();

				Assert.IsTrue(flyout.IsOpen, "The selection flyout should open after a touch release over the selection");
				var presenter = (FrameworkElement)((FrameworkElement)flyout.Content).Parent;
				var presenterBounds = (presenter ?? (FrameworkElement)flyout.Content).GetAbsoluteBounds();
				Assert.IsTrue(
					Math.Abs((presenterBounds.Left + presenterBounds.Right) / 2 - touch.X) < 60,
					$"The flyout ({presenterBounds}) should open near the touch ({touch}), not on the mirrored side");
			}
			finally
			{
				flyout.Hide();
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[Timeout(120_000)]
		[DataRow(FlowDirection.LeftToRight)]
		[DataRow(FlowDirection.RightToLeft)]
		public async Task When_ShiftF10_Context_Menu_At_Pointer(FlowDirection flowDirection)
		{
			// Shift+F10 raises ContextMenuOpening at the last pointer position, in window coordinates.
			var SUT = new RichTextBlock { Width = 300, FontSize = 24, FlowDirection = flowDirection, Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red), Margin = new Thickness(30, 20, 0, 0) };
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "Hello world" });
			SUT.Blocks.Add(paragraph);
			var host = CreateWhiteHost(SUT);
			Point? opening = null;
			SUT.ContextMenuOpening += (_, e) =>
			{
				opening = new Point(e.CursorLeft, e.CursorTop);
				e.Handled = true;
			};

			try
			{
				await UITestHelper.Load(host);
				var ink = await GetInkBounds(host);
				var touch = new Point(ink.X + ink.Width / 2, ink.Y + ink.Height / 2);

				var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
				using (var finger = injector.GetFinger())
				{
					finger.Press(touch);
					finger.Release();
				}
				await WindowHelper.WaitForIdle();

				SUT.Focus(FocusState.Programmatic);
				SUT.SelectAll();
				await WindowHelper.WaitForIdle();
				SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.F10, VirtualKeyModifiers.Shift));
				await WindowHelper.WaitForIdle();

				Assert.IsNotNull(opening, "Shift+F10 should raise ContextMenuOpening");
				Assert.AreEqual(touch.X, opening!.Value.X, 2, "The menu's x should be the last pointer position's");
				Assert.AreEqual(touch.Y, opening.Value.Y, 2, "The menu's y should be the last pointer position's");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		private static Border CreateWhiteHost(UIElement child) => new()
		{
			Background = new SolidColorBrush(Microsoft.UI.Colors.White),
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Child = child,
		};

		// The bounds of the red ink in window coordinates.
		private static async Task<Rect> GetInkBounds(FrameworkElement host)
		{
			var shot = await UITestHelper.ScreenShot(host);
			var ink = ImageAssert.GetColorBounds(shot, Microsoft.UI.Colors.Red, tolerance: 60);
			Assert.IsTrue(ink.Width > 0, "Precondition: the text should be drawn");

			var bounds = host.GetAbsoluteBounds();
			return new Rect(bounds.X + ink.X, bounds.Y + ink.Y, ink.Width, ink.Height);
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
			// Wraps to the overflow's second line, where the release lands: on text, but not on the link. Empty space
			// beside or below a link resolves through the text view, which may land in the link (as WinUI's HitTestLink).
			second.Inlines.Add(new Run { Text = " plain" });
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
				var offLink = new Point(bounds.X + 12, bounds.Y + 37);

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
	}
}
