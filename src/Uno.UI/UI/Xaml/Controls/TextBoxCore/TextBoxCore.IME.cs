#nullable enable
using System;
using Windows.Foundation;
using Microsoft.UI.Xaml.Input;
using Uno.UI.Xaml.Controls.Extensions;

namespace Microsoft.UI.Xaml.Controls;

internal sealed partial class TextBoxCore : IImeSessionHost
{
	private bool _isComposing;
	private bool _platformTextApplyInProgress;
	// True when the current composition session has the platform applying text directly
	// (e.g., Android's InputConnection). In this mode, key events arrive independently
	// from composition events and should NOT be swallowed by the IsComposing check.
	private bool _compositionAppliedByPlatform;
	private int _compositionStartIndex;
	private int _compositionLength;
	private int _compositionResolvedLength;
	// The selection that the composition's first applied text replaces. Until some composition text is applied, the composition
	// has no text of its own (_compositionLength is 0), and an empty replacement leaves the selection alone.
	private int _compositionSelectionLength;
	private bool _compositionApplied;
	// A platform that applies the text itself reports each update before syncing its text, which can still be refused: the range
	// to go back to then, and whether the last update was refused.
	private (int startIndex, int length, int resolvedLength) _compositionRangeBeforePlatformSync;
	private bool _wasPlatformSyncRefused;

	internal bool ShouldSwallowKeyDuringComposition => _isComposing && !_compositionAppliedByPlatform;


	internal bool IsComposing => _isComposing;
	internal int CompositionStartIndex => _compositionStartIndex;
	internal int CompositionLength => _compositionLength;

	/// <summary>
	/// The underline range within the composition, starting after any already-resolved characters.
	/// Used by the renderer to only underline the active (unresolved) portion of the preedit.
	/// </summary>
	internal int CompositionUnderlineStart => _compositionStartIndex + _compositionResolvedLength;
	internal int CompositionUnderlineLength => Math.Max(0, _compositionLength - _compositionResolvedLength);

	// --- IImeSessionHost positioning surface (read by the platform IME extensions) ---

	XamlRoot? IImeSessionHost.XamlRoot => Owner.XamlRoot;

	TextBoxView? IImeSessionHost.TextBoxView => TextBoxView;

	int IImeSessionHost.SelectionStart => SelectionStart;

	int IImeSessionHost.SelectionLength => SelectionLength;

	bool IImeSessionHost.IsBackwardSelection => IsBackwardSelection;

	InputScope IImeSessionHost.InputScope => InputScope;

	bool IImeSessionHost.IsTextPredictionEnabled => IsTextPredictionEnabled;

	CandidateWindowAlignment IImeSessionHost.DesiredCandidateWindowAlignment => CandidateWindowAlignment.Default;

	string IImeSessionHost.Text => Text;

	bool IImeSessionHost.AcceptsReturn => AcceptsReturn;

	bool IImeSessionHost.IsSpellCheckEnabled => IsSpellCheckEnabled;

	bool IImeSessionHost.CanAcceptTextInput => !IsReadOnly && Owner.IsTabStop;

	int IImeSessionHost.MaxLength => MaxLength;

	bool IImeSessionHost.IsComposing => _isComposing;

	CharacterCasing IImeSessionHost.CharacterCasing => CharacterCasing;

	void IImeSessionHost.UpdateTextFromNative(string text, int selectionStart, int selectionLength)
	{
		if (IsNativeEchoOfCurrentSelection(selectionStart, selectionLength)
			&& IsNativeEchoOfCurrentText(text))
		{
			// ProcessTextInput is skipped, so clear what its finally would have.
			if (!_isInputModifyingText)
			{
				_platformTextApplyInProgress = false;
			}

			return;
		}

		// Pending so the text change itself lands the caret here instead of passing through 0.
		SetPendingSelection(selectionStart, selectionLength);
		ProcessTextInput(text, out var applied);

		// The native caret belongs to the native text, so a refused edit keeps the TextBox's own selection.
		if (applied)
		{
			Select(selectionStart, selectionLength);
		}
	}

	void IImeSessionHost.SelectFromNative(int selectionStart, int selectionLength)
	{
		if (!IsNativeEchoOfCurrentSelection(selectionStart, selectionLength))
		{
			Select(selectionStart, selectionLength);
		}
	}

