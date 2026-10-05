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
	/// Index of the composition within the text when the platform reports it, or -1 to keep
	/// the caret position captured when the composition started.
	/// </summary>
	public int StartIndex { get; }

	/// <summary>
	/// When true, the platform applied a text change for this update and reports it right after
	/// this event, so TextCompositionChanged is raised once that text is in place.
	/// </summary>
	public bool TextChangePending { get; }

	public ImeCompositionEventArgs(string text, int cursorPosition = -1, int resolvedLength = 0, bool textAlreadyApplied = false, int startIndex = -1, bool textChangePending = false)
	{
		Text = text;
		CursorPosition = cursorPosition;
		ResolvedLength = resolvedLength;
		TextAlreadyApplied = textAlreadyApplied;
		StartIndex = startIndex;
		TextChangePending = textChangePending;
	}
}
