#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Input;
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
	partial class RichEditBox : IImeSessionHost
	{
		private bool _isComposing;
		// True when the current composition session has the platform applying text directly
		// (e.g., Android's InputConnection). In this mode, key events arrive independently from
		// composition events and should NOT be swallowed by the IsComposing check.
		private bool _compositionAppliedByPlatform;
		private bool _platformTextApplyInProgress;
		private (int start, int length)? _nativeCompositionRangeBeforeUpdate;
		private int _compositionStartIndex;
		private int _compositionLength;
		private int _compositionResolvedLength;
		private bool _compositionHasCommittedText;

		// Guards the document text choke point so composition-internal ReplaceRange calls don't cancel
		// the very composition that produced them (see CancelCompositionOnExternalChange).
		private bool _suppressCompositionExternalCancel;

		// Tracks the open composition undo group so the whole composition is one undoable action.
		private bool _compositionUndoGroupOpen;
		private int _compositionEventReentrancyCount;
		private InputScope? _defaultImeInputScope;

		internal bool ShouldSwallowKeyDuringComposition => _isComposing && !_compositionAppliedByPlatform;

		internal bool IsComposing => _isComposing;
		internal int CompositionStartIndex => _compositionStartIndex;
		internal int CompositionLength => _compositionLength;

		internal bool TryGetAccessibilityCompositionRange(bool conversionTarget, out int start, out int end)
		{
			if (!_isComposing)
			{
				start = 0;
				end = 0;
				return false;
			}

			var textLength = GetPlainTextLength();
			var compositionStart = Math.Clamp(_compositionStartIndex, 0, textLength);
			var compositionLength = Math.Clamp(_compositionLength, 0, textLength - compositionStart);
			if (!conversionTarget)
			{
				start = compositionStart;
				end = compositionStart + compositionLength;
				return true;
			}

			var resolvedLength = Math.Clamp(_compositionResolvedLength, 0, compositionLength);
			start = compositionStart + resolvedLength;
			end = compositionStart + compositionLength;
			return start < end;
		}

		// --- IImeSessionHost positioning surface (read by the platform IME extensions) ---

		XamlRoot? IImeSessionHost.XamlRoot => XamlRoot;

		TextBoxView? IImeSessionHost.TextBoxView => _textBoxView;

		int IImeSessionHost.SelectionStart => _selection.start;

		int IImeSessionHost.SelectionLength => _selection.length;

		bool IImeSessionHost.IsBackwardSelection => _selection.selectionEndsAtTheStart;

		InputScope IImeSessionHost.InputScope => InputScope ?? (_defaultImeInputScope ??= new InputScope
		{
			Names = { new InputScopeName { NameValue = InputScopeNameValue.Default } },
		});

		bool IImeSessionHost.IsTextPredictionEnabled => IsTextPredictionEnabled;

		CandidateWindowAlignment IImeSessionHost.DesiredCandidateWindowAlignment => DesiredCandidateWindowAlignment;

		string IImeSessionHost.Text => GetPlainTextContent();

		bool IImeSessionHost.AcceptsReturn => AcceptsReturn;

		bool IImeSessionHost.IsSpellCheckEnabled => IsSpellCheckEnabled;

		bool IImeSessionHost.CanAcceptTextInput => IsEnabled && !IsReadOnly && IsTabStop;

		int IImeSessionHost.MaxLength => MaxLength;

		bool IImeSessionHost.IsComposing => _isComposing;

		CharacterCasing IImeSessionHost.CharacterCasing => CharacterCasing;

		void IImeSessionHost.UpdateTextFromNative(string text, int selectionStart, int selectionLength)
			=> UpdateTextFromNative(text, selectionStart, selectionLength);

		void IImeSessionHost.SelectFromNative(int selectionStart, int selectionLength)
			=> SelectFromNative(selectionStart, selectionLength);

		bool IImeSessionHost.RaisePaste() => RaisePasteIsHandled();

		private void StartImeSession()
			=> ActivateImeForFocusOrigin(_imeFocusOrigin);

		private void ActivateImeForFocusOrigin(FocusState focusState)
		{
			if (!IsEnabled)
			{
				return;
			}

			var suppressSoftwareKeyboard =
				PreventKeyboardDisplayOnProgrammaticFocus && focusState == FocusState.Programmatic;
			_textBoxView?.OnFocusStateChanged(focusState, suppressSoftwareKeyboard);
			ImeSessionCoordinator.StartSession(
				this,
				new ImeSessionActivation(focusState, suppressSoftwareKeyboard));
		}

		private void ActivateImeForUserInteraction(FocusState focusState)
		{
			if (!IsEnabled)
			{
				return;
			}

			_textBoxView?.OnFocusStateChanged(focusState, suppressSoftwareKeyboard: false);
			ImeSessionCoordinator.StartSession(
				this,
				new ImeSessionActivation(focusState, IsSoftwareKeyboardSuppressed: false));
		}

		private void EndImeSession()
		{
			ImeSessionCoordinator.EndSession(this);

			// Defensively reset composition state in case the extension's CompositionEnded event didn't
			// fire, so subsequent key events aren't swallowed at the IsComposing check in OnPostKeyDown.
			_compositionAppliedByPlatform = false;
			_platformTextApplyInProgress = false;
			_nativeCompositionRangeBeforeUpdate = null;
			if (_isComposing)
			{
				var compositionText = GetCurrentCompositionText();
				var hadConversionTarget = TryGetAccessibilityCompositionRange(
					conversionTarget: true,
					out var previousConversionStart,
					out var previousConversionEnd);
				var startIndex = _compositionStartIndex;
				var length = _compositionLength;
				_isComposing = false;
				_compositionLength = 0;
				_compositionStartIndex = 0;
				_compositionResolvedLength = 0;
				_compositionHasCommittedText = false;

				CloseCompositionUndoGroup();
				RaiseTextEditCompositionEvent(AutomationTextEditChangeType.CompositionFinalized, compositionText);
				RaiseConversionTargetChangedIfNeeded(
					hadConversionTarget,
					previousConversionStart,
					previousConversionEnd);
				InvokeCompositionEvent(
					() => TextCompositionEnded?.Invoke(this, new TextCompositionEndedEventArgs(startIndex, length)));
			}
			else
			{
				CloseCompositionUndoGroup();
			}
		}

		void IImeSessionHost.OnImeCompositionStarted()
		{
			if (!IsEnabled || IsReadOnly)
			{
				return;
			}

			_isComposing = true;
			_compositionAppliedByPlatform = false;
			_platformTextApplyInProgress = false;
			_nativeCompositionRangeBeforeUpdate = null;
			_compositionHasCommittedText = false;
			_compositionStartIndex = _selection.start;
			// Initialize from the current selection length so the first ReplaceCompositionText replaces
			// the selected range, matching normal typing behavior.
			_compositionLength = _selection.length;
			_compositionResolvedLength = 0;

			// Open one undo group for the whole composition so a single Undo removes the composed word.
			OpenCompositionUndoGroup();

			var args = new TextCompositionStartedEventArgs(_compositionStartIndex, _compositionLength);
			InvokeCompositionEvent(() => TextCompositionStarted?.Invoke(this, args));
		}

		void IImeSessionHost.OnImeCompositionUpdated(string compositionText, int cursorPosition, int resolvedLength, bool textAlreadyApplied)
		{
			if (IsReadOnly || !_isComposing)
			{
				return;
			}

			var hadConversionTarget = TryGetAccessibilityCompositionRange(
				conversionTarget: true,
				out var previousConversionStart,
				out var previousConversionEnd);
			if (textAlreadyApplied)
			{
				// The platform (e.g., Android InputConnection) already applied the text. Suppress the
				// external-change cancel for the document sync that follows, and mark the session so key
				// events aren't swallowed by the IsComposing check.
				PreparePlatformCompositionUpdate();
			}
			else
			{
				_compositionLength = ReplaceCompositionText(compositionText, cursorPosition);
			}

			if (textAlreadyApplied)
			{
				_compositionLength = compositionText.Length;
			}
			_compositionResolvedLength = Math.Clamp(resolvedLength, 0, _compositionLength);

			RaiseTextEditCompositionEvent(AutomationTextEditChangeType.Composition, compositionText);
			RaiseConversionTargetChangedIfNeeded(
				hadConversionTarget,
				previousConversionStart,
				previousConversionEnd);
			var args = new TextCompositionChangedEventArgs(_compositionStartIndex, _compositionLength);
			InvokeCompositionEvent(() => TextCompositionChanged?.Invoke(this, args));
		}

		private void PreparePlatformCompositionUpdate()
		{
			if (!_platformTextApplyInProgress)
			{
				_nativeCompositionRangeBeforeUpdate = (_compositionStartIndex, _compositionLength);
			}
			_platformTextApplyInProgress = true;
			_compositionAppliedByPlatform = true;
		}

		void IImeSessionHost.ReconcileCompositionFromNative(int start, int length)
		{
			if (!_isComposing)
			{
				return;
			}

			var hadConversionTarget = TryGetAccessibilityCompositionRange(
				conversionTarget: true,
				out var previousConversionStart,
				out var previousConversionEnd);
			var textLength = GetPlainTextLength();
			_compositionStartIndex = Math.Clamp(start, 0, textLength);
			_compositionLength = Math.Clamp(length, 0, textLength - _compositionStartIndex);
			_compositionResolvedLength = Math.Clamp(_compositionResolvedLength, 0, _compositionLength);

			InvalidateImeRender();
			RaiseConversionTargetChangedIfNeeded(hadConversionTarget, previousConversionStart, previousConversionEnd);
			Uno.Helpers.UIElementAccessibilityHelper.NotifyTextControlStateChanged(this);
		}

		void IImeSessionHost.OnImeCompositionPartiallyCommitted(
			string committedText,
			string compositionText,
			int cursorPosition,
			int resolvedLength,
			bool textAlreadyApplied)
		{
			if (IsReadOnly || !_isComposing)
			{
				return;
			}

			var hadConversionTarget = TryGetAccessibilityCompositionRange(
				conversionTarget: true,
				out var previousConversionStart,
				out var previousConversionEnd);
			var committedLength = committedText.Length;
			var compositionLength = compositionText.Length;
			if (textAlreadyApplied)
			{
				PreparePlatformCompositionUpdate();
			}
			else
			{
				var combinedText = committedText + compositionText;
				var combinedCursorPosition = cursorPosition >= 0
					? committedText.Length + Math.Min(cursorPosition, compositionText.Length)
					: -1;
				var insertedLength = ReplaceCompositionText(combinedText, combinedCursorPosition);
				committedLength = Math.Min(committedLength, insertedLength);
				compositionLength = Math.Min(compositionLength, insertedLength - committedLength);
			}

			_compositionStartIndex = Math.Min(_compositionStartIndex + committedLength, GetPlainTextLength());
			_compositionLength = Math.Min(compositionLength, GetPlainTextLength() - _compositionStartIndex);
			_compositionResolvedLength = Math.Clamp(resolvedLength, 0, _compositionLength);
			_compositionHasCommittedText |= committedLength > 0;

			if (committedText.Length > 0)
			{
				RaiseTextEditCompositionEvent(AutomationTextEditChangeType.CompositionFinalized, committedText);
			}
			RaiseTextEditCompositionEvent(AutomationTextEditChangeType.Composition, compositionText);
			RaiseConversionTargetChangedIfNeeded(
				hadConversionTarget,
				previousConversionStart,
				previousConversionEnd);
			var args = new TextCompositionChangedEventArgs(_compositionStartIndex, _compositionLength);
			InvokeCompositionEvent(() => TextCompositionChanged?.Invoke(this, args));
		}

		void IImeSessionHost.OnImeCompositionCompleted(string committedText, bool textAlreadyApplied)
		{
			if (!IsEnabled || IsReadOnly)
			{
				return;
			}

			var hadConversionTarget = TryGetAccessibilityCompositionRange(
				conversionTarget: true,
				out var previousConversionStart,
				out var previousConversionEnd);
			var committedLength = committedText.Length;
			var startIndex = _compositionStartIndex;
			var completed = false;
			try
			{
				if (!textAlreadyApplied)
				{
					committedLength = ReplaceCompositionText(committedText);
				}
				completed = true;
			}
			finally
			{
				_isComposing = false;
				_compositionAppliedByPlatform = false;
				_platformTextApplyInProgress = false;
				_compositionLength = 0;
				_compositionStartIndex = 0;
				_compositionResolvedLength = 0;
				_compositionHasCommittedText = false;
				CloseCompositionUndoGroup();
				if (!completed)
				{
					InvalidateImeRender();
				}
			}
			RaiseTextEditCompositionEvent(AutomationTextEditChangeType.CompositionFinalized, committedText);
			RaiseConversionTargetChangedIfNeeded(
				hadConversionTarget,
				previousConversionStart,
				previousConversionEnd);
			InvokeCompositionEvent(
				() => TextCompositionEnded?.Invoke(this, new TextCompositionEndedEventArgs(startIndex, committedLength)));
		}

		void IImeSessionHost.OnImeCompositionCanceled(bool textAlreadyApplied)
		{
			if (!_isComposing)
			{
				_compositionAppliedByPlatform = false;
				_platformTextApplyInProgress = false;
				return;
			}

			var hadConversionTarget = TryGetAccessibilityCompositionRange(
				conversionTarget: true,
				out var previousConversionStart,
				out var previousConversionEnd);
			var startIndex = _compositionStartIndex;
			var canceled = false;
			try
			{
				if (!textAlreadyApplied)
				{
					ReplaceCompositionText(string.Empty);
				}
				canceled = true;
			}
			finally
			{
				_isComposing = false;
				_compositionAppliedByPlatform = false;
				_platformTextApplyInProgress = false;
				_compositionLength = 0;
				_compositionStartIndex = 0;
				_compositionResolvedLength = 0;
				if (_compositionHasCommittedText)
				{
					CloseCompositionUndoGroup();
				}
				else
				{
					DiscardCompositionUndoGroup();
				}
				_compositionHasCommittedText = false;
				if (!canceled)
				{
					InvalidateImeRender();
				}
			}

			RaiseTextEditCompositionEvent(AutomationTextEditChangeType.CompositionFinalized, string.Empty);
			RaiseConversionTargetChangedIfNeeded(
				hadConversionTarget,
				previousConversionStart,
				previousConversionEnd);
			InvokeCompositionEvent(
				() => TextCompositionEnded?.Invoke(this, new TextCompositionEndedEventArgs(startIndex, 0)));
		}

		void IImeSessionHost.OnImeCompositionEnded()
		{
			if (!_isComposing)
			{
				_compositionAppliedByPlatform = false;
				_platformTextApplyInProgress = false;
				CloseCompositionUndoGroup();
				return;
			}

			// Composition ended without explicit commit — keep the inserted text as-is (matches WinUI).
			var compositionText = GetCurrentCompositionText();
			var hadConversionTarget = TryGetAccessibilityCompositionRange(
				conversionTarget: true,
				out var previousConversionStart,
				out var previousConversionEnd);
			var startIndex = _compositionStartIndex;
			var length = _compositionLength;
			_isComposing = false;
			_compositionAppliedByPlatform = false;
			_platformTextApplyInProgress = false;
			_compositionLength = 0;
			_compositionStartIndex = 0;
			_compositionResolvedLength = 0;
			_compositionHasCommittedText = false;

			CloseCompositionUndoGroup();
			RaiseTextEditCompositionEvent(AutomationTextEditChangeType.CompositionFinalized, compositionText);
			RaiseConversionTargetChangedIfNeeded(
				hadConversionTarget,
				previousConversionStart,
				previousConversionEnd);
			InvokeCompositionEvent(
				() => TextCompositionEnded?.Invoke(this, new TextCompositionEndedEventArgs(startIndex, length)));
		}

		void IImeSessionHost.OnCandidateWindowBoundsChanged(Rect bounds)
		{
			_candidateWindowBoundsChanged?.Invoke(this, new CandidateWindowBoundsChangedEventArgs(bounds));
			m_firedCandidateWindowEventAfterFocus = true;
		}

		private async Task<IReadOnlyList<string>> GetLinguisticAlternativesCoreAsync(CancellationToken cancellationToken)
		{
			if (!_isComposing || _compositionLength <= 0)
			{
				return Array.Empty<string>();
			}

			var text = GetPlainTextContent();
			var start = Math.Clamp(_compositionStartIndex, 0, text.Length);
			var length = Math.Min(_compositionLength, text.Length - start);
			if (length <= 0)
			{
				return Array.Empty<string>();
			}

			var compositionText = text.Substring(start, length);
			var prefix = text[..start];
			var postfix = text[(start + length)..];
			var candidates = await ImeSessionCoordinator.GetLinguisticAlternativesAsync(
				this,
				compositionText,
				cancellationToken);
			cancellationToken.ThrowIfCancellationRequested();

			if (candidates.Count == 0)
			{
				return Array.Empty<string>();
			}

			var alternatives = new string[candidates.Count];
			for (var i = 0; i < candidates.Count; i++)
			{
				alternatives[i] = prefix + candidates[i] + postfix;
			}

			return Array.AsReadOnly(alternatives);
		}

		/// <summary>
		/// Replaces the active preedit region with <paramref name="newText"/> through the TOM (preserving
		/// the surrounding character-format runs) and places the caret at the IME-reported position.
		/// </summary>
		private int ReplaceCompositionText(string newText, int cursorPosition = -1)
		{
			var text = GetPlainTextContent();
			// Clamp indices in case the text was modified out-of-band leaving the composition span stale.
			var startIndex = Math.Min(_compositionStartIndex, text.Length);
			var endIndex = Math.Min(_compositionStartIndex + _compositionLength, text.Length);

			var caretOffset = cursorPosition >= 0
				? Math.Min(cursorPosition, newText.Length)
				: newText.Length;

			var textVersion = Document.TextVersion;
			var insertedLength = 0;
			var replaced = false;
			var selectionSynchronized = false;
			_suppressCompositionExternalCancel = true;
			try
			{
				if (Document.IsRangeProtected(startIndex, endIndex))
				{
					return _compositionLength;
				}

				RunWithDeferredSelectionSync(() => insertedLength = Document.ReplaceRange(startIndex, endIndex, newText));
				replaced = true;
				return insertedLength;
			}
			finally
			{
				try
				{
					if (replaced || Document.TextVersion != textVersion)
					{
						var currentLength = GetPlainTextLength();
						_compositionStartIndex = Math.Min(startIndex, currentLength);
						_compositionLength = replaced
							? insertedLength
							: Math.Clamp(currentLength - (text.Length - (endIndex - startIndex)), 0, currentLength - _compositionStartIndex);
						SetInteractiveSelectionFromComposition(_compositionStartIndex + Math.Min(caretOffset, _compositionLength));
						selectionSynchronized = true;
					}
				}
				finally
				{
					_suppressCompositionExternalCancel = false;
					if ((!replaced || !selectionSynchronized) && Document.TextVersion != textVersion)
					{
						InvalidateImeRender();
					}
				}
			}
		}

		/// <summary>
		/// Places a degenerate caret during composition without going through the cancellable
		/// SelectionChanging path (composition-driven caret moves must not be cancellable).
		/// </summary>
		private void SetInteractiveSelectionFromComposition(int caret)
		{
			var length = GetPlainTextLength();
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

			var compositionText = GetCurrentCompositionText();
			var hadConversionTarget = TryGetAccessibilityCompositionRange(
				conversionTarget: true,
				out var previousConversionStart,
				out var previousConversionEnd);
			var startIndex = _compositionStartIndex;
			var length = _compositionLength;
			_isComposing = false;
			_compositionAppliedByPlatform = false;
			_platformTextApplyInProgress = false;
			_compositionLength = 0;
			_compositionStartIndex = 0;
			_compositionResolvedLength = 0;
			_compositionHasCommittedText = false;

			CloseCompositionUndoGroup();

			// End and restart the session so further IME input still works while the active host stays in sync.
			ImeSessionCoordinator.RestartSession(this);
			RaiseTextEditCompositionEvent(AutomationTextEditChangeType.CompositionFinalized, compositionText);
			RaiseConversionTargetChangedIfNeeded(
				hadConversionTarget,
				previousConversionStart,
				previousConversionEnd);
			InvokeCompositionEvent(
				() => TextCompositionEnded?.Invoke(this, new TextCompositionEndedEventArgs(startIndex, length)));
		}

		private void InvokeCompositionEvent(Action invoke)
		{
			try
			{
				// TextBoxBase::SendTextCompositionEvent defers notifications raised by another composition handler.
				if (_compositionEventReentrancyCount != 0)
				{
					_ = Dispatcher.RunAsync(global::Windows.UI.Core.CoreDispatcherPriority.Normal, () => InvokeCompositionEvent(invoke));
					return;
				}

				_compositionEventReentrancyCount++;
				try
				{
					invoke();
				}
				finally
				{
					_compositionEventReentrancyCount--;
				}
			}
			finally
			{
				InvalidateImeRender();
			}
		}

		private string GetCurrentCompositionText()
		{
			var text = GetPlainTextContent();
			var start = Math.Clamp(_compositionStartIndex, 0, text.Length);
			var length = Math.Clamp(_compositionLength, 0, text.Length - start);
			return text.Substring(start, length);
		}

		private void RaiseTextEditCompositionEvent(AutomationTextEditChangeType changeType, string changedText)
			=> (FrameworkElementAutomationPeer.FromElement(this) as RichEditBoxAutomationPeer)?
				.RaisePlatformTextEditTextChangedEvent(changeType, new[] { changedText });

		private void RaiseConversionTargetChangedIfNeeded(bool previouslyAvailable, int previousStart, int previousEnd)
		{
			var currentlyAvailable = TryGetAccessibilityCompositionRange(
				conversionTarget: true,
				out var currentStart,
				out var currentEnd);
			if (previouslyAvailable != currentlyAvailable
				|| previouslyAvailable && (previousStart != currentStart || previousEnd != currentEnd))
			{
				(FrameworkElementAutomationPeer.FromElement(this) as RichEditBoxAutomationPeer)?
					.RaiseAutomationEvent(AutomationEvents.ConversionTargetChanged);
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

		private void DiscardCompositionUndoGroup()
		{
			if (_compositionUndoGroupOpen)
			{
				_compositionUndoGroupOpen = false;
				Document.DiscardUndoGroup();
			}
		}

		private void InvalidateImeRender()
		{
			_textBoxView?.DisplayBlock.InvalidateInlines(false);
			ImeSessionCoordinator.UpdateSession(this, ImeSessionUpdate.CandidateWindowAlignment);
		}

		/// <summary>
		/// Installs a fake IME extension for testing. Returns a disposable that restores the original.
		/// </summary>
		internal static IDisposable SetImeExtensionForTesting(IImeTextBoxExtension extension)
			=> ImeSessionCoordinator.SetExtensionForTesting(extension);
	}
}