	// Native selections are normalized and can't tell which end the caret is on, so re-applying an echo
	// of the current range would flip a backward selection forward.
	private bool IsNativeEchoOfCurrentSelection(int selectionStart, int selectionLength)
		=> selectionStart == _selection.start && selectionLength == _selection.length;

	// Native text reports line breaks as '\n'; compares without allocating a normalized copy per keystroke.
	private bool IsNativeEchoOfCurrentText(string text)
	{
		var current = Text;
		if (text.Length != current.Length)
		{
			return false;
		}

		for (var i = 0; i < current.Length; i++)
		{
			var c = current[i] == '\r' ? '\n' : current[i];
			if (c != text[i])
			{
				return false;
			}
		}

		return true;
	}

	bool IImeSessionHost.RaisePaste()
	{
		var args = new TextControlPasteEventArgs();
		RaisePaste(args);
		return args.Handled;
	}

	private static void InitializeIme() => ImeSessionCoordinator.Initialize();

	private void StartImeSession(FocusState focusState)
	{
		// Don't route IME composition events to PasswordBox — password text
		// must not be processed as composition text, even if the extension
		// no-ops for PasswordBox (any later global composition event would
		// otherwise be forwarded to this PasswordBox via the coordinator's active host).
		if (_host.IsPassword)
		{
			return;
		}

		ImeSessionCoordinator.StartSession(this, new ImeSessionActivation(focusState, IsSoftwareKeyboardSuppressed));

		// Geometry tracking costs a LayoutUpdated subscription, so only the TextBox with a live session pays it.
		_imeGeometryTrackingRequested = true;
		_lastImeSelection = null;
		_lastImeLayoutState = null;
		AttachImeGeometryTracking();
	}

	private void EndImeSession()
	{
		// Symmetric with StartImeSession — PasswordBoxes never activate the
		// IME session, so don't tear it down (which would clobber the active host).
		if (_host.IsPassword)
		{
			return;
		}

		ImeSessionCoordinator.EndSession(this);
		_imeGeometryTrackingRequested = false;
		DetachImeGeometryTracking();

		// Defensively reset composition state in case the extension's CompositionEnded
		// event didn't fire (e.g., extension's _isComposing already false but TextBox's
		// _isComposing still true from a stale CompositionStarted). Without this, all
		// subsequent key events would be swallowed at the IsComposing check in OnKeyDown.
		_compositionAppliedByPlatform = false;
		if (_isComposing)
		{
			var startIndex = _compositionStartIndex;
			var length = _compositionLength;
			_isComposing = false;
			_compositionLength = 0;
			_compositionStartIndex = 0;
			_compositionResolvedLength = 0;

			_host.RaiseTextCompositionEnded(new TextCompositionEndedEventArgs(startIndex, length));
			InvalidateTextBoxRender();
		}
	}

	void IImeSessionHost.OnImeCompositionStarted()
	{
		if (IsReadOnly)
		{
			return;
		}

		_isComposing = true;
		_compositionAppliedByPlatform = false;
		_platformTextApplyInProgress = false;
		_compositionStartIndex = SelectionStart;
		// The first ReplaceCompositionText replaces the selected range, matching normal typing behavior.
		_compositionSelectionLength = SelectionLength;
		_compositionLength = 0;
		_compositionResolvedLength = 0;
		_compositionApplied = false;
		_wasPlatformSyncRefused = false;

		_host.RaiseTextCompositionStarted(new TextCompositionStartedEventArgs(_compositionStartIndex, _compositionSelectionLength));
	}

