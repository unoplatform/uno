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
		if (range.Length <= 0 || currentText is null)
		{
			return false;
		}

		return (int)(range.Location + range.Length) <= currentText.Length
			&& currentText.Substring((int)range.Location, (int)range.Length) == replacementString;
	}
}
