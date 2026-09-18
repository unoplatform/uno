#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DirectUI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Xaml.Controls.Extensions;
using Windows.Foundation;
using FontStretch = Windows.UI.Text.FontStretch;
using FontStyle = Windows.UI.Text.FontStyle;
using FontWeights = Microsoft.UI.Text.FontWeights;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public partial class Given_RichEditBox
{
	[TestMethod]
	public void When_UIA_Attributes_ResolveInheritedFormatting()
	{
		var editor = new RichEditBox
		{
			FontWeight = FontWeights.SemiBold,
			FontStyle = FontStyle.Italic,
			Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red),
		};
		var peer = new RichEditBoxAutomationPeer(editor);
		editor.Document.SetText(TextSetOptions.None, "ab");
		var range = new TextRangeAdapter(peer, editor, 0, 2);
		Assert.AreEqual(600, range.GetAttributeValue((int)AutomationTextAttributesEnum.FontWeightAttribute));
		Assert.AreEqual(0x0000ff, range.GetAttributeValue((int)AutomationTextAttributesEnum.ForegroundColorAttribute));
		Assert.AreEqual(true, range.GetAttributeValue((int)AutomationTextAttributesEnum.IsItalicAttribute));
		Assert.AreEqual(TextConstants.AutoColor, editor.Document.GetRange(0, 2).CharacterFormat.ForegroundColor);

		var first = editor.Document.GetRange(0, 1).CharacterFormat;
		first.Weight = 600;
		first.Italic = FormatEffect.On;
		first.ForegroundColor = Microsoft.UI.Colors.Red;
		Assert.AreEqual(600, range.GetAttributeValue((int)AutomationTextAttributesEnum.FontWeightAttribute));
		Assert.AreEqual(0x0000ff, range.GetAttributeValue((int)AutomationTextAttributesEnum.ForegroundColorAttribute));
		Assert.AreEqual("ab", range.FindAttribute((int)AutomationTextAttributesEnum.FontWeightAttribute, 600, false)?.GetText(-1));
		Assert.AreEqual("ab", range.FindAttribute((int)AutomationTextAttributesEnum.ForegroundColorAttribute, 0x0000ff, false)?.GetText(-1));

		editor.FontWeight = FontWeights.Bold;
		editor.FontStyle = FontStyle.Normal;
		editor.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Blue);
		Assert.AreEqual(TextAttributeValueSentinel.Mixed, range.GetAttributeValue((int)AutomationTextAttributesEnum.FontWeightAttribute));
		Assert.AreEqual(TextAttributeValueSentinel.Mixed, range.GetAttributeValue((int)AutomationTextAttributesEnum.ForegroundColorAttribute));
		Assert.AreEqual(TextAttributeValueSentinel.Mixed, range.GetAttributeValue((int)AutomationTextAttributesEnum.IsItalicAttribute));
		Assert.AreEqual("b", range.FindAttribute((int)AutomationTextAttributesEnum.FontWeightAttribute, 700, false)?.GetText(-1));
		Assert.AreEqual("b", range.FindAttribute((int)AutomationTextAttributesEnum.ForegroundColorAttribute, 0xff0000, false)?.GetText(-1));

		var caret = new TextRangeAdapter(peer, editor, 2, 2);
		Assert.AreEqual(700, caret.GetAttributeValue((int)AutomationTextAttributesEnum.FontWeightAttribute));
		Assert.AreEqual(0xff0000, caret.GetAttributeValue((int)AutomationTextAttributesEnum.ForegroundColorAttribute));
		editor.Document.Selection.SetRange(2, 2);
		editor.Document.Selection.CharacterFormat.Weight = 0;
		Assert.AreEqual(0, caret.GetAttributeValue((int)AutomationTextAttributesEnum.FontWeightAttribute));

		editor.Document.SetText(TextSetOptions.None, string.Empty);
		var empty = new TextRangeAdapter(peer, editor, 0, 0);
		Assert.AreEqual(700, empty.GetAttributeValue((int)AutomationTextAttributesEnum.FontWeightAttribute));
		Assert.AreEqual(0xff0000, empty.GetAttributeValue((int)AutomationTextAttributesEnum.ForegroundColorAttribute));
	}

	[TestMethod]
	public async Task When_ExplicitFontStretchReset_SurvivesCloneAndUndo()
	{
		// The native combined italic/stretch clone probe returned FontStretch.Undefined on the target.
		// Keep the stronger stretch-persistence guarantee as a managed-engine regression.
		var editor = new RichEditBox { FontStretch = FontStretch.Expanded };
		try
		{
			WindowHelper.WindowContent = editor;
			await WindowHelper.WaitForLoaded(editor);
			editor.Document.SetText(TextSetOptions.None, "ab");
			var range = editor.Document.GetRange(0, 1);
			Assert.AreEqual(FontStretch.Expanded, range.CharacterFormat.FontStretch);
			editor.Document.ClearUndoRedoHistory();
			range.CharacterFormat.FontStretch = FontStretch.Normal;

			var reset = range.CharacterFormat.GetClone();
			Assert.AreEqual(FontStretch.Normal, reset.FontStretch);
			editor.Document.Undo();
			Assert.AreEqual(FontStretch.Expanded, range.CharacterFormat.FontStretch);
			editor.Document.Redo();
			Assert.AreEqual(FontStretch.Normal, range.CharacterFormat.FontStretch);

			var other = editor.Document.GetRange(1, 2);
			Assert.AreEqual(FontStretch.Expanded, other.CharacterFormat.FontStretch);
			other.CharacterFormat.SetClone(reset);
			Assert.AreEqual(FontStretch.Normal, other.CharacterFormat.FontStretch);
			editor.Document.Undo();
			Assert.AreEqual(FontStretch.Expanded, other.CharacterFormat.FontStretch);
			editor.Document.Redo();
			Assert.AreEqual(FontStretch.Normal, other.CharacterFormat.FontStretch);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public void When_ExplicitFontResets_KeepRunBoundariesAndHistory()
	{
		var editor = new RichEditBox { FontStyle = FontStyle.Italic, FontStretch = FontStretch.Expanded };
		var document = editor.Document;
		document.SetText(TextSetOptions.None, "ab");
		var first = document.GetRange(0, 1).CharacterFormat;
		first.Italic = FormatEffect.Off;
		first.FontStretch = FontStretch.Normal;
		Assert.AreEqual(2, document.CharacterRunCount);
		Assert.IsTrue(document.HasVisualCharacterFormatting);
		Assert.IsTrue(document.IsVisualCharacterFormattingProfileValid());
		var explicitState = document.FormatRuns[0].Format;
		var inheritedState = document.FormatRuns[1].Format;
		Assert.IsTrue(explicitState.ItalicExplicit);
		Assert.IsTrue(explicitState.FontStretchExplicit);
		Assert.IsFalse(CharacterFormatState.CanCoalesce(explicitState, inheritedState));
		Assert.IsTrue(explicitState.Equals(explicitState.Clone()));
		Assert.AreEqual(explicitState.GetHashCode(), explicitState.Clone().GetHashCode());
		Assert.AreEqual(FormatEffect.On, document.GetRange(1, 2).CharacterFormat.Italic);
		Assert.AreEqual(FontStretch.Expanded, document.GetRange(1, 2).CharacterFormat.FontStretch);

		document.ClearUndoRedoHistory();
		var second = document.GetRange(1, 2).CharacterFormat;
		second.Italic = FormatEffect.Off;
		second.FontStretch = FontStretch.Normal;
		Assert.AreEqual(1, document.CharacterRunCount);
		document.Undo();
		document.Undo();
		Assert.AreEqual(2, document.CharacterRunCount);
		Assert.AreEqual(FormatEffect.On, document.GetRange(1, 2).CharacterFormat.Italic);
		Assert.AreEqual(FontStretch.Expanded, document.GetRange(1, 2).CharacterFormat.FontStretch);
		document.Redo();
		document.Redo();
		Assert.AreEqual(1, document.CharacterRunCount);

		var target = new RichEditBox { FontStyle = FontStyle.Italic, FontStretch = FontStretch.Expanded };
		target.Document.GetRange(0, 0).FormattedText = document.GetRange(0, 2);
		Assert.AreEqual(FormatEffect.Off, target.Document.GetRange(0, 2).CharacterFormat.Italic);
		Assert.AreEqual(FontStretch.Normal, target.Document.GetRange(0, 2).CharacterFormat.FontStretch);
		target.Document.Selection.SetRange(2, 2);
		target.Document.Selection.TypeText("c");
		Assert.AreEqual(1, target.Document.CharacterRunCount);
		Assert.AreEqual(FormatEffect.Off, target.Document.GetRange(2, 3).CharacterFormat.Italic);
		Assert.AreEqual(FontStretch.Normal, target.Document.GetRange(2, 3).CharacterFormat.FontStretch);
	}

	[TestMethod]
	public void When_PendingFontResets_OverrideInheritedFormatting()
	{
		var editor = new RichEditBox { FontStyle = FontStyle.Italic, FontStretch = FontStretch.Expanded };
		editor.Document.SetText(TextSetOptions.None, "a");
		editor.Document.Selection.SetRange(1, 1);
		editor.Document.Selection.CharacterFormat.Italic = FormatEffect.Off;
		editor.Document.Selection.CharacterFormat.FontStretch = FontStretch.Normal;
		var pending = editor.Document.Selection.CharacterFormat.GetClone();
		Assert.AreEqual(FormatEffect.Off, pending.Italic);
		Assert.AreEqual(FontStretch.Normal, pending.FontStretch);
		editor.Document.Selection.TypeText("x");
		Assert.AreEqual(FormatEffect.Off, editor.Document.GetRange(1, 2).CharacterFormat.Italic);
		Assert.AreEqual(FontStretch.Normal, editor.Document.GetRange(1, 2).CharacterFormat.FontStretch);
		editor.Document.Undo();
		editor.Document.Redo();
		Assert.AreEqual(FormatEffect.Off, editor.Document.GetRange(1, 2).CharacterFormat.Italic);
		Assert.AreEqual(FontStretch.Normal, editor.Document.GetRange(1, 2).CharacterFormat.FontStretch);
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_ExplicitFontResets_RenderInBothEditorPaths(bool bounded)
	{
		var editor = new RichEditBox
		{
			Width = 320,
			Height = 100,
			FontStyle = FontStyle.Italic,
			FontStretch = FontStretch.Expanded,
		};
		try
		{
			editor.Document.SetText(TextSetOptions.FormatRtf, BuildAlternatingRunRtf(bounded ? 8200 : 2, 1));
			WindowHelper.WindowContent = editor;
			await WindowHelper.WaitForLoaded(editor);
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(bounded, editor.UsesBoundedRichLayout);
			var before = GetProjectedFormattingRun(editor, bounded);
			Assert.AreEqual(FontStyle.Italic, before.FontStyle);
			Assert.AreEqual(FontStretch.Expanded, before.FontStretch);

			var range = editor.Document.GetRange(0, editor.Document.TextLength);
			range.CharacterFormat.Italic = FormatEffect.Off;
			range.CharacterFormat.FontStretch = FontStretch.Normal;
			await WindowHelper.WaitForIdle();
			var after = GetProjectedFormattingRun(editor, bounded);
			Assert.AreEqual(FontStyle.Normal, after.FontStyle);
			Assert.AreEqual(FontStretch.Normal, after.FontStretch);
			Assert.AreEqual(bounded, editor.UsesBoundedRichLayout);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	private static Run GetProjectedFormattingRun(RichEditBox editor, bool bounded)
	{
		if (!bounded)
		{
			return GetDisplayBlock(editor).Inlines.OfType<Run>().First();
		}

		var source = GetDisplayBlock(editor).CustomTextLayout;
		Assert.IsNotNull(source);
		var run = source.GetType().GetField("_run", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(source) as Run;
		Assert.IsNotNull(run);
		return run;
	}

	[TestMethod]
	public void When_Rtf_ExplicitFontResets_OverrideInheritedFormatting()
	{
		var editor = new RichEditBox { FontStyle = FontStyle.Italic, FontStretch = FontStretch.Expanded };
		editor.Document.SetText(TextSetOptions.FormatRtf, @"{\rtf1 a{\i0 b}{\plain c}}");
		Assert.AreEqual(FormatEffect.On, editor.Document.GetRange(0, 1).CharacterFormat.Italic);
		Assert.AreEqual(FormatEffect.Off, editor.Document.GetRange(1, 2).CharacterFormat.Italic);
		Assert.AreEqual(FormatEffect.Off, editor.Document.GetRange(2, 3).CharacterFormat.Italic);
		Assert.AreEqual(FontStretch.Normal, editor.Document.GetRange(2, 3).CharacterFormat.FontStretch);
	}

	[TestMethod]
	public void When_TextChanged_HandlerThrows_LeavesDispatcherCallbackUncaught()
	{
		var editor = new RichEditBox();
		var failure = new InvalidOperationException("TextChanged handler");
		RoutedEventHandler handler = (_, _) => throw failure;
		editor.TextChanged += handler;
		try
		{
			// Exercise the dispatched callback directly so the test does not inject an unhandled UI-thread failure.
			Assert.AreSame(failure, Assert.ThrowsExactly<InvalidOperationException>(editor.OnTextChangedHandler));
		}
		finally
		{
			editor.TextChanged -= handler;
		}
	}

	[TestMethod]
	public async Task When_SelectionChanged_HandlerThrows_PreservesAcceptedSelection()
	{
		var editor = new RichEditBox();
		var failure = new InvalidOperationException("SelectionChanged handler");
		var notifications = 0;
		RoutedEventHandler handler = (_, _) =>
		{
			if (++notifications == 1)
			{
				throw failure;
			}
		};
		try
		{
			WindowHelper.WindowContent = editor;
			await WindowHelper.WaitForLoaded(editor);
			editor.Document.SetText(TextSetOptions.None, "abc");
			editor.SelectionChanged += handler;
			Assert.AreSame(failure, Assert.ThrowsExactly<InvalidOperationException>(() => editor.Document.Selection.SetRange(1, 2)));
			Assert.AreEqual(1, editor.Document.Selection.StartPosition);
			Assert.AreEqual(2, editor.Document.Selection.EndPosition);
			Assert.AreEqual(1, editor.SelectionStartForTesting);
			Assert.AreEqual(1, editor.SelectionLengthForTesting);

			editor.Document.Selection.SetRange(3, 3);
			Assert.AreEqual(2, notifications);
			Assert.AreEqual(3, editor.SelectionStartForTesting);
			Assert.AreEqual(0, editor.SelectionLengthForTesting);
		}
		finally
		{
			editor.SelectionChanged -= handler;
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_TextChanging_DuringCompositionCommit_PropagatesAndClosesUndoGroup()
	{
		var fake = new FakeImeTextBoxExtension();
		using var ime = RichEditBox.SetImeExtensionForTesting(fake);
		var editor = new RichEditBox();
		var failure = new InvalidOperationException("Composition text handler");
		TypedEventHandler<RichEditBox, RichEditBoxTextChangingEventArgs> handler = (_, _) => throw failure;
		try
		{
			WindowHelper.WindowContent = editor;
			await WindowHelper.WaitForLoaded(editor);
			var host = (IImeSessionHost)editor;
			host.OnImeCompositionStarted();
			host.OnImeCompositionUpdated("pre", 3, 0, false);
			editor.TextChanging += handler;
			Assert.AreSame(failure, Assert.ThrowsExactly<InvalidOperationException>(() => host.OnImeCompositionCompleted("final", false)));
			editor.TextChanging -= handler;
			Assert.IsFalse(editor.IsComposing);
			Assert.AreEqual("final", GetDisplayBlock(editor).Text);
			Assert.AreEqual(5, editor.SelectionStartForTesting);

			editor.Document.Undo();
			GetTextWithoutFinalEop(editor.Document, out var text);
			Assert.AreEqual(string.Empty, text);
			editor.Document.Selection.TypeText("next");
			editor.Document.Undo();
			GetTextWithoutFinalEop(editor.Document, out text);
			Assert.AreEqual(string.Empty, text);
		}
		finally
		{
			editor.TextChanging -= handler;
			((IImeSessionHost)editor).OnImeCompositionEnded();
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[DataRow("started")]
	[DataRow("changed")]
	[DataRow("ended")]
	public async Task When_CompositionHandlerThrows_StateRemainsConsistent(string stage)
	{
		var fake = new FakeImeTextBoxExtension();
		using var ime = RichEditBox.SetImeExtensionForTesting(fake);
		var editor = new RichEditBox();
		var failure = new InvalidOperationException("Composition handler");
		TypedEventHandler<RichEditBox, TextCompositionStartedEventArgs> started = (_, _) => throw failure;
		TypedEventHandler<RichEditBox, TextCompositionChangedEventArgs> changed = (_, _) => throw failure;
		TypedEventHandler<RichEditBox, TextCompositionEndedEventArgs> ended = (_, _) => throw failure;
		try
		{
			WindowHelper.WindowContent = editor;
			await WindowHelper.WaitForLoaded(editor);
			var host = (IImeSessionHost)editor;
			if (stage == "started")
			{
				editor.TextCompositionStarted += started;
				Assert.AreSame(failure, Assert.ThrowsExactly<InvalidOperationException>(host.OnImeCompositionStarted));
				editor.TextCompositionStarted -= started;
			}
			else
			{
				host.OnImeCompositionStarted();
			}
			if (stage == "changed")
			{
				editor.TextCompositionChanged += changed;
				Assert.AreSame(failure, Assert.ThrowsExactly<InvalidOperationException>(() => host.OnImeCompositionUpdated("x", 1, 0, false)));
				editor.TextCompositionChanged -= changed;
			}
			else
			{
				host.OnImeCompositionUpdated("x", 1, 0, false);
			}
			if (stage == "ended")
			{
				editor.TextCompositionEnded += ended;
				Assert.AreSame(failure, Assert.ThrowsExactly<InvalidOperationException>(() => host.OnImeCompositionCompleted("x", false)));
				editor.TextCompositionEnded -= ended;
			}
			else
			{
				host.OnImeCompositionCompleted("x", false);
			}
			Assert.IsFalse(editor.IsComposing);
			Assert.AreEqual("x", GetDisplayBlock(editor).Text);
			editor.Document.Undo();
			GetTextWithoutFinalEop(editor.Document, out var undone);
			Assert.AreEqual(string.Empty, undone);

			var startedCount = 0;
			editor.TextCompositionStarted += (_, _) => startedCount++;
			host.OnImeCompositionStarted();
			Assert.AreEqual(1, startedCount, "The event-depth guard must reset after a handler throws.");
			host.OnImeCompositionCanceled(false);
		}
		finally
		{
			editor.TextCompositionStarted -= started;
			editor.TextCompositionChanged -= changed;
			editor.TextCompositionEnded -= ended;
			((IImeSessionHost)editor).OnImeCompositionEnded();
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Composition_ReentrantDocumentEdit_DefersEnded()
	{
		var fake = new FakeImeTextBoxExtension();
		using var ime = RichEditBox.SetImeExtensionForTesting(fake);
		var editor = new RichEditBox();
		var events = new List<string>();
		try
		{
			WindowHelper.WindowContent = editor;
			await WindowHelper.WaitForLoaded(editor);
			editor.Document.SetText(TextSetOptions.None, "abc");
			editor.Document.Selection.SetRange(1, 2);
			editor.TextCompositionStarted += (_, _) =>
			{
				events.Add("started");
				editor.Document.SetText(TextSetOptions.None, "replacement");
				events.Add("returned");
			};
			editor.TextCompositionEnded += (_, args) => events.Add($"ended:{args.StartIndex}:{args.Length}");
			((IImeSessionHost)editor).OnImeCompositionStarted();
			CollectionAssert.AreEqual(new[] { "started", "returned" }, events);
			await WindowHelper.WaitForIdle();
			CollectionAssert.AreEqual(new[] { "started", "returned", "ended:1:1" }, events);
			Assert.IsFalse(editor.IsComposing);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}
}