	void IImeSessionHost.OnImeCompositionUpdated(string compositionText, int cursorPosition, int resolvedLength, bool textAlreadyApplied, int compositionStart)
	{
		if (IsReadOnly)
		{
			return;
		}

		if (textAlreadyApplied)
		{
			// The platform (e.g., Android InputConnection) already applied the text.
			// Suppress CancelCompositionOnExternalChange for the ProcessTextInput call
			// that will follow from the platform's text sync (EndBatchEdit).
			_platformTextApplyInProgress = true;
			// Mark the session so key events aren't swallowed by the IsComposing check
			// (Android key events are independent of composition events).
			_compositionAppliedByPlatform = true;
			OnPlatformCompositionUpdate();
			if (compositionStart >= 0)
			{
				// An IME re-composing an existing word starts the composition before the caret.
				_compositionStartIndex = compositionStart;
			}
		}
		else if (!ReplaceCompositionText(compositionText, cursorPosition))
		{
			// Keep the range on the text that is really there, so clearing or cancelling the composition can't delete a neighbor.
			return;
		}

		_compositionLength = compositionText.Length;
		_compositionResolvedLength = resolvedLength;

		_host.RaiseTextCompositionChanged(new TextCompositionChangedEventArgs(_compositionStartIndex, _compositionLength));
		InvalidateTextBoxRender();
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

		if (textAlreadyApplied)
		{
			_platformTextApplyInProgress = true;
			_compositionAppliedByPlatform = true;
			OnPlatformCompositionUpdate();
		}
		else
		{
			var combinedText = committedText + compositionText;
			var combinedCursorPosition = cursorPosition >= 0
				? committedText.Length + Math.Min(cursorPosition, compositionText.Length)
				: -1;
			if (!ReplaceCompositionText(combinedText, combinedCursorPosition))
			{
				// The committed part goes in on its own when it fits; the composition that follows it is dropped.
				if (committedText.Length == 0 || !ReplaceCompositionText(committedText))
				{
					// Nothing was applied: the range keeps covering the text that is really there.
					return;
				}

				compositionText = string.Empty;
			}
		}

		_compositionStartIndex = Math.Min(_compositionStartIndex + committedText.Length, Text.Length);
		_compositionLength = Math.Min(compositionText.Length, Text.Length - _compositionStartIndex);
		_compositionResolvedLength = Math.Clamp(resolvedLength, 0, _compositionLength);

		_host.RaiseTextCompositionChanged(new TextCompositionChangedEventArgs(_compositionStartIndex, _compositionLength));
		InvalidateTextBoxRender();
	}

	void IImeSessionHost.OnImeCompositionCompleted(string committedText, bool textAlreadyApplied)
	{
		if (IsReadOnly)
		{
			return;
		}

		TrySetCurrentlyTyping(true);
		var startIndex = _compositionStartIndex;
		var committedLength = committedText.Length;
		if (textAlreadyApplied ? _wasPlatformSyncRefused : !ReplaceCompositionText(committedText))
		{
			// A refused commit leaves the composition text that was already there, so report that range.
			startIndex = Math.Min(startIndex, Text.Length);
			committedLength = Math.Min(_compositionLength, Text.Length - startIndex);
		}

		_isComposing = false;
		_compositionAppliedByPlatform = false;
		_compositionLength = 0;
		_compositionStartIndex = 0;
		_compositionResolvedLength = 0;

		_host.RaiseTextCompositionEnded(new TextCompositionEndedEventArgs(startIndex, committedLength));
		InvalidateTextBoxRender();
	}

	void IImeSessionHost.OnImeCompositionCanceled(bool textAlreadyApplied)
	{
		if (!_isComposing)
		{
			_compositionAppliedByPlatform = false;
			_platformTextApplyInProgress = false;
			return;
		}

		var startIndex = _compositionStartIndex;
		if (!textAlreadyApplied)
		{
			ReplaceCompositionText(string.Empty);
		}

		_isComposing = false;
		_compositionAppliedByPlatform = false;
		_platformTextApplyInProgress = false;
		_compositionLength = 0;
		_compositionStartIndex = 0;
		_compositionResolvedLength = 0;

		_host.RaiseTextCompositionEnded(new TextCompositionEndedEventArgs(startIndex, 0));
		InvalidateTextBoxRender();
	}

	void IImeSessionHost.OnImeCompositionEnded()
	{
		if (!_isComposing)
		{
			_compositionAppliedByPlatform = false;
			return;
		}

		// Composition ended without explicit commit — keep text as-is (matches WinUI behavior).
		// The composition text was already inserted via ProcessTextInput during OnImeCompositionUpdated.
		var startIndex = _compositionStartIndex;
		var length = _compositionLength;
		_isComposing = false;
		_compositionAppliedByPlatform = false;
		_compositionLength = 0;
		_compositionStartIndex = 0;
		_compositionResolvedLength = 0;

		_host.RaiseTextCompositionEnded(new TextCompositionEndedEventArgs(startIndex, length));
		InvalidateTextBoxRender();
	}

