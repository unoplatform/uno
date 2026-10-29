#nullable enable

using System;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls
{
	// Keyboard formatting accelerators (Ctrl+B / Ctrl+I / Ctrl+U) for RichEditBox on Skia.
	//
	// WinUI's RichEditBox delegates these accelerators to the native RichEdit core, which toggles the
	// bold/italic/underline character format over the current selection and consults
	// DisabledFormattingAccelerators before acting. Because Uno's editing engine is the functional
	// managed TOM, we replicate that behavior here: apply the toggle over the interactive selection via
	// the Text Object Model (which preserves the run model and records one undo entry), gated by the
	// DisabledFormattingAccelerators flags. The character-format toggle is tri-state aware — a fully
	// formatted selection turns the effect off, otherwise (off or mixed) it turns the effect on, matching
	// the native behavior of "make it all bold, or clear bold if already all bold".
	partial class RichEditBox
	{
		/// <summary>
		/// Applies the bold/italic/underline toggle mapped to <paramref name="accelerator"/> over the
		/// current selection, unless the accelerator is disabled via <see cref="DisabledFormattingAccelerators"/>
		/// or the control is read-only. Returns true when a formatting change was applied (so the key is
		/// marked handled).
		/// </summary>
		private bool TryToggleFormattingAccelerator(DisabledFormattingAccelerators accelerator)
		{
			if (IsReadOnly)
			{
				return false;
			}

			if ((_enabledFormattingAccelerators & accelerator) != accelerator)
			{
				// The app disabled this accelerator — RichEditBox ignores the shortcut.
				return false;
			}

			var start = _selection.start;
			var end = _selection.start + _selection.length;

			// A degenerate caret toggles a "pending" insertion-point format applied to subsequently typed
			// text; a non-degenerate selection toggles the format over the selected characters. Both use
			// the same read-toggle-write path — the TOM establishes the pending format for a caret and
			// mutates the run model for a selection.
			var format = Document.GetRange(start, end).CharacterFormat;
			switch (accelerator)
			{
				case DisabledFormattingAccelerators.Bold:
					format.Bold = format.Bold == FormatEffect.On ? FormatEffect.Off : FormatEffect.On;
					break;
				case DisabledFormattingAccelerators.Italic:
					format.Italic = format.Italic == FormatEffect.On ? FormatEffect.Off : FormatEffect.On;
					break;
				case DisabledFormattingAccelerators.Underline:
					format.Underline = format.Underline == UnderlineType.Single ? UnderlineType.None : UnderlineType.Single;
					break;
			}

			return true;
		}
	}
}
