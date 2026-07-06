#nullable enable

using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls
{
	// Uno-specific functional implementation of the RichEditBox change-notification events for Skia.
	//
	// TextChanged and SelectionChanged (both RoutedEventHandler) are raised from the shared document
	// text choke point (OnDocumentTextChanged) and the selection render choke point
	// (UpdateDisplaySelection) respectively, mirroring WinUI's "fire after the content/selection has
	// actually changed" semantics. Each is de-duplicated against the last-raised value so that
	// format-only edits (which don't change the plain text) and pure re-renders / focus changes (which
	// don't change the selection span) do not raise spurious notifications.
	//
	// TODO Uno: TextChanging / SelectionChanging (TypedEventHandler with cancellable-style args) are
	// follow-ups. SelectionChanged is currently only raised while the control is focused, because the
	// reverse TOM->caret sync that drives it is focused-only by design (see OnTomSelectionChanged).
	public partial class RichEditBox
	{
		/// <summary>
		/// Occurs when the content of the text box changes, i.e. the plain text of the underlying
		/// <see cref="Document"/> differs from its previous value.
		/// </summary>
		public event RoutedEventHandler? TextChanged;

		/// <summary>
		/// Occurs when the selection (caret position or selected span) of the text box changes.
		/// </summary>
		public event RoutedEventHandler? SelectionChanged;

		// Last plain-text value for which TextChanged was raised. Initial document content is empty
		// (RichEditTextDocument starts with an empty buffer), so the baseline starts empty too.
		private string _lastRaisedText = string.Empty;

		// Last (start, length) selection span for which SelectionChanged was raised.
		private (int start, int length) _lastRaisedSelection;

		private void RaiseTextChangedIfNeeded()
		{
			var text = GetPlainTextContent();
			if (text == _lastRaisedText)
			{
				return;
			}

			_lastRaisedText = text;
			TextChanged?.Invoke(this, new RoutedEventArgs());
		}

		private void RaiseSelectionChangedIfNeeded()
		{
			var current = (_selection.start, _selection.length);
			if (current == _lastRaisedSelection)
			{
				return;
			}

			_lastRaisedSelection = current;
			SelectionChanged?.Invoke(this, new RoutedEventArgs());
		}
	}
}
