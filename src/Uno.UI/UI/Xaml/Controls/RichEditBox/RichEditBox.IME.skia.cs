#nullable enable

using System;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Uno.UI.Xaml.Controls.Extensions;

namespace Microsoft.UI.Xaml.Controls
{
	// Interactive IME composition for RichEditBox on Skia.
	//
	// RichEditBox participates in the shared IME session model exactly like TextBox: it implements
	// IImeSessionHost (positioning surface + composition callbacks) and activates/deactivates through
	// the shared ImeSessionCoordinator on focus/blur. The one global OS IME is arbitrated by the
	// coordinator's single active-host reference, so TextBox and RichEditBox never cross-fire.
	//
	// Composition text is applied through the functional Text Object Model (Document.ReplaceRange), so
	// the character-format run model outside the preedit is preserved. The whole composition is wrapped
	// in a single BeginUndoGroup/EndUndoGroup so it collapses to ONE undo entry (matching WinUI, where a
	// single Ctrl+Z removes the entire IME-composed word). The composition underline is rendered "for
	// free" by the shared DisplayBlock, which reads IsComposing/CompositionUnderline* off ITextBoxViewHost.
	//
	// Runtime-validated on Win32 (IMM32). Other Skia platforms are compile-checked through the shared
	// contract; RichEditBox-on-Android IME (native InputConnection path) is a documented maintenance gap.
	partial class RichEditBox : IImeSessionHost
	{
		private bool _isComposing;
		// True when the current composition session has the platform applying text directly
		// (e.g., Android's InputConnection). In this mode, key events arrive independently from
		// composition events and should NOT be swallowed by the IsComposing check.
		private bool _compositionAppliedByPlatform;
		private bool _platformTextApplyInProgress;
		private int _compositionStartIndex;
		private int _compositionLength;
		private int _compositionResolvedLength;

		// Guards the document text choke point so composition-internal ReplaceRange calls don't cancel
		// the very composition that produced them (see CancelCompositionOnExternalChange).
		private bool _suppressCompositionExternalCancel;

		// Tracks the open composition undo group so the whole composition is one undoable action.
		private bool _compositionUndoGroupOpen;

		internal bool ShouldSwallowKeyDuringComposition => _isComposing && !_compositionAppliedByPlatform;

		internal bool IsComposing => _isComposing;

		public event TypedEventHandler<RichEditBox, TextCompositionStartedEventArgs>? TextCompositionStarted;
		public event TypedEventHandler<RichEditBox, TextCompositionChangedEventArgs>? TextCompositionChanged;
		public event TypedEventHandler<RichEditBox, TextCompositionEndedEventArgs>? TextCompositionEnded;

		// --- IImeSessionHost positioning surface (read by the platform IME extensions) ---

		XamlRoot? IImeSessionHost.XamlRoot => XamlRoot;

		TextBoxView? IImeSessionHost.TextBoxView => _textBoxView;

		int IImeSessionHost.SelectionStart => _selection.start;

		int IImeSessionHost.SelectionLength => _selection.length;

		bool IImeSessionHost.IsBackwardSelection => _selection.selectionEndsAtTheStart;

		private void StartImeSession() => ImeSessionCoordinator.StartSession(this);

		private void EndImeSession()
		{
			ImeSessionCoordinator.EndSession(this);

			// Defensively reset composition state in case the extension's CompositionEnded event didn't
			// fire, so subsequent key events aren't swallowed at the IsComposing check in OnPostKeyDown.
			_compositionAppliedByPlatform = false;
			if (_isComposing)
			{
				var startIndex = _compositionStartIndex;
				var length = _compositionLength;
				_isComposing = false;
				_compositionLength = 0;
				_compositionStartIndex = 0;
				_compositionResolvedLength = 0;

				CloseCompositionUndoGroup();
				TextCompositionEnded?.Invoke(this, new TextCompositionEndedEventArgs(startIndex, length));
				InvalidateImeRender();
			}
			else
			{
				CloseCompositionUndoGroup();
			}
		}

		void IImeSessionHost.OnImeCompositionStarted()
		{
			if (IsReadOnly)
			{
				return;
			}

			_isComposing = true;
			_compositionStartIndex = _selection.start;
			// Initialize from the current selection length so the first ReplaceCompositionText replaces
			// the selected range, matching normal typing behavior.
			_compositionLength = _selection.length;
			_compositionResolvedLength = 0;

			// Open one undo group for the whole composition so a single Undo removes the composed word.
			OpenCompositionUndoGroup();

			TextCompositionStarted?.Invoke(this, new TextCompositionStartedEventArgs(_compositionStartIndex, _compositionLength));
		}

		void IImeSessionHost.OnImeCompositionUpdated(string compositionText, int cursorPosition, int resolvedLength, bool textAlreadyApplied)
		{
			if (IsReadOnly)
			{
				return;
			}

			if (textAlreadyApplied)
			{
				// The platform (e.g., Android InputConnection) already applied the text. Suppress the
				// external-change cancel for the document sync that follows, and mark the session so key
				// events aren't swallowed by the IsComposing check.
				_platformTextApplyInProgress = true;
				_compositionAppliedByPlatform = true;
			}
			else
			{
				ReplaceCompositionText(compositionText, cursorPosition);
			}

			_compositionLength = compositionText.Length;
			_compositionResolvedLength = resolvedLength;

			TextCompositionChanged?.Invoke(this, new TextCompositionChangedEventArgs(_compositionStartIndex, _compositionLength));
			InvalidateImeRender();
		}

