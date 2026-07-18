#nullable enable

using System.Collections.Generic;

namespace Microsoft.UI.Text
{
	// A detached snapshot of a range's text and the resolved formatting carried by each UTF-16 code
	// unit. This is the common transfer shape for FormattedText and future RTF/clipboard/object support.
	internal sealed record RichTextFragment(
		string Text,
		List<CharacterFormatState> CharacterStates,
		List<ParagraphFormatState> ParagraphStates,
		ParagraphFormatState TerminalParagraphState,
		bool HasExplicitTerminalParagraphState)
	{
		internal RichTextFragment(
			string text,
			List<CharacterFormatState> characterStates,
			List<ParagraphFormatState> paragraphStates)
			: this(text, characterStates, paragraphStates, new ParagraphFormatState(), false)
		{
		}

		internal RichTextFragment(
			string text,
			List<CharacterFormatState> characterStates,
			List<ParagraphFormatState> paragraphStates,
			ParagraphFormatState terminalParagraphState)
			: this(text, characterStates, paragraphStates, terminalParagraphState, true)
		{
		}

		internal static RichTextFragment Empty() => new(string.Empty, new(), new(), new ParagraphFormatState(), true);
	}
}
