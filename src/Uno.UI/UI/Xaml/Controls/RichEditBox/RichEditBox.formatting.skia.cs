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
		/// Identifies the <see cref="DisabledFormattingAccelerators"/> dependency property. Functional on
		/// Skia (the generated stub excludes Skia so this hand-authored registration takes over).
		/// </summary>
		public static global::Microsoft.UI.Xaml.DependencyProperty DisabledFormattingAcceleratorsProperty { get; } =
			global::Microsoft.UI.Xaml.DependencyProperty.Register(
				nameof(DisabledFormattingAccelerators),
				typeof(global::Microsoft.UI.Xaml.Controls.DisabledFormattingAccelerators),
				typeof(global::Microsoft.UI.Xaml.Controls.RichEditBox),
				new global::Microsoft.UI.Xaml.FrameworkPropertyMetadata(default(global::Microsoft.UI.Xaml.Controls.DisabledFormattingAccelerators)));

		/// <summary>
		/// Gets or sets which built-in keyboard formatting accelerators (Ctrl+B/I/U) are disabled.
		/// </summary>
		public global::Microsoft.UI.Xaml.Controls.DisabledFormattingAccelerators DisabledFormattingAccelerators
		{
			get => (global::Microsoft.UI.Xaml.Controls.DisabledFormattingAccelerators)GetValue(DisabledFormattingAcceleratorsProperty);
			set => SetValue(DisabledFormattingAcceleratorsProperty, value);
		}

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

			if ((this.DisabledFormattingAccelerators & accelerator) == accelerator)
			{
				// The app disabled this accelerator — RichEditBox ignores the shortcut.
				return false;
			}

			var start = _selection.start;
			var end = _selection.start + _selection.length;

			// A degenerate caret would toggle a "pending" format applied to subsequently typed text.
			// That pending-format state isn't modeled yet. TODO Uno: caret formatting toggle.
			if (start == end)
			{
				return false;
			}

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
