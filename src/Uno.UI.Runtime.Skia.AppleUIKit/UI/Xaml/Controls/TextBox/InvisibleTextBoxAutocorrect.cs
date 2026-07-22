using System;
using Foundation;

namespace Uno.WinUI.Runtime.Skia.AppleUIKit.Controls;

internal static class InvisibleTextBoxAutocorrect
{
	/// <summary>
	/// Detects the no-op text replacement iOS autocorrection fires when the caret leaves the middle
	/// of a word — it proposes replacing a range with its own current content and then appends a
	/// stray separator space. On Skia the caret is driven programmatically on the off-screen input
	/// proxy, so iOS misfires this on every tap-to-reposition. The delegates reject the no-op to
	/// suppress that autospace; genuine edits and real corrections change the text, so they are not
	/// matched here and pass through untouched.
	/// </summary>
	internal static bool IsNoOpReplacement(string? currentText, NSRange range, string replacementString)
	{
		// Bound-check in nint space before the int casts, so a pathological or NSNotFound
		// range can't overflow into a negative offset/length and make AsSpan throw.
		if (currentText is null
			|| range.Location < 0
			|| range.Length <= 0
			|| range.Location > currentText.Length
			|| range.Length > currentText.Length - range.Location)
		{
			return false;
		}

		return currentText.AsSpan((int)range.Location, (int)range.Length).SequenceEqual(replacementString);
	}
}
