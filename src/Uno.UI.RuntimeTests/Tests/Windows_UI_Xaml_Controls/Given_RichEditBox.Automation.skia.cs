#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DirectUI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Automation.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public partial class Given_RichEditBox
{
	[TestMethod]
	public void When_Accessibility_Text_Input_Exceeds_Source_Limit_Is_Rejected()
	{
		var editor = new RichEditBox { MaxLength = 4 };
		editor.Document.SetText(TextSetOptions.None, "ab");
		editor.Document.Selection.SetRange(1, 1);

		Assert.IsFalse(editor.ApplyAccessibilityTextInput("a123456789b", 10, 10));
		Assert.AreEqual("ab", editor.Document.GetTextInRange(0, editor.Document.TextLength));
	}

	[TestMethod]
	public async Task When_Automation_TextEdit_Uses_Active_IME_Ranges_And_Events()
	{
		var fake = new FakeImeTextBoxExtension();
		using var imeDisposable = RichEditBox.SetImeExtensionForTesting(fake);
		var listener = new TextEditAutomationListener();
		var sut = new RichEditBox();
		try
		{
			WindowHelper.WindowContent = sut;
			await WindowHelper.WaitForLoaded(sut);
			sut.Document.SetText(TextSetOptions.None, "AB");
			sut.Document.Selection.SetRange(1, 1);
			sut.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(sut);
			Assert.IsNotNull(peer);
			var textEditProvider = peer.GetPattern(PatternInterface.TextEdit) as ITextEditProvider;
			Assert.IsNotNull(textEditProvider);
			Assert.AreSame(peer.GetPattern(PatternInterface.Text), textEditProvider);
			var provider = textEditProvider!;

			AutomationPeer.TestAutomationPeerListener = listener;
			fake.SimulateCompositionStart();
			fake.SimulateCompositionUpdate("nihao", cursorPosition: 2, resolvedLength: 2);

			Assert.AreEqual("nihao", provider.GetActiveComposition().GetText(-1));
			Assert.AreEqual("hao", provider.GetConversionTarget().GetText(-1));
			Assert.AreEqual(AutomationTextEditChangeType.Composition, listener.TextEditChanges[0].ChangeType);
			CollectionAssert.AreEqual(new[] { "nihao" }, listener.TextEditChanges[0].ChangedData);
			Assert.AreEqual(1, listener.Events.Count(eventId => eventId == AutomationEvents.ConversionTargetChanged));

			fake.SimulateCompositionComplete("你好");

			Assert.IsNull(provider.GetActiveComposition());
			Assert.IsNull(provider.GetConversionTarget());
			Assert.AreEqual(AutomationTextEditChangeType.CompositionFinalized, listener.TextEditChanges[1].ChangeType);
			CollectionAssert.AreEqual(new[] { "你好" }, listener.TextEditChanges[1].ChangedData);
			Assert.AreEqual(2, listener.Events.Count(eventId => eventId == AutomationEvents.ConversionTargetChanged));
		}
		finally
		{
			AutomationPeer.TestAutomationPeerListener = null;
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Automation_Range_Contains_Link_And_Image_Children()
	{
		var sut = new RichEditBox { Width = 320, Height = 140 };
		try
		{
			WindowHelper.WindowContent = sut;
			await WindowHelper.WaitForLoaded(sut);
			sut.Document.SetText(TextSetOptions.None, "prefix link suffix");
			sut.Document.GetRange(7, 11).Link = "\"javascript:alert(1)\"";
			sut.Document.GetRange(sut.Document.TextLength, sut.Document.TextLength)
				.InsertImage(20, 14, 10, VerticalCharacterAlignment.Baseline, "logo", CreateImageStream(SKColors.Red));
			await WindowHelper.WaitForIdle();

			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(sut);
			var textProvider = peer?.GetPattern(PatternInterface.Text) as ITextProvider;
			Assert.IsNotNull(peer);
			Assert.IsNotNull(textProvider);
			var provider = textProvider!;

			var firstChildren = provider.DocumentRange.GetChildren();
			var secondChildren = provider.DocumentRange.GetChildren();
			Assert.HasCount(2, firstChildren);
			Assert.HasCount(2, secondChildren);

			var linkProvider = firstChildren.Single(child =>
				child.AutomationPeer?.GetAutomationControlType() == AutomationControlType.Hyperlink);
			var imageProvider = firstChildren.Single(child =>
				child.AutomationPeer?.GetAutomationControlType() == AutomationControlType.Image);
			var secondLink = secondChildren.Single(child =>
				child.AutomationPeer?.GetAutomationControlType() == AutomationControlType.Hyperlink);
			var secondImage = secondChildren.Single(child =>
				child.AutomationPeer?.GetAutomationControlType() == AutomationControlType.Image);

			Assert.AreSame(linkProvider.AutomationPeer, secondLink.AutomationPeer);
			Assert.AreSame(imageProvider.AutomationPeer, secondImage.AutomationPeer);
			Assert.AreEqual("link", linkProvider.AutomationPeer?.GetName());
			Assert.AreEqual("logo", imageProvider.AutomationPeer?.GetName());
			var linkRange = provider.RangeFromChild(linkProvider);
			var imageRange = provider.RangeFromChild(imageProvider);
			var linkTextChild = linkProvider.AutomationPeer?.GetPattern(PatternInterface.TextChild) as ITextChildProvider;
			var imageTextChild = imageProvider.AutomationPeer?.GetPattern(PatternInterface.TextChild) as ITextChildProvider;
			Assert.AreEqual("link", linkRange.GetText(-1));
			Assert.AreEqual("logo", imageRange.GetText(-1));
			Assert.IsNotNull(linkTextChild);
			Assert.IsNotNull(imageTextChild);
			var linkChild = linkTextChild!;
			var imageChild = imageTextChild!;
			Assert.AreSame(peer, linkChild.TextContainer.AutomationPeer);
			Assert.AreSame(peer, imageChild.TextContainer.AutomationPeer);
			Assert.IsTrue(linkChild.TextRange.Compare(linkRange));
			Assert.IsTrue(imageChild.TextRange.Compare(imageRange));
			var movedTextChildRange = linkChild.TextRange;
			movedTextChildRange.MoveEndpointByUnit(TextPatternRangeEndpoint.Start, TextUnit.Character, 1);
			Assert.AreEqual("link", provider.RangeFromChild(linkProvider).GetText(-1));
			Assert.AreEqual("link", linkChild.TextRange.GetText(-1));
			Assert.IsInstanceOfType<IInvokeProvider>(linkProvider.AutomationPeer?.GetPattern(PatternInterface.Invoke));
			Assert.IsNull(imageProvider.AutomationPeer?.GetPattern(PatternInterface.Invoke));

			((IInvokeProvider)linkProvider.AutomationPeer!.GetPattern(PatternInterface.Invoke)!).Invoke();
			Assert.IsFalse(RichEditBox.TryGetLinkUri("\"javascript:alert(1)\"", out _));

			sut.Document.GetRange(0, 0).SetText(TextSetOptions.None, "X");
			await WindowHelper.WaitForIdle();

			var rebasedChildren = provider.DocumentRange.GetChildren();
			var rebasedLink = rebasedChildren.Single(child =>
				child.AutomationPeer?.GetAutomationControlType() == AutomationControlType.Hyperlink);
			Assert.AreSame(linkProvider.AutomationPeer, rebasedLink.AutomationPeer);
			Assert.AreEqual("link", provider.RangeFromChild(rebasedLink).GetText(-1));
			Assert.AreEqual("link", linkChild.TextRange.GetText(-1));
			Assert.AreEqual("logo", imageChild.TextRange.GetText(-1));

			sut.Document.GetRange(8, 12).Link = string.Empty;
			await WindowHelper.WaitForIdle();

			Assert.HasCount(1, provider.DocumentRange.GetChildren());
			Assert.IsNull(provider.RangeFromChild(linkProvider));
			Assert.AreEqual("link", linkChild.TextRange.GetText(-1));
			Assert.AreEqual("logo", imageChild.TextRange.GetText(-1));
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Automation_Range_Enclosing_Element_Uses_Innermost_Text_Object()
	{
		var sut = new RichEditBox { Width = 320, Height = 140 };
		try
		{
			WindowHelper.WindowContent = sut;
			await WindowHelper.WaitForLoaded(sut);
			sut.Document.SetText(TextSetOptions.None, "prefix link suffix ");
			sut.Document.GetRange(7, 11).Link = "\"https://example.com\"";
			sut.Document.GetRange(sut.Document.TextLength, sut.Document.TextLength)
				.InsertImage(20, 14, 10, VerticalCharacterAlignment.Baseline, "logo", CreateImageStream(SKColors.Red));
			await WindowHelper.WaitForIdle();

			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(sut);
			var textProvider = peer?.GetPattern(PatternInterface.Text) as ITextProvider;
			Assert.IsNotNull(peer);
			Assert.IsNotNull(textProvider);
			var provider = textProvider!;
			var children = provider.DocumentRange.GetChildren();
			var linkProvider = children.Single(child =>
				child.AutomationPeer?.GetAutomationControlType() == AutomationControlType.Hyperlink);
			var imageProvider = children.Single(child =>
				child.AutomationPeer?.GetAutomationControlType() == AutomationControlType.Image);
			var linkRange = provider.RangeFromChild(linkProvider);
			var imageRange = provider.RangeFromChild(imageProvider);

			Assert.AreSame(linkProvider.AutomationPeer, linkRange.GetEnclosingElement().AutomationPeer);
			var interior = linkRange.Clone();
			interior.MoveEndpointByUnit(TextPatternRangeEndpoint.Start, TextUnit.Character, 1);
			interior.MoveEndpointByUnit(TextPatternRangeEndpoint.End, TextUnit.Character, -1);
			Assert.AreSame(linkProvider.AutomationPeer, interior.GetEnclosingElement().AutomationPeer);

			var partial = linkRange.Clone();
			partial.MoveEndpointByUnit(TextPatternRangeEndpoint.Start, TextUnit.Character, -1);
			Assert.AreSame(peer, partial.GetEnclosingElement().AutomationPeer);

			var caret = linkRange.Clone();
			caret.MoveEndpointByUnit(TextPatternRangeEndpoint.Start, TextUnit.Character, 1);
			caret.MoveEndpointByRange(
				TextPatternRangeEndpoint.End,
				caret,
				TextPatternRangeEndpoint.Start);
			Assert.AreSame(linkProvider.AutomationPeer, caret.GetEnclosingElement().AutomationPeer);

			Assert.AreSame(imageProvider.AutomationPeer, imageRange.GetEnclosingElement().AutomationPeer);
			var imageInterior = imageRange.Clone();
			imageInterior.MoveEndpointByUnit(TextPatternRangeEndpoint.Start, TextUnit.Character, 1);
			imageInterior.MoveEndpointByUnit(TextPatternRangeEndpoint.End, TextUnit.Character, -1);
			Assert.AreSame(imageProvider.AutomationPeer, imageInterior.GetEnclosingElement().AutomationPeer);

			var imagePartial = imageRange.Clone();
			imagePartial.MoveEndpointByUnit(TextPatternRangeEndpoint.Start, TextUnit.Character, -1);
			Assert.AreSame(peer, imagePartial.GetEnclosingElement().AutomationPeer);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Automation_TextRange2_Shows_Available_RichEditBox_Flyouts()
	{
		var contextFlyout = new MenuFlyout();
		contextFlyout.Items.Add(new MenuFlyoutItem { Text = "Context" });
		var selectionFlyout = new MenuFlyout();
		selectionFlyout.Items.Add(new MenuFlyoutItem { Text = "Selection" });
		var sut = new RichEditBox
		{
			Width = 320,
			Height = 140,
			ContextFlyout = contextFlyout,
			SelectionFlyout = selectionFlyout,
		};
		var contextOpened = 0;
		var selectionOpened = 0;
		var contextMenuOpening = 0;
		contextFlyout.Opened += (_, _) => contextOpened++;
		selectionFlyout.Opened += (_, _) => selectionOpened++;
		sut.ContextMenuOpening += (_, args) =>
		{
			contextMenuOpening++;
			Assert.IsGreaterThanOrEqualTo(0, args.CursorLeft);
			Assert.IsGreaterThanOrEqualTo(0, args.CursorTop);
		};

		try
		{
			WindowHelper.WindowContent = sut;
			await WindowHelper.WaitForLoaded(sut);
			sut.Document.SetText(TextSetOptions.None, "context menu");
			sut.Document.Selection.SetRange(0, 0);
			await WindowHelper.WaitForIdle();

			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(sut);
			var textProvider = peer?.GetPattern(PatternInterface.Text) as ITextProvider;
			Assert.IsNotNull(textProvider);
			var provider = textProvider!;
			var range = provider.DocumentRange.FindText("context", backward: false, ignoreCase: false);
			var range2 = range as ITextRangeProvider2;
			Assert.IsNotNull(range2);
			var provider2 = range2!;

			provider2.ShowContextMenu();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, contextOpened);
			Assert.AreEqual(1, contextMenuOpening);
			Assert.AreEqual(0, sut.Document.Selection.StartPosition);
			Assert.AreEqual(0, sut.Document.Selection.EndPosition);
			contextFlyout.Hide();
			await WindowHelper.WaitForIdle();

			sut.ContextFlyout = null;
			provider2.ShowContextMenu();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, selectionOpened);
			selectionFlyout.Hide();
			await WindowHelper.WaitForIdle();

			sut.SelectionFlyout = null!;
			sut.IsSpellCheckEnabled = false;
			var proofingFlyout = (MenuFlyout)sut.ProofingMenuFlyout;
			var proofingOpened = 0;
			proofingFlyout.Items.Add(new MenuFlyoutItem { Text = "Proofing" });
			proofingFlyout.Opened += (_, _) => proofingOpened++;
			var caret = range.Clone();
			caret.MoveEndpointByRange(
				TextPatternRangeEndpoint.End,
				caret,
				TextPatternRangeEndpoint.Start);

			((ITextRangeProvider2)caret).ShowContextMenu();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, proofingOpened);
			proofingFlyout.Hide();
			await WindowHelper.WaitForIdle();
			proofingFlyout.Items.Clear();

			((ITextRangeProvider2)caret).ShowContextMenu();
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Automation_Ranges_Are_Retained_They_Rebase_With_The_Document()
	{
		const string firstLinkText = "first-link";
		const string secondLinkText = "second-link";
		var sut = new RichEditBox { Width = 420, Height = 140, TextWrapping = TextWrapping.NoWrap };
		try
		{
			WindowHelper.WindowContent = sut;
			await WindowHelper.WaitForLoaded(sut);
			var text = $"prefix {firstLinkText} middle {secondLinkText} suffix";
			var firstLinkStart = text.IndexOf(firstLinkText, StringComparison.Ordinal);
			var secondLinkStart = text.IndexOf(secondLinkText, StringComparison.Ordinal);
			sut.Document.SetText(TextSetOptions.None, text);
			sut.Document.GetRange(firstLinkStart, firstLinkStart + firstLinkText.Length).Link = "\"https://contoso.example/shared\"";
			sut.Document.GetRange(secondLinkStart, secondLinkStart + secondLinkText.Length).Link = "\"https://contoso.example/shared\"";
			using (var stream = CreateImageStream(SKColors.Red))
			{
				sut.Document.GetRange(sut.Document.TextLength, sut.Document.TextLength)
					.InsertImage(20, 14, 10, VerticalCharacterAlignment.Baseline, "retained image", stream);
			}
			await WindowHelper.WaitForIdle();

			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(sut);
			var textProvider = peer?.GetPattern(PatternInterface.Text) as ITextProvider;
			Assert.IsNotNull(textProvider);

			var documentRange = textProvider.DocumentRange;
			var secondRange = documentRange.FindText(secondLinkText, backward: false, ignoreCase: false);
			var secondClone = secondRange?.Clone();
			var children = documentRange.GetChildren();
			var firstLink = children.Single(child => child.AutomationPeer?.GetName() == firstLinkText);
			var secondLink = children.Single(child => child.AutomationPeer?.GetName() == secondLinkText);
			var firstChildRange = textProvider.RangeFromChild(firstLink);
			var secondChildRange = textProvider.RangeFromChild(secondLink);
			Assert.IsNotNull(secondRange);
			Assert.IsNotNull(secondClone);
			Assert.AreEqual(secondLinkText, secondChildRange.GetText(-1));

			sut.Document.GetRange(0, 0).Text = "head ";
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(secondLinkText, secondRange.GetText(-1));
			Assert.AreEqual(secondLinkText, secondClone.GetText(-1));
			Assert.IsTrue(secondRange.Compare(secondClone));
			Assert.AreEqual(
				0,
				secondRange.CompareEndpoints(
					TextPatternRangeEndpoint.Start,
					secondClone,
					TextPatternRangeEndpoint.Start));
			Assert.AreEqual(true, secondRange.GetAttributeValue((int)AutomationTextAttributesEnum.LinkAttribute));
			Assert.AreEqual(secondLinkText, secondChildRange.GetText(-1));
			Assert.AreEqual(
				secondLinkText,
				documentRange.FindAttribute(
					(int)AutomationTextAttributesEnum.LinkAttribute,
					true,
					backward: true)?.GetText(-1));
			secondRange.GetBoundingRectangles(out var rectangles);
			Assert.IsGreaterThanOrEqualTo(4, rectangles.Length);

			sut.Document.GetText(TextGetOptions.None, out var currentText);
			var currentSecondStart = currentText.IndexOf(secondLinkText, StringComparison.Ordinal);
			sut.Document.GetRange(currentSecondStart - 1, currentSecondStart - 1).Text = "inserted ";
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(secondLinkText, secondRange.GetText(-1));
			Assert.AreEqual(secondLinkText, secondChildRange.GetText(-1));
			Assert.AreEqual(secondLinkText, documentRange.FindText(secondLinkText, false, false)?.GetText(-1));

			sut.Document.GetText(TextGetOptions.None, out currentText);
			var currentFirstStart = currentText.IndexOf(firstLinkText, StringComparison.Ordinal);
			sut.Document.GetRange(currentFirstStart, currentFirstStart + firstLinkText.Length + 1).Text = string.Empty;
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(string.Empty, firstChildRange.GetText(-1));
			Assert.AreEqual(secondLinkText, secondRange.GetText(-1));
			Assert.AreEqual(secondLinkText, secondChildRange.GetText(-1));
			Assert.HasCount(2, documentRange.GetChildren());
			Assert.IsNull(textProvider.RangeFromChild(firstLink));

			sut.Document.Undo();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(secondLinkText, secondRange.GetText(-1));
			Assert.AreEqual(secondLinkText, secondClone.GetText(-1));
			Assert.HasCount(3, documentRange.GetChildren());

			var moved = secondRange.Clone();
			Assert.AreEqual(1, moved.MoveEndpointByUnit(TextPatternRangeEndpoint.Start, TextUnit.Character, 1));
			Assert.AreEqual("econd-link", moved.GetText(-1));
			moved.MoveEndpointByRange(
				TextPatternRangeEndpoint.Start,
				secondRange,
				TextPatternRangeEndpoint.Start);
			Assert.IsTrue(moved.Compare(secondRange));
			Assert.AreEqual(1, moved.Move(TextUnit.Character, 1));
			Assert.IsFalse(moved.Compare(secondRange));

			var expanded = secondRange.Clone();
			expanded.ExpandToEnclosingUnit(TextUnit.Format);
			Assert.AreEqual(secondLinkText, expanded.GetText(-1));
			secondRange.Select();
			Assert.AreEqual(secondLinkText, sut.Document.Selection.Text);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Automation_Text_Object_Scrolls_Into_View()
	{
		var sut = new RichEditBox
		{
			Width = 180,
			Height = 60,
			TextWrapping = TextWrapping.NoWrap,
		};
		try
		{
			WindowHelper.WindowContent = sut;
			await WindowHelper.WaitForLoaded(sut);
			var prefix = string.Join('\r', Enumerable.Range(0, 20).Select(index => $"Line {index:D2}")) + "\r";
			sut.Document.SetText(TextSetOptions.None, prefix + "link");
			sut.Document.GetRange(prefix.Length, prefix.Length + 4).Link = "\"javascript:alert(1)\"";
			await WindowHelper.WaitForIdle();

			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(sut);
			var textProvider = peer?.GetPattern(PatternInterface.Text) as ITextProvider;
			var linkProvider = textProvider?.DocumentRange.GetChildren().Single();
			var linkPeer = linkProvider?.AutomationPeer;
			Assert.IsNotNull(textProvider);
			Assert.IsNotNull(linkProvider);
			Assert.IsNotNull(linkPeer);
			Assert.IsTrue(linkPeer.IsOffscreen());

			var linkRange = textProvider.RangeFromChild(linkProvider);
			linkRange.ScrollIntoView(alignToTop: false);
			await WindowHelper.WaitForIdle();

			Assert.IsFalse(linkPeer.IsOffscreen());
			var bounds = linkPeer.GetBoundingRectangle();
			Assert.IsGreaterThan(0, bounds.Width);
			Assert.IsGreaterThan(0, bounds.Height);

			linkPeer.SetFocus();
			Assert.AreEqual(prefix.Length, sut.Document.Selection.StartPosition);
			Assert.AreEqual(prefix.Length + 4, sut.Document.Selection.EndPosition);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Automation_Spelling_Annotations_Are_Exposed()
	{
		UnicodeText.SpellCheckingServiceOverrideForTesting = new DeterministicSpellCheckingService();
		var sut = new RichEditBox { Width = 320, Height = 120 };
		try
		{
			WindowHelper.WindowContent = sut;
			await WindowHelper.WaitForLoaded(sut);
			sut.Document.SetText(TextSetOptions.None, "correct typo");
			await WindowHelper.WaitForIdle();

			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(sut);
			var textProvider = peer?.GetPattern(PatternInterface.Text) as ITextProvider;
			var textProvider2 = textProvider as ITextProvider2;
			Assert.IsNotNull(textProvider);
			Assert.IsNotNull(textProvider2);

			var annotationTypes = textProvider.DocumentRange.GetAttributeValue(
				(int)AutomationTextAttributesEnum.AnnotationTypesAttribute) as int[];
			var annotationObjects = textProvider.DocumentRange.GetAttributeValue(
				(int)AutomationTextAttributesEnum.AnnotationObjectsAttribute) as IRawElementProviderSimple[];
			Assert.IsNotNull(annotationTypes);
			Assert.IsNotNull(annotationObjects);
			CollectionAssert.AreEqual(new[] { (int)AnnotationType.SpellingError }, annotationTypes);
			Assert.HasCount(1, annotationObjects);

			var annotationPeer = annotationObjects[0].AutomationPeer;
			var annotationProvider = annotationPeer?.GetPattern(PatternInterface.Annotation) as IAnnotationProvider;
			Assert.IsNotNull(annotationPeer);
			Assert.IsNotNull(annotationProvider);
			Assert.AreEqual("typo", annotationPeer.GetName());
			Assert.AreEqual("type, typo-fix", annotationPeer.GetHelpText());
			Assert.AreEqual((int)AnnotationType.SpellingError, annotationProvider.AnnotationTypeId);
			Assert.AreEqual("Spelling error", annotationProvider.AnnotationTypeName);
			Assert.AreSame(peer, annotationProvider.Target.AutomationPeer);
			Assert.AreEqual("typo", textProvider2.RangeFromAnnotation(annotationObjects[0]).GetText(-1));

			var correctRange = textProvider.DocumentRange.FindText("correct", backward: false, ignoreCase: false);
			Assert.IsNotNull(correctRange);
			Assert.HasCount(
				0,
				(int[])correctRange.GetAttributeValue((int)AutomationTextAttributesEnum.AnnotationTypesAttribute));
			Assert.HasCount(
				0,
				(IRawElementProviderSimple[])correctRange.GetAttributeValue((int)AutomationTextAttributesEnum.AnnotationObjectsAttribute));
		}
		finally
		{
			UnicodeText.SpellCheckingServiceOverrideForTesting = null;
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(
		ConditionMode.Include,
		RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaMacOS | RuntimeTestPlatforms.SkiaAndroid)]
	public void When_Native_Accessibility_Text_Actions_Preserve_Direction_And_Validate_Ranges()
	{
		var sut = new RichEditBox();
		sut.Document.SetText(TextSetOptions.None, "abcdef");
		sut.Document.Selection.SetRange(5, 1);
		var peer = FrameworkElementAutomationPeer.CreatePeerForElement(sut);

		Assert.IsNotNull(peer);
		Assert.IsNull(peer.GetPattern(PatternInterface.Value));
		sut.GetAccessibilitySelection(out var start, out var end, out var isBackward);

		Assert.AreEqual(1, start);
		Assert.AreEqual(5, end);
		Assert.IsTrue(isBackward);
		Assert.IsTrue(sut.ApplyAccessibilitySelection(2, 4, isBackward: true));
		Assert.AreEqual(2, sut.Document.Selection.StartPosition);
		Assert.AreEqual(4, sut.Document.Selection.EndPosition);
		Assert.IsTrue(sut.Document.Selection.Options.HasFlag(SelectionOptions.StartActive));

		Assert.IsTrue(sut.ApplyAccessibilityTextInput("xyz", 0, 2, isBackward: true));
		Assert.AreEqual("xyz", sut.GetAccessibilityText());
		Assert.AreEqual(0, sut.Document.Selection.StartPosition);
		Assert.AreEqual(2, sut.Document.Selection.EndPosition);
		Assert.IsTrue(sut.Document.Selection.Options.HasFlag(SelectionOptions.StartActive));

		sut.IsReadOnly = true;
		Assert.IsTrue(sut.ApplyAccessibilitySelection(0, 1));
		Assert.IsFalse(sut.ApplyAccessibilityTextInput("blocked", 0, 0));
		Assert.AreEqual("xyz", sut.GetAccessibilityText());
		Assert.AreEqual(0, sut.Document.Selection.StartPosition);
		Assert.AreEqual(1, sut.Document.Selection.EndPosition);

		Assert.IsFalse(sut.ApplyAccessibilitySelection(-1, 1));
		Assert.IsFalse(sut.ApplyAccessibilitySelection(2, 1));
		Assert.IsFalse(sut.ApplyAccessibilitySelection(0, 4));
		Assert.IsFalse(sut.ApplyAccessibilityTextInput("abc", -1, 1));
		Assert.IsFalse(sut.ApplyAccessibilityTextInput("abc", 2, 1));
		Assert.IsFalse(sut.ApplyAccessibilityTextInput("abc", 0, 4));
		Assert.AreEqual(0, sut.Document.Selection.StartPosition);
		Assert.AreEqual(1, sut.Document.Selection.EndPosition);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(
		ConditionMode.Include,
		RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaMacOS | RuntimeTestPlatforms.SkiaAndroid)]
	public async Task When_Native_Accessibility_Text_Actions_Respect_Protection_And_Raise_Events()
	{
		var sut = new RichEditBox { Width = 320, Height = 120 };
		try
		{
			WindowHelper.WindowContent = sut;
			await WindowHelper.WaitForLoaded(sut);
			sut.Document.SetText(TextSetOptions.None, "abcdef");
			sut.Document.GetRange(2, 3).CharacterFormat.ProtectedText = FormatEffect.On;
			await WindowHelper.WaitForIdle();

			var textChanging = 0;
			var textChanged = 0;
			var selectionChanged = 0;
			sut.TextChanging += (_, _) => textChanging++;
			sut.TextChanged += (_, _) => textChanged++;
			sut.SelectionChanged += (_, _) => selectionChanged++;

			Assert.IsFalse(sut.ApplyAccessibilityTextInput("abXdef", 3, 3));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("abcdef", sut.GetAccessibilityText());
			Assert.AreEqual(0, textChanging);
			Assert.AreEqual(0, textChanged);

			Assert.IsTrue(sut.ApplyAccessibilitySelection(2, 3));
			Assert.AreEqual(1, selectionChanged);

			sut.Document.GetRange(2, 3).CharacterFormat.ProtectedText = FormatEffect.Off;
			await WindowHelper.WaitForIdle();
			textChanging = 0;
			textChanged = 0;
			Assert.IsTrue(sut.ApplyAccessibilityTextInput("abXdef", 3, 3));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("abXdef", sut.GetAccessibilityText());
			Assert.AreEqual(1, textChanging);
			Assert.AreEqual(1, textChanged);
			Assert.IsGreaterThanOrEqualTo(2, selectionChanged);

			sut.IsEnabled = false;
			Assert.IsFalse(sut.ApplyAccessibilitySelection(0, 1));
			Assert.IsFalse(sut.ApplyAccessibilityTextInput("blocked", 0, 0));
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public void When_Automation_Range_Queries_Additional_Formatting_Attributes()
	{
		var sut = new RichEditBox
		{
			Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White),
		};
		sut.Document.SetText(TextSetOptions.None, "one\rtwo");
		sut.Document.GetRange(0, 3).CharacterFormat.BackgroundColor = Microsoft.UI.Colors.Red;
		var paragraph = sut.Document.GetRange(0, 3).ParagraphFormat;
		paragraph.SetIndents(3, 9, 12);
		paragraph.ListType = MarkerType.Bullet;
		paragraph.ListStyle = MarkerStyle.Minus;
		paragraph.AddTab(24, TabAlignment.Left, TabLeader.Spaces);
		paragraph.AddTab(48, TabAlignment.Right, TabLeader.Dashes);
		paragraph.RightToLeft = FormatEffect.On;

		var peer = FrameworkElementAutomationPeer.CreatePeerForElement(sut);
		var textProvider = peer?.GetPattern(PatternInterface.Text) as ITextProvider;
		Assert.IsNotNull(textProvider);
		var first = textProvider.DocumentRange.FindText("one", backward: false, ignoreCase: false);
		Assert.IsNotNull(first);

		Assert.AreEqual(255, first.GetAttributeValue((int)AutomationTextAttributesEnum.BackgroundColorAttribute));
		Assert.AreEqual(
			AutomationBulletStyle.DashBullet,
			first.GetAttributeValue((int)AutomationTextAttributesEnum.BulletStyleAttribute));
		Assert.AreEqual(9f, first.GetAttributeValue((int)AutomationTextAttributesEnum.IndentationLeadingAttribute));
		Assert.AreEqual(12f, first.GetAttributeValue((int)AutomationTextAttributesEnum.IndentationTrailingAttribute));
		CollectionAssert.AreEqual(
			new[] { 24d, 48d },
			(double[])first.GetAttributeValue((int)AutomationTextAttributesEnum.TabsAttribute));
		Assert.AreEqual(
			AutomationFlowDirections.RightToLeft,
			first.GetAttributeValue((int)AutomationTextAttributesEnum.TextFlowDirectionsAttribute));
		Assert.AreEqual(
			TextAttributeValueSentinel.Mixed,
			textProvider.DocumentRange.GetAttributeValue((int)AutomationTextAttributesEnum.BackgroundColorAttribute));
		Assert.AreEqual(
			TextAttributeValueSentinel.Mixed,
			textProvider.DocumentRange.GetAttributeValue((int)AutomationTextAttributesEnum.BulletStyleAttribute));

		sut.Document.Selection.SetRange(3, 0);
		var selection = textProvider.GetSelection()[0];
		Assert.AreEqual(
			AutomationActiveEnd.Start,
			selection.GetAttributeValue((int)AutomationTextAttributesEnum.SelectionActiveEndAttribute));
		sut.Document.Selection.SetRange(0, 3);
		selection = textProvider.GetSelection()[0];
		Assert.AreEqual(
			AutomationActiveEnd.End,
			selection.GetAttributeValue((int)AutomationTextAttributesEnum.SelectionActiveEndAttribute));
		Assert.AreEqual(
			AutomationActiveEnd.None,
			textProvider.DocumentRange.GetAttributeValue((int)AutomationTextAttributesEnum.SelectionActiveEndAttribute));
	}

	[TestMethod]
	public async Task When_Structure_Changes_Before_Child_Enumeration_And_Existing_Child_Name_Changes()
	{
		var sut = new RichEditBox { Width = 320, Height = 120 };
		var listener = new TextEditAutomationListener();
		try
		{
			WindowHelper.WindowContent = sut;
			await WindowHelper.WaitForLoaded(sut);
			sut.Document.SetText(TextSetOptions.None, "one");
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(sut);
			Assert.IsNotNull(peer);
			AutomationPeer.TestAutomationPeerListener = listener;

			sut.Document.GetRange(0, 3).Link = "\"https://example.com\"";
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(listener.Events.Contains(AutomationEvents.StructureChanged));
			var textProvider = peer.GetPattern(PatternInterface.Text) as ITextProvider;
			Assert.IsNotNull(textProvider);
			var child = textProvider.DocumentRange.GetChildren().Single();
			Assert.IsNotNull(child);
			var originalPeer = child.AutomationPeer;
			var originalBounds = originalPeer?.GetBoundingRectangle();

			sut.Document.GetRange(0, 3).Text = "two";
			await WindowHelper.WaitForIdle();

			var current = textProvider.DocumentRange.GetChildren().Single();
			Assert.AreSame(originalPeer, current.AutomationPeer);
			Assert.AreEqual("two", current.AutomationPeer?.GetName());
			Assert.IsTrue(listener.PropertyChanges.Any(change =>
				ReferenceEquals(change.Peer, originalPeer)
				&& change.Property == AutomationElementIdentifiers.NameProperty
				&& Equals(change.OldValue, "one")
				&& Equals(change.NewValue, "two")));

			sut.Document.GetRange(0, 0).SetText(
				TextSetOptions.FormatRtf,
				@"{\rtf1 a much longer prefix }");
			await WindowHelper.WaitForIdle();

			current = textProvider.DocumentRange.GetChildren().Single();
			Assert.AreSame(originalPeer, current.AutomationPeer);
			Assert.AreNotEqual(originalBounds, current.AutomationPeer?.GetBoundingRectangle());
			Assert.IsTrue(listener.PropertyChanges.Any(change =>
				ReferenceEquals(change.Peer, originalPeer)
				&& change.Property == AutomationElementIdentifiers.BoundingRectangleProperty));
		}
		finally
		{
			AutomationPeer.TestAutomationPeerListener = null;
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Duplicate_Image_Identity_Is_Repaired_The_Survivor_Keeps_Its_Peer()
	{
		var sut = new RichEditBox { Width = 320, Height = 120 };
		try
		{
			WindowHelper.WindowContent = sut;
			await WindowHelper.WaitForLoaded(sut);
			using var stream = CreateImageStream(SKColors.Red);
			var image = InlineImageState.CreateFromStream(
				stream,
				width: 20,
				height: 14,
				ascent: 10,
				VerticalCharacterAlignment.Baseline,
				"duplicate image");
			var fragment = sut.Document.CreateInlineImageFragment(0, image);
			sut.Document.ReplaceRangeWithFragment(0, 0, fragment, sourceRange: null);
			sut.Document.ReplaceRangeWithFragment(1, 1, fragment, sourceRange: null);
			await WindowHelper.WaitForIdle();

			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(sut);
			var textProvider = peer?.GetPattern(PatternInterface.Text) as ITextProvider;
			Assert.IsNotNull(textProvider);
			var initial = textProvider.DocumentRange.GetChildren();
			Assert.HasCount(2, initial);
			var removed = initial[0];
			var survivor = initial[1];
			Assert.AreNotSame(removed.AutomationPeer, survivor.AutomationPeer);

			sut.Document.GetRange(0, 1).Text = string.Empty;
			await WindowHelper.WaitForIdle();

			var current = textProvider.DocumentRange.GetChildren().Single();
			Assert.AreSame(survivor.AutomationPeer, current.AutomationPeer);
			Assert.AreNotSame(removed.AutomationPeer, current.AutomationPeer);
			Assert.IsNull(textProvider.RangeFromChild(removed));
			Assert.AreEqual("duplicate image", textProvider.RangeFromChild(survivor).GetText(-1));
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	private sealed class TextEditAutomationListener : IAutomationPeerListener, ITextEditAutomationPeerListener
	{
		internal List<AutomationEvents> Events { get; } = new();
		internal List<(AutomationTextEditChangeType ChangeType, string[] ChangedData)> TextEditChanges { get; } = new();
		internal List<(AutomationPeer Peer, AutomationProperty Property, object OldValue, object NewValue)> PropertyChanges { get; } = new();

		public bool ListenerExistsHelper(AutomationEvents eventId) => true;

		public void OnAutomationEvent(AutomationPeer peer, AutomationEvents eventId)
			=> NotifyAutomationEvent(peer, eventId);

		public void NotifyAutomationEvent(AutomationPeer peer, AutomationEvents eventId)
			=> Events.Add(eventId);

		public void NotifyTextEditTextChangedEvent(
			AutomationPeer peer,
			AutomationTextEditChangeType changeType,
			IReadOnlyList<string> changedData)
			=> TextEditChanges.Add((changeType, changedData.ToArray()));

		public void NotifyInvalidatePeer(AutomationPeer peer)
		{
		}

		public void NotifyPropertyChangedEvent(
			AutomationPeer peer,
			AutomationProperty automationProperty,
			object oldValue,
			object newValue)
			=> PropertyChanges.Add((peer, automationProperty, oldValue, newValue));

		public void NotifyNotificationEvent(
			AutomationPeer peer,
			AutomationNotificationKind notificationKind,
			AutomationNotificationProcessing notificationProcessing,
			string displayString,
			string activityId)
		{
		}
	}

	private sealed class DeterministicSpellCheckingService : ISpellCheckingService
	{
		public List<(int correctionStart, int correctionEnd)?> SpellCheck(
			List<int> wordBoundaries,
			string text)
		{
			var corrections = new List<(int correctionStart, int correctionEnd)?>(wordBoundaries.Count);
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

		public (int replaceIndexStart, int replaceIndexEnd, List<string> suggestions)? GetSpellCheckSuggestions(
			string text,
			List<int> wordBoundaries,
			int correctionStart,
			int correctionEnd)
			=> text.Substring(correctionStart, correctionEnd - correctionStart) == "typo"
				? (correctionStart, correctionEnd, new List<string> { "type", "typo-fix" })
				: null;
	}
}