		void IImeSessionHost.OnImeCompositionCompleted(string committedText, bool textAlreadyApplied)
		{
			if (IsReadOnly)
			{
				return;
			}

			if (!textAlreadyApplied)
			{
				ReplaceCompositionText(committedText);
			}

			var startIndex = _compositionStartIndex;
			var committedLength = committedText.Length;
			_isComposing = false;
			_compositionAppliedByPlatform = false;
			_compositionLength = 0;
			_compositionStartIndex = 0;
			_compositionResolvedLength = 0;

			CloseCompositionUndoGroup();
			TextCompositionEnded?.Invoke(this, new TextCompositionEndedEventArgs(startIndex, committedLength));
			InvalidateImeRender();
		}

		void IImeSessionHost.OnImeCompositionEnded()
		{
			if (!_isComposing)
			{
				_compositionAppliedByPlatform = false;
				CloseCompositionUndoGroup();
				return;
			}

			// Composition ended without explicit commit — keep the inserted text as-is (matches WinUI).
			var startIndex = _compositionStartIndex;
			var length = _compositionLength;
			_isComposing = false;
			_compositionAppliedByPlatform = false;
			_compositionLength = 0;
			_compositionStartIndex = 0;
			_compositionResolvedLength = 0;

			CloseCompositionUndoGroup();
			TextCompositionEnded?.Invoke(this, new TextCompositionEndedEventArgs(startIndex, length));
			InvalidateImeRender();
		}

		/// <summary>
		/// Replaces the active preedit region with <paramref name="newText"/> through the TOM (preserving
		/// the surrounding character-format runs) and places the caret at the IME-reported position.
		/// </summary>
		private void ReplaceCompositionText(string newText, int cursorPosition = -1)
		{
			var text = GetPlainTextContent();
			// Clamp indices in case the text was modified out-of-band leaving the composition span stale.
			var startIndex = Math.Min(_compositionStartIndex, text.Length);
			var endIndex = Math.Min(_compositionStartIndex + _compositionLength, text.Length);

			var caretOffset = cursorPosition >= 0
				? Math.Min(cursorPosition, newText.Length)
				: newText.Length;

			_suppressCompositionExternalCancel = true;
			try
			{
				Document.ReplaceRange(startIndex, endIndex, newText);
			}
			finally
			{
				_suppressCompositionExternalCancel = false;
			}

			SetInteractiveSelectionFromComposition(startIndex + caretOffset);
		}

		/// <summary>
		/// Places a degenerate caret during composition without going through the cancellable
		/// SelectionChanging path (composition-driven caret moves must not be cancellable).
		/// </summary>
		private void SetInteractiveSelectionFromComposition(int caret)
		{
			var length = GetPlainTextContent().Length;
			caret = Math.Clamp(caret, 0, length);
			_selection = (caret, 0, false);

			if (_textBoxView is { } view)
			{
				_caretXOffset = (float)view.DisplayBlock.ParsedText.GetRectForIndex(caret).Left;
			}

			// Mirror into the Text Object Model for programmatic reads (internal push, no reverse-sync).
			Document.SetSelectionRangeInternal(caret, caret);

			_caretBlinkVisible = true;
			if (FocusState != FocusState.Unfocused)
			{
				EnsureCaretTimerHooked();
				_caretTimer.Start();
			}

			UpdateDisplaySelection();
		}

		/// <summary>
		/// Called from <see cref="OnDocumentTextChanged"/>. If the text changed externally (not by the
		/// composition path), cancel the active composition and restart the session so IME still works.
		/// </summary>
		private void CancelCompositionOnExternalChange()
		{
			if (!_isComposing || _suppressCompositionExternalCancel)
			{
				// Not composing, or the change came from ReplaceCompositionText — nothing to do.
				return;
			}

			if (_platformTextApplyInProgress)
			{
				// The platform already applied the text (e.g., Android). Don't cancel — just clear.
				_platformTextApplyInProgress = false;
				return;
			}

			var startIndex = _compositionStartIndex;
			var length = _compositionLength;
			_isComposing = false;
			_compositionAppliedByPlatform = false;
			_compositionLength = 0;
			_compositionStartIndex = 0;
			_compositionResolvedLength = 0;

			CloseCompositionUndoGroup();

			// End and restart the session so further IME input still works while the active host stays in sync.
			ImeSessionCoordinator.Extension?.EndImeSession();
			TextCompositionEnded?.Invoke(this, new TextCompositionEndedEventArgs(startIndex, length));
			InvalidateImeRender();

			if (ReferenceEquals(ImeSessionCoordinator.ActiveHost, this))
			{
				ImeSessionCoordinator.Extension?.StartImeSession(this);
			}
		}

		private void OpenCompositionUndoGroup()
		{
			if (!_compositionUndoGroupOpen)
			{
				_compositionUndoGroupOpen = true;
				Document.BeginUndoGroup();
			}
		}

		private void CloseCompositionUndoGroup()
		{
			if (_compositionUndoGroupOpen)
			{
				_compositionUndoGroupOpen = false;
				Document.EndUndoGroup();
			}
		}

		private void InvalidateImeRender() => _textBoxView?.DisplayBlock.InvalidateInlines(false);

		/// <summary>
		/// Installs a fake IME extension for testing. Returns a disposable that restores the original.
		/// </summary>
		internal static IDisposable SetImeExtensionForTesting(IImeTextBoxExtension extension)
			=> ImeSessionCoordinator.SetExtensionForTesting(extension);
	}
}
