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

	public ImeCompositionEventArgs(string text, int cursorPosition = -1, int resolvedLength = 0)
	{
		Text = text;
		CursorPosition = cursorPosition;
		ResolvedLength = resolvedLength;
	}
}