	void IImeSessionHost.OnCandidateWindowBoundsChanged(Rect bounds)
	{
	}

	// Runs before the update changes the range.
	private void OnPlatformCompositionUpdate()
	{
		_compositionRangeBeforePlatformSync = (_compositionStartIndex, _compositionLength, _compositionResolvedLength);
		_wasPlatformSyncRefused = false;
		_compositionApplied = true;
	}

	/// <summary>
	/// The text the platform synced after reporting a composition update was refused, so no text change ends the platform's text
	/// apply, and the range goes back to the text that is really there.
	/// </summary>
	private void OnPlatformSyncRefused()
	{
		_platformTextApplyInProgress = false;
		if (!_isComposing)
		{
			return;
		}

		(_compositionStartIndex, _compositionLength, _compositionResolvedLength) = _compositionRangeBeforePlatformSync;
		_wasPlatformSyncRefused = true;

		_host.RaiseTextCompositionChanged(new TextCompositionChangedEventArgs(_compositionStartIndex, _compositionLength));
		InvalidateTextBoxRender();
	}

	/// <summary>
	/// Replaces the composition range with <paramref name="newText"/>, through the user-input path.
	/// </summary>
	/// <returns>Whether the replacement was applied: MaxLength or a BeforeTextChanging handler can refuse it, and an empty
	/// replacement before any composition text was applied leaves the selection it would replace.</returns>
	private bool ReplaceCompositionText(string newText, int cursorPosition = -1)
	{
		if (!_compositionApplied && newText.Length == 0)
		{
			return false;
		}

		var text = Text;
		// Clamp indices to text bounds in case the platform modified the text
		// out-of-band (e.g., Android IME autocorrect during a composition session)
		// leaving _compositionStartIndex/_compositionLength stale.
		var startIndex = Math.Min(_compositionStartIndex, text.Length);
		var length = _compositionApplied ? _compositionLength : _compositionSelectionLength;
		var endIndex = Math.Min(_compositionStartIndex + length, text.Length);
		var replaced = text[..startIndex] + newText + text[endIndex..];

		// Place the caret at the IME-reported cursor position within the composition,
		// or at the end of the new text if not available. Clamp to valid range
		// since some IMEs may report offsets beyond the current preedit length.
		var caretOffset = cursorPosition >= 0
			? Math.Min(cursorPosition, newText.Length)
			: newText.Length;

		_pendingSelection = (startIndex + caretOffset, 0);
		var applied = ProcessTextInputKeepingHistory(replaced);
		_compositionApplied |= applied;
		return applied;
	}

	/// <summary>
	/// Called from OnTextChangedPartial. If the text was changed externally
	/// (not by the IME composition path), cancel the active composition.
	/// </summary>
	private void CancelCompositionOnExternalChange()
	{
		if (!_isComposing || _suppressCurrentlyTyping)
		{
			// Not composing, or the change came from ReplaceCompositionText — nothing to do.
			return;
		}

		if (_platformTextApplyInProgress)
		{
			// The platform already applied the text (e.g., Android EndBatchEdit), so don't cancel the composition.
			// While its text is processed, edits by TextChanging handlers belong to it too: ProcessTextInput clears the flag.
			if (!_isInputModifyingText)
			{
				_platformTextApplyInProgress = false;
			}
			return;
		}

		var startIndex = _compositionStartIndex;
		var length = _compositionLength;
		_isComposing = false;
		_compositionAppliedByPlatform = false;
		_compositionLength = 0;
		_compositionStartIndex = 0;
		_compositionResolvedLength = 0;

		// End and restart the session so that further IME input still works
		// while the active host reference stays in sync.
		ImeSessionCoordinator.RestartSession(this);
		_host.RaiseTextCompositionEnded(new TextCompositionEndedEventArgs(startIndex, length));
		InvalidateTextBoxRender();
	}

	private void InvalidateTextBoxRender() => TextBoxView?.DisplayBlock.InvalidateInlines(false);

	/// <summary>
	/// Installs a fake IME extension for testing. The extension's events are
	/// forwarded to the active host. Returns a disposable that restores the original.
	/// </summary>
	internal static IDisposable SetImeExtensionForTesting(IImeTextBoxExtension extension)
		=> ImeSessionCoordinator.SetExtensionForTesting(extension);
}
