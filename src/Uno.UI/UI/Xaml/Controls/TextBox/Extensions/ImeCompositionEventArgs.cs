using System;

namespace Uno.UI.Xaml.Controls.Extensions;

/// <summary>
/// Event args for IME composition updates and completions.
/// </summary>
internal class ImeCompositionEventArgs : EventArgs
{
	/// <summary>
	/// The composition string (during composition) or committed text (on completion).
	/// </summary>
	public string Text { get; }

	/// <summary>
	/// Cursor position within the composition string, or -1 if not available.
	/// </summary>
	public int CursorPosition { get; }

	/// <summary>
	/// Number of leading characters in the composition that are already resolved
	/// (e.g., converted to final characters). The underline should start after these.
	/// </summary>
	public int ResolvedLength { get; }

	/// <summary>
	/// When true, the platform has already applied the text to the TextBox
	/// (e.g., Android's InputConnection), so the IME handler should only
	/// update composition tracking state without calling ProcessTextInput again.
	/// </summary>
	public bool TextAlreadyApplied { get; }

	/// <summary>
	/// Index of the composition in the TextBox text when the platform already applied it, or -1 if not available.
	/// </summary>
	public int CompositionStart { get; }

	public ImeCompositionEventArgs(string text, int cursorPosition = -1, int resolvedLength = 0, bool textAlreadyApplied = false, int compositionStart = -1)
	{
		Text = text;
		CursorPosition = cursorPosition;
		ResolvedLength = resolvedLength;
		TextAlreadyApplied = textAlreadyApplied;
		CompositionStart = compositionStart;
	}
}

/// <summary>
/// Event args for an IME result that commits a prefix while continuing composition.
/// </summary>
internal sealed class ImePartialCompositionEventArgs : EventArgs
{
	public ImePartialCompositionEventArgs(
		string committedText,
		string compositionText,
		int cursorPosition,
		int resolvedLength,
		bool textAlreadyApplied = false)
	{
		CommittedText = committedText;
		CompositionText = compositionText;
		CursorPosition = cursorPosition;
		ResolvedLength = resolvedLength;
		TextAlreadyApplied = textAlreadyApplied;
	}

	public string CommittedText { get; }

	public string CompositionText { get; }

	public int CursorPosition { get; }

	public int ResolvedLength { get; }

	public bool TextAlreadyApplied { get; }
}
