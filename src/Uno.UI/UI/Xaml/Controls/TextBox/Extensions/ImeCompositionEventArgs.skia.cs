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

	public ImeCompositionEventArgs(string text, int cursorPosition = -1, int resolvedLength = 0, bool textAlreadyApplied = false)
	{
		Text = text;
		CursorPosition = cursorPosition;
		ResolvedLength = resolvedLength;
		TextAlreadyApplied = textAlreadyApplied;
	}
}
