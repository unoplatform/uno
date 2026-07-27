#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#if __SKIA__
using Microsoft.UI.Xaml.Documents;
#endif
using Uno.UI.Samples.Controls;

namespace Uno.UI.Samples.Content.UITests.RichEditBoxControl;

#if __SKIA__ || !HAS_UNO
[Sample(
	"RichEditBox",
	Name = "RichEditBox_UIAutomation",
	Description = "Deterministic fixture for external RichEditBox UI Automation clients.",
	IsManualTest = true,
	IgnoreInSnapshotTests = true)]
#endif
public sealed partial class RichEditBox_UIAutomation : Page
{
	private static readonly byte[] _imageBytes = Convert.FromBase64String(
		"iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAusB9Y9ZQmcAAAAASUVORK5CYII=");

#if __SKIA__
	private bool _useShortComposition;
	private static readonly ISpellCheckingService _spellCheckingService = new DeterministicSpellCheckingService();
#endif

	public RichEditBox_UIAutomation()
	{
		this.InitializeComponent();
		var contextFlyout = new Microsoft.UI.Xaml.Controls.MenuFlyout();
		contextFlyout.Items.Add(new Microsoft.UI.Xaml.Controls.MenuFlyoutItem { Text = "UIA context command" });
		Editor.ContextFlyout = contextFlyout;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
#if __SKIA__
		UnicodeText.SpellCheckingServiceOverrideForTesting = _spellCheckingService;
#endif
		Editor.Document.GetText(TextGetOptions.None, out var existingText);
		if (string.IsNullOrEmpty(existingText) || existingText == "\r")
		{
			var text = string.Join('\r', Enumerable.Range(0, 12).Select(index => $"Line {index:D2}"))
				+ "\rprefix first-link second-link suffix"
				+ "\rformat-one"
				+ "\rformat-two"
				+ "\rtypo ";
			var firstLinkStart = text.IndexOf("first-link", StringComparison.Ordinal);
			var secondLinkStart = text.IndexOf("second-link", StringComparison.Ordinal);
			var formatStart = text.IndexOf("format-one", StringComparison.Ordinal);
			Editor.Document.SetText(TextSetOptions.None, text);
			Editor.Document.GetRange(firstLinkStart, firstLinkStart + "first-link".Length).Link = "\"javascript:alert(1)\"";
			Editor.Document.GetRange(secondLinkStart, secondLinkStart + "second-link".Length).Link = "\"javascript:alert(1)\"";
			Editor.Document.GetRange(formatStart, formatStart + "format-one".Length).CharacterFormat.BackgroundColor = Microsoft.UI.Colors.Red;
			var paragraphFormat = Editor.Document.GetRange(formatStart, formatStart + "format-one".Length).ParagraphFormat;
			paragraphFormat.SetIndents(3, 9, 12);
			paragraphFormat.ListType = MarkerType.Bullet;
			paragraphFormat.ListStyle = MarkerStyle.Minus;
			paragraphFormat.AddTab(24, TabAlignment.Left, TabLeader.Spaces);
			paragraphFormat.AddTab(48, TabAlignment.Right, TabLeader.Dashes);
			paragraphFormat.RightToLeft = FormatEffect.On;
			using var stream = new MemoryStream(_imageBytes).AsRandomAccessStream();
			Editor.Document.GetRange(text.Length, text.Length)
				.InsertImage(20, 14, 10, VerticalCharacterAlignment.Baseline, "fixture image", stream);
		}
		StructureEditor.Document.SetText(TextSetOptions.None, "pending");

#if __SKIA__
		Editor.Document.Selection.SetRange(0, 0);
		Editor.Focus(FocusState.Programmatic);
		var imeHost = (IImeSessionHost)Editor;
		imeHost.OnImeCompositionStarted();
		imeHost.OnImeCompositionUpdated("nihao", cursorPosition: 2, resolvedLength: 2, textAlreadyApplied: false);
		Status.Text = "Composition active: nihao; conversion target: hao.";
#else
		Status.Text = "Native fixture loaded; use a real IME to exercise composition.";
#endif
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
#if __SKIA__
		UnicodeText.SpellCheckingServiceOverrideForTesting = null;
		if (Editor.IsComposing)
		{
			((IImeSessionHost)Editor).OnImeCompositionEnded();
		}
#endif
	}

