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
	// The paired "changing" events use TypedEventHandler with their own args. TextChanging fires
	// immediately before TextChanged with IsContentChanging == true (our architecture applies the
	// edit before the choke point, so the content is already changed by the time we notify — the
	// Changing -> Changed ordering and the IsContentChanging flag are still faithful). SelectionChanging
	// is cancellable: it fires from the interactive selection choke point (SetInteractiveSelection)
	// before the change is committed, and a handler setting Cancel = true aborts it.
	//
	// TODO Uno: SelectionChanging is currently only raised for interactive (keyboard/pointer/clipboard)
	// selection changes, not for programmatic Document.Selection changes (which mutate the TOM before
	// the control observes them, so honoring Cancel there would require reverting the TOM). Both
	// SelectionChanged and SelectionChanging are only raised while the control is focused, because the
	// reverse TOM->caret sync that drives them is focused-only by design (see OnTomSelectionChanged).
	//
	// The clipboard events (CopyingToClipboard, CuttingToClipboard, Paste) are raised from the
	// RichEditBox clipboard methods (see RichEditBox.clipboard.skia.cs) before the corresponding
	// clipboard operation; a handler setting Handled = true suppresses the default behavior. Cut raises
	// CuttingToClipboard (not CopyingToClipboard), matching WinUI.
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

		/// <summary>
		/// Occurs just before the text content of the text box changes.
		/// </summary>
		public event global::Windows.Foundation.TypedEventHandler<RichEditBox, RichEditBoxTextChangingEventArgs>? TextChanging;

		/// <summary>
		/// Occurs just before the selection changes. A handler may cancel the pending change by setting
		/// <see cref="RichEditBoxSelectionChangingEventArgs.Cancel"/> to <c>true</c>.
		/// </summary>
		public event global::Windows.Foundation.TypedEventHandler<RichEditBox, RichEditBoxSelectionChangingEventArgs>? SelectionChanging;

		/// <summary>
		/// Occurs when text is copied to the clipboard. A handler may set
		/// <see cref="TextControlCopyingToClipboardEventArgs.Handled"/> to suppress the default copy.
		/// </summary>
		public event global::Windows.Foundation.TypedEventHandler<RichEditBox, TextControlCopyingToClipboardEventArgs>? CopyingToClipboard;

		/// <summary>
		/// Occurs when text is cut to the clipboard. A handler may set
		/// <see cref="TextControlCuttingToClipboardEventArgs.Handled"/> to suppress the default cut.
		/// </summary>
		public event global::Windows.Foundation.TypedEventHandler<RichEditBox, TextControlCuttingToClipboardEventArgs>? CuttingToClipboard;

		/// <summary>
		/// Occurs when text is pasted from the clipboard. A handler may set
		/// <see cref="TextControlPasteEventArgs.Handled"/> to suppress the default paste.
		/// </summary>
		public event TextControlPasteEventHandler? Paste;

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

			// WinUI raises TextChanging (content is changing) immediately before TextChanged.
			TextChanging?.Invoke(this, new RichEditBoxTextChangingEventArgs(isContentChanging: true));
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

		/// <summary>
		/// Raises the cancellable <see cref="SelectionChanging"/> event for a proposed interactive
		/// selection change and returns whether a handler cancelled it.
		/// </summary>
		private bool RaiseSelectionChangingIsCancelled(int selectionStart, int selectionLength)
		{
			if (SelectionChanging is not { } handler)
			{
				return false;
			}

			var args = new RichEditBoxSelectionChangingEventArgs(selectionStart, selectionLength);
			handler.Invoke(this, args);
			return args.Cancel;
		}

		/// <summary>Raises <see cref="CopyingToClipboard"/> and returns whether a handler suppressed it.</summary>
		private bool RaiseCopyingToClipboardIsHandled()
		{
			if (CopyingToClipboard is not { } handler)
			{
				return false;
			}

			var args = new TextControlCopyingToClipboardEventArgs();
			handler.Invoke(this, args);
			return args.Handled;
		}

		/// <summary>Raises <see cref="CuttingToClipboard"/> and returns whether a handler suppressed it.</summary>
		private bool RaiseCuttingToClipboardIsHandled()
		{
			if (CuttingToClipboard is not { } handler)
			{
				return false;
			}

			var args = new TextControlCuttingToClipboardEventArgs();
			handler.Invoke(this, args);
			return args.Handled;
		}

		/// <summary>Raises <see cref="Paste"/> and returns whether a handler suppressed it.</summary>
		private bool RaisePasteIsHandled()
		{
			if (Paste is not { } handler)
			{
				return false;
			}

			var args = new TextControlPasteEventArgs();
			handler.Invoke(this, args);
			return args.Handled;
		}
	}
}