	private void OnUpdateComposition(object sender, RoutedEventArgs e)
	{
#if __SKIA__
		var imeHost = (IImeSessionHost)Editor;
		if (!Editor.IsComposing)
		{
			imeHost.OnImeCompositionStarted();
		}

		_useShortComposition = !_useShortComposition;
		if (_useShortComposition)
		{
			imeHost.OnImeCompositionUpdated("ni", cursorPosition: 1, resolvedLength: 1, textAlreadyApplied: false);
			Status.Text = "Composition updated: ni; conversion target: i.";
		}
		else
		{
			imeHost.OnImeCompositionUpdated("nihaoma", cursorPosition: 2, resolvedLength: 2, textAlreadyApplied: false);
			Status.Text = "Composition updated: nihaoma; conversion target: haoma.";
		}
#endif
	}

	private void OnCompleteComposition(object sender, RoutedEventArgs e)
	{
#if __SKIA__
		var imeHost = (IImeSessionHost)Editor;
		if (!Editor.IsComposing)
		{
			imeHost.OnImeCompositionStarted();
			imeHost.OnImeCompositionUpdated(
				_useShortComposition ? "ni" : "nihaoma",
				cursorPosition: _useShortComposition ? 1 : 2,
				resolvedLength: _useShortComposition ? 1 : 2,
				textAlreadyApplied: false);
		}

		imeHost.OnImeCompositionCompleted("你好", textAlreadyApplied: false);
		Status.Text = "Composition finalized: 你好.";
#endif
	}

	private void OnInsertPrefix(object sender, RoutedEventArgs e)
	{
		Editor.Document.GetRange(0, 0).SetText(TextSetOptions.None, "X");
		Status.Text = "Inserted prefix.";
	}

	private void OnInsertMiddle(object sender, RoutedEventArgs e)
	{
		Editor.Document.GetText(TextGetOptions.None, out var text);
		var start = text.IndexOf("second-link", StringComparison.Ordinal);
		if (start >= 0)
		{
			Editor.Document.GetRange(start - 1, start - 1).SetText(TextSetOptions.None, "inserted ");
			Status.Text = "Inserted middle text.";
		}
	}

	private void OnDeleteMiddle(object sender, RoutedEventArgs e)
	{
		Editor.Document.GetText(TextGetOptions.None, out var text);
		var start = text.IndexOf("inserted ", StringComparison.Ordinal);
		if (start >= 0)
		{
			Editor.Document.GetRange(start, start + "inserted ".Length).Text = string.Empty;
			Status.Text = "Deleted middle text.";
		}
	}

	private void OnUndo(object sender, RoutedEventArgs e)
	{
		if (Editor.Document.CanUndo())
		{
			Editor.Document.Undo();
			Status.Text = "Undid document edit.";
		}
	}

	private void OnRemoveFirstDuplicateLink(object sender, RoutedEventArgs e)
	{
		Editor.Document.GetText(TextGetOptions.None, out var text);
		var start = text.IndexOf("first-link ", StringComparison.Ordinal);
		if (start >= 0)
		{
			Editor.Document.GetRange(start, start + "first-link ".Length).Text = string.Empty;
			Status.Text = "Removed first duplicate-target link.";
		}
	}

	private void OnAddStructureLink(object sender, RoutedEventArgs e)
	{
		StructureEditor.Document.GetText(TextGetOptions.None, out var text);
		var length = text.EndsWith('\r') ? text.Length - 1 : text.Length;
		StructureEditor.Document.GetRange(0, length).Link = "\"https://example.com\"";
		Status.Text = "Added structure link.";
	}

	private void OnRenameStructureLink(object sender, RoutedEventArgs e)
	{
		StructureEditor.Document.GetText(TextGetOptions.None, out var text);
		var length = text.EndsWith('\r') ? text.Length - 1 : text.Length;
		StructureEditor.Document.GetRange(0, length).Text = "renamed";
		Status.Text = "Renamed structure link.";
	}

	private void OnExpandStructurePrefix(object sender, RoutedEventArgs e)
	{
		StructureEditor.Document.GetRange(0, 0).SetText(
			TextSetOptions.FormatRtf,
			@"{\rtf1 a much longer prefix }");
		Status.Text = "Expanded structure prefix.";
	}

	private void OnContextMenuOpening(object sender, ContextMenuEventArgs e)
		=> Status.Text = $"Context menu opening at {e.CursorLeft:F1},{e.CursorTop:F1}.";

#if __SKIA__
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
#endif
}
